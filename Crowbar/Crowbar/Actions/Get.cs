using Crowbar.Data;
using Crowbar.Models;
using System.Security.Claims;
using Crowbar.Actions;
using System.Threading;
using Crowbar.Utils;

namespace Crowbar.Actions
{
    public partial class ForumActions
    {

        public async Task<string[]> GetInviteCodes(ClaimsPrincipal user)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.USER))
                return [];
            var userManager = GetUserManager();

            var crowbarUser = await userManager.FindByNameAsync(user.Identity.Name);
            if (crowbarUser is null || crowbarUser.InviteCodes is null) return [];
            return crowbarUser.InviteCodes;
        }

        public async Task<string[]> GetInviteCodes()
        {
            List<string> codes = new();
            foreach (var user in _context.Users)
                codes.AddRange(user.InviteCodes);
            return codes.ToArray();
        }

        /// <summary>
        /// Checks if a user has liked a specific thread.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="thread"></param>
        /// <returns>'True' for "The user has indeed liked the thread" and 'False' for "The user hasn't liked the thread"</returns>
        public bool HasLikedOrDisliked(ClaimsPrincipal user, Models.Thread thread)
            => thread.Likes.ToList().Contains(user.Identity.Name) 
            || thread.Dislikes.ToList().Contains(user.Identity.Name);
        
        /// <summary></summary>
        /// <param name="user"></param>
        /// <param name="username"></param>
        /// <param name="_context"></param>
        /// <returns>
        /// All the roles the specified username has.
        /// </returns>
        public string[] GetRoles(ClaimsPrincipal user, string username)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.USER))
                return [];

            var roles = _context.Roles.ToList();
            var targetUser = _context.Users.ToList().Find(x => x.UserName == username);
            var userRoles = _context.UserRoles.ToList().Where(x => x.UserId == targetUser.Id);
            List<string> outputRoles = new();
            foreach (var role in roles)
            {
                var roleObj = roles.Find(x => x.Id == role.Id);
                if (userRoles.ToList().Any(x => x.RoleId == roleObj.Id))
                    outputRoles.Add(roleObj.Name);
            }
            return outputRoles.ToArray();
        }

        /// <summary></summary>
        /// <param name="user"></param>
        /// <param name="username"></param>
        /// <param name="_context"></param>
        /// <returns>
        /// All the threads tied to the specified username.
        /// </returns>
        public Models.Thread[] GetThreads(ClaimsPrincipal user, string username)
            => _context.Threads.ToList().Where(x => x.Creator == username).ToArray();

        /// <summary></summary>
        /// <param name="user"></param>
        /// <param name="username"></param>
        /// <param name="_context"></param>
        /// <returns>
        /// All the comments tied to the specified username.
        /// </returns>
        public Models.Comment[] GetComments(ClaimsPrincipal user, string username)
            => _context.Comments.ToList().Where(x => x.Creator == username).ToArray();

        /// <summary>Gets the site's settings. If there isn't one, it creates ones with default values.</summary>
        /// <param name="_context"></param>
        /// <returns>
        /// Site settings defined in the admin panel.
        /// </returns>
        public SiteSettings? GetSiteSettings()
        {
            if (!_context.SiteSettings.Any())
                Forum.SeedForum(_context).Wait();
            return _context.SiteSettings.First();
        }
    }
}
