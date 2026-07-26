namespace CHDMounter;

/// <summary>
/// The main application window for the Dokan-based CHD mounter.
/// </summary>
public partial class MainWindow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
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
