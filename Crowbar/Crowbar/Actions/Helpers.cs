using Crowbar.Data;
using Crowbar.Models;
using Crowbar.Pages.utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using NuGet.Configuration;
using NuGet.Packaging;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;

namespace Crowbar.Actions
{
    public partial class ForumActions
    {
        /// <summary>
        /// Checks if the specified object contains all the required fields,
        /// checks if the fields marked as ascii only fulfill that constraint.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="required"></param>
        /// <param name="asciiOnly"></param>
        /// <param name="modelName"></param>
        /// <param name="modelState"></param>
        /// <returns>
        /// true for "it is valid" and false for "it is not valid".
        /// </returns>
        public static readonly string AsciiOnly = @"^[ -~]+$";
        private static readonly string AlphNumOnly = @"^[a-zA-Z0-9._ ]+$";
        public bool IsValidModel(object? obj = null,
                                   string[]? required = null,
                                   string[]? asciiOnly = null,
                                   string[]? alphNumOnly = null,
                                   string[]? under255 = null,
                                   string modelName = "",
                                   ModelStateDictionary? modelState = null
                                   )
        {
            List<string> asciiOnlyList = asciiOnly?.ToList() ?? [];
            HashSet<string> requiredSet = required?.ToHashSet() ?? [];
            List<string> alphNumOnlyList = alphNumOnly?.ToList() ?? [];
            List<string> under255List = under255?.ToList() ?? [];

            requiredSet.AddRange(asciiOnlyList);
            requiredSet.AddRange(alphNumOnlyList);
            requiredSet.AddRange(under255List);

            var errors = new Dictionary<string, string>();
            bool failed = false;
            foreach (var field in requiredSet)
            {
                object? fieldValue;
                fieldValue = obj?.GetType()
                    ?.GetProperty(field)
                    ?.GetValue(obj, null);
                if (fieldValue is null)
                {
                    modelState?.AddModelError($"{modelName}.{field}", "required parameter missing");
                    failed = true;
                    continue;
                }
            }
            if (failed)
                return false;

            foreach (var field in asciiOnlyList)
            {
                string? fieldValue = (string?)obj?.GetType()
                    ?.GetProperty(field)
                    ?.GetValue(obj, null);
                if (!Regex.IsMatch(fieldValue.ToString(), AsciiOnly))
                {
                    modelState?.AddModelError($"{modelName}.{field}", $"field contains non ascii characters");
                    failed = true;
                    continue;
                }
            }

            foreach (var field in alphNumOnlyList)
            {
                string? fieldValue = (string?)obj?.GetType()
                    ?.GetProperty(field)
                    ?.GetValue(obj, null);
                if (!Regex.IsMatch(fieldValue.ToString(), AlphNumOnly))
                {
                    modelState?.AddModelError($"{modelName}.{field}", $"field contains non alpha-numeric characters");
                    failed = true;
                    continue;
                }
            }

            foreach (var field in under255List)
            {
                string? fieldValue = (string?)obj?.GetType()
                    ?.GetProperty(field)
                    ?.GetValue(obj, null);
                if ((fieldValue ?? "").Length > 255)
                {
                    modelState?.AddModelError($"{modelName}.{field}", $"is over 255 characters");
                    failed = true;
                    continue;
                }
            }
            return !failed;
        }

        /// <summary>
        /// Checks if the user has a valid session. If the user changes his
        /// username the change doesn't reflect on the claim instantly, but
        /// this detects it never the less.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="_context"></param>
        /// <returns>
        /// true for "this is a valid session" and false for "this session is not valid and the user has been logged out"
        /// </returns>
        public async Task<bool> IsValidSession(ClaimsPrincipal user)
        {
            var userManager = GetUserManager();
            var signInManager = GetSignInManager();
            if (user is null)
                return false;
            var crowbarUser = await userManager.FindByNameAsync(user.Identity.Name);
            if (crowbarUser is null)
            {
                await signInManager.SignOutAsync();
                return false;
            }
            return true;
        }

        public SignInManager<CrowbarUser> GetSignInManager()
            => (SignInManager<CrowbarUser>)_context.GetService(typeof(SignInManager<CrowbarUser>));
        public UserManager<CrowbarUser> GetUserManager()
            => (UserManager<CrowbarUser>)_context.GetService(typeof(UserManager<CrowbarUser>));
        public RoleManager<IdentityRole> GetRoleManger()
            => (RoleManager<IdentityRole>)_context.GetService(typeof(RoleManager<IdentityRole>));

        /// <summary>
        /// Updates the rate limits to match the most recent ones
        /// specified in the admin panel.
        /// </summary>
        /// <param name="user"></param>
        private static Dictionary<string, int>? limitMap;
        private static Dictionary<string, int> trackingMap = new();
        private static DateTime lastCleared = DateTime.Now;
        public void UpdateLimits()
        {
            var siteSettings = GetSiteSettings();
            if (siteSettings is null) return;

            limitMap = new Dictionary<string, int>()
                {
                    {"ThreadLimit", siteSettings.ThreadLimit },
                    {"ThreadEditLimit", siteSettings.ThreadEditLimit},
                    {"CommentLimit", siteSettings.CommentLimit},
                    {"CommentEditLimit", siteSettings.CommentEditLimit},
                    {"ProfileChangeLimit", siteSettings.ProfileChangeLimit},
                };
            trackingMap.Clear();
        }

        /// <summary></summary>
        /// <param name="user"></param>
        /// <param name="action"></param>
        /// <returns>
        /// How many times the user can do the specified action
        /// before it goes over the rate limit set in the admin panel.
        /// </returns>
        internal int LimitCheck(ClaimsPrincipal user, string action)
        {
            if (limitMap is null)
                UpdateLimits();

            if (DateTime.Now.Subtract(lastCleared).Minutes >= 60) { 
                lastCleared = DateTime.Now;
                trackingMap.Clear();
            }

            var key = $"{user.Identity.Name}:{action}";
            if (!trackingMap.ContainsKey(key))
                trackingMap[key] = 0;
            if (limitMap[action] - trackingMap[key] < 0) return -1;
            trackingMap[key]++;
            return limitMap[action] - trackingMap[key];
        }
    }
}
