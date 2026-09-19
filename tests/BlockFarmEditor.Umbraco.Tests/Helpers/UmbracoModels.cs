using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;

namespace BlockFarmEditor.Umbraco.Tests.Helpers;

/// <summary>Builders for the real Umbraco model classes the services work with.</summary>
internal static class UmbracoModels
{
    public const string TextBoxEditorAlias = "Umbraco.TextBox";

    public static readonly IShortStringHelper ShortStringHelper =
        new DefaultShortStringHelper(new DefaultShortStringHelperConfig().WithDefault(new RequestHandlerSettings()));

    public static PropertyType PropertyType(string alias, Guid? dataTypeKey = null, string editorAlias = TextBoxEditorAlias, string? name = null) =>
        new(ShortStringHelper, editorAlias, ValueStorageType.Ntext, alias)
        {
            Key = Guid.NewGuid(),
            Name = name ?? alias,
            DataTypeKey = dataTypeKey ?? Guid.NewGuid()
        };

    public static PropertyGroup Group(string alias, string name, params IPropertyType[] propertyTypes) =>
        PropertyGroup(alias, name, PropertyGroupType.Group, propertyTypes);

    public static PropertyGroup Tab(string alias, string name, params IPropertyType[] propertyTypes) =>
        PropertyGroup(alias, name, PropertyGroupType.Tab, propertyTypes);

    private static PropertyGroup PropertyGroup(string alias, string name, PropertyGroupType type, IPropertyType[] propertyTypes) =>
        new(new PropertyTypeCollection(false, propertyTypes))
        {
            Key = Guid.NewGuid(),
            Alias = alias,
            Name = name,
            Type = type
        };

    private static int _nextDataTypeId = 1000;

    /// <summary>A persisted data type: it has an identity, which is what property types link to.</summary>
    public static Mock<IDataType> DataType(Guid key, string name = "Textstring", string editorAlias = TextBoxEditorAlias, IDictionary<string, object>? configuration = null)
    {
        var dataType = new Mock<IDataType>();
        dataType.SetupGet(x => x.Id).Returns(Interlocked.Increment(ref _nextDataTypeId));
        dataType.SetupGet(x => x.HasIdentity).Returns(true);
        dataType.SetupGet(x => x.Key).Returns(key);
        dataType.SetupGet(x => x.Name).Returns(name);
        dataType.SetupGet(x => x.EditorAlias).Returns(editorAlias);
        dataType.SetupProperty(x => x.EditorUiAlias);
        dataType.SetupProperty(x => x.ConfigurationData, configuration ?? new Dictionary<string, object>());
        return dataType;
    }

    /// <summary>
    /// A mocked content type backed by real groups/property types. Composition members default to the
    /// type's own members, as they do for a content type without compositions.
    /// </summary>
    public static Mock<IContentType> ContentType(
        string alias,
        Guid? key = null,
        IEnumerable<PropertyGroup>? groups = null,
        IEnumerable<IPropertyType>? noGroupPropertyTypes = null,
        IEnumerable<IContentTypeComposition>? compositions = null)
    {
        var ownGroups = (groups ?? []).ToList();
        var ownLooseProperties = (noGroupPropertyTypes ?? []).ToList();
        var ownCompositions = (compositions ?? []).ToList();
        var ownProperties = ownGroups.SelectMany(x => x.PropertyTypes?.AsEnumerable() ?? []).Concat(ownLooseProperties).ToList();

        var contentType = new Mock<IContentType>();
        contentType.SetupGet(x => x.Key).Returns(key ?? Guid.NewGuid());
        contentType.SetupGet(x => x.Alias).Returns(alias);
        contentType.SetupProperty(x => x.Name, alias);
        contentType.SetupProperty(x => x.Description);
        contentType.SetupProperty(x => x.Icon);
        contentType.SetupProperty(x => x.IsElement, true);
        contentType.SetupProperty(x => x.AllowedAsRoot);
        contentType.SetupProperty(x => x.Variations);
        contentType.SetupGet(x => x.PropertyGroups).Returns(new PropertyGroupCollection(ownGroups));
        contentType.SetupGet(x => x.PropertyTypes).Returns(ownProperties);
        contentType.SetupGet(x => x.NoGroupPropertyTypes).Returns(ownLooseProperties);
        contentType.SetupGet(x => x.ContentTypeComposition).Returns(ownCompositions);
        contentType.SetupGet(x => x.CompositionPropertyGroups).Returns(ownGroups.Concat(ownCompositions.SelectMany(x => x.CompositionPropertyGroups)));
        contentType.SetupGet(x => x.CompositionPropertyTypes).Returns(ownProperties.Concat(ownCompositions.SelectMany(x => x.CompositionPropertyTypes)));
        return contentType;
    }
}
