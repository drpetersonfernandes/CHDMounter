using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SimpleChdDrive.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;

    public AppSettings Settings { get; private set; } = new();

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
