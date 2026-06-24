using BjjBracketManager.Models;
using System.IO;
using System.Text;

namespace BjjBracketManager.Services;

public class ChaveamentoPdfExportService
{
    private const double PageWidth = 842;
    private const double PageHeight = 595;
    private const double Margin = 34;
    private const double HeaderHeight = 72;
    private const double CenterGap = 46;

    private readonly BracketGenerator _bracketGenerator = new();

    public void ExportarCategoria(CategoriaGrupo categoria, string caminhoArquivo)
    {
        ExportarPaginas(new[] { categoria }, caminhoArquivo);
    }

    public void ExportarTodas(IEnumerable<CategoriaGrupo> categorias, string caminhoArquivo)
    {
        ExportarPaginas(categorias.Where(c => c.Atletas.Count > 0), caminhoArquivo);
    }

    private void ExportarPaginas(IEnumerable<CategoriaGrupo> categorias, string caminhoArquivo)
    {
        var paginas = categorias
            .Select(CriarPaginaCategoria)
            .ToList();

        if (paginas.Count == 0)
            throw new InvalidOperationException("Não há chaves para exportar.");

        EscreverPdf(caminhoArquivo, paginas);
    }

    private PdfPageContent CriarPaginaCategoria(CategoriaGrupo categoria)
    {
        var chaveamento = _bracketGenerator.CriarChaveamento(categoria);
        var primeiraRodada = chaveamento.Lutas
            .Where(l => l.Rodada == 1)
            .OrderBy(l => l.Posicao)
            .ToList();

        var atletas = primeiraRodada
            .SelectMany(l => new[] { l.Atleta1, l.Atleta2 })
            .ToList();

        return new PdfPageContent
        {
            CategoriaNome = categoria.Nome,
            QuantidadeAtletas = categoria.QuantidadeAtletas,
            TamanhoChave = chaveamento.TamanhoChave,
            Atletas = atletas
        };
    }

    private static void EscreverPdf(string caminhoArquivo, IReadOnlyList<PdfPageContent> paginas)
    {
        using var stream = File.Create(caminhoArquivo);
        var offsets = new List<long> { 0 };

        void EscreverAscii(string texto)
        {
            var bytes = Encoding.ASCII.GetBytes(texto);
            stream.Write(bytes);
        }

        void EscreverBytes(byte[] bytes)
        {
            stream.Write(bytes);
        }

        void EscreverObjeto(int numero, Action escreverConteudo)
        {
            offsets.Add(stream.Position);
            EscreverAscii($"{numero} 0 obj\n");
            escreverConteudo();
            EscreverAscii("\nendobj\n");
        }

        EscreverAscii("%PDF-1.4\n");

        var objetosPagina = paginas
            .Select((_, indice) => new
            {
                Pagina = 4 + indice * 2,
                Conteudo = 5 + indice * 2
            })
            .ToList();

        EscreverObjeto(1, () => EscreverAscii("<< /Type /Catalog /Pages 2 0 R >>"));
        EscreverObjeto(2, () =>
        {
            var filhos = string.Join(" ", objetosPagina.Select(o => $"{o.Pagina} 0 R"));
            EscreverAscii($"<< /Type /Pages /Kids [{filhos}] /Count {paginas.Count} >>");
        });
        EscreverObjeto(3, () => EscreverAscii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));

        for (var i = 0; i < paginas.Count; i++)
        {
            var objetos = objetosPagina[i];
            var conteudo = CriarConteudoPagina(paginas[i]);

            EscreverObjeto(objetos.Pagina, () =>
            {
                EscreverAscii($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth:0} {PageHeight:0}] ");
                EscreverAscii($"/Resources << /Font << /F1 3 0 R >> >> /Contents {objetos.Conteudo} 0 R >>");
            });

