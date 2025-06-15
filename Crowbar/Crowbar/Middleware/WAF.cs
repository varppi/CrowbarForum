using Crowbar.Data;
using Crowbar.Models;
using Crowbar.Pages.utils;
using Crowbar.Utils;
using Humanizer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NuGet.Packaging;
using System.IO.Pipelines;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Web;

namespace Crowbar.Middleware
{
    public static class WAF
    {
        private static string[] restricted = ["identity/"];
        private static string[] handlesUploads = ["threads/"];
        private static string[] exemptPaths = ["admin/"];
        private static string[] maliciousRegex = [..System.IO.File.ReadAllLines("Settings/WafRegex.txt")];

        private static string notFoundLogo = @"
#  #  #####  #  #    #     # ##### #########   ##### ##### #   # #     # ####
#  #  #   #  #  #    # #   # #   #     #       #     #   # #   # # #   # #   #
####  #   #  ####    #  #  # #   #     #       ##### #   # #   # #  #  # #    #
   #  #   #     #    #   # # #   #     #       #     #   # #   # #   # # #   #
   #  #####     #    #     # #####     #       #     ##### ##### #     # ####
        ".Trim();
        private static string internalServerErrorLogo = @"
 _____ _   _ _____ ___________ _   _   ___   _       _____ ___________ _   _ ___________ 
|_   _| \ | |_   _|  ___| ___ \ \ | | / _ \ | |     /  ___|  ___| ___ \ | | |  ___| ___ \
  | | |  \| | | | | |__ | |_/ /  \| |/ /_\ \| |     \ `--.| |__ | |_/ / | | | |__ | |_/ /
  | | | . ` | | | |  __||    /| . ` ||  _  || |      `--. \  __||    /| | | |  __||    / 
 _| |_| |\  | | | | |___| |\ \| |\  || | | || |____ /\__/ / |___| |\ \\ \_/ / |___| |\ \ 
 \___/\_| \_/ \_/ \____/\_| \_\_| \_/\_| |_/\_____/ \____/\____/\_| \_|\___/\____/\_| \_|
                                                                                         
                                                                                         
 _________________ ___________                                                           
|  ___| ___ \ ___ \  _  | ___ \                                                          
| |__ | |_/ / |_/ / | | | |_/ /                                                          
|  __||    /|    /| | | |    /                                                           
| |___| |\ \| |\ \\ \_/ / |\ \                                                           
\____/\_| \_\_| \_|\___/\_| \_|                                                                                                                                                 
        ".Trim();
        private static string blockedLogo = @"
 ___  ___ _____ ___ _  _ _____ ___   _   _    _ __   __ 
| _ \/ _ \_   _| __| \| |_   _|_ _| /_\ | |  | |\ \ / / 
|  _/ (_) || | | _|| .` | | |  | | / _ \| |__| |_\ V /  
|_|  \___/ |_| |___|_|\_| |_| |___/_/ \_\____|____|_|   
 __  __   _   _    ___ ___ ___ ___  _   _ ___  
|  \/  | /_\ | |  |_ _/ __|_ _/ _ \| | | / __| 
| |\/| |/ _ \| |__ | | (__ | | (_) | |_| \__ \ 
|_|  |_/_/ \_\____|___\___|___\___/ \___/|___/                                                          
 _   _ ___ ___ ___   ___ _  _ ___ _   _ _____                                                          
| | | / __| __| _ \ |_ _| \| | _ \ | | |_   _|                                                         
| |_| \__ \ _||   /  | || .` |  _/ |_| | | |                                                           
 \___/|___/___|_|_\ |___|_|\_|_|  \___/  |_|                                                                                                                                 
 ___ _    ___   ___ _  _____ ___                                                                       
| _ ) |  / _ \ / __| |/ / __|   \                                                                      
| _ \ |_| (_) | (__| ' <| _|| |) |                                                                     
|___/____\___/ \___|_|\_\___|___/                                                                      
        ";

        private static string chars = "qwertyuiopasdfghjklzxvbnm#+$0123456789<>|,.-_!\"@=";
        private static bool hasAdmin = false;

        /// <summary>
        /// Enables WAF for the 'WebApplication' instance.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseWaf(this WebApplication app)
        {
            app.Use(async (context, next) =>{
                try
                {
                    await WafHandler(context, next, app);
                }
                catch (Exception ex) {
                    app.Logger.LogError(0, ex, ex.Message);
                }
            });

            return app;
        }

        /// <summary>
        /// The WAF middleware/handler.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        public async static Task WafHandler(HttpContext context, RequestDelegate next, WebApplication app)
        {
            context.Response.Headers.AddSecurityHeaders();

            if (context.Request.Path.Value is null)
            {
                await next(context);
                return;
            }

            // Check if admin account is already set up
            if (!hasAdmin)
            {
                var scope = app.Services.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<UserManager<CrowbarUser>>();
                hasAdmin = Forum.HasAdmin(ctx);
            }
            if (!hasAdmin && !context.Request.Path.Value.Contains(".")
                && !context.Request.Path.Value.ToLower().Replace("/", "").StartsWith("admin"))
            {
                context.Response.Redirect("/admin");
                return;
            }
            /////

            // Path related
            if (context.IsRequesting(restricted)) 
            { 
                context.Response.StatusCode = 404; return;
            }

            if (context.IsRequesting(handlesUploads) && context.Request.Method == "POST")
                context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = 100_000_000;
            ////


            // Check if body contains any malicious code
            if (context.Request.Body is not null)
            {
                var isMalicious = await context.HasMaliciousContent();
                if (isMalicious) return;
            }
            ////

            await next.Invoke(context);

            switch(context.Response.StatusCode)
            {
                case 404:
                    context.Waf404();
                    return;
                case 500:
                    context.Waf500();
                    return;
            }
        }

        /// <summary>
        /// Checks if the user's requested path matches or is under one or more
        /// of the paths in the 'paths' argument.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="paths"></param>
        /// <returns>
        /// 'True' for "yes, the user has requested a path that is in the 'paths' argument" and 
        /// 'False' for "no, the user didn't request any of the paths found in the 'paths' argument"
        /// </returns>
        public static bool IsRequesting(this HttpContext context, string[] paths)
        {
            foreach (string path in paths)
            {
                if (Regex.Replace(context.Request.Path.Value, @"^\/*", "")
                    .ToLower().StartsWith(path))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if the request body matches any of the regexes contained in the "WafRegex.txt" file.
        /// </summary>
        /// <param name="context"></param>
        /// <returns>'True' for "this request is malicious" and 'False' for "this request is clean"</returns>
        public async static Task<bool> HasMaliciousContent(this HttpContext context)
        {
            if (context.IsRequesting(exemptPaths)) return false;
            
            context.Request.EnableBuffering();
            var bodyBytes = new byte[context.Request.Body.Length];
            var memStream = new MemoryStream();
            await context.Request.Body.CopyToAsync(memStream);
            var body = Encoding.UTF8.GetString(memStream.ToArray());
            body = WebUtility.UrlDecode(body);
            memStream.Position = 0;
            context.Request.Body = memStream;
            foreach (var reg in maliciousRegex)
            {
                if (Regex.IsMatch(body, reg.Trim()))
                {
                    context.WafBlocked();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Displays a custom access denied page for when the WAF has detected
        /// a malicious request.
        /// </summary>
        /// <param name="context"></param>
        public async static void WafBlocked(this HttpContext context)
        {
            var payload = blockedLogo;
            await RandomGarbage(payload, context);
        }


        /// <summary>
        /// Displays a custom 500 page with ascii art
        /// </summary>
        /// <param name="context"></param>
        public async static void Waf500(this HttpContext context)
        {
            var payload = internalServerErrorLogo;
            await RandomGarbage(payload, context);
        }

        /// <summary>
        /// Displays a custom 404 page with ascii art
        /// </summary>
        /// <param name="context"></param>
        public async static void Waf404(this HttpContext context) 
        {
            var payload = notFoundLogo.Replace("#", chars[new Random().Next(chars.Length)].ToString());
            await RandomGarbage(payload, context);
        }


        /// <summary>
        /// Generates a randomized response to confuse any potential bots.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static async Task RandomGarbage(string payload, HttpContext context)
        {
            int[] codeList = [200, 404, 500, 403, 401, 302, 301];
            var payload1 = Encoding.UTF8.GetBytes(payload+new string('\n', 100));
            int size = new Random().Next(1000);
            var payload2 = new StringBuilder();
            for (int i = 0; i < new Random().Next(10000000); i++)
                payload2.Append(chars[new Random().Next(chars.Length)].ToString());
            var finalPayload = payload1.Concat(Encoding.UTF8.GetBytes(payload2.ToString())).ToArray();
            context.Response.StatusCode = new Random().Next(codeList.Length - 1);
            await context.Response.Body.FlushAsync();
            await context.Response.Body.WriteAsync(finalPayload.ToArray());
        }

        /// <summary>
        /// Adds security headers to protect from client side attacks.
        /// </summary>
        /// <param name="headers"></param>
        public static void AddSecurityHeaders(this IHeaderDictionary headers)
        {
            // Allows for only the captcha related javascript to run
            headers.Append("Content-Security-Policy",
                @"default-src: 'none'; script-src 'self' 'sha256-VIgJpYd8rRNZHATtBvUvRbeJ/2zfyDxmfZ2RGfZbbvk=' 'sha256-I7rCX1I1HIpKaRkiLSFlqch4/PHhFz+BpdGRlwwRMws'; worker-src 'self' blob:");

            // Makes sure not a single permission is granted
            headers.Append("Permissions-Policy", "accelerometer=(), autoplay=(), camera=(), document-domain=(), encrypted-media=(), fullscreen=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), midi=(), payment=(), picture-in-picture=(), screen-wake-lock=(), sync-xhr=(self), usb=(), xr-spatial-tracking=()");

            // Iframe usage denied to prevent clickjacking
            headers.Append("X-Frame-Options", "deny");

            // Mime sniffing disabled to prevent malicious file downloads
            headers.Append("X-Content-Type-Options", "nosniff");

            // The less tracking the better
            headers.Append("Referrer-Policy", "no-referrer");
        }
    }
}
