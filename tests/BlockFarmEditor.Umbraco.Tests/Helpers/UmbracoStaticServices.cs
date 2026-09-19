using System.Collections.Concurrent;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;

namespace BlockFarmEditor.Umbraco.Tests.Helpers;

/// <summary>
/// Umbraco's "friendly" extension methods (<c>content.Value(alias)</c>, <c>content.Url()</c>) resolve their
/// dependencies from the process-wide <see cref="StaticServiceProvider"/>, once, in a static initializer.
/// Tests that reach those extensions call <see cref="EnsureInitialized"/> to back the locator with
/// inert auto-mocks plus the few deterministic fakes below.
/// </summary>
internal static class UmbracoStaticServices
{
    public const string ContentUrl = "https://example.test/some-page/";

    private static readonly Lock InitializationLock = new();
    private static bool _initialized;

    public static void EnsureInitialized()
    {
        lock (InitializationLock)
        {
            if (_initialized)
            {
                return;
            }

            var urlProvider = new Mock<IPublishedUrlProvider>();
            urlProvider
                .Setup(x => x.GetUrl(It.IsAny<IPublishedContent>(), It.IsAny<UrlMode>(), It.IsAny<string?>(), It.IsAny<Uri?>()))
                .Returns(ContentUrl);

            StaticServiceProvider.Instance = new AutoMockingServiceProvider()
                .With<IPublishedValueFallback>(new NoopPublishedValueFallback())
                .With(urlProvider.Object);

            _initialized = true;
        }
    }

    private sealed class AutoMockingServiceProvider : IServiceProvider
    {
        private readonly ConcurrentDictionary<Type, object?> _services = new();

        public AutoMockingServiceProvider With<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance;
            return this;
        }

        public object? GetService(Type serviceType) =>
            _services.GetOrAdd(serviceType, static type => type.IsInterface
                ? ((Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(type))!).Object
                : null);
    }
}