            EscreverObjeto(objetos.Conteudo, () =>
            {
                EscreverAscii($"<< /Length {conteudo.Length} >>\nstream\n");
                EscreverBytes(conteudo);
                EscreverAscii("\nendstream");
            });
        }

        var inicioXref = stream.Position;
        EscreverAscii($"xref\n0 {offsets.Count}\n");
        EscreverAscii("0000000000 65535 f \n");

        foreach (var offset in offsets.Skip(1))
            EscreverAscii($"{offset:D10} 00000 n \n");

        EscreverAscii("trailer\n");
        EscreverAscii($"<< /Size {offsets.Count} /Root 1 0 R >>\n");
        EscreverAscii("startxref\n");
        EscreverAscii($"{inicioXref}\n");
        EscreverAscii("%%EOF");
    }

    private static byte[] CriarConteudoPagina(PdfPageContent pagina)
    {
        var builder = new StringBuilder();
        var comandos = new PdfCommandBuilder(builder);

        comandos.SetStrokeWidth(1.3);
        comandos.DrawText(38, 562, 15, "Gerador de Chaveamentos");
        comandos.DrawText(38, 541, 11, TruncarTexto(pagina.CategoriaNome, 105));
        comandos.DrawText(38, 523, 9, $"{pagina.QuantidadeAtletas} atletas | Chave de {pagina.TamanhoChave}");

        DesenharChaveamento(comandos, pagina);

        return Encoding.Latin1.GetBytes(builder.ToString());
    }

    private static void DesenharChaveamento(PdfCommandBuilder comandos, PdfPageContent pagina)
    {
        if (pagina.TamanhoChave <= 1)
        {
            var nome = pagina.Atletas.FirstOrDefault()?.Nome ?? string.Empty;
            comandos.DrawBox(PageWidth / 2 - 95, 285, 190, 24, nome);
            return;
        }

        var metade = pagina.TamanhoChave / 2;
        var esquerda = pagina.Atletas.Take(metade).ToList();
        var direita = pagina.Atletas.Skip(metade).Take(metade).ToList();
        var sideRounds = (int)Math.Log2(metade);
        var levels = sideRounds + 1;

        var boxWidth = Math.Clamp((PageWidth - Margin * 2 - CenterGap) / (levels * 2.25), 96, 150);
        var boxHeight = Math.Clamp((PageHeight - HeaderHeight - Margin) / Math.Max(metade * 1.8, 1), 18, 26);
        var availableHeight = PageHeight - HeaderHeight - Margin;
        var top = PageHeight - HeaderHeight;
        var centerY = top - availableHeight / 2;
        var leftAreaWidth = (PageWidth - Margin * 2 - CenterGap) / 2;
        var leftXs = CriarXsEsquerda(levels, boxWidth, leftAreaWidth);
        var rightXs = CriarXsDireita(levels, boxWidth, leftAreaWidth);

        var leftCenters = DesenharLado(
            comandos,
            esquerda,
            leftXs,
            top,
            availableHeight,
            boxWidth,
            boxHeight,
            mirror: false);

        var rightCenters = DesenharLado(
            comandos,
            direita,
            rightXs,
            top,
            availableHeight,
            boxWidth,
            boxHeight,
            mirror: true);

        var leftFinalX = leftXs[^1];
        var rightFinalX = rightXs[^1];
        var finalLineY = leftCenters.Count > 0 ? leftCenters[^1][0] : centerY;

        if (rightCenters.Count > 0)
            finalLineY = (finalLineY + rightCenters[^1][0]) / 2;

        comandos.Line(leftFinalX + boxWidth, finalLineY, rightFinalX, finalLineY);
    }

    private static List<List<double>> DesenharLado(
        PdfCommandBuilder comandos,
        IReadOnlyList<Atleta?> atletas,
        IReadOnlyList<double> xs,
        double top,
        double availableHeight,
        double boxWidth,
        double boxHeight,
        bool mirror)
    {
        var currentCenters = CriarLeafCenters(atletas.Count, top, availableHeight);
        var allCenters = new List<List<double>> { currentCenters };

        for (var level = 0; level < xs.Count; level++)
        {
            var x = xs[level];
            var centers = allCenters[level];

            for (var i = 0; i < centers.Count; i++)
            {
                var texto = level == 0 ? atletas[i]?.Nome ?? string.Empty : string.Empty;
                comandos.DrawBox(x, centers[i] - boxHeight / 2, boxWidth, boxHeight, texto);
            }

            if (level == xs.Count - 1)
                break;

            var nextCenters = new List<double>();
            var nextX = xs[level + 1];

            for (var i = 0; i < centers.Count; i += 2)
            {
                var y1 = centers[i];
                var y2 = centers[Math.Min(i + 1, centers.Count - 1)];
                var parentY = (y1 + y2) / 2;
                nextCenters.Add(parentY);

                if (mirror)
                    DesenharConectorDireita(comandos, x, nextX, boxWidth, y1, y2, parentY);
                else
                    DesenharConectorEsquerda(comandos, x, nextX, boxWidth, y1, y2, parentY);
            }

            allCenters.Add(nextCenters);
        }

        return allCenters;
    }

    private static List<double> CriarLeafCenters(int quantidade, double top, double availableHeight)
    {
        if (quantidade <= 1)
            return new List<double> { top - availableHeight / 2 };

        var passo = availableHeight / quantidade;
        return Enumerable.Range(0, quantidade)
            .Select(i => top - passo * (i + 0.5))
            .ToList();
    }

    private static List<double> CriarXsEsquerda(int levels, double boxWidth, double leftAreaWidth)
    {
        var start = Margin;
        var end = Margin + leftAreaWidth - boxWidth;

        return CriarXs(levels, start, end);
    }

    private static List<double> CriarXsDireita(int levels, double boxWidth, double leftAreaWidth)
    {
        var rightAreaStart = PageWidth - Margin - leftAreaWidth;
        var start = PageWidth - Margin - boxWidth;
        var end = rightAreaStart;

        return CriarXs(levels, start, end);
    }

    private static List<double> CriarXs(int levels, double start, double end)
    {
        if (levels <= 1)
            return new List<double> { start };

        var step = (end - start) / (levels - 1);
        return Enumerable.Range(0, levels)
            .Select(i => start + step * i)
            .ToList();
    }

    private static void DesenharConectorEsquerda(
        PdfCommandBuilder comandos,
        double childX,
        double parentX,
        double boxWidth,
        double y1,
        double y2,
        double parentY)
    {
        var childRight = childX + boxWidth;
        var midX = (childRight + parentX) / 2;

        comandos.Line(childRight, y1, midX, y1);
        comandos.Line(childRight, y2, midX, y2);
        comandos.Line(midX, y1, midX, y2);
        comandos.Line(midX, parentY, parentX, parentY);
    }

    private static void DesenharConectorDireita(
        PdfCommandBuilder comandos,
        double childX,
        double parentX,
        double boxWidth,
        double y1,
        double y2,
        double parentY)
    {
        var parentRight = parentX + boxWidth;
        var midX = (childX + parentRight) / 2;

        comandos.Line(childX, y1, midX, y1);
        comandos.Line(childX, y2, midX, y2);
        comandos.Line(midX, y1, midX, y2);
        comandos.Line(midX, parentY, parentRight, parentY);
    }

    private static string TruncarTexto(string texto, int tamanho)
    {
        if (texto.Length <= tamanho)
            return texto;

        return texto[..Math.Max(0, tamanho - 3)] + "...";
    }

    private sealed class PdfPageContent
    {
        public string CategoriaNome { get; init; } = string.Empty;
        public int QuantidadeAtletas { get; init; }
        public int TamanhoChave { get; init; }
        public List<Atleta?> Atletas { get; init; } = new();
    }

    private sealed class PdfCommandBuilder
    {
        private readonly StringBuilder _builder;

        public PdfCommandBuilder(StringBuilder builder)
        {
            _builder = builder;
        }

        public void SetStrokeWidth(double width)
        {
            _builder.AppendLine($"{Format(width)} w");
        }

        public void DrawBox(double x, double y, double width, double height, string text)
        {
            _builder.AppendLine($"{Format(x)} {Format(y)} {Format(width)} {Format(height)} re S");

            if (string.IsNullOrWhiteSpace(text))
                return;

            var fontSize = CalcularFonteParaCaixa(text, width, height);
            var lines = QuebrarTextoParaCaixa(text, width, fontSize).Take(2).ToList();
            var lineHeight = fontSize + 1;
            var totalTextHeight = lines.Count * lineHeight;
            var firstLineY = y + (height + totalTextHeight) / 2 - fontSize;

            for (var i = 0; i < lines.Count; i++)
                DrawText(x + 4, firstLineY - i * lineHeight, fontSize, lines[i]);
        }

        public void DrawText(double x, double y, double fontSize, string text)
        {
            _builder.AppendLine("BT");
            _builder.AppendLine($"/F1 {Format(fontSize)} Tf");
            _builder.AppendLine($"{Format(x)} {Format(y)} Td");
            _builder.Append('(');
            _builder.Append(EscaparTextoPdf(text));
            _builder.AppendLine(") Tj");
            _builder.AppendLine("ET");
        }

        public void Line(double x1, double y1, double x2, double y2)
        {
            _builder.AppendLine($"{Format(x1)} {Format(y1)} m {Format(x2)} {Format(y2)} l S");
        }

        private static string Format(double value)
        {
            return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string EscaparTextoPdf(string texto)
        {
            return texto
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)")
                .Replace("\r", string.Empty)
                .Replace("\n", " ");
        }

        private static double CalcularFonteParaCaixa(string text, double width, double height)
        {
            var availableWidth = width - 8;
            var availableHeight = height - 4;

            for (var fontSize = 8.0; fontSize >= 5.5; fontSize -= 0.5)
            {
                var lines = QuebrarTextoParaCaixa(text, width, fontSize).Take(3).ToList();

                if (lines.Count <= 2 && lines.Count * (fontSize + 1) <= availableHeight)
                    return fontSize;

                if (lines.Count == 1 && EstimarLarguraTexto(lines[0], fontSize) <= availableWidth)
                    return fontSize;
            }

            return 5.5;
        }

        private static IEnumerable<string> QuebrarTextoParaCaixa(string text, double width, double fontSize)
        {
            var maxWidth = width - 8;
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = string.Empty;

            foreach (var word in words)
            {
                var candidate = string.IsNullOrEmpty(line) ? word : $"{line} {word}";

                if (EstimarLarguraTexto(candidate, fontSize) <= maxWidth)
                {
                    line = candidate;
                    continue;
                }

                if (!string.IsNullOrEmpty(line))
                    yield return line;

                line = EstimarLarguraTexto(word, fontSize) <= maxWidth
                    ? word
                    : TruncarParaLargura(word, maxWidth, fontSize);
            }

            if (!string.IsNullOrEmpty(line))
                yield return line;
        }

        private static string TruncarParaLargura(string text, double maxWidth, double fontSize)
        {
            if (EstimarLarguraTexto(text, fontSize) <= maxWidth)
                return text;

            var result = text;

            while (result.Length > 3 && EstimarLarguraTexto(result + "...", fontSize) > maxWidth)
                result = result[..^1];

            return result + "...";
        }

        private static double EstimarLarguraTexto(string text, double fontSize)
        {
            return text.Length * fontSize * 0.48;
        }
    }
}
