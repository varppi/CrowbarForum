using Crowbar.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Crowbar.Areas.Identity.Pages.Account.Manage
{
    public class InviteCodesModel : PageModel
    {
        private readonly ForumActions Actions;
        public InviteCodesModel(ForumActions actions) 
        { 
            Actions = actions;
        }

        public string[] InviteCodes { get; set; } = [];

        public IActionResult OnGet()
        {
            return NotFound();
        }
        public async Task<IActionResult> OnPostAsync()
        {
            this.InviteCodes = await Actions.GetInviteCodes(User);
            
            if (this.InviteCodes.Length == 0)
            {
                this.InviteCodes = await Actions.AddInviteCodes(User);
                if (this.InviteCodes.Length == 0)
                {
                    ModelState.AddModelError(string.Empty, "Error generating invite codes. You might not have the permissions to do that.");
                    return Page();
                }
            }

            return Page();
        }
    }
}
