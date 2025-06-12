using Crowbar.Data;
using Crowbar.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data;

namespace Crowbar.Utils
{
    public static class Forum
    {
        /// <summary>
        /// Checks if the forum has a single admin user yet.
        /// </summary>
        /// <param name="userManager"></param>
        /// <returns></returns>
        public static bool HasAdmin(this UserManager<CrowbarUser> userManager)
        {
            foreach (var user in userManager.Users.ToList())
            {
                var isAdmin = userManager.IsInRoleAsync(user, "admin")
                    .GetAwaiter().GetResult();
                if (isAdmin)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Initializes roles and settings.
        /// </summary>
        /// <param name="dbcontext"></param>
        /// <returns></returns>
        public async static Task SeedForum(DbContext dbcontext) {

            var roleManager = (RoleManager<IdentityRole>)dbcontext.GetService(typeof(RoleManager<IdentityRole>));
            var context = (ApplicationDbContext)dbcontext.GetService(typeof(ApplicationDbContext));

            string[] roles = ["admin"];
            foreach(var role in roles)
                if (!await roleManager.RoleExistsAsync(role))
                    roleManager.CreateAsync(new IdentityRole(role)).Wait();

            if (!context.SiteSettings.Any())
                context.SiteSettings.Add(new Models.SiteSettings
                {
                    EnableRegistration = false,
                    EnableLoginCaptcha = false,
                    EnableRegistrationCaptcha = false,
                    FrontPageHtml = "<h1>Welcome!</h1>",
                    GlobalCss = "",
                    ForumName = "Crowbar",
                    Theme = "dark",
                    ThreadLimit = 2,
                    CommentLimit = 10,
                    ThreadEditLimit = 120,
                    CommentEditLimit = 120,
                    ProfileChangeLimit = 20,
                    AttachmentLimit = 5,
                    HideThreadsFromNonMembers = false,
                    DisableAnonDownloads = true,
                });
            context.SaveChanges();
        }
    }
}
