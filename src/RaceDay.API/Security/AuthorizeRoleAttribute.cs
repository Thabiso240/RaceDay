using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace RaceDay.API.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _allowedRoles;

    public AuthorizeRoleAttribute(params string[] roles)
    {
        _allowedRoles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Check if authenticated
        if (user == null || !user.Identity?.IsAuthenticated == true)
        {
            context.Result = new ObjectResult(new
            {
                statusCode = 401,
                error = "Unauthorized",
                message = "Access denied. Valid session authentication is required to access this endpoint."
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        // Check role restriction if specified
        if (_allowedRoles.Length > 0)
        {
            var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
            if (string.IsNullOrEmpty(userRole) || !_allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
            {
                context.Result = new ObjectResult(new
                {
                    statusCode = 403,
                    error = "Forbidden",
                    message = $"Access denied. Your current role '{userRole ?? "Unknown"}' does not have permission to execute this operation. Allowed roles: {string.Join(", ", _allowedRoles)}."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
