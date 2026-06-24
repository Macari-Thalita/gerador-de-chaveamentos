using BjjBracketManager.Models;
using BjjBracketManager.Services;
using Microsoft.Win32;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace BjjBracketManager;

public partial class MainWindow : Window
{
    private readonly InscricaoImportService _importService = new();
    private readonly CategoriaService _categoriaService = new();
    private readonly BracketGenerator _bracketGenerator = new();
    private readonly ChaveamentoPdfExportService _pdfExportService = new();

    private ObservableCollection<Atleta> _atletas = new();
    private ObservableCollection<CategoriaGrupo> _categorias = new();
    private Chaveamento? _chaveamentoAtual;

    public MainWindow()
    {
        InitializeComponent();
        AtualizarEstadoBotoes();
    }

    private void Importar_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Planilhas e texto (*.xlsx;*.txt;*.tsv)|*.xlsx;*.txt;*.tsv|Todos os arquivos (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var atletas = _importService.Importar(dialog.FileName);
            _atletas = new ObservableCollection<Atleta>(atletas);
            GridAtletas.ItemsSource = _atletas;
            LimparCategoriasEChaveamento();
            AtualizarStatusImportacao();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao importar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RevisarDados_Click(object sender, RoutedEventArgs e)
    {
        if (_atletas.Count == 0)
        {
            MessageBox.Show("Importe as inscrições antes de revisar os dados.");
            return;
        }

        var janela = new CategoriaConfigWindow(_atletas)
        {
            Owner = this
        };

        if (janela.ShowDialog() != true)
            return;

        _importService.ReavaliarPossiveisDuplicados(_atletas);
        GridAtletas.Items.Refresh();
        LimparCategoriasEChaveamento();

        TxtStatus.Text = _atletas.Count == 0
            ? "Dados revisados. Nenhum atleta restante."
            : "Dados revisados. Gere as categorias novamente.";
    }

    private void GerarCategorias_Click(object sender, RoutedEventArgs e)
    {
        if (_atletas.Count == 0)
        {
            MessageBox.Show("Importe as inscrições antes de gerar as categorias.");
            return;
        }

        _categorias = _categoriaService.AgruparPorCategoria(_atletas);
        GridCategorias.ItemsSource = _categorias;
        GridLutas.ItemsSource = null;
        _chaveamentoAtual = null;
        AtualizarEstadoBotoes();
        TxtStatus.Text = $"{_categorias.Count} categorias encontradas.";
    }

    private void CriarChaveamento_Click(object sender, RoutedEventArgs e)
    {
        if (GridCategorias.SelectedItem is not CategoriaGrupo categoria)
        {
            MessageBox.Show("Selecione uma categoria antes de criar o chaveamento.");
            return;
        }

        try
        {
            _chaveamentoAtual = _bracketGenerator.CriarChaveamento(categoria);
            GridLutas.ItemsSource = _chaveamentoAtual.Lutas;
            GridLutas.Items.Refresh();
            AtualizarEstadoBotoes();
            TxtStatus.Text = $"Chave de {_chaveamentoAtual.TamanhoChave} criada para {categoria.Nome}.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao criar chaveamento", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GridCategorias_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_chaveamentoAtual is null)
            return;

        _chaveamentoAtual = null;
        GridLutas.ItemsSource = null;
        AtualizarEstadoBotoes();
        TxtStatus.Text = "Categoria selecionada. Crie o chaveamento.";
    }

    private void GitHubLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
        {
            UseShellExecute = true
        });

        e.Handled = true;
    }

    private void ExportarChaveamento_Click(object sender, RoutedEventArgs e)
    {
        if (_categorias.Count == 0)
        {
            MessageBox.Show("Gere as categorias antes de exportar.");
            return;
        }

        var janela = new ExportarChaveamentoWindow(_chaveamentoAtual is not null)
        {
            Owner = this
        };

        if (janela.ShowDialog() != true)
            return;

        if (janela.OpcaoSelecionada == ExportarChaveamentoOpcao.TodasAsChaves)
        {
            ExportarTodasAsChaves();
            return;
        }

        ExportarChaveAtual();
    }

    private void ExportarChaveAtual()
    {
        if (GridCategorias.SelectedItem is not CategoriaGrupo categoria)
        {
            MessageBox.Show("Selecione uma categoria antes de exportar a chave atual.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = CriarNomeArquivoPdf(categoria.Nome)
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _pdfExportService.ExportarCategoria(categoria, dialog.FileName);
            TxtStatus.Text = $"PDF exportado: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao exportar chaveamento", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportarTodasAsChaves()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = "chaveamentos-todas-as-categorias.pdf"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            _pdfExportService.ExportarTodas(_categorias, dialog.FileName);
            TxtStatus.Text = $"PDF exportado: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao exportar chaveamentos", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LimparCategoriasEChaveamento()
    {
        _categorias = new ObservableCollection<CategoriaGrupo>();
        _chaveamentoAtual = null;
        GridCategorias.ItemsSource = null;
        GridLutas.ItemsSource = null;
        AtualizarEstadoBotoes();
    }

    private void AtualizarEstadoBotoes()
    {
        BtnImportar.IsEnabled = true;
        BtnRevisarDados.IsEnabled = _atletas.Count > 0;
        BtnGerarCategorias.IsEnabled = _atletas.Count > 0;
        BtnCriarChaveamento.IsEnabled = _categorias.Count > 0;
        BtnExportarChaveamento.IsEnabled = _categorias.Count > 0;
    }

    private void AtualizarStatusImportacao()
    {
        var duplicados = _atletas.Count(a => a.PossivelDuplicado);
        TxtStatus.Text = duplicados == 0
            ? $"{_atletas.Count} atletas importados."
            : $"{_atletas.Count} atletas importados. {duplicados} possíveis duplicados.";
    }

    private static string CriarNomeArquivoPdf(string categoriaNome)
    {
        var invalidados = System.IO.Path.GetInvalidFileNameChars();
        var nomeSeguro = new string(categoriaNome
            .Select(ch => invalidados.Contains(ch) ? '-' : ch)
            .ToArray());

        return $"chaveamento-{nomeSeguro}.pdf";
    }
}
