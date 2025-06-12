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
            public required string Category { get; set; }
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
                Category = Input.Category,
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
