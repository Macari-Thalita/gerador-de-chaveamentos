using BjjBracketManager.Models;
using System.Collections.ObjectModel;

namespace BjjBracketManager.Services;

public class BracketGenerator
{
    public Chaveamento CriarChaveamento(CategoriaGrupo categoria, int? tamanhoEscolhido = null)
    {
        if (categoria.Atletas.Count == 0)
            throw new InvalidOperationException("Categoria sem atletas.");

        var tamanhoChave = tamanhoEscolhido ?? ObterProximaPotenciaDeDois(categoria.Atletas.Count);

        if (tamanhoChave < categoria.Atletas.Count)
            throw new InvalidOperationException("O tamanho da chave não pode ser menor que a quantidade de atletas.");

        var atletas = DistribuirAtletas(categoria.Atletas.ToList());
        var posicoes = new List<Atleta?>();
        posicoes.AddRange(atletas);

        while (posicoes.Count < tamanhoChave)
            posicoes.Add(null);

        var lutas = new ObservableCollection<Luta>();
        var posicao = 1;

        for (var i = 0; i < posicoes.Count; i += 2)
        {
            var luta = new Luta
            {
                Rodada = 1,
                Posicao = posicao,
                Atleta1 = posicoes[i],
                Atleta2 = posicoes[i + 1]
            };

            if (luta.Atleta1 is not null && luta.Atleta2 is null)
                luta.Vencedor = luta.Atleta1;
            else if (luta.Atleta1 is null && luta.Atleta2 is not null)
                luta.Vencedor = luta.Atleta2;

            lutas.Add(luta);
            posicao++;
        }

        return new Chaveamento
        {
            CategoriaNome = categoria.Nome,
            TamanhoChave = tamanhoChave,
            Lutas = lutas
        };
    }

    public void RecalcularProximaRodada(Chaveamento chaveamento)
    {
        var todas = chaveamento.Lutas.ToList();
        var rodadaAtual = 1;

        while (true)
        {
            var lutasDaRodada = todas
                .Where(l => l.Rodada == rodadaAtual)
                .OrderBy(l => l.Posicao)
                .ToList();

            if (lutasDaRodada.Count <= 1)
                break;

            var vencedores = lutasDaRodada.Select(l => l.Vencedor).ToList();
            var rodadaSeguinte = rodadaAtual + 1;
            var lutasSeguinteEsperadas = (int)Math.Ceiling(lutasDaRodada.Count / 2.0);

            for (var i = 0; i < lutasSeguinteEsperadas; i++)
            {
                var luta = todas.FirstOrDefault(l => l.Rodada == rodadaSeguinte && l.Posicao == i + 1);
                if (luta is null)
                {
                    luta = new Luta { Rodada = rodadaSeguinte, Posicao = i + 1 };
                    chaveamento.Lutas.Add(luta);
                    todas.Add(luta);
                }

                luta.Atleta1 = vencedores.ElementAtOrDefault(i * 2);
                luta.Atleta2 = vencedores.ElementAtOrDefault(i * 2 + 1);

                if (luta.Atleta1 is null || luta.Atleta2 is null)
                    luta.Vencedor = luta.Atleta1 ?? luta.Atleta2;
                else if (luta.Vencedor != luta.Atleta1 && luta.Vencedor != luta.Atleta2)
                    luta.Vencedor = null;
            }

            rodadaAtual++;
        }
    }

    private static int ObterProximaPotenciaDeDois(int quantidade)
    {
        var tamanho = 2;
        while (tamanho < quantidade)
            tamanho *= 2;
        return tamanho;
    }

    private static List<Atleta> DistribuirAtletas(List<Atleta> atletas)
    {
        var filasPorEquipe = atletas
            .GroupBy(a => string.IsNullOrWhiteSpace(a.Equipe) ? $"sem-equipe-{a.Id}" : a.Equipe.Trim().ToLowerInvariant())
            .Select(g => new Queue<Atleta>(g.OrderBy(a => a.Nome)))
            .OrderByDescending(q => q.Count)
            .ToList();

        var distribuidos = new List<Atleta>();

        while (filasPorEquipe.Any(q => q.Count > 0))
        {
            foreach (var fila in filasPorEquipe.Where(q => q.Count > 0))
                distribuidos.Add(fila.Dequeue());
        }

        return distribuidos;
    }
}
