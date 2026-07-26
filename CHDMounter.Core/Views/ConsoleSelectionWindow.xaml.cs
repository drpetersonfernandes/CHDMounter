using System.Windows;
using VideoGameFileSystemParser.Parsers;

namespace CHDMounter.Core.Views;

public partial class ConsoleSelectionWindow
{
    public ConsoleType SelectedConsoleType { get; private set; } = ConsoleType.Unknown;

    public ConsoleSelectionWindow()
    {
        InitializeComponent();

        var consoles = ParserFactory.GetAllSupportedConsoles();
        ConsoleComboBox.ItemsSource = consoles;
        ConsoleComboBox.DisplayMemberPath = "Name";
        ConsoleComboBox.SelectedIndex = 0;
    }

    public ConsoleSelectionWindow(string chdPath) : this()
    {
        var fileName = Path.GetFileName(chdPath);
        ChdPathTextBlock.Text = !string.IsNullOrEmpty(fileName) ? fileName : chdPath;
        ChdPathTextBlock.ToolTip = chdPath;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (ConsoleComboBox.SelectedItem is ConsoleInfo ci)
        {
            SelectedConsoleType = ci.Type;
            DialogResult = true;
        }

        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
