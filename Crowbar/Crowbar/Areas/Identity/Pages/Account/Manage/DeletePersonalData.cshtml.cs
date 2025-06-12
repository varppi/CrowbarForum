// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using Crowbar.Actions;
using Crowbar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Crowbar.Areas.Identity.Pages.Account.Manage
{
    [Authorize]
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<CrowbarUser> _userManager;
        private readonly SignInManager<CrowbarUser> _signInManager;
        private readonly ILogger<DeletePersonalDataModel> _logger;
        private readonly ForumActions _actions;

        public DeletePersonalDataModel(
            UserManager<CrowbarUser> userManager,
            SignInManager<CrowbarUser> signInManager,
            ILogger<DeletePersonalDataModel> logger,
            ForumActions actions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _actions = actions;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public bool RequirePassword { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (Input.Password is null)
                {
                    return Page();
                }
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Incorrect password.");
                    return Page();
                }
            }

            var success = await _actions.RemoveUser(User, User.Identity.Name);
            if (!success)
                return StatusCode((int)HttpStatusCode.InternalServerError, "Something went wrong! Make sure you're not trying to delete the head admin account!");

            await _signInManager.SignOutAsync();

            return Redirect("~/");
        }
    }
}
