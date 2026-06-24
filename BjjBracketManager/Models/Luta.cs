namespace BjjBracketManager.Models;

public class Luta
{
    public int Rodada { get; set; }
    public int Posicao { get; set; }
    public Atleta? Atleta1 { get; set; }
    public Atleta? Atleta2 { get; set; }
    public Atleta? Vencedor { get; set; }

    public string NomeAtleta1 => Atleta1?.Nome ?? "BYE";
    public string NomeAtleta2 => Atleta2?.Nome ?? "BYE";
    public string NomeVencedor => Vencedor?.Nome ?? string.Empty;
    public List<Atleta?> VencedoresPossiveis => new[] { null, Atleta1, Atleta2 }
        .Distinct()
        .ToList();
}
