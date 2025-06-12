using Crowbar.Actions;
using Crowbar.Data;
using Crowbar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Threading;

namespace Crowbar.Pages.Threads
{
    public class ViewThreadModel : PageModel
    {
        private readonly ApplicationDbContext Context;
        private readonly UserManager<CrowbarUser> UserManager;
        private readonly ForumActions Actions;

        // Thread ID
        [BindProperty(SupportsGet =true)]
        public int Id { get => _id; 
            set {
                _id = value;
                if (value < 0) _id = 0;
            } 
        }
        private int _id;


        // Comment
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            public string Comment { get; set; }
        }

        [BindProperty]
        public InputModelModify InputModify { get; set; }


        // Modify thread
        public class InputModelModify
        {
            [Required]
            public string Title { get; set; }
            [Required]
            public string Content { get; set; }
            public IFormFile[]? Attachments { get; set; }
            public int RemoveAttachment { get; set; } = -1;
        }
        
        // Misc
        [BindProperty]
        public string? Action { get; set; }
        [BindProperty]
        public int AttachmentId { get; set; } = -1;

        [BindProperty(SupportsGet = true)]
        public int CPage { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int ReplyTo { get; set; } = -1;

        [BindProperty(SupportsGet = true)]
        public string? ConfirmAction { get; set; }

        public ViewThreadModel(ApplicationDbContext context, 
                            UserManager<CrowbarUser> userManager, 
                            ForumActions actions)
        {
            Context = context;
            UserManager = userManager;
            Actions = actions;
        }

        public IActionResult OnGet()
        {
            if (Actions.GetSiteSettings().HideThreadsFromNonMembers && !User.Identity.IsAuthenticated)
                return LocalRedirect("/login");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            string[] anonActions = ["download_attachment"];
            if (!anonActions.Contains(Action ?? "") && !Actions.HasLevelRequired(User, ForumActions.AccessLevelRequired.USER))
                return Unauthorized();
            var thread = Context.Threads.Find(Id);
            if (thread is null)
                return NotFound();

            if (Action is null)
            {
                var comment = new Comment
                {
                    ReplyTo = ReplyTo,
                    Content = Input.Comment,
                    Creator = User.Identity.Name,
                    For = Id,
                    Published = DateTime.Now,
                };
                var commentId = Actions.AddComment(User, comment, ModelState);
                if (commentId == -1)
                    return Page();
                return RedirectToPage();
            }

            switch (Action)
            {
                case "remove_like_dislike":
                    if (!User.Identity.IsAuthenticated)
                        return LocalRedirect("/login");
                    await Actions.LikeDislikeRemove(User, thread);
                    return RedirectToPage();
                case "dislike":
                    if (!User.Identity.IsAuthenticated)
                        return LocalRedirect("/login");
                    await Actions.DislikeThread(User, thread);
                    return RedirectToPage();
                case "like":
                    if (!User.Identity.IsAuthenticated)
                        return LocalRedirect("/login");
                    await Actions.LikeThread(User, thread);
                    return RedirectToPage();
                case "download_attachment":
                    if (thread.Attachments.IsNullOrEmpty())
                        return NotFound();
                    if (AttachmentId == -1) 
                        return NotFound();
                    if (Actions.GetSiteSettings().DisableAnonDownloads && !User.Identity.IsAuthenticated)
                        return LocalRedirect("/login");
                    var attachment = Context.Files.Find(AttachmentId);
                    if (!thread.Attachments.Contains(attachment.Id)) return NotFound();
                    Response.Headers.Add(new("Content-Disposition", $"attachment; filename={attachment.FileName}"));
                    return File(attachment.FileData, "application/force-download");
                case "delete":
                    if (!User.Identity.IsAuthenticated)
                        return LocalRedirect("/login");
                    if (thread is null)
                        return NotFound();
                    if (ConfirmAction != "confirm")
                        return LocalRedirect($"/Threads/{thread.Id}?ConfirmAction=require");
                    var success = Actions.RemoveThread(User, thread);
                    if (!success)
                        return BadRequest();
                    return LocalRedirect($"/Threads");
                case "modify":
                    if (!User.Identity.IsAuthenticated)
                        return LocalRedirect("/login");

                    if (InputModify.Content is null || InputModify.Title is null)
                    {
                        InputModify.Content = thread.Content;
                        InputModify.Title = thread.Title;
                        return Page();
                    }

                    List<int> fileIds = thread.Attachments.ToList();

                    foreach(var attachmentFile in InputModify.Attachments ?? [])
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
                    if (InputModify.RemoveAttachment != -1)
                        fileIds = fileIds.ToList().Where(x => x != InputModify.RemoveAttachment).ToList();

                    var updatedThread = new Models.Thread
                    {
                        Creator = thread.Creator,
                        Category = thread.Category,
                        Content = InputModify.Content,
                        Title = InputModify.Title,
                        Attachments = fileIds.ToArray(),
                    };
                    var threadId = Actions.EditThread(User, thread, updatedThread, ModelState);
                    if (threadId == -1 || InputModify.RemoveAttachment != -1)
                        return Page();
                    return LocalRedirect($"/Threads/{threadId}");
                default:
                    return BadRequest();
            }
        }
    }
}
