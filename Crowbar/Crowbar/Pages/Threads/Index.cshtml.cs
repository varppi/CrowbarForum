using Crowbar.Data;
using Crowbar.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Crowbar.Pages.Threads
{
    public class IndexModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int CPage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Category { get; set; }
        public void OnGet()
        {
        }
    }
}
