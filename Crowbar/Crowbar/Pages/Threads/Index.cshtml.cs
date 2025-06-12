using Crowbar.Actions;
using Crowbar.Data;
using Crowbar.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Crowbar.Pages.Threads
{
    public class IndexModel : PageModel
    {
        private readonly ForumActions Actions;

        [BindProperty(SupportsGet = true)]
        public int CPage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Category { get; set; }

        public IndexModel(ForumActions actions)
        {
            Actions = actions;
        }
        public IActionResult OnGet()
        {
            if (Actions.GetSiteSettings().HideThreadsFromNonMembers && !User.Identity.IsAuthenticated)
                return LocalRedirect("/login");
            return Page();
        }
    }
}
