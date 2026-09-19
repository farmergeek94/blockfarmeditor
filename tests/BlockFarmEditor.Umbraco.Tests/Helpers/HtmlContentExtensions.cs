using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace BlockFarmEditor.Umbraco.Tests.Helpers;

internal static class HtmlContentExtensions
{
    public static string Render(this IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }
}
