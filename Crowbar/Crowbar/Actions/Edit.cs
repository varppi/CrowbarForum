using Crowbar.Data;
using Crowbar.Models;
using Crowbar.Pages.utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Crowbar.Actions
{
    public partial class ForumActions
    {
        public async Task LikeDislikeRemove(ClaimsPrincipal user, Models.Thread thread)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.USER))
                return;

            var userManager = GetUserManager();
            if (!thread.Likes.Contains(user.Identity.Name) 
                && !thread.Dislikes.Contains(user.Identity.Name))
                return;
            thread.Likes ??= [];
            thread.Likes = thread.Likes.ToList().Where(x => x != user.Identity.Name).Distinct().ToArray();
            thread.Dislikes ??= [];
            thread.Dislikes = thread.Dislikes.ToList().Where(x => x != user.Identity.Name).Distinct().ToArray();
            _context.SaveChanges();
        }

        public async Task DislikeThread(ClaimsPrincipal user, Models.Thread thread)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.USER))
                return;

            var userManager = GetUserManager();
            if (thread.Likes.Contains(user.Identity.Name))
                return;
            if (thread.Dislikes.Contains(user.Identity.Name))
                return;
            thread.Dislikes ??= [];
            thread.Dislikes = thread.Dislikes.Append(user.Identity.Name).Distinct().ToArray();
            thread.Dislikes = thread.Dislikes.ToList().Where(x => userManager.FindByNameAsync(x).GetAwaiter().GetResult() is not null).ToArray();
            _context.SaveChanges();
        }

        public async Task LikeThread(ClaimsPrincipal user, Models.Thread thread)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.USER))
                return;
            var userManager = GetUserManager();
            if (thread.Likes.Contains(user.Identity.Name))
                return;
            if (thread.Dislikes.Contains(user.Identity.Name))
                return;
            thread.Likes ??= [];
            thread.Likes = thread.Likes.Append(user.Identity.Name).Distinct().ToArray();
            thread.Likes = thread.Likes.ToList().Where(x => userManager.FindByNameAsync(x).GetAwaiter().GetResult() is not null).ToArray();
            _context.SaveChanges();
        }

        public int EditThread(ClaimsPrincipal user, Models.Thread thread, Models.Thread updatedThread, ModelStateDictionary modelState)
        {
            if (!HasLevelRequired(user, thread))
                return -1;

            var actionsLeft = LimitCheck(user, "ThreadEditLimit");
            if (actionsLeft < 0 && !user.IsInRole("admin"))
            {
                modelState.AddModelError(string.Empty, "thread editing rate limited");
                return -1;
            };

            var isValid = IsValidModel(obj: updatedThread,
                required: ["Attachments"],
                alphNumOnly: ["Title", "Category", "Creator"],
                under255: ["Title", "Category", "Creator"],
                modelName: "InputModify",
                modelState: modelState);
            if (!isValid)
                return -1;

            if (updatedThread.Content.Length > 10000)
            {
                modelState.AddModelError("InputModify.Content", "body is too big");
                return -1;
            }
            if (updatedThread.Attachments.Length > GetSiteSettings().AttachmentLimit)
            {
                modelState.AddModelError("InputModify.Attachments", "too many attachments");
                return -1;
            }
            thread.Title = updatedThread.Title;
            thread.Content = updatedThread.Content;
            thread.Category = updatedThread.Category;
            thread.Attachments = updatedThread.Attachments;
            _context.SaveChanges();
            return thread.Id;
        }

        public bool EditComment(ClaimsPrincipal user, Comment comment, string newContent, ModelStateDictionary modelState)
        {
            if (!HasLevelRequired(user, comment))
                return false;

            var actionsLeft = LimitCheck(user, "CommentEditLimit");
            if (actionsLeft < 0 && !user.IsInRole("admin"))
            {
                modelState.AddModelError(string.Empty, "comment editing rate limited or disabled");
                return false;
            };

            if (comment.Content is null) return false;

            if (newContent.Length > 1000)
            {
                modelState.AddModelError("Input.Content", "comment is too long");
                return false;
            }

            var isValid = IsValidModel(obj: comment,
                required: ["Content"],
                alphNumOnly: ["Creator"],
                under255: ["Creator"],
                modelName: "Input",
                modelState: modelState);
            if (!isValid)
                return false;

            comment.Content = newContent;
            _context.SaveChanges();
            return true;
        }

        public bool EditCategory(ClaimsPrincipal user, Category category, Category updatedCategory, ModelStateDictionary modelState)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.ADMIN))
                return false;

            var isValid = IsValidModel(obj: updatedCategory,
                alphNumOnly: ["Name", "Description"],
                under255: ["Name", "Description"],
                modelName: "InputModify",
                modelState: modelState);
            if (!isValid)
                return false;
            if (updatedCategory.Name.Length > 50)
            {
                modelState.AddModelError("InputModify.Name", "name is too long");
                return false;
            }
            if (updatedCategory.Description.Length > 200)
            {
                modelState.AddModelError("InputModify.Description", "description is too long");
                return false;
            }

            category.Name = updatedCategory.Name;
            category.Description = updatedCategory.Description;
            _context.SaveChanges();
            return true;
        }

        public async Task<bool> EditUser(ClaimsPrincipal user, CrowbarUser target, CrowbarUser updatedUser, string password, string role, ModelStateDictionary modelState)
        {
            if (!HasLevelRequired(user, target))            
                return false;
            
            var roleManager = GetRoleManger();
            var userManager = GetUserManager();

            var username = updatedUser.UserName.ToLower().Trim();
            var userEditing = await userManager.FindByNameAsync(user.Identity.Name);
            updatedUser.Description ??= "";
            
            var targetRole = await roleManager.FindByNameAsync("admin");
            var targetIsAdmin = _context.UserRoles.ToList().Find(x => x.RoleId == targetRole.Id && x.UserId == target.Id) is not null;
            var userEditingIsAdmin = _context.UserRoles.ToList().Find(x => x.RoleId == targetRole.Id && x.UserId == userEditing.Id) is not null;
            if (role is null) return false;
            if (targetIsAdmin && target.UserName != user.Identity.Name && user.Identity.Name != "admin")
            {
                modelState.AddModelError(string.Empty, "cannot change properties of another admin");
                return false;
            }
            
            var actionsLeft = LimitCheck(user, "ProfileChangeLimit");
            if (actionsLeft < 0 && !user.IsInRole("admin"))
            {
                modelState.AddModelError(string.Empty, "profile changes rate limited or disabled");
                return false;
            };

            if (userEditingIsAdmin && user.Identity.Name != "admin" && role == "admin")
            {
                modelState.AddModelError("InputModify.Role", "only the 'admin' user can promote other users to admins");
                return false;
            }

            if ((updatedUser.UserName != "admin" && target.UserName == "admin") || ((role != "" && role != "admin") && target.UserName == "admin"))
            {
                modelState.AddModelError("InputModify.Username", "there has to be at least one user with the name and role of admin");
                return false;
            }
            
            if ((await userManager.FindByNameAsync(updatedUser.UserName)) is not null && (target.UserName != updatedUser.UserName))
            {
                modelState.AddModelError("InputModify.Username", "somebody already has that name");
                return false;
            }

            var isValid = IsValidModel(obj: updatedUser,
                required: ["UserName"],
                alphNumOnly: ["UserName"],
                under255: ["UserName"],
                modelName: "InputModify",
                modelState: modelState);
            if (!isValid)
                return false;

            if (!Regex.IsMatch(username, @"^[a-zA-Z0-9_.]{3,30}$"))
            {
                modelState.AddModelError("InputModify.Username", "username must be ascii only, at least 3 characters long and 30 at maximum");
                return false;
            }

            if (updatedUser.Description.Length > 2500)
            {
                modelState.AddModelError("InputModify.Description", "description is too long");
                return false;
            }

            if (role != "user" && !role.IsNullOrEmpty())
            {
                if (!Regex.IsMatch(role, AsciiOnly))
                {
                    modelState.AddModelError("InputModify.Role", "role name must be ascii only, at least 3 characters long and 30 at maximum");
                    return false;
                }
                var roleObj = _context.Roles.ToList().Find(x => x.Name == role);
                if (roleObj is null)
                {
                    modelState.AddModelError("InputModify.Role", "role doesn't exist");
                    return false;
                }
                await userManager.AddToRoleAsync(target, roleObj.Name);
            }
            else if (role == "user")
            {
                var roles = _context.UserRoles.ToList().Where(x => x.UserId == target.Id);
                foreach (var roleObj in roles)
                {
                    var roleName = _context.Roles.ToList().Find(x => x.Id == roleObj.RoleId);
                    await userManager.RemoveFromRoleAsync(target, roleName.Name);
                }
            }

            var oldUsername = target.UserName;
            target.ProfilePicture = updatedUser.ProfilePicture;
            target.UserName = username;
            target.Description = updatedUser.Description;
            if (!password.IsNullOrEmpty())
            {
                string resetToken = await userManager.GeneratePasswordResetTokenAsync(target);
                var result = userManager.ResetPasswordAsync(target, resetToken, password);
                if (!result.Result.Succeeded) return false;
            }

            var ownedThreads = _context.Threads.ToList().Where(x => x.Creator == oldUsername);
            foreach (var thread in ownedThreads)
                thread.Creator = username;

            var ownedComments = _context.Comments.ToList().Where(x => x.Creator == oldUsername);
            foreach (var comment in ownedComments)
                comment.Creator = username;

            userManager.UpdateSecurityStampAsync(target).Wait();
            _context.SaveChanges();
            return true;
        }

        public bool EditSiteSettings(ClaimsPrincipal user, SiteSettings newSettings, ModelStateDictionary modelState)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.ADMIN))
                return false;

            var isValid = IsValidModel(obj: newSettings,
                required: [
                    "EnableRegistration",
                    "EnableLoginCaptcha",
                    "EnableRegistrationCaptcha",
                    "FrontPageHtml",
                    "GlobalCss",
                    "ThreadLimit",
                    "CommentLimit",
                    "CommentEditLimit",
                    "ThreadEditLimit",
                    "ProfileChangeLimit",
                    "AttachmentLimit",
                    "HideThreadsFromNonMembers",
                    "DisableAnonDownloads"
                ],
                alphNumOnly: ["ForumName", "Theme"],
                under255: ["ForumName", "Theme"],
                modelName: "InputModifySite",
                modelState: modelState);
            if (!isValid)
                return false;

            if (!_context.SiteSettings.Any())
            {
                _context.SiteSettings.Add(new SiteSettings
                {
                    EnableRegistration = newSettings.EnableRegistration,
                    EnableLoginCaptcha = newSettings.EnableLoginCaptcha,
                    EnableRegistrationCaptcha = newSettings.EnableRegistrationCaptcha,
                    FrontPageHtml = newSettings.FrontPageHtml,
                    GlobalCss = newSettings.GlobalCss,
                    ForumName = newSettings.ForumName,
                    Theme = newSettings.Theme == "dark" ? "dark" : "light",
                    ThreadLimit = newSettings.ThreadLimit,
                    CommentLimit = newSettings.CommentLimit,
                    ThreadEditLimit = newSettings.ThreadEditLimit,
                    CommentEditLimit = newSettings.CommentEditLimit,
                    ProfileChangeLimit = newSettings.ProfileChangeLimit,
                    AttachmentLimit = newSettings.AttachmentLimit,
                    HideThreadsFromNonMembers = newSettings.HideThreadsFromNonMembers,
                    DisableAnonDownloads = newSettings.DisableAnonDownloads,
                });
                _context.SaveChanges();
                return true;
            }

            newSettings.FrontPageHtml = new Regex(@"<script>|</script>", RegexOptions.IgnoreCase).Replace(newSettings.FrontPageHtml, "");
            newSettings.GlobalCss = new Regex(@"</|style|<[a-z0-9]+>", RegexOptions.IgnoreCase).Replace(newSettings.GlobalCss, "");

            var settings = _context.SiteSettings.First();
            settings.EnableRegistration = newSettings.EnableRegistration;
            settings.EnableLoginCaptcha = newSettings.EnableLoginCaptcha;
            settings.EnableRegistrationCaptcha = newSettings.EnableRegistrationCaptcha;
            settings.FrontPageHtml = newSettings.FrontPageHtml;
            settings.GlobalCss = newSettings.GlobalCss;
            settings.ForumName = newSettings.ForumName;
            settings.Theme = newSettings.Theme == "dark" ? "dark" : "light";
            settings.ThreadLimit = newSettings.ThreadLimit;
            settings.CommentLimit = newSettings.CommentLimit;
            settings.ThreadEditLimit = newSettings.ThreadEditLimit;
            settings.CommentEditLimit = newSettings.CommentEditLimit;
            settings.ProfileChangeLimit = newSettings.ProfileChangeLimit;
            settings.AttachmentLimit = newSettings.AttachmentLimit;
            settings.HideThreadsFromNonMembers = newSettings.HideThreadsFromNonMembers;
            settings.DisableAnonDownloads = newSettings.DisableAnonDownloads;
            _context.SaveChanges();
            UpdateLimits();
            return true;
        }

    }
}
