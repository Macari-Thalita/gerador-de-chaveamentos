using BjjBracketManager.Models;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace BjjBracketManager;

public partial class CategoriaConfigWindow : Window
{
    private const string ObservacaoDuplicado = "Possível inscrição duplicada";

    private readonly ObservableCollection<Atleta> _atletas;
    private readonly ObservableCollection<Atleta> _revisao;

    public CategoriaConfigWindow(ObservableCollection<Atleta> atletas)
    {
        InitializeComponent();
        _atletas = atletas;
        _revisao = new ObservableCollection<Atleta>(atletas.Select(CopiarAtleta));
        GridRevisao.ItemsSource = _revisao;
    }

    private void AjustarDuplicatas_Click(object sender, RoutedEventArgs e)
    {
        GridRevisao.CommitEdit();
        GridRevisao.CommitEdit(DataGridEditingUnit.Row, true);

        var removidos = RemoverDuplicatasDaRevisao();
        ReavaliarDuplicatasDaRevisao();
        GridRevisao.Items.Refresh();

        MessageBox.Show(
            removidos == 0
                ? "Nenhuma duplicata encontrada."
                : $"{removidos} registro(s) duplicado(s) removido(s).",
            "Ajustar duplicatas",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Aplicar_Click(object sender, RoutedEventArgs e)
    {
        GridRevisao.CommitEdit();
        GridRevisao.CommitEdit(DataGridEditingUnit.Row, true);

        _atletas.Clear();

        foreach (var atleta in _revisao.Select(CopiarAtleta))
            _atletas.Add(atleta);

        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private int RemoverDuplicatasDaRevisao()
    {
        var manterIds = _revisao
            .Where(a => !string.IsNullOrWhiteSpace(a.Nome))
            .GroupBy(CriarChaveDuplicidade)
            .Select(g => g.First().Id)
            .ToHashSet();

        var remover = _revisao
            .Where(a => !string.IsNullOrWhiteSpace(a.Nome) && !manterIds.Contains(a.Id))
            .ToList();

        foreach (var atleta in remover)
            _revisao.Remove(atleta);

        return remover.Count;
    }

    private void ReavaliarDuplicatasDaRevisao()
    {
        foreach (var atleta in _revisao)
        {
            atleta.PossivelDuplicado = false;
            atleta.Observacoes = RemoverObservacaoDuplicado(atleta.Observacoes);
        }

        var gruposDuplicados = _revisao
            .Where(a => !string.IsNullOrWhiteSpace(a.Nome))
            .GroupBy(CriarChaveDuplicidade)
            .Where(g => g.Count() > 1);

        foreach (var grupo in gruposDuplicados)
        {
            foreach (var atleta in grupo)
            {
                atleta.PossivelDuplicado = true;
                atleta.Observacoes = AdicionarObservacaoDuplicado(atleta.Observacoes);
            }
        }
    }

    private static DuplicateKey CriarChaveDuplicidade(Atleta atleta)
    {
        return new DuplicateKey(
            CriarChaveTexto(atleta.Nome),
            CriarChaveTexto(atleta.CategoriaIdade),
            CriarChaveTexto(atleta.Genero),
            CriarChaveTexto(atleta.Peso),
            CriarChaveTexto(atleta.Graduacao));
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

    private static Atleta CopiarAtleta(Atleta atleta)
    {
        return new Atleta
        {
            Id = atleta.Id,
            Nome = atleta.Nome,
            Genero = atleta.Genero,
            Peso = atleta.Peso,
            Graduacao = atleta.Graduacao,
            CategoriaIdade = atleta.CategoriaIdade,
            Equipe = atleta.Equipe,
            PossivelDuplicado = atleta.PossivelDuplicado,
            Observacoes = atleta.Observacoes
        };
    }

    private sealed record DuplicateKey(
        string Nome,
        string CategoriaIdade,
        string Genero,
        string Peso,
        string Graduacao);
}
