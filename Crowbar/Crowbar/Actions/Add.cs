using Crowbar.Data;
using Crowbar.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using Crowbar.Actions;
using System.Threading;
using Crowbar.Encryption;

namespace Crowbar.Actions
{
    public partial class ForumActions
    {
        public async Task<string[]> AddInviteCodes(ClaimsPrincipal user) 
        {
            var inviteSetting = GetSiteSettings().InviteOnly;
            AccessLevelRequired accessLevelRequired = inviteSetting switch {
                "user" => AccessLevelRequired.USER,
                "admin" => AccessLevelRequired.ADMIN,
                _ => AccessLevelRequired.ADMIN
            };
            if (!HasLevelRequired(user, accessLevelRequired))
                return [];

            var inviteCodes = new string[5];
            for (var i = 0; i < 5; i++)
                inviteCodes[i] = EncryptionLayer.RandomText(32);

            var userManager = GetUserManager();
            var crowbarUser = await userManager.FindByNameAsync(user.Identity.Name);
            if (crowbarUser is null) return [];
            var success = await EditUser(user, crowbarUser, new CrowbarUser
            {
                UserName = crowbarUser.UserName,
                Description = crowbarUser.Description,
                ProfilePicture = crowbarUser.ProfilePicture,
                InviteCodes = inviteCodes,
            }, "", "", new());
            if (!success) return [];

            return inviteCodes;
        }

        public int AddFile(ClaimsPrincipal user, Models.File file)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.USER))
                return -1;

            var isValid = IsValidModel(obj: file,
                required: ["FileName", "FileContentType", "FileData"],
                asciiOnly: ["FileName"]
                );
            if (!isValid) return -1;

            if (file.FileName.Length > 255) return -1;
            
            _context.Files.Add(file);
            _context.SaveChanges();
            return file.Id;
        }

        public int AddThread(ClaimsPrincipal user, Models.Thread thread, ModelStateDictionary modelState)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.USER))
                return -1;

            var actionsLeft = LimitCheck(user, "ThreadLimit");
            if (actionsLeft < 0 && !user.IsInRole("admin"))
            {
                modelState.AddModelError(string.Empty, "thread creations are rate limited or disabled");
                return -1;
            };

            var isValid = IsValidModel(obj: thread,
                required: ["Content"],
                alphNumOnly: ["Creator", "Title", "Category"],
                under255: ["Creator", "Title", "Category"],
                modelName: "Input",
                modelState: modelState);
            if (!isValid)
                return -1;

            if (thread.Content.Length > 10000)
            {
                modelState.AddModelError("Input.Content", "body is too big");
                return -1;
            }
            if (thread.Attachments.Length > GetSiteSettings().AttachmentLimit)
            {
                modelState.AddModelError("Input.Attachments", "too many attachments");
                return -1;
            }
            _context.Threads.Add(thread);
            _context.SaveChanges();
            return thread.Id;
        }

        public int AddComment(ClaimsPrincipal user, Comment comment, ModelStateDictionary modelState)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.USER))
                return -1;

            var actionsLeft = LimitCheck(user, "CommentLimit");
            if (actionsLeft < 0 && !user.IsInRole("admin"))
            {
                modelState.AddModelError(string.Empty, "comments are rate limited or disabled");
                return -1;
            };

            if (comment.Content is null) return -1;

            if (comment.Content.Length > 1000)
            {
                modelState.AddModelError("Input.Content", "comment is too long");
                return -1;
            }

            if (comment.ReplyTo != -1)
            {
                var replyTo = _context.Comments.Find(comment.ReplyTo);
                if (replyTo is null || replyTo.For != comment.For)
                {
                    modelState.AddModelError("ReplyTo", "cannot reply to this comment");
                    return -1;
                }
            }

            var isValid = IsValidModel(obj: comment,
                required: ["Content", "ReplyTo"],
                alphNumOnly: ["Creator"],
                under255: ["Creator"],
                modelName: "Input",
                modelState: modelState);
            if (!isValid)
                return -1;

            _context.Comments.Add(comment);
            _context.SaveChanges();
            return comment.Id;
        }

        public int AddCategory(ClaimsPrincipal user, Category category, ModelStateDictionary modelState)
        {
            if (!HasLevelRequired(user, AccessLevelRequired.ADMIN))
                return -1;

            var isValid = IsValidModel(obj: category,
                alphNumOnly: ["Name", "Description"],
                under255: ["Name", "Description"],
                modelName: "Input",
                modelState: modelState);
            if (!isValid) return -1;

            _context.Categories.Add(category);
            _context.SaveChanges();
            return category.Id;
        }
    }
}
