using BlockFarmEditor.Umbraco.Core.Models.BuilderModels;
using BlockFarmEditor.Umbraco.Library.Services;
using BlockFarmEditor.Umbraco.Tests.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;

namespace BlockFarmEditor.Umbraco.Tests.Services;

public class BlockFarmEditorContextTests
{
    private const int LoginNodeId = 1001;
    private const int NoAccessNodeId = 1002;
    private const string ProtectedPath = "-1,1234";

    private readonly Mock<IUmbracoContextAccessor> _umbracoContextAccessor = new();
    private readonly Mock<IMemberManager> _memberManager = new();
    private readonly Mock<IPublicAccessService> _publicAccessService = new();
    private readonly Mock<IUmbracoContext> _umbracoContext = new();
    private readonly Mock<IPublishedContentCache> _contentCache = new();
    private readonly BlockFarmEditorContext _context;

    public BlockFarmEditorContextTests()
    {
        UmbracoStaticServices.EnsureInitialized();

        _umbracoContext.SetupGet(x => x.Content).Returns(_contentCache.Object);
        _publicAccessService.Setup(x => x.IsProtected(It.IsAny<string>())).Returns(Attempt<PublicAccessEntry?>.Fail());

        _context = new BlockFarmEditorContext(_umbracoContextAccessor.Object, _memberManager.Object, _publicAccessService.Object, new MemoryCache(new MemoryCacheOptions()));
    }

    #region Arrange helpers

    private static Mock<IPublishedContent> Content(object? blockFarmValue = null, bool hasBlockFarmProperty = true, string? culture = null, string path = ProtectedPath)
    {
        var contentType = new Mock<IPublishedContentType>();
        var content = new Mock<IPublishedContent>();
        content.SetupGet(x => x.Key).Returns(Guid.NewGuid());
        content.SetupGet(x => x.Path).Returns(path);
        content.SetupGet(x => x.ContentType).Returns(contentType.Object);

        if (hasBlockFarmProperty)
        {
            contentType.Setup(x => x.GetPropertyType(BlockFarmEditorContext.BlockFarmEditorEditorAlias)).Returns(Mock.Of<IPublishedPropertyType>());

            var property = new Mock<IPublishedProperty>();
            property.Setup(x => x.HasValue(culture, null)).Returns(blockFarmValue != null);
            property.Setup(x => x.GetValue(culture, null)).Returns(blockFarmValue);
            content.Setup(x => x.GetProperty(BlockFarmEditorContext.BlockFarmEditorEditorAlias)).Returns(property.Object);
        }

        return content;
    }

    private static PageDefinition PageWithBlocks() => new() { Blocks = [new BlockDefinition<IPublishedElement> { Unique = Guid.NewGuid() }] };

    /// <summary>
    /// <c>HasAccessAsync</c> is an extension method that evaluates the entry's rules against the member's
    /// username and roles, so access is arranged through real rules rather than by mocking the outcome.
    /// </summary>
    private void Protect(string path, string allowedRole = "Gold")
    {
        var rule = new PublicAccessRule { RuleType = Constants.Conventions.PublicAccess.MemberRoleRuleType, RuleValue = allowedRole };
        var entry = new PublicAccessEntry(Guid.NewGuid(), 1234, LoginNodeId, NoAccessNodeId, [rule]) { Id = 1 };
        _publicAccessService.Setup(x => x.IsProtected(path)).Returns(Attempt<PublicAccessEntry?>.Succeed(entry));
        _publicAccessService.Setup(x => x.GetEntryForContent(path)).Returns(entry);
    }

    private MemberIdentityUser SignedInMember(params string[] roles)
    {
        var member = new MemberIdentityUser { Key = Guid.NewGuid(), UserName = "member@example.test" };
        _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(member);
        _memberManager.Setup(x => x.GetRolesAsync(member)).ReturnsAsync(roles);
        return member;
    }

