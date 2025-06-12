using Crowbar.Encryption;
using Crowbar.Utils;
using System.Security.Claims;
using System.Text;

namespace Crowbar.Actions
{
    public partial class ForumActions
    {
        /// <summary>
        /// Destroys all the data on the forum except the admin users.
        /// </summary>
        /// <param name="user"></param>
        public async void Nuke(ClaimsPrincipal user)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.ADMIN))
                return;

            var count = 0;
            foreach (var targetUser in _context.Users)
            {
                if (targetUser is null || targetUser.UserName is null) continue;
                if (targetUser.UserName == user.Identity.Name) continue;
                await RemoveUser(user, targetUser.UserName);
                count++;
            }

            foreach (var category in _context.Categories)
                RemoveCategory(user, category);

            foreach (var thread in _context.Threads)
                RemoveThread(user, thread);

            foreach (var comment in _context.Comments)
                RemoveComment(user, comment);

            foreach (var settings in _context.SiteSettings)
                _context.SiteSettings.Remove(settings);

            await Forum.SeedForum(_context);
            _context.SaveChanges();
        }
    }
}
