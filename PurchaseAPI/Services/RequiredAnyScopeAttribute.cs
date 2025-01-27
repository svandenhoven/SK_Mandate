using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace PurchaseAPI.Services
{
    public class RequiredAnyScopeAttribute : AuthorizeAttribute, IAuthorizationFilter
    {
        private readonly string[] _requiredScopes;

        public RequiredAnyScopeAttribute(params string[] requiredScopes)
        {
            _requiredScopes = requiredScopes;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            var scopeClaim = user.FindFirst("http://schemas.microsoft.com/identity/claims/scope");

            if (scopeClaim == null || !_requiredScopes.Any(scope => scopeClaim.Value.Split(' ').Contains(scope)))
            {
                context.Result = new Microsoft.AspNetCore.Mvc.ForbidResult();
            }
        }
    }
}