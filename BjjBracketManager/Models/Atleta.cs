namespace BjjBracketManager.Models;

public class Atleta
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Genero { get; set; } = string.Empty;
    public string Peso { get; set; } = string.Empty;
    public string Graduacao { get; set; } = string.Empty;
    public string CategoriaIdade { get; set; } = string.Empty;
    public string Equipe { get; set; } = string.Empty;
    public bool PossivelDuplicado { get; set; }
    public string Observacoes { get; set; } = string.Empty;

    public string CategoriaKey => $"{CategoriaIdade} | {Genero} | {Peso} | {Graduacao}";
}
