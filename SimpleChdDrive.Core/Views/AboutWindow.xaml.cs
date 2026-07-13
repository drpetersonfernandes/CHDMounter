using System.Windows;

namespace SimpleChdDrive.Core.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
