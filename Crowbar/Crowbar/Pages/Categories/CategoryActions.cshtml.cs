using Crowbar.Actions;
using Crowbar.Data;
using Crowbar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading;

namespace Crowbar.Pages.Categories
{
    public class CategoryActionsModel : PageModel
    {
        private readonly ApplicationDbContext Context;
        private readonly ForumActions Actions;


        // Category ID
        [BindProperty(SupportsGet = true)]
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                if (value < 0) _id = 0;
            }
        }
        private int _id;

        // Modify category
        [BindProperty]
        public InputModifyModel InputModify { get; set; }

        public class InputModifyModel
        {
            [Required]
            public string Name { get; set; }
            [Required]
            public string Description { get; set; }
            [Required]
            public bool AdminOnly { get; set; }
        }

        // Misc
        [BindProperty]
        public string Action { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? ConfirmAction { get; set; }

        public CategoryActionsModel(ApplicationDbContext context, ForumActions actions)
        {
            Context = context;
            Actions = actions;
        }

        public IActionResult OnGet()
        {
            return LocalRedirect($"/Threads?Category={Id}");
        }

        public IActionResult OnPost()
        {
            if (!User.IsInRole("admin")) return Unauthorized();
            var category = Context.Categories.Find(Id);
            if (category is null)
                return NotFound();

            bool success;
            switch (Action)
            {
                case "delete":
                    if (ConfirmAction != "confirm")
                        return LocalRedirect($"/Categories?ConfirmAction=require&ConfirmFor={category.Id}");
                    success = Actions.RemoveCategory(User, category);
                    if (!success)
                        return StatusCode(403, "not allowed");
                    return LocalRedirect($"/Categories");
                case "modify":
                    if (ModelState.IsValid)
                    {
                        success = Actions.EditCategory(User, category,
                            new Models.Category
                            {
                                Name = InputModify.Name,
                                Description = InputModify.Description,
                                AdminOnly = InputModify.AdminOnly,
                            }, ModelState);
                        if (!success) return Page();
                        else return LocalRedirect($"/Categories");
                    }
                    InputModify.Name = category.Name;
                    InputModify.Description = category.Description;
                    InputModify.AdminOnly = category.AdminOnly ?? false;
                    return Page();
            }

            return BadRequest();
        }
    }
}
