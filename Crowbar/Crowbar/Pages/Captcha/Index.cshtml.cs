using Crowbar.Captcha;
using Ixnas.AltchaNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Crowbar.Pages.Captcha
{
    public class IndexModel : PageModel
    {
        private readonly AltchaService AltchaInstance;

        public IndexModel(CaptchaContainer captchaCont)
        {
            AltchaInstance = captchaCont.AltchaServiceReal;
        }

        public IActionResult OnGet()
        {
            var challenge = AltchaInstance.Generate();
            return new JsonResult(challenge);
        }
    }
}
