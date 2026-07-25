namespace SimpleChdDrive;

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
