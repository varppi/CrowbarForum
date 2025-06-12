using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Crowbar.Pages.utils
{
    public class AccessDeniedReturnModel : PageModel
    {
        public IActionResult OnGet()
            => LocalRedirect("/login");
    }
}
