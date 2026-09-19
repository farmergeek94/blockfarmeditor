using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BlockFarmEditor.Umbraco.Tests.TagHelpers;

internal static class TagHelperTestHelper
{
    public static TagHelperContext Context() => new([], new Dictionary<object, object>(), Guid.NewGuid().ToString("N"));

    public static TagHelperOutput Output(string tagName) =>
        new(tagName, [], (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));
}
