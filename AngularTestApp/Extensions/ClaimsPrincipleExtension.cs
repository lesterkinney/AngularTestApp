using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace API.Extensions
{
    public static class ClaimsPrincipalExtension
    {
        public static string GetUserName(this ClaimsPrincipal user)
        {
            var username = user.FindFirst(ClaimTypes.Name)?.Value;
            return username;
        }

        public static int GetUserId(this ClaimsPrincipal user)
        {

            return int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }
    }
}
