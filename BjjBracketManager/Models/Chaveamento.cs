using System.Collections.ObjectModel;

namespace BjjBracketManager.Models;

public class Chaveamento
{
    public string CategoriaNome { get; set; } = string.Empty;
    public int TamanhoChave { get; set; }
    public ObservableCollection<Luta> Lutas { get; set; } = new();
}
