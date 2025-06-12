using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Crowbar.Areas.Identity.Pages.Account.Manage
{
    public class AccessDeniedModel : PageModel
    {
        public IActionResult OnGet()
        {
            return StatusCode(403, "not allowed");
        }
    }
}
