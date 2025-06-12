using Crowbar.Data;
using Crowbar.Models;
using Crowbar.Utils;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Threading;

namespace Crowbar.Actions
{
    public partial class ForumActions
    {
        public bool RemoveThread(ClaimsPrincipal user, Models.Thread thread)
        {
            if (!HasLevelRequired(user, thread))
                return false;

            var comments = _context.Comments.ToList().Where(x => x.For == thread.Id);
            _context.Comments.RemoveRange(comments);
            _context.Threads.Remove(thread);
            _context.SaveChanges();
            return true;
        }

        public bool RemoveComment(ClaimsPrincipal user, Comment comment)
        {
            if (!HasLevelRequired(user, comment))
                return false;

            _context.Comments.Remove(comment);
            _context.SaveChanges();
            return true;
        }

        public bool RemoveCategory(ClaimsPrincipal user, Category category)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.ADMIN))
                return false;

            var threadsInside = _context.Threads.ToList()
                 .Where(x => x.Category == $"{category.Id}")
                 .ToList();
            foreach (var thread in threadsInside)
            {
                var comments = _context.Comments.ToList().Where(x => x.For == thread.Id);
                _context.Comments.RemoveRange(comments);
                _context.Threads.Remove(thread);
            }

            _context.Categories.Remove(category);
            _context.SaveChanges();
            return true;
        }

        public async Task<bool> RemoveUser(ClaimsPrincipal user, string username)
        {

            var userManager = GetUserManager();
            var roleManager = GetRoleManger();
            var targetUser = await userManager.FindByNameAsync(username);
            if (targetUser is null) return false;
            if (!HasLevelRequired(user, targetUser))
                return false;

            var role = await roleManager.FindByNameAsync("admin");
            if (role is null) return false;
            if (_context.UserRoles.ToList().Find(x => x.RoleId == role.Id && x.UserId == targetUser.Id) is not null)
                return false;

            var ownedThreads = _context.Threads.ToList().Where(x => x.Creator == targetUser.UserName);
            foreach (var thread in ownedThreads)
                _context.Threads.Remove(thread);

            var ownedComments = _context.Comments.ToList().Where(x => x.Creator == targetUser.UserName);
            foreach (var comment in ownedComments)
                _context.Comments.Remove(comment);

            userManager.DeleteAsync(targetUser).Wait();
            userManager.UpdateSecurityStampAsync(targetUser).Wait();
            return true;
        }

        /// <summary>
        /// Allows for user deletion without an active session. Use only if absolutely necessary 
        /// and do not allow this to be controller by normal users.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public async Task<bool> SuperRemoveUser(string username)
        {
            var userManager = GetUserManager();
            var targetUser = await userManager.FindByNameAsync(username);
            if (targetUser is null) return false;

            var ownedThreads = _context.Threads.ToList().Where(x => x.Creator == targetUser.UserName);
            foreach (var thread in ownedThreads)
                _context.Threads.Remove(thread);

            var ownedComments = _context.Comments.ToList().Where(x => x.Creator == targetUser.UserName);
            foreach (var comment in ownedComments)
                _context.Comments.Remove(comment);

            userManager.DeleteAsync(targetUser).Wait();
            userManager.UpdateSecurityStampAsync(targetUser).Wait();
            return true;
        }
    }
}
