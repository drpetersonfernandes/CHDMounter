using System.Windows.Threading;

namespace SimpleChdDrive.Core.Tests.Services;

public class ServiceProviderTests
{
    [Fact]
    public void Register_ThenGet_ReturnsSameInstance()
    {
        var service = new TestService();
        ServiceProvider.Register<ITestService>(service);
        var resolved = ServiceProvider.Get<ITestService>();
        Assert.Same(service, resolved);
    }

    [Fact]
    public void Get_UnregisteredService_ThrowsInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(() => ServiceProvider.Get<INeverRegisteredService>());
    }

    [Fact]
    public void TryGet_UnregisteredService_ReturnsNull()
    {
        var result = ServiceProvider.TryGet<INeverRegisteredService>();
        Assert.Null(result);
    }

    [Fact]
    public void TryGet_RegisteredService_ReturnsInstance()
    {
        var service = new TestService();
        ServiceProvider.Register<ITestService>(service);
        var resolved = ServiceProvider.TryGet<ITestService>();
        Assert.Same(service, resolved);
    }

    [Fact]
    public void Register_Twice_OverwritesWithLast()
    {
        var service1 = new TestService();
        var service2 = new TestService();
        ServiceProvider.Register<ITestService>(service1);
        ServiceProvider.Register<ITestService>(service2);
        Assert.Same(service2, ServiceProvider.Get<ITestService>());
    }

    [Fact]
    public void DisposeAllServices_DisposesDisposableServices()
    {
        var disposable = new DisposableTestService();
        ServiceProvider.Register<IDisposableService>(disposable);
        ServiceProvider.DisposeAllServices();
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void DisposeAllServices_NonDisposableServices_DoNotThrow()
    {
        var service = new TestService();
        ServiceProvider.Register<ITestService>(service);
        var exception = Record.Exception(() => ServiceProvider.DisposeAllServices());
        Assert.Null(exception);
    }

    [Fact]
    public void DisposeAllServices_DisposeException_DoesNotThrow()
    {
        var throwing = new ThrowingDisposableService();
        ServiceProvider.Register<IThrowingDisposable>(throwing);
        var exception = Record.Exception(() => ServiceProvider.DisposeAllServices());
        Assert.Null(exception);
    }

    [Fact]
    public void DisposeAllServices_ClearsAllServices()
    {
        var service = new TestService();
        ServiceProvider.Register<ITestService>(service);
        ServiceProvider.DisposeAllServices();
        Assert.Null(ServiceProvider.TryGet<ITestService>());
    }

    public interface ITestService { }
    public interface INeverRegisteredService { }
    public interface IDisposableService : IDisposable { }
    public interface IThrowingDisposable : IDisposable { }

    public class TestService : ITestService { }

    public class DisposableTestService : IDisposableService
    {
        public bool IsDisposed { get; private set; }
        public void Dispose() { IsDisposed = true; }
    }

    public class ThrowingDisposableService : IThrowingDisposable
    {
        public void Dispose() => throw new InvalidOperationException("Test exception");
    }
}
