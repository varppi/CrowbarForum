using Crowbar.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;

namespace Crowbar.Pages.Profiles
{
    public class ProfilePictureModel : PageModel
    {
        private readonly ApplicationDbContext Context;

        [BindProperty(SupportsGet =true)]
        public string Name { get; set; }

        public ProfilePictureModel(ApplicationDbContext context)
        {
            Context = context;
        }

        public IActionResult OnGet()
        {
            var user = Context.Users.ToList().Find(x => x.UserName == Name);
            if (user is null)
                return NotFound();

            var profilePic = System.IO.File.ReadAllBytes($"Assets/default-pfp.png");
            

            if (user.ProfilePicture is not null)
                profilePic = user.ProfilePicture;
            return File(profilePic, "image/png");
        }
    }
}
