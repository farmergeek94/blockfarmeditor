using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;

namespace BlockFarmEditor.Umbraco.Tests.Controllers;

internal static class ControllerTestExtensions
{
    public static T WithHttpContext<T>(this T controller, string? body = null) where T : Controller
    {
        var httpContext = new DefaultHttpContext();
        if (body != null)
        {
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
            RouteData = new RouteData(),
            ActionDescriptor = new ControllerActionDescriptor { ControllerName = "Test", ActionName = "Test" }
        };
        return controller;
    }

    public static T SignedInAs<T>(this T controller, string username) where T : Controller
    {
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, username)], authenticationType: "Test"));
        return controller;
    }

    public static Guid AddUser(this Mock<IUserService> userService, string username)
    {
        var key = Guid.NewGuid();
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Key).Returns(key);
        userService.Setup(x => x.GetByUsername(username)).Returns(user.Object);
        return key;
    }

    public static Guid AddAdminUser(this Mock<IUserService> userService)
    {
        var key = Guid.NewGuid();
        var user = new Mock<IUser>();
        user.SetupGet(x => x.Key).Returns(key);
        userService.Setup(x => x.GetUserById(-1)).Returns(user.Object);
        return key;
    }

    /// <summary>The actions return anonymous objects; going through JSON lets tests read them like a client would.</summary>
    public static JsonElement Json(this ObjectResult result) =>
        JsonSerializer.SerializeToElement(result.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public static void AssertServerError(this IActionResult result, string expectedMessage)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.Equal(expectedMessage, objectResult.Value);
    }
}
