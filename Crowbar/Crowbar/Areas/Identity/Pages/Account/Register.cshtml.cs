// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Crowbar.Actions;
using Crowbar.Captcha;
using Crowbar.Data;
using Crowbar.Models;
using Ixnas.AltchaNet;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Crowbar.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<CrowbarUser> _signInManager;
        private readonly UserManager<CrowbarUser> _userManager;
        private readonly IUserStore<CrowbarUser> _userStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly AltchaService _altchaService;
        private readonly ForumActions _actions;
        public RegisterModel(UserManager<CrowbarUser> userManager,
                            IUserStore<CrowbarUser> userStore,
                            SignInManager<CrowbarUser> signInManager,
                            ILogger<RegisterModel> logger,
                            ApplicationDbContext context,
                            CaptchaContainer captchaCont,
                            ForumActions actions)
        {
            _userManager = userManager;
            _userStore = userStore;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _altchaService = captchaCont.AltchaServiceReal;
            _actions = actions;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }
        [BindProperty]
        public string Altcha { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

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
            //[Required]
            //[EmailAddress]
            //[Display(Name = "Email")]
            //public string Email { get; set; }
            [Required]
            [Display(Name = "UserName")]
            public string UserName { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
            public string InvitationCode { get; set; }
        }


        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            if (_context.SiteSettings.Any() && !_context.SiteSettings.First().EnableRegistration)
            {
                ModelState.AddModelError(string.Empty, "registration is disabled");
                return Page();
            }
            if (_actions.GetSiteSettings().InviteOnly != "anyone")
            {
                var valid = await _actions.ValidateInvitationCode(Input.InvitationCode);
                if (!valid)
                {
                    ModelState.AddModelError("Input.InvitationCode", "the invitation code is invalid");
                    return Page();
                }
            }

            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                Input.UserName = Input.UserName.ToLower().Trim();
                if (!Regex.IsMatch(Input.UserName, @"^[a-zA-Z0-9_.]{3,30}$"))
                {
                    ModelState.AddModelError("Input.UserName", "username must be ascii only, at least 3 characters long and 30 at maximum");
                    return Page();
                }
                if (_actions.GetSiteSettings().EnableRegistrationCaptcha)
                {
                    if (Altcha is null) Altcha = "";
                    var response = _altchaService.Validate(Altcha);
                    if (Altcha.IsNullOrEmpty() || !(await response).IsValid)
                    {
                        ModelState.AddModelError(string.Empty, "invalid captcha");
                        return Page();
                    }
                }

                var user = CreateUser();
                await _userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (!result.Errors.Any())
                    return LocalRedirect("/login");
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private CrowbarUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<CrowbarUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(CrowbarUser)}'. " +
                    $"Ensure that '{nameof(CrowbarUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

    }
}
