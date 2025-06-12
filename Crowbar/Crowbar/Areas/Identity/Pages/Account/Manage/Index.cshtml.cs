// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Crowbar.Actions;
using Crowbar.Data;
using Crowbar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Crowbar.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly UserManager<CrowbarUser> _userManager;
        private readonly SignInManager<CrowbarUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ForumActions _actions;

        public IndexModel(
            UserManager<CrowbarUser> userManager,
            SignInManager<CrowbarUser> signInManager,
            ApplicationDbContext context,
            ForumActions actions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _actions = actions;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }

        public bool Is2fa { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModifyModel InputModify { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModifyModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Display(Name = "UserName"), Required]
            public string UserName { get; set; }
            [Display(Name = "Profile picture")]
            public IFormFile ProfilePicture { get; set; }
            public string Description { get; set; }
            public string RemoveProfilePicture { get; set; }
        }

        private async Task LoadAsync(CrowbarUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            InputModify = new InputModifyModel
            {
                UserName = userName,
                RemoveProfilePicture = user.ProfilePicture is null ? null : "false",
                Description = user.Description,
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            Is2fa = await _userManager.GetTwoFactorEnabledAsync(user);

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            InputModify.UserName = InputModify.UserName.ToLower().Trim();

            var user3 = await _userManager.FindByNameAsync(user.UserName);
            if (user3 is null) return Page();
            if (InputModify.UserName != user.UserName)
            {
                var user2 = await _userManager.FindByNameAsync(InputModify.UserName);
                if (user2 is not null)
                {
                    ModelState.AddModelError(string.Empty, "Somebody already has that username.");
                    return Page();
                }
            }

            var profilePic = user3.ProfilePicture;
            if (InputModify.ProfilePicture is not null)
            {
                string[] allowedMimeTypes = { "image/jpeg", "image/png", "image/gif" };
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

                var ending = "."+ InputModify.ProfilePicture.FileName.Split(".").Last();
                if (!allowedMimeTypes.Contains(InputModify.ProfilePicture.ContentType) 
                    || !allowedExtensions.Contains(ending))
                {
                    ModelState.AddModelError("InputModify.ProfilePicture", "invalid profile picture");
                    return Page();
                }

                var pfpStream = new MemoryStream();
                InputModify.ProfilePicture.CopyTo(pfpStream);
                profilePic = pfpStream.ToArray();
            }
            if (InputModify.RemoveProfilePicture == "true")
                profilePic = null;

            var updatedUser = new CrowbarUser
            {
                UserName = InputModify.UserName ?? User.Identity.Name,
                ProfilePicture = profilePic,
                Description = InputModify.Description ?? "",
            };
            var success = await _actions.EditUser(User, user3, updatedUser, "", "",  ModelState);
            if (!success)
                return Page();
            
            await _userManager.SetUserNameAsync(user, InputModify.UserName);

            
            await _signInManager.RefreshSignInAsync(user);
            return RedirectToPage();
        }
    }
}
