using System.Windows;

namespace SimpleChdDrive.Core.Views;

public partial class ConsoleSelectionWindow : Window
{
    public ConsoleType SelectedConsoleType { get; private set; } = ConsoleType.Unknown;

    public ConsoleSelectionWindow()
    {
        InitializeComponent();

        var consoles = ParserFactory.GetAllSupportedConsoles();
        ConsoleComboBox.ItemsSource = consoles;
        ConsoleComboBox.DisplayMemberPath = "Item2";
        ConsoleComboBox.SelectedIndex = 0;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (ConsoleComboBox.SelectedItem is (ConsoleType type, _))
        {
            SelectedConsoleType = type;
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