    private void CurrentRequest(string url, IPublishedContent? content, bool inPreviewMode, string? culture = null)
    {
        var request = new Mock<IPublishedRequest>();
        request.SetupGet(x => x.PublishedContent).Returns(content);
        request.SetupGet(x => x.Culture).Returns(culture);

        _umbracoContext.SetupGet(x => x.OriginalRequestUrl).Returns(new Uri(url));
        _umbracoContext.SetupGet(x => x.CleanedUmbracoUrl).Returns(new Uri(url));
        _umbracoContext.SetupGet(x => x.PublishedRequest).Returns(request.Object);
        _umbracoContext.SetupGet(x => x.InPreviewMode).Returns(inPreviewMode);

        var umbracoContext = _umbracoContext.Object;
        _umbracoContextAccessor.Setup(x => x.TryGetUmbracoContext(out umbracoContext)).Returns(true);
    }

    private Task SetPageDefinition(IPublishedContent? content, string? culture = null, bool editMode = false, bool preview = false) =>
        _context.SetPageDefinition(_umbracoContext.Object, content, "example.test", culture, editMode, preview);

    #endregion

    [Fact]
    public void NewContext_IsInLiveMode_WithNoPage()
    {
        Assert.False(_context.IsEditMode);
        Assert.False(_context.IsPreview);
        Assert.Equal(Guid.Empty, _context.ContentUnique);
        Assert.Null(_context.PageDefinition);
        Assert.Null(_context._currentScope);
    }

    [Fact]
    public void EditorAlias_MatchesThePropertyEditorRegisteredInTheBackoffice()
    {
        Assert.Equal("blockfarmeditor_page_propertyeditor", BlockFarmEditorContext.BlockFarmEditorEditorAlias);
    }

    #region SetPageDefinition(context, content, ...)

    [Fact]
    public async Task SetPageDefinition_WithoutContent_ChangesNothing()
    {
        await SetPageDefinition(null, editMode: true, preview: true);

        Assert.Null(_context.PageDefinition);
        Assert.False(_context.IsEditMode);
        Assert.Equal(Guid.Empty, _context.ContentUnique);
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(false, true, false, true)]
    [InlineData(true, true, true, true)]
    public async Task SetPageDefinition_EditModeRequiresPreview(bool editMode, bool preview, bool expectedEditMode, bool expectedPreview)
    {
        await SetPageDefinition(Content().Object, editMode: editMode, preview: preview);

        Assert.Equal(expectedEditMode, _context.IsEditMode);
        Assert.Equal(expectedPreview, _context.IsPreview);
    }

    [Fact]
    public async Task SetPageDefinition_RecordsTheContentKey()
    {
        var content = Content();

        await SetPageDefinition(content.Object);

        Assert.Equal(content.Object.Key, _context.ContentUnique);
    }

    [Fact]
    public async Task SetPageDefinition_UsesThePageDefinitionStoredOnTheContent()
    {
        var page = PageWithBlocks();

        await SetPageDefinition(Content(page).Object);

        Assert.Same(page, _context.PageDefinition);
    }

    [Fact]
    public async Task SetPageDefinition_ReadsTheValueForTheRequestedCulture()
    {
        var danish = PageWithBlocks();

        await SetPageDefinition(Content(danish, culture: "da-DK").Object, culture: "da-DK");

        Assert.Same(danish, _context.PageDefinition);
    }

    [Fact]
    public async Task SetPageDefinition_WhenContentTypeHasNoBlockFarmProperty_UsesAnEmptyPage()
    {
        await SetPageDefinition(Content(hasBlockFarmProperty: false).Object);

        Assert.NotNull(_context.PageDefinition);
        Assert.Empty(_context.PageDefinition.Blocks);
        _publicAccessService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{\"blocks\":[]}")]
    public async Task SetPageDefinition_WhenValueIsNotAPageDefinition_UsesAnEmptyPage(object? value)
    {
        await SetPageDefinition(Content(value).Object);

        Assert.NotNull(_context.PageDefinition);
        Assert.Empty(_context.PageDefinition.Blocks);
    }

