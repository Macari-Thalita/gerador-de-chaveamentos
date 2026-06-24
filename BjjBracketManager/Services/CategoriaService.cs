using BjjBracketManager.Models;
using System.Collections.ObjectModel;

namespace BjjBracketManager.Services;

public class CategoriaService
{
    public ObservableCollection<CategoriaGrupo> AgruparPorCategoria(IEnumerable<Atleta> atletas)
    {
        var grupos = atletas
            .Where(a => !string.IsNullOrWhiteSpace(a.Nome))
            .GroupBy(a => new
            {
                a.CategoriaIdade,
                a.Genero,
                a.Peso,
                a.Graduacao
            })
            .OrderBy(g => g.Key.CategoriaIdade)
            .ThenBy(g => g.Key.Genero)
            .ThenBy(g => g.Key.Peso)
            .ThenBy(g => g.Key.Graduacao)
            .Select(g => new CategoriaGrupo
            {
                CategoriaIdade = g.Key.CategoriaIdade,
                Genero = g.Key.Genero,
                Peso = g.Key.Peso,
                Graduacao = g.Key.Graduacao,
                Nome = $"{g.Key.CategoriaIdade} | {g.Key.Genero} | {g.Key.Peso} | {g.Key.Graduacao}",
                Atletas = new ObservableCollection<Atleta>(g.OrderBy(a => a.Nome))
            });

        return new ObservableCollection<CategoriaGrupo>(grupos);
    }
}
