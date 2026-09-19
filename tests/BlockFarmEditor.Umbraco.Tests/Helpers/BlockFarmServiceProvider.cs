using System.Text.Json;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Library.Converters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Serialization;

namespace BlockFarmEditor.Umbraco.Tests.Helpers;

/// <summary>
/// The converters pull their dependencies out of an <see cref="IServiceProvider"/>; this wires one up with
/// mocks for the Umbraco services and the real Umbraco JSON serializer.
/// </summary>
internal sealed class BlockFarmServiceProvider
{
    public Mock<IPublishedContentTypeCache> PublishedContentTypeCache { get; } = new();
    public Mock<IPublishedModelFactory> ModelFactory { get; } = new();
    public Mock<IVariationContextAccessor> VariationContextAccessor { get; } = new();
    public Mock<IContentTypeService> ContentTypeService { get; } = new();
    public Mock<IDataTypeService> DataTypeService { get; } = new();
    public Mock<IBlockDefinitionService> BlockDefinitionService { get; } = new();
    public List<IDataEditor> DataEditors { get; } = [];
    public IJsonSerializer JsonSerializer { get; } = new SystemTextJsonSerializer(new DefaultJsonSerializerEncoderFactory());

    private readonly Lazy<IServiceProvider> _provider;

    public BlockFarmServiceProvider(Action<IServiceCollection>? configure = null)
    {
        ModelFactory.Setup(x => x.CreateModel(It.IsAny<IPublishedElement>())).Returns((IPublishedElement element) => element);
        VariationContextAccessor.SetupGet(x => x.VariationContext).Returns(new VariationContext());
        BlockDefinitionService.Setup(x => x.GetConfigMaps()).Returns([]);

        _provider = new Lazy<IServiceProvider>(() =>
        {
            var services = new ServiceCollection();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddSingleton(PublishedContentTypeCache.Object);
            services.AddSingleton(ModelFactory.Object);
            services.AddSingleton(VariationContextAccessor.Object);
            services.AddSingleton(ContentTypeService.Object);
            services.AddSingleton(DataTypeService.Object);
            services.AddSingleton(BlockDefinitionService.Object);
            services.AddSingleton(JsonSerializer);
            services.AddSingleton(new PropertyEditorCollection(new DataEditorCollection(() => DataEditors)));
            configure?.Invoke(services);
            return services.BuildServiceProvider();
        });
    }

    public IServiceProvider Provider => _provider.Value;

    /// <summary>Same shape as <c>BlockDefinitionService.JsonSerializerReaderOptions</c>.</summary>
    public JsonSerializerOptions ReaderOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new BuilderPropertiesConverter(Provider) }
    };

    /// <summary>Same shape as <c>BlockDefinitionService.JsonSerializerWriterOptions</c>.</summary>
    public JsonSerializerOptions WriterOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new BuilderPropertiesWriter(Provider) }
    };

    /// <summary>Registers a published element type so blocks of that type deserialize with properties.</summary>
    public Guid AddPublishedElementType(params (string Alias, Type ClrType)[] properties)
    {
        var key = Guid.NewGuid();
        var contentType = new Mock<IPublishedContentType>();
        contentType.SetupGet(x => x.Key).Returns(key);
        contentType.SetupGet(x => x.ItemType).Returns(PublishedItemType.Element);
        contentType.SetupGet(x => x.PropertyTypes).Returns(properties.Select(x => PublishedPropertyType(x.Alias, x.ClrType)).ToList());
        PublishedContentTypeCache.Setup(x => x.Get(PublishedItemType.Element, key)).Returns(contentType.Object);
        return key;
    }

    private static IPublishedPropertyType PublishedPropertyType(string alias, Type clrType)
    {
        var propertyType = new Mock<IPublishedPropertyType>();
        propertyType.SetupGet(x => x.Alias).Returns(alias);
        propertyType.SetupGet(x => x.ModelClrType).Returns(clrType);
        propertyType.SetupGet(x => x.CacheLevel).Returns(PropertyCacheLevel.Element);
        propertyType.SetupGet(x => x.DeliveryApiCacheLevel).Returns(PropertyCacheLevel.Element);
        propertyType.SetupGet(x => x.DeliveryApiCacheLevelForExpansion).Returns(PropertyCacheLevel.Element);
        return propertyType.Object;
    }
}
