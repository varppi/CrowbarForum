using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;
using System.Text;
using System.Text.RegularExpressions;

namespace Crowbar.Pages.utils
{
    public class qrcodeModel : PageModel
    {
        private readonly QRCodeGenerator qrCodeGenerator = new();

        [BindProperty(SupportsGet = true)]
        public string Secret { get; set; }

        public IActionResult OnGet()
        {
            if (Secret is null || !secretParse(Secret, out string SecretParsed))
                return BadRequest();
            var qrData = qrCodeGenerator.CreateQrCode(SecretParsed, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            var qrCodeImage = qrCode.GetGraphic(50);
            return File(qrCodeImage, "image/png");
        }
        
        private bool secretParse(string Secret, out string SecretParsed)
        {
            var cuts = Secret.Split(" ");
            var noSpaces = string.Join("", cuts).ToLower();
            var regexCheck = Regex.IsMatch(noSpaces, @"^[a-z0-9]{32}$");
            SecretParsed = $"otpauth://totp/Crowbar?secret={noSpaces}";
            return regexCheck && cuts.Length == 8;
        }
    }
}
