using Crowbar.Actions;
using Crowbar.Data;
using Crowbar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Crowbar.Pages.Profiles
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext Context;
        private readonly ForumActions Actions;

        [BindProperty(SupportsGet = true)]
        public int CPage { get; set; }

        [BindProperty(SupportsGet =true), Required]
        public string Name { get; set; }
        public CrowbarUser ProfileUser { get; set; }
        public string Role { get; set; }
        public Models.Thread[] Threads { get; set; }
        public Models.Comment[] Comments { get; set; }

        public IndexModel(ApplicationDbContext context, ForumActions actions)
        {
            Context = context;
            Actions = actions;
        }

        public async Task<IActionResult> OnGet()
        {
            ProfileUser = Context.Users.ToList().Find(x => x.UserName == Name);
            if (User is null)
                return NotFound();

            var getRoles = Actions.GetRoles(User, Name);
            Role = getRoles.Any() ? getRoles[0] : "user";
            Threads = Actions.GetThreads(User, Name);
            Comments = Actions.GetComments(User, Name);
            return Page();
        }
    }
}
