namespace SimpleChdDrive.Core.Services;

public interface IUserNotificationService
{
    void ShowInfo(string message, string title);
    void ShowWarning(string message, string title);
    void ShowError(string message, string title);
}

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
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    public void ShowWarning(string message, string title)
    {
        _loggingService.LogError($"[{title}] {message}");
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
    }

    public void ShowError(string message, string title)
    {
        _loggingService.LogError($"[{title}] {message}");
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }
}
