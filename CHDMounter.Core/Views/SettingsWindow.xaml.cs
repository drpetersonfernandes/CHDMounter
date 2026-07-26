using System.Windows;

namespace CHDMounter.Core.Views;

public partial class SettingsWindow
{
    private readonly ISettingsService _settingsService;

    public SettingsWindow(ISettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        AutoOpenDriveCheckBox.IsChecked = _settingsService.Settings.AutoOpenMountedDrive;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.Settings.AutoOpenMountedDrive = AutoOpenDriveCheckBox.IsChecked == true;
        _settingsService.Save();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
