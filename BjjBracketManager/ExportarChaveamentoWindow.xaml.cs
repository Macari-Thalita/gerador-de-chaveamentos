using System.Windows;

namespace BjjBracketManager;

public partial class ExportarChaveamentoWindow : Window
{
    public ExportarChaveamentoOpcao OpcaoSelecionada =>
        OpcaoTodasChaves.IsChecked == true
            ? ExportarChaveamentoOpcao.TodasAsChaves
            : ExportarChaveamentoOpcao.ChaveAtual;

    public ExportarChaveamentoWindow(bool possuiChaveAtual)
    {
        InitializeComponent();

        OpcaoChaveAtual.IsEnabled = possuiChaveAtual;

        if (!possuiChaveAtual)
            OpcaoTodasChaves.IsChecked = true;
    }

    private void Continuar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}

public enum ExportarChaveamentoOpcao
{
    ChaveAtual,
    TodasAsChaves
}
