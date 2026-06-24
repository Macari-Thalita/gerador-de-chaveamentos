using BjjBracketManager.Models;
using ClosedXML.Excel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BjjBracketManager.Services;

public class InscricaoImportService
{
    private const string ObservacaoDuplicado = "Possível inscrição duplicada";

    private readonly AtletaNormalizer _normalizer = new();

    public List<Atleta> Importar(string caminhoArquivo)
    {
        var extensao = Path.GetExtension(caminhoArquivo).ToLowerInvariant();

        var atletas = extensao switch
        {
            ".xlsx" => ImportarXlsx(caminhoArquivo),
            ".txt" or ".tsv" => ImportarTabulado(caminhoArquivo),
            _ => throw new InvalidOperationException("Formato não suportado. Use .xlsx, .txt ou .tsv.")
        };

        ReavaliarPossiveisDuplicados(atletas);
        return atletas;
    }

    private List<Atleta> ImportarXlsx(string caminhoArquivo)
    {
        using var workbook = new XLWorkbook(caminhoArquivo);
        var worksheet = workbook.Worksheets.First();
        var linhas = worksheet.RangeUsed()?.RowsUsed().ToList() ?? new List<IXLRangeRow>();

        if (linhas.Count <= 1)
            return new List<Atleta>();

        var cabecalho = linhas[0].Cells().Select(c => c.GetString()).ToList();
        var colunas = MapearColunas(cabecalho);
        var atletas = new List<Atleta>();
        var id = 1;

        foreach (var linha in linhas.Skip(1))
        {
            var atleta = new Atleta
            {
                Id = id++,
                Nome = linha.Cell(colunas.Nome + 1).GetString(),
                Genero = linha.Cell(colunas.Genero + 1).GetString(),
                Peso = linha.Cell(colunas.Peso + 1).GetString(),
                Graduacao = linha.Cell(colunas.Graduacao + 1).GetString(),
                CategoriaIdade = linha.Cell(colunas.CategoriaIdade + 1).GetString(),
                Equipe = linha.Cell(colunas.Equipe + 1).GetString()
            };

            if (!string.IsNullOrWhiteSpace(atleta.Nome))
                atletas.Add(_normalizer.Normalizar(atleta));
        }

        return atletas;
    }

    private List<Atleta> ImportarTabulado(string caminhoArquivo)
    {
        var tabela = LerTabelaTabulada(caminhoArquivo);

        if (tabela.Count <= 1)
            return new List<Atleta>();

        var colunas = MapearColunas(tabela[0]);
        var atletas = new List<Atleta>();
        var id = 1;

        foreach (var linha in tabela.Skip(1))
        {
            if (linha.All(string.IsNullOrWhiteSpace))
                continue;

            var atleta = new Atleta
            {
                Id = id++,
                Nome = ObterValor(linha, colunas.Nome),
                Genero = ObterValor(linha, colunas.Genero),
                Peso = ObterValor(linha, colunas.Peso),
                Graduacao = ObterValor(linha, colunas.Graduacao),
                CategoriaIdade = ObterValor(linha, colunas.CategoriaIdade),
                Equipe = ObterValor(linha, colunas.Equipe)
            };

            if (!string.IsNullOrWhiteSpace(atleta.Nome))
                atletas.Add(_normalizer.Normalizar(atleta));
        }

        return atletas;
    }

    public void ReavaliarPossiveisDuplicados(IEnumerable<Atleta> atletas)
    {
        var lista = atletas.ToList();

        foreach (var atleta in lista)
        {
            atleta.PossivelDuplicado = false;
            atleta.Observacoes = RemoverObservacaoDuplicado(atleta.Observacoes);
        }

        var grupos = lista
            .Where(a => !string.IsNullOrWhiteSpace(a.Nome))
            .GroupBy(a => new
            {
                Nome = CriarChaveTexto(a.Nome),
                a.CategoriaIdade,
                a.Genero,
                a.Peso,
                a.Graduacao
            })
            .Where(g => g.Count() > 1);

        foreach (var grupo in grupos)
        {
            foreach (var atleta in grupo)
            {
                atleta.PossivelDuplicado = true;
                atleta.Observacoes = AdicionarObservacaoDuplicado(atleta.Observacoes);
            }
        }
    }

    private static MapeamentoColunas MapearColunas(IReadOnlyList<string> cabecalho)
    {
        var colunas = new MapeamentoColunas
        {
            Nome = EncontrarColuna(cabecalho, EhColunaNome),
            Genero = EncontrarColuna(cabecalho, EhColunaGenero),
            Peso = EncontrarColuna(cabecalho, EhColunaPeso),
            Graduacao = EncontrarColuna(cabecalho, EhColunaGraduacao),
            CategoriaIdade = EncontrarColuna(cabecalho, EhColunaCategoriaIdade),
            Equipe = EncontrarColuna(cabecalho, EhColunaEquipe)
        };

        if (colunas.EhValido)
            return colunas;

        throw new InvalidOperationException(
            "Não foi possível identificar as colunas obrigatórias no cabeçalho: " +
            string.Join(", ", colunas.ObterAusentes()) + ".");
    }

