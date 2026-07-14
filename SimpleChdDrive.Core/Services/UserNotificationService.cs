using System.Windows;

namespace SimpleChdDrive.Core.Services;

public class UserNotificationService : IUserNotificationService
{
    private readonly ILoggingService _loggingService;

    public UserNotificationService(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public void ShowInfo(string message, string title)
    {
        _loggingService.Log($"[{title}] {message}");
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public void ShowWarning(string message, string title)
    {
        _loggingService.LogError($"[{title}] {message}");
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void ShowError(string message, string title)
    {
        _loggingService.LogError($"[{title}] {message}");
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
