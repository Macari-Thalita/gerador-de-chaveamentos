using BjjBracketManager.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BjjBracketManager.Services;

public class AtletaNormalizer
{
    public Atleta Normalizar(Atleta atleta)
    {
        atleta.Nome = LimparEspacos(atleta.Nome);
        atleta.Genero = NormalizarGenero(atleta.Genero);
        atleta.Peso = LimparEspacos(atleta.Peso).ToLowerInvariant();
        atleta.Graduacao = NormalizarGraduacao(atleta.Graduacao);
        atleta.CategoriaIdade = LimparEspacos(atleta.CategoriaIdade).ToLowerInvariant();
        atleta.Equipe = LimparEspacos(atleta.Equipe);
        return atleta;
    }

    private static string NormalizarGenero(string valor)
    {
        var texto = RemoverAcentos(LimparEspacos(valor)).ToLowerInvariant();
        return texto switch
        {
            "masculino" or "m" => "Masculino",
            "feminino" or "f" => "Feminino",
            _ => LimparEspacos(valor)
        };
    }

    private static string NormalizarGraduacao(string valor)
    {
        var texto = RemoverAcentos(LimparEspacos(valor)).ToLowerInvariant();

        return texto switch
        {
            "branca/cinza" or "branca" or "cinza" => "Branca/Cinza",
            "amarela/laranja/verde" or "amarela" or "laranja" or "verde" => "Amarela/Laranja/Verde",
            "azul" => "Azul",
            "roxa" => "Roxa",
            "marrom" => "Marrom",
            "preta" => "Preta",
            _ => LimparEspacos(valor)
        };
    }

    private static string LimparEspacos(string valor)
    {
        return Regex.Replace(valor.Trim(), @"\s+", " ");
    }

    private static string RemoverAcentos(string texto)
    {
        var normalized = texto.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                builder.Append(ch);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