    #endregion

    #region Member protection

    [Fact]
    public async Task ProtectedContent_ForAnonymousVisitor_ShowsTheLoginPage_AndLeavesEditMode()
    {
        var loginPageDefinition = PageWithBlocks();
        Protect(ProtectedPath);
        _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync((MemberIdentityUser?)null);
        _contentCache.Setup(x => x.GetById(LoginNodeId)).Returns(Content(loginPageDefinition, path: "-1,1001").Object);

        await SetPageDefinition(Content(PageWithBlocks()).Object, editMode: true, preview: true);

        Assert.Same(loginPageDefinition, _context.PageDefinition);
        Assert.False(_context.IsEditMode);
        Assert.True(_context.IsPreview);
    }

    [Fact]
    public async Task ProtectedContent_ForAnonymousVisitor_WithoutALoginPage_UsesAnEmptyPage()
    {
        Protect(ProtectedPath);
        _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync((MemberIdentityUser?)null);

        await SetPageDefinition(Content(PageWithBlocks()).Object);

        Assert.NotNull(_context.PageDefinition);
        Assert.Empty(_context.PageDefinition.Blocks);
    }

    [Fact]
    public async Task ProtectedContent_ForMemberWithAccess_ShowsTheContent()
    {
        var page = PageWithBlocks();
        Protect(ProtectedPath);
        SignedInMember("Gold");

        await SetPageDefinition(Content(page).Object, editMode: true, preview: true);

        Assert.Same(page, _context.PageDefinition);
        Assert.True(_context.IsEditMode);
    }

    [Fact]
    public async Task ProtectedContent_ForMemberWithoutAccess_ShowsTheNoAccessPage_AndLeavesEditMode()
    {
        var noAccessPageDefinition = PageWithBlocks();
        Protect(ProtectedPath);
        SignedInMember("Bronze");
        _contentCache.Setup(x => x.GetById(NoAccessNodeId)).Returns(Content(noAccessPageDefinition, path: "-1,1002").Object);

        await SetPageDefinition(Content(PageWithBlocks()).Object, editMode: true, preview: true);

        Assert.Same(noAccessPageDefinition, _context.PageDefinition);
        Assert.False(_context.IsEditMode);
    }

    [Fact]
    public async Task ProtectedContent_ForMemberWithoutAccess_AndNoErrorPage_UsesAnEmptyPage()
    {
        Protect(ProtectedPath);
        SignedInMember("Bronze");

        await SetPageDefinition(Content(PageWithBlocks()).Object);

        Assert.NotNull(_context.PageDefinition);
        Assert.Empty(_context.PageDefinition.Blocks);
    }

    [Fact]
    public async Task ProtectedContent_AccessDecisionIsCachedPerMemberAndContent()
    {
        Protect(ProtectedPath);
        var member = SignedInMember("Gold");
        var content = Content(PageWithBlocks()).Object;
        var otherContent = Content(PageWithBlocks()).Object;

        await SetPageDefinition(content);
        await SetPageDefinition(content);

        _memberManager.Verify(x => x.GetRolesAsync(member), Times.Once);

        await SetPageDefinition(otherContent);

        _memberManager.Verify(x => x.GetRolesAsync(member), Times.Exactly(2));
    }

    [Fact]
    public async Task ProtectedContent_CachedDecisionsAreNotSharedBetweenMembers()
    {
        var page = PageWithBlocks();
        var content = Content(page).Object;
        Protect(ProtectedPath);

        SignedInMember("Gold");
        await SetPageDefinition(content);
        Assert.Same(page, _context.PageDefinition);

        SignedInMember("Bronze");
        await SetPageDefinition(content);
        Assert.NotSame(page, _context.PageDefinition);
    }

    #endregion

    #region SetPageDefinition()

