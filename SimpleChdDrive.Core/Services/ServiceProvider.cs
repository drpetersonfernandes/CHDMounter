using System.Diagnostics;

namespace SimpleChdDrive.Core.Services;

public static class ServiceProvider
{
    private static readonly ConcurrentDictionary<Type, object> Services = new();

    public static void Register<T>(T implementation) where T : notnull
    {
        Services[typeof(T)] = implementation;
    }

    public static T Get<T>() where T : notnull
    {
        if (Services.TryGetValue(typeof(T), out var service))
            return (T)service;

        throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }

    public static T TryGet<T>() where T : class
    {
        if (Services.TryGetValue(typeof(T), out var service))
            return (T)service;

        return null!;
    }

    public static void DisposeAllServices()
    {
        foreach (var kvp in Services)
        {
            if (kvp.Value is IDisposable disposable)
            {
                try { disposable.Dispose(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ServiceProvider: Failed to dispose {kvp.Key.Name}: {ex.Message}");
                }
            }
        }

        Services.Clear();
    }
}
