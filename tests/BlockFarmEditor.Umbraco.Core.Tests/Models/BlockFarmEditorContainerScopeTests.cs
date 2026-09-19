using System.Text;
using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.Core.Models;
using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace BlockFarmEditor.Umbraco.Core.Tests.Models;

public class BlockFarmEditorContainerScopeTests
{
    private static readonly Guid PageKey = new(PageDefinition.GuidUnique);

    private static IContainerDefinition Container(Guid? contentTypeKey)
    {
        var container = new Mock<IContainerDefinition>();
        container.SetupGet(x => x.ContentTypeKey).Returns(contentTypeKey);
        return container.Object;
    }

    private static Mock<IBlockFarmEditorContext> Context()
    {
        var context = new Mock<IBlockFarmEditorContext>();
        context.SetupProperty(x => x._currentScope);
        return context;
    }

    [Fact]
    public void Block_ExposesTheContainerItWasCreatedFor()
    {
        var page = new PageDefinition();

        var scope = new BlockFarmEditorContainerScope(Context().Object, page, null);

        Assert.Same(page, scope.Block);
    }

    [Fact]
    public void IdentifierList_ForRootScope_ContainsOnlyTheContainersKey()
    {
        var scope = new BlockFarmEditorContainerScope(Context().Object, new PageDefinition(), null);

        Assert.Equal([PageKey], scope.IdentifierList);
    }

    [Fact]
    public void IdentifierList_ForNestedScope_AppendsToTheParentsList()
    {
        var context = Context().Object;
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        var root = new BlockFarmEditorContainerScope(context, new PageDefinition(), null);
        var child = new BlockFarmEditorContainerScope(context, Container(first), root);
        var grandChild = new BlockFarmEditorContainerScope(context, Container(second), child);

        Assert.Equal([PageKey, first, second], grandChild.IdentifierList);
        Assert.Equal([PageKey, first], child.IdentifierList);
    }

    [Fact]
    public void IdentifierList_WhenContainerHasNoContentTypeKey_InheritsTheParentsListUnchanged()
    {
        var context = Context().Object;
        var root = new BlockFarmEditorContainerScope(context, new PageDefinition(), null);

        var child = new BlockFarmEditorContainerScope(context, Container(null), root);

        Assert.Equal([PageKey], child.IdentifierList);
    }

    [Fact]
    public void IdentifierList_WhenNoKeyAndNoParent_IsEmpty()
    {
        var scope = new BlockFarmEditorContainerScope(Context().Object, Container(null), null);

        Assert.Empty(scope.IdentifierList);
    }

    [Fact]
    public void Dispose_RestoresThePreviousScopeOnTheContext()
    {
        var context = Context();
        var root = new BlockFarmEditorContainerScope(context.Object, new PageDefinition(), null);
        var child = new BlockFarmEditorContainerScope(context.Object, Container(Guid.NewGuid()), root);
        context.Object._currentScope = child;

        child.Dispose();

        Assert.Same(root, context.Object._currentScope);
    }

    [Fact]
    public void Dispose_OfRootScope_ClearsTheContextScope()
    {
        var context = Context();
        var root = new BlockFarmEditorContainerScope(context.Object, new PageDefinition(), null);
        context.Object._currentScope = root;

        root.Dispose();

        Assert.Null(context.Object._currentScope);
    }

    [Fact]
    public void GetIdentityPath_IsBase64OfPipeSeparatedIdentifiers()
    {
        var context = Context().Object;
        var key = Guid.NewGuid();
        var root = new BlockFarmEditorContainerScope(context, new PageDefinition(), null);
        var child = new BlockFarmEditorContainerScope(context, Container(key), root);

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(child.GetIdentityPath()));

        Assert.Equal($"{PageKey}|{key}", decoded);
    }

    [Fact]
    public void ParseIdentityPath_RoundTripsGetIdentityPath()
    {
        var context = Context().Object;
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var root = new BlockFarmEditorContainerScope(context, new PageDefinition(), null);
        var child = new BlockFarmEditorContainerScope(context, Container(first), root);
        var grandChild = new BlockFarmEditorContainerScope(context, Container(second), child);

        var parsed = BlockFarmEditorContainerScope.ParseIdentityPath(grandChild.GetIdentityPath());

        Assert.Equal([PageKey.ToString(), first.ToString(), second.ToString()], parsed);
    }

    [Fact]
    public void ParseIdentityPath_OfEmptyScope_YieldsSingleEmptySegment()
    {
        var scope = new BlockFarmEditorContainerScope(Context().Object, Container(null), null);

        var parsed = BlockFarmEditorContainerScope.ParseIdentityPath(scope.GetIdentityPath());

        Assert.Equal([string.Empty], parsed);
    }

    [Fact]
    public void ParseIdentityPath_WithInvalidBase64_Throws()
    {
        Assert.Throws<FormatException>(() => BlockFarmEditorContainerScope.ParseIdentityPath("not base64!"));
    }
}
