using Crowbar.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Crowbar.Actions;

namespace Crowbar.Pages.Threads
{
    [Authorize]
    public class NewModel : PageModel
    {
        private readonly ApplicationDbContext Context;
        private readonly ForumActions Actions;

        [BindProperty, Required]
        public InputModel Input { get; set; }
        [BindProperty(SupportsGet =true)]
        public int Category { get; set; }

        public class InputModel
        {
            [Required]
            public required string Title { get; set; }
            [Required]
            public required string Content { get; set; }
            [Required]
            public required int Category { get; set; }
            public IFormFile[]? Attachments { get; set; }
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
            if (!Actions.HasLevelRequired(User, ForumActions.AccessLevelRequired.USER))
                return LocalRedirect("/login");
            var category = Context.Categories.Find(Input.Category);
            if (category is null)
                return BadRequest();
            Console.WriteLine($"Admin only: {category.AdminOnly} has access: {Actions.HasLevelRequired(User, ForumActions.AccessLevelRequired.ADMIN)}");
            if ((category.AdminOnly ?? false) && !Actions.HasLevelRequired(User, ForumActions.AccessLevelRequired.ADMIN))
            {
                ModelState.AddModelError(string.Empty, "this category is admin only");
                return Page();
            }

            List<int> fileIds = new();

            foreach (var attachmentFile in Input.Attachments ?? [])
            {
                var attachmentDataStream = new MemoryStream();
                attachmentFile.CopyTo(attachmentDataStream);
                var fileData = attachmentDataStream.ToArray();
                var fileName = attachmentFile.FileName;
                var fileContentType = attachmentFile.ContentType;
                var fileId = Actions.AddFile(User, new Models.File
                {
                    FileName = fileName,
                    FileContentType = fileContentType,
                    FileData = fileData,
                });
                if (fileId != -1) fileIds.Add(fileId);
            }

            var thread = new Models.Thread
            {
                Category = $"{Input.Category}",
                Published = DateTime.Now,
                Creator = User.Identity.Name,
                Content = Input.Content,
                Title = Input.Title,
                Attachments = fileIds.ToArray(),
            };

            var id = Actions.AddThread(User, thread, ModelState);
            if (id == -1)
                return Page();

            return LocalRedirect($"/Threads/{id}");
        }
    }
}
