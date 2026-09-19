using BlockFarmEditor.Umbraco.Core.Interfaces;
using BlockFarmEditor.Umbraco.TagHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;

namespace BlockFarmEditor.Umbraco.Tests.TagHelpers;

public class RegisterBlockFarmEditorTagHelperTests
{
    private readonly Mock<IBlockFarmEditorContext> _context = new();
    private readonly Mock<IUrlHelperFactory> _urlHelperFactory = new();
    private readonly ViewContext _viewContext = new();

    private void SiteHostedAt(string pathBase)
    {
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper.Setup(x => x.Content(It.IsAny<string>())).Returns((string path) => pathBase + path.TrimStart('~'));
        _urlHelperFactory.Setup(x => x.GetUrlHelper(_viewContext)).Returns(urlHelper.Object);
    }

    private string Process()
    {
        var output = TagHelperTestHelper.Output("register-block-farm");
        new RegisterBlockFarmEditorTagHelper(_context.Object, _urlHelperFactory.Object) { ViewContext = _viewContext }
            .Process(TagHelperTestHelper.Context(), output);

        Assert.Null(output.TagName);
        return output.Content.GetContent();
    }

    [Fact]
    public void LiveMode_RendersNothing()
    {
        _context.SetupGet(x => x.IsEditMode).Returns(false);

        Assert.Equal(string.Empty, Process());
        _urlHelperFactory.VerifyNoOtherCalls();
    }

    [Fact]
    public void EditMode_RendersEditorAssets_AndTheCurrentContentKey()
    {
        var contentKey = Guid.NewGuid();
        _context.SetupGet(x => x.IsEditMode).Returns(true);
        _context.SetupGet(x => x.ContentUnique).Returns(contentKey);
        SiteHostedAt(string.Empty);

        var html = Process();

        Assert.Contains("<link rel='stylesheet' href='/App_Plugins/BlockFarmEditor.ClientScripts.RCL/block-editor/dist/block-editor.css'>", html);
        Assert.Contains("<script type='module' src='/App_Plugins/BlockFarmEditor.ClientScripts.RCL/block-editor/dist/block-editor.js'></script>", html);
        Assert.Contains($"window.blockFarmEditorUnique = '{contentKey}';", html);
        Assert.Contains("window.blockFarmEditorBasePath = '';", html);
    }

    [Fact]
    public void EditMode_WhenHostedInAVirtualDirectory_PrefixesAssetsAndBasePath()
    {
        _context.SetupGet(x => x.IsEditMode).Returns(true);
        SiteHostedAt("/site");

        var html = Process();

        Assert.Contains("href='/site/App_Plugins/BlockFarmEditor.ClientScripts.RCL/block-editor/dist/block-editor.css'", html);
        Assert.Contains("src='/site/App_Plugins/BlockFarmEditor.ClientScripts.RCL/block-editor/dist/block-editor.js'", html);
        Assert.Contains("window.blockFarmEditorBasePath = '/site';", html);
    }
}
