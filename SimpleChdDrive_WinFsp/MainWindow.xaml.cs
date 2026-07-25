namespace SimpleChdDrive_WinFsp;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        InitializeMainWindow();
    }

    protected override string[] GetStartupArgs()
    {
        return App.StartupArgs;
    }
}
