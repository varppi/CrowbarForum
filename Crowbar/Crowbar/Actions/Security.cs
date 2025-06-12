using Crowbar.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Security.Claims;

namespace Crowbar.Actions
{
    public partial class ForumActions
    {
        public enum AccessLevelRequired
        {
            NONE, // <-- Anonymous access
            USER, // <-- User must be authenticated
            SAME_USER, // <-- User must have made the action target
            ADMIN      // <-- User must be the administrator of the site
        }

        /// <summary>
        /// Checks if the user passed in as the first parameter has the level of access required
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="user">User that is doing the action</param>
        /// <param name="targetObject">Target of the action (optional)</param>
        /// <returns>'True' for "user has access to this" and 'False' for "no, the user does not meet the access level requirements"</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public bool HasLevelRequired<T>(ClaimsPrincipal user, T? targetObject=default) where T : class
        {
            if (user is null || user.Identity is null)
                throw new ArgumentNullException(nameof(user));
            
            // Level USER and ADMIN
            if (!IsValidSession(user).GetAwaiter().GetResult()) return false;
            if (!user.Identity.IsAuthenticated) return false;
            if (user.IsInRole("admin"))
                return true;

            // Level SAME_USER
            if (targetObject is null)
                throw new ArgumentNullException(nameof(targetObject));

            string? targetUsername = targetObject.GetType().Name switch
            {
                "Thread" => ((Models.Thread)Convert.ChangeType(targetObject, typeof(Models.Thread))).Creator,
                "Comment" => ((Models.Comment)Convert.ChangeType(targetObject, typeof(Models.Comment))).Creator,
                "ClaimsPrincipal" => ((ClaimsPrincipal)Convert.ChangeType(targetObject, typeof(ClaimsPrincipal))).Identity?.Name ?? "",
                "CrowbarUser" => ((CrowbarUser)Convert.ChangeType(targetObject, typeof(CrowbarUser))).UserName ?? "",
                _ => throw new ArgumentOutOfRangeException(nameof(targetObject)),
            };

            return targetUsername == user.Identity.Name;
        }

        /// <summary>
        /// Checks if the user passed in as the first parameter has the level of access required
        /// </summary>
        /// <param name="user">User that is doing the action</param>
        /// <param name="accessLevel">Access level required to do the action</param>
        /// <returns>'True' for "user has access to this" and 'False' for "no, the user does not meet the access level requirements"</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool HasLevelRequired(ClaimsPrincipal user, AccessLevelRequired accessLevel)
        {
            if (user.Identity is null)
                throw new ArgumentNullException(nameof(user));

            if (!IsValidSession(user).GetAwaiter().GetResult()) return false;

            // Level ADMIN and NONE
            if (user.IsInRole("admin") || accessLevel == AccessLevelRequired.NONE)
                return true;

            // Level USER
            return user.Identity.IsAuthenticated;
        }
    }
}
