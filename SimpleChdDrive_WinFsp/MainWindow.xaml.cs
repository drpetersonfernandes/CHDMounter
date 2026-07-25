using SimpleChdDrive.Core.Views;

namespace SimpleChdDrive_WinFsp;

public partial class MainWindow : MainWindowBase
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeMainWindow();
    }

    protected override string[] GetStartupArgs() => App.StartupArgs;
}
