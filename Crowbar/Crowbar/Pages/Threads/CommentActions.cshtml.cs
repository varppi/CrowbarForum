using Crowbar.Actions;
using Crowbar.Data;
using Crowbar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Crowbar.Pages.Threads
{
    [Authorize]
    public class CommentActionsModel : PageModel
    {
        private readonly ApplicationDbContext Context;
        private readonly ForumActions Actions;

        [BindProperty]
        public int ThreadId
        {
            get => _threadId;
            set
            {
                _threadId = value;
                if (value < 0) _threadId = 0;
            }
        }
        private int _threadId;

        [BindProperty(SupportsGet = true)]
        public int CommentId
        {
            get => _commentId;
            set
            {
                _commentId = value;
                if (value < 0) _commentId = 0;
            }
        }
        private int _commentId;

        [BindProperty, Required]
        public string Action { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            public string Content { get; set; }
        }

        public CommentActionsModel(ApplicationDbContext context, ForumActions actions) { 
            Context = context;
            Actions = actions;
        }
        
        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync() {
            var comment = Context.Comments.ToList().Find(x => x.Id == CommentId && x.For == ThreadId);
            if (comment is null)
                return NotFound();

            switch (Action)
            {
                case "delete":
                    return removeComment(comment);
                case "modify":
                    if (ModelState.IsValid)
                    {
                        var success = Actions.EditComment(User, comment, Input.Content, ModelState);
                        if (!success) return Page();
                        else return LocalRedirect($"/Threads/{ThreadId}");
                    }
                    Input.Content = comment.Content;
                    return Page();
            }

            return BadRequest();
        }

        private IActionResult removeComment(Comment comment)
        {
            var success = Actions.RemoveComment(User, comment);
            if (success)
                return LocalRedirect($"/Threads/{ThreadId}");
            return StatusCode(403, "not allowed");
        }
    }
}
