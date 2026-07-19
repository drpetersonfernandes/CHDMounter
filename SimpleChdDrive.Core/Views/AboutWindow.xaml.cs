using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace SimpleChdDrive.Core.Views;

public partial class AboutWindow
{
    public AboutWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
            VersionText.Text = version;
        }
        catch
        {
            VersionText.Text = "1.0.0";
        }

        CheckForUpdates();
    }

    private void CheckForUpdates()
    {
        var result = UpdateChecker.Result;
        if (result is { HasUpdate: true })
        {
            UpdateBanner.Visibility = Visibility.Visible;
            UpdateText.Text = $"A new version ({result.LatestVersion}) is available!";
            UpdateLink.Text = "Click here to download";
            UpdateLink.Tag = result.DownloadUrl;
        }
    }

    private void UpdateLink_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string url })
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // ignored
            }
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