    private static int EncontrarColuna(IReadOnlyList<string> cabecalho, Func<string, bool> criterio)
    {
        for (var i = 0; i < cabecalho.Count; i++)
        {
            if (criterio(NormalizarCabecalho(cabecalho[i])))
                return i;
        }

        return -1;
    }

    private static bool EhColunaNome(string cabecalho)
    {
        return cabecalho.Contains("nome do atleta") ||
               cabecalho.Contains("nome completo") ||
               cabecalho.Contains("seu nome");
    }

    private static bool EhColunaGenero(string cabecalho)
    {
        return cabecalho.Contains("genero");
    }

    private static bool EhColunaPeso(string cabecalho)
    {
        return cabecalho.Contains("peso");
    }

    private static bool EhColunaGraduacao(string cabecalho)
    {
        return cabecalho.Contains("graduacao");
    }

    private static bool EhColunaCategoriaIdade(string cabecalho)
    {
        return cabecalho.Contains("categoria") ||
               cabecalho.Contains("idade");
    }

    private static bool EhColunaEquipe(string cabecalho)
    {
        return cabecalho.Contains("equipe");
    }

    private static List<List<string>> LerTabelaTabulada(string caminhoArquivo)
    {
        var conteudo = File.ReadAllText(caminhoArquivo, Encoding.UTF8);
        var tabela = new List<List<string>>();
        var linha = new List<string>();
        var campo = new StringBuilder();
        var dentroDeAspas = false;

        for (var i = 0; i < conteudo.Length; i++)
        {
            var ch = conteudo[i];

            if (ch == '"')
            {
                if (dentroDeAspas && i + 1 < conteudo.Length && conteudo[i + 1] == '"')
                {
                    campo.Append('"');
                    i++;
                }
                else
                {
                    dentroDeAspas = !dentroDeAspas;
                }
            }
            else if (ch == '\t' && !dentroDeAspas)
            {
                linha.Add(campo.ToString());
                campo.Clear();
            }
            else if ((ch == '\r' || ch == '\n') && !dentroDeAspas)
            {
                if (ch == '\r' && i + 1 < conteudo.Length && conteudo[i + 1] == '\n')
                    i++;

                linha.Add(campo.ToString());
                campo.Clear();
                tabela.Add(linha);
                linha = new List<string>();
            }
            else
            {
                campo.Append(ch);
            }
        }

        if (campo.Length > 0 || linha.Count > 0)
        {
            linha.Add(campo.ToString());
            tabela.Add(linha);
        }

        return tabela;
    }

    private static string ObterValor(IReadOnlyList<string> linha, int indice)
    {
        return indice >= 0 && indice < linha.Count ? linha[indice] : string.Empty;
    }

    private static string NormalizarCabecalho(string valor)
    {
        return CriarChaveTexto(valor)
            .Replace("?", string.Empty)
            .Replace("!", string.Empty);
    }

    private static string CriarChaveTexto(string valor)
    {
        var texto = valor.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var ch in texto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return Regex.Replace(builder.ToString().ToLowerInvariant(), @"\s+", " ");
    }

    private static string RemoverObservacaoDuplicado(string observacoes)
    {
        var partes = SepararObservacoes(observacoes)
            .Where(p => !p.Equals(ObservacaoDuplicado, StringComparison.OrdinalIgnoreCase));

        return string.Join("; ", partes);
    }

    private static string AdicionarObservacaoDuplicado(string observacoes)
    {
        var partes = SepararObservacoes(observacoes).ToList();

        if (!partes.Any(p => p.Equals(ObservacaoDuplicado, StringComparison.OrdinalIgnoreCase)))
            partes.Add(ObservacaoDuplicado);

        return string.Join("; ", partes);
    }

    private static IEnumerable<string> SepararObservacoes(string observacoes)
    {
        return observacoes
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p));
    }

    private sealed class MapeamentoColunas
    {
        public int Nome { get; init; } = -1;
        public int Genero { get; init; } = -1;
        public int Peso { get; init; } = -1;
        public int Graduacao { get; init; } = -1;
        public int CategoriaIdade { get; init; } = -1;
        public int Equipe { get; init; } = -1;

        public bool EhValido =>
            Nome >= 0 &&
            Genero >= 0 &&
            Peso >= 0 &&
            Graduacao >= 0 &&
            CategoriaIdade >= 0 &&
            Equipe >= 0;

        public IEnumerable<string> ObterAusentes()
        {
            if (Nome < 0)
                yield return "Nome";
            if (Genero < 0)
                yield return "Gênero";
            if (Peso < 0)
                yield return "Peso";
            if (Graduacao < 0)
                yield return "Graduação";
            if (CategoriaIdade < 0)
                yield return "Categoria de idade";
            if (Equipe < 0)
                yield return "Equipe";
        }
    }
}
