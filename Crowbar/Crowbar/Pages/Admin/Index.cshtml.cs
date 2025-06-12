using Crowbar.Actions;
using Crowbar.Data;
using Crowbar.Models;
using Crowbar.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace Crowbar.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<CrowbarUser> UserManager;
        private readonly IUserStore<CrowbarUser> UserStore;
        private readonly ApplicationDbContext Context;
        private readonly ForumActions Actions;

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name ="password")]
            public string Password { get; set; }
            [Required]
            [DataType(DataType.Password)]
            public string Password2 { get; set; }
        }

        public IndexModel(UserManager<CrowbarUser> userManager, 
                        IUserStore<CrowbarUser> userStore, 
                        ApplicationDbContext context, 
                        ForumActions actions) {
            UserManager = userManager;
            UserStore = userStore;
            Context = context;
            Actions = actions;
        }
        public IActionResult OnGet()
        {
            if (User.IsInRole("admin"))
                return LocalRedirect("/Admin/Dashboard");
            if (UserManager.HasAdmin())
                return LocalRedirect("/login");
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Forum.HasAdmin(UserManager))
                return LocalRedirect("/login");

            if (Input.Password != Input.Password2)
            {
                ModelState.AddModelError("Input.Password", "Passwords don't match");
                ModelState.AddModelError("Input.Password2", "Passwords don't match");
                return Page();
            }

            var adminUser = Activator.CreateInstance<CrowbarUser>();
            if (await UserManager.FindByNameAsync("admin") is not null)
            {
                var success = await Actions.SuperRemoveUser("admin");
                if (!success)
                {
                    ModelState.AddModelError(string.Empty, "Ran into an error trying to delete the existing admin user.");
                    return Page();
                }
                ModelState.AddModelError(string.Empty, "The user admin already existed, but it's deleted now. Try again.");
                return Page();
            }
            await UserStore.SetUserNameAsync(adminUser, "admin", CancellationToken.None);
            var result = await UserManager.CreateAsync(adminUser, Input.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }
            await UserManager.AddToRoleAsync(adminUser, "admin");
            return LocalRedirect("/login");
        }
    }
}
