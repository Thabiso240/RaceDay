using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using RaceDay.API.Security;

namespace RaceDay.Tests;

public class RoleEnforcementTests
{
    private static AuthorizationFilterContext CreateAuthFilterContext(int? userId, string? role)
    {
        var httpContext = new DefaultHttpContext();
        if (userId.HasValue && !string.IsNullOrEmpty(role))
        {
            var claims = new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role)
            };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuth");
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(identity);
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Fact]
    public void AuthorizeRole_UnauthenticatedUser_Returns401Unauthorized()
    {
        // Arrange: anonymous request without authentication
        var filter = new AuthorizeRoleAttribute("Organiser");
        var context = CreateAuthFilterContext(null, null);

        // Act
        filter.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public void AuthorizeRole_ParticipantAttemptingOrganiserEndpoint_Returns403Forbidden()
    {
        // Arrange: Participant trying to call an Organiser-only endpoint
        var filter = new AuthorizeRoleAttribute("Organiser");
        var context = CreateAuthFilterContext(10, "Participant");

        // Act
        filter.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public void AuthorizeRole_OrganiserAttemptingParticipantEndpoint_Returns403Forbidden()
    {
        // Arrange: Organiser trying to call a Participant-only endpoint (e.g. race enrolment)
        var filter = new AuthorizeRoleAttribute("Participant");
        var context = CreateAuthFilterContext(5, "Organiser");

        // Act
        filter.OnAuthorization(context);

        // Assert
        Assert.NotNull(context.Result);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public void AuthorizeRole_OrganiserCallingOrganiserEndpoint_AllowsExecution()
    {
        // Arrange: Organiser accessing permitted endpoint
        var filter = new AuthorizeRoleAttribute("Organiser");
        var context = CreateAuthFilterContext(1, "Organiser");

        // Act
        filter.OnAuthorization(context);

        // Assert: result must remain null to allow the controller pipeline to proceed
        Assert.Null(context.Result);
    }

    [Fact]
    public void AuthorizeRole_ParticipantCallingParticipantEndpoint_AllowsExecution()
    {
        // Arrange: Participant accessing permitted endpoint
        var filter = new AuthorizeRoleAttribute("Participant");
        var context = CreateAuthFilterContext(2, "Participant");

        // Act
        filter.OnAuthorization(context);

        // Assert
        Assert.Null(context.Result);
    }
}
