using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Library.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Editors;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Serialization;

namespace BlockFarmEditor.Umbraco.Tests.Helpers;

/// <summary>
/// Mocks for the Umbraco services the block pipeline talks to, the real Umbraco JSON serializer, and a real
/// <see cref="BlockPropertyValueMapper"/> wired to them.
/// </summary>
internal sealed class BlockFarmServiceProvider
{
    public Mock<IPublishedContentTypeCache> PublishedContentTypeCache { get; } = new();
    public Mock<IPublishedModelFactory> ModelFactory { get; } = new();
    public Mock<IVariationContextAccessor> VariationContextAccessor { get; } = new();
    public Mock<IContentTypeService> ContentTypeService { get; } = new();
    public Mock<IDataTypeService> DataTypeService { get; } = new();
    public Mock<IDataTypeConfigurationCache> DataTypeConfigurationCache { get; } = new();
    public Mock<IBlockDefinitionService> BlockDefinitionService { get; } = new();
    public List<IDataEditor> DataEditors { get; } = [];
    public List<ContentPropertyData> FromEditorCalls { get; } = [];
    public IJsonSerializer JsonSerializer { get; } = new SystemTextJsonSerializer(new DefaultJsonSerializerEncoderFactory());

    private readonly Lazy<IServiceProvider> _provider;
    private readonly Lazy<BlockPropertyValueMapper> _mapper;

    public BlockFarmServiceProvider(Action<IServiceCollection>? configure = null)
    {
        ModelFactory.Setup(x => x.CreateModel(It.IsAny<IPublishedElement>())).Returns((IPublishedElement element) => element);
        VariationContextAccessor.SetupGet(x => x.VariationContext).Returns(new VariationContext());
        BlockDefinitionService.Setup(x => x.GetConfigMaps()).Returns([]);

        _provider = new Lazy<IServiceProvider>(() =>
        {
            var services = new ServiceCollection();
            services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
            services.AddSingleton(DataTypeService.Object);
            services.AddSingleton(BlockDefinitionService.Object);
            services.AddSingleton(JsonSerializer);
            configure?.Invoke(services);
            return services.BuildServiceProvider();
        });

        _mapper = new Lazy<BlockPropertyValueMapper>(() => new BlockPropertyValueMapper(
            Provider,
            ContentTypeService.Object,
            PublishedContentTypeCache.Object,
            ModelFactory.Object,
            VariationContextAccessor.Object,
            new PropertyEditorCollection(new DataEditorCollection(() => DataEditors)),
            DataTypeConfigurationCache.Object,
            Mock.Of<IConfigurationEditorJsonSerializer>(),
            BlockDefinitionService.Object,
            NullLogger<BlockPropertyValueMapper>.Instance));
    }

    public IServiceProvider Provider => _provider.Value;

    public BlockPropertyValueMapper Mapper => _mapper.Value;

    /// <summary>
    /// Installs a property editor whose value editor echoes values (optionally transformed) and records what
    /// it was handed in <see cref="FromEditorCalls"/>.
    /// </summary>
    public Mock<IDataEditor> AddEditor(string alias, string valueType = ValueTypes.String, Func<object?, object?>? fromEditor = null, Func<object?, object?>? toEditor = null)
    {
        var valueEditor = new Mock<IDataValueEditor>();
        valueEditor.SetupGet(x => x.ValueType).Returns(valueType);
        valueEditor
            .Setup(x => x.FromEditor(It.IsAny<ContentPropertyData>(), It.IsAny<object?>()))
            .Returns((ContentPropertyData data, object? _) =>
            {
                FromEditorCalls.Add(data);
                return (fromEditor ?? (value => value))(data.Value);
            });
        valueEditor
            .Setup(x => x.ToEditor(It.IsAny<IProperty>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns((IProperty property, string? _, string? _) => (toEditor ?? (value => value))(property.GetValue()));

        var editor = new Mock<IDataEditor>();
        editor.SetupGet(x => x.Alias).Returns(alias);
        editor.Setup(x => x.GetValueEditor()).Returns(valueEditor.Object);
        DataEditors.Add(editor.Object);
        return editor;
    }

    /// <summary>Registers a backoffice content type, which is what editor/stored value mapping works from.</summary>
    public Mock<IContentType> AddContentType(string alias, params IPropertyType[] propertyTypes)
    {
        var contentType = UmbracoModels.ContentType(alias, noGroupPropertyTypes: propertyTypes);
        ContentTypeService.Setup(x => x.Get(contentType.Object.Key)).Returns(contentType.Object);
        return contentType;
    }

    /// <summary>Registers a published element type, which is what building published blocks works from.</summary>
    public Guid AddPublishedElementType(params string[] propertyAliases) =>
        AddPublishedElementType([.. propertyAliases.Select(alias => (alias, UmbracoModels.TextBoxEditorAlias))]);

    public Guid AddPublishedElementType(params (string Alias, string EditorAlias)[] properties)
    {
        var key = Guid.NewGuid();
        var contentType = new Mock<IPublishedContentType>();
        contentType.SetupGet(x => x.Key).Returns(key);
        contentType.SetupGet(x => x.ItemType).Returns(PublishedItemType.Element);
        contentType.SetupGet(x => x.PropertyTypes).Returns(properties.Select(x => PublishedPropertyType(x.Alias, x.EditorAlias)).ToList());
        PublishedContentTypeCache.Setup(x => x.Get(PublishedItemType.Element, key)).Returns(contentType.Object);
        return key;
    }

    private static IPublishedPropertyType PublishedPropertyType(string alias, string editorAlias)
    {
        var propertyType = new Mock<IPublishedPropertyType>();
        propertyType.SetupGet(x => x.Alias).Returns(alias);
        propertyType.SetupGet(x => x.EditorAlias).Returns(editorAlias);
        propertyType.SetupGet(x => x.CacheLevel).Returns(PropertyCacheLevel.Element);
        propertyType.SetupGet(x => x.DeliveryApiCacheLevel).Returns(PropertyCacheLevel.Element);
        propertyType.SetupGet(x => x.DeliveryApiCacheLevelForExpansion).Returns(PropertyCacheLevel.Element);
        return propertyType.Object;
    }
}
