using System.Collections.ObjectModel;

namespace BjjBracketManager.Models;

public class CategoriaGrupo
{
    public string Nome { get; set; } = string.Empty;
    public string CategoriaIdade { get; set; } = string.Empty;
    public string Genero { get; set; } = string.Empty;
    public string Peso { get; set; } = string.Empty;
    public string Graduacao { get; set; } = string.Empty;
    public int QuantidadeAtletas => Atletas.Count;
    public ObservableCollection<Atleta> Atletas { get; set; } = new();
}
