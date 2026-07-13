namespace SimpleChdDrive.Core.Services;

public static class ServiceProvider
{
    private static readonly ConcurrentDictionary<Type, object> _services = new();

    public static void Register<T>(T implementation) where T : notnull
    {
        _services[typeof(T)] = implementation;
    }

    public static T Get<T>() where T : notnull
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return (T)service;

        throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    public static T TryGet<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return (T)service;

        return null;
    }

    public static void DisposeAllServices()
    {
        foreach (var kvp in _services)
        {
            if (kvp.Value is IDisposable disposable)
            {
                try { disposable.Dispose(); }
                catch { }
            }
        }
        _services.Clear();
    }
}