    [Fact]
    public async Task SetPageDefinitionFromRequest_WithoutAnUmbracoContext_ChangesNothing()
    {
        await _context.SetPageDefinition();

        Assert.Null(_context.PageDefinition);
    }

    [Fact]
    public async Task SetPageDefinitionFromRequest_UsesThePublishedRequestsContent()
    {
        var page = PageWithBlocks();
        var content = Content(page);
        CurrentRequest("https://example.test/some-page/", content.Object, inPreviewMode: false);

        await _context.SetPageDefinition();

        Assert.Same(page, _context.PageDefinition);
        Assert.Equal(content.Object.Key, _context.ContentUnique);
        Assert.False(_context.IsEditMode);
        Assert.False(_context.IsPreview);
    }

    [Theory]
    [InlineData("?editmode=true", true, true)]
    [InlineData("?editmode=true", false, false)]
    [InlineData("?editmode=false", true, false)]
    [InlineData("?editmode=TRUE", true, false)]
    [InlineData("", true, false)]
    [InlineData("?other=1&editmode=true", true, true)]
    public async Task SetPageDefinitionFromRequest_EntersEditMode_OnlyForPreviewRequestsWithTheEditModeFlag(string query, bool inPreviewMode, bool expectedEditMode)
    {
        CurrentRequest($"https://example.test/some-page/{query}", Content(PageWithBlocks()).Object, inPreviewMode);

        await _context.SetPageDefinition();

        Assert.Equal(expectedEditMode, _context.IsEditMode);
        Assert.Equal(inPreviewMode, _context.IsPreview);
    }

    [Fact]
    public async Task SetPageDefinitionFromRequest_UsesTheRequestCulture()
    {
        var danish = PageWithBlocks();
        CurrentRequest("https://example.test/da/", Content(danish, culture: "da-DK").Object, inPreviewMode: false, culture: "da-DK");

        await _context.SetPageDefinition();

        Assert.Same(danish, _context.PageDefinition);
    }

    [Fact]
    public async Task SetPageDefinitionFromRequest_WithoutRoutedContent_ChangesNothing()
    {
        CurrentRequest("https://example.test/missing/", content: null, inPreviewMode: false);

        await _context.SetPageDefinition();

        Assert.Null(_context.PageDefinition);
    }

    #endregion

    #region Scopes

    [Fact]
    public void GetBlockScope_WithoutAnActiveScope_ReturnsARootScopeForThePage()
    {
        _context.PageDefinition = PageWithBlocks();

        var scope = _context.GetBlockScope();

        Assert.NotNull(scope);
        Assert.Same(_context.PageDefinition, scope.Block);
        Assert.Equal([new Guid(PageDefinition.GuidUnique)], scope.IdentifierList);
        Assert.Null(_context._currentScope);
    }

    [Fact]
    public void GetBlockScope_ForABlock_BecomesTheActiveScope()
    {
        _context.PageDefinition = PageWithBlocks();
        var block = Definitions.Block(Guid.NewGuid());

        var scope = _context.GetBlockScope(block);

        Assert.NotNull(scope);
        Assert.Same(scope, _context._currentScope);
        Assert.Same(scope, _context.GetBlockScope());
        Assert.Same(block, scope.Block);
    }

    [Fact]
    public void BlockScopes_Nest_AndUnwindOnDispose()
    {
        _context.PageDefinition = PageWithBlocks();
        var outerKey = Guid.NewGuid();
        var innerKey = Guid.NewGuid();

        using (var outer = _context.GetBlockScope(Definitions.Block(outerKey)))
        {
            using (var inner = _context.GetBlockScope(Definitions.Block(innerKey)))
            {
                Assert.Same(inner, _context._currentScope);
                Assert.Equal([outerKey, innerKey], inner!.IdentifierList);
            }

            Assert.Same(outer, _context._currentScope);
        }

        Assert.Null(_context._currentScope);
    }

    #endregion
}
