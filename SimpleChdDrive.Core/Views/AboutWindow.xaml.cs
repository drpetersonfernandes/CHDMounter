using System.Windows;

namespace SimpleChdDrive.Core.Views;

public partial class AboutWindow
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
