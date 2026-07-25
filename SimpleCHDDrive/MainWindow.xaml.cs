using SimpleChdDrive.Core.Views;

namespace SimpleChdDrive;

public partial class MainWindow : MainWindowBase
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeMainWindow();
    }

    protected override string[] GetStartupArgs() => App.StartupArgs;
}
