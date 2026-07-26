using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CHDMounter.Core.Services;

/// <summary>
/// Manages loading and saving of application settings using DPAPI encryption.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;

    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    public AppSettings Settings { get; private set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class and loads settings from disk.
    /// </summary>
    /// <param name="appName">The application name used to determine the settings folder path.</param>
    public SettingsService(string appName)
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            appName);
        _settingsFilePath = Path.Combine(folder, "settings.dat");
        Load();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
                return;

            var encrypted = File.ReadAllBytes(_settingsFilePath);
            var decrypted = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(decrypted);
            Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            Settings = new AppSettings();
        }
    }

    /// <summary>
    /// Saves the current settings to disk with DPAPI encryption.
    /// </summary>
    public void Save()
    {
        try
        {
            var folder = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);

            var json = JsonSerializer.Serialize(Settings);
            var data = Encoding.UTF8.GetBytes(json);
            var encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(_settingsFilePath, encrypted);
        }
        catch
        {
            // ignored
        }
    }
}
