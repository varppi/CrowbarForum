using Crowbar.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Crowbar.Pages.Categories
{
    public class IndexModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int CPage { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? ConfirmAction { get; set; }
        [BindProperty(SupportsGet = true)]
        public int ConfirmFor { get; set; }
        public void OnGet()
        {
        }
    }
}
