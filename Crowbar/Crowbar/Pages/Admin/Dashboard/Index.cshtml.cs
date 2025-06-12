using Crowbar.Actions;
using Crowbar.Data;
using Crowbar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Crowbar.Pages.Admin.Dashboard
{
    [Authorize(Roles = "admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext Context;
        private readonly UserManager<CrowbarUser> UserManager;
        private readonly ForumActions Actions;

        [BindProperty]
        public string? Action { get; set; }
        [BindProperty]
        public string? Confirmation { get; set; }
        [BindProperty(SupportsGet =true)]
        public int CPage { get; set; }
        [BindProperty]
        public string Target { get; set; }

        [BindProperty]
        public InputModifyModel InputModify { get; set; }

        public class InputModifyModel
        {
            [Required]
            public string Username { get; set; }
            public string? Password { get; set; }
            [Required]
            public string Role { get; set; }
        }

        [BindProperty]
        public InputModifySiteModel InputModifySite { get; set; }

        public class InputModifySiteModel
        {
            [Required]
            public string? EnableRegistration { get; set; }
            [Required]
            public string? EnableLoginCaptcha { get; set; }
            [Required]
            public string? EnableRegistrationCaptcha { get; set; }
            [Required]
            public string? FrontPageHtml { get; set; }
            [Required]
            public string? GlobalCss { get; set; }
            [Required]
            public string? ForumName { get; set; }
            [Required]
            public string? Theme { get; set; }
            [Required]
            public int ThreadLimit { get; set; }
            [Required]
            public int CommentLimit { get; set; }
            [Required]
            public int ThreadEditLimit { get; set; }
            [Required]
            public int CommentEditLimit { get; set; }
            [Required]
            public int ProfileChangeLimit { get; set; }
            [Required]
            public int AttachmentLimit { get; set; }

        }

        public IndexModel(ApplicationDbContext context, UserManager<CrowbarUser> userManager, ForumActions actions) { 
            Context = context;
            UserManager = userManager;
            Actions = actions;
        }

        public void OnGet()
        {
            var siteSettings = Actions.GetSiteSettings();
            InputModifySite = new();
            InputModifySite.EnableRegistration = siteSettings.EnableRegistration.ToString().ToLower();
            InputModifySite.EnableLoginCaptcha = siteSettings.EnableLoginCaptcha.ToString().ToLower();
            InputModifySite.EnableRegistrationCaptcha = siteSettings.EnableRegistrationCaptcha.ToString().ToLower();

            InputModifySite.FrontPageHtml = siteSettings.FrontPageHtml;
            InputModifySite.GlobalCss = siteSettings.GlobalCss;
            InputModifySite.ForumName = siteSettings.ForumName;
            InputModifySite.Theme = siteSettings.Theme;
            
            InputModifySite.ThreadLimit = siteSettings.ThreadLimit;
            InputModifySite.ThreadEditLimit = siteSettings.ThreadEditLimit;
            InputModifySite.CommentLimit = siteSettings.CommentLimit;
            InputModifySite.ThreadEditLimit = siteSettings.ThreadEditLimit;
            InputModifySite.CommentEditLimit = siteSettings.CommentEditLimit;
            InputModifySite.ProfileChangeLimit = siteSettings.ProfileChangeLimit;
            InputModifySite.AttachmentLimit = siteSettings.AttachmentLimit;
        }

        public async Task<IActionResult> OnPostAsync() {
            bool success;
            switch (Action ?? "") {
                case "nuke":
                    if ((Confirmation ?? "").ToLower() != "i want to nuke the forum")
                    {
                        ModelState.AddModelError("Confirmation", "invalid confirmation");
                        return Page();
                    }
                    Actions.Nuke(User);
                    return RedirectToPage();
                case "modify_site":
                    success = Actions.EditSiteSettings(User, 
                        new SiteSettings { 
                            EnableRegistration= InputModifySite.EnableRegistration == "true",
                            EnableLoginCaptcha= InputModifySite.EnableLoginCaptcha == "true",
                            EnableRegistrationCaptcha= InputModifySite.EnableRegistrationCaptcha == "true",
                            FrontPageHtml = InputModifySite.FrontPageHtml ?? string.Empty,
                            GlobalCss = InputModifySite.GlobalCss ?? string.Empty,
                            ForumName = InputModifySite.ForumName ?? string.Empty,
                            Theme = InputModifySite.Theme ?? "dark",
                            ThreadLimit=InputModifySite.ThreadLimit < 0 ? 0 : InputModifySite.ThreadLimit,
                            CommentLimit= InputModifySite.CommentLimit < 0 ? 0 : InputModifySite.CommentLimit,
                            ThreadEditLimit= InputModifySite.ThreadEditLimit < 0 ? 0 : InputModifySite.ThreadEditLimit,
                            CommentEditLimit= InputModifySite.CommentEditLimit < 0 ? 0 : InputModifySite.CommentEditLimit,
                            ProfileChangeLimit= InputModifySite.ProfileChangeLimit < 0 ? 0 : InputModifySite.ProfileChangeLimit,
                            AttachmentLimit = InputModifySite.AttachmentLimit < 0 ? 0 : InputModifySite.AttachmentLimit,
                        }, ModelState);
                    if (!success) return Page();
                    return RedirectToPage();
                case "modify_user":
                    var user = await UserManager.FindByNameAsync(Target);
                    if (user is null)
                        return NotFound();
                    if (InputModify.Username is null)
                    {
                        InputModify.Username = user.UserName;
                        Target = user.UserName;
                        return Page();
                    }
                    var updatedUser = new CrowbarUser
                    {
                        UserName = InputModify.Username,
                        ProfilePicture = user.ProfilePicture,
                        Description = user.Description,
                    };
                    success = await Actions.EditUser(User, user, updatedUser, InputModify.Password, InputModify.Role, ModelState);
                    if (!success) return Page();
                    return RedirectToPage();
                case "delete_user":
                    if (Target is null)
                        return BadRequest();
                    success = await Actions.RemoveUser(User, Target);
                    if (!success)
                        return StatusCode((int)HttpStatusCode.InternalServerError, "Something went wrong! Make sure you're not trying to delete another admin account!");
                    return RedirectToPage();
            }
            return RedirectToPage();
        }
    }
}
