using Crowbar.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Crowbar.Actions;

namespace Crowbar.Pages.Categories
{
    [Authorize(Roles ="admin")]
    public class NewModel : PageModel
    {
        public readonly ApplicationDbContext Context;
        private readonly ForumActions Actions;


        [BindProperty, Required]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            public string Name { get; set; }
            [Required]
            public string Description { get; set; }
        }

        public NewModel(ApplicationDbContext context, ForumActions actions)
        {
            Context = context;
            Actions = actions;
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            var category = new Models.Category
            {
                Name = Input.Name,
                Description = Input.Description,
            };

            var id = Actions.AddCategory(User, category, ModelState);
            if (id == -1)
                return Page();

            return LocalRedirect($"/Categories/{id}");
        }
    }
}
