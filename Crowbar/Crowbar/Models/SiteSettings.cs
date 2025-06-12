using Crowbar.Encryption;

namespace Crowbar.Models
{
    public class SiteSettings
    {
        private string? _forumName;
        private string? _frontPageHtml;
        private string? _globalCss;
        private string? _theme;

        public int Id { get; set; }
        public bool EnableRegistration { get; set; }
        public bool EnableLoginCaptcha { get; set; }
        public bool EnableRegistrationCaptcha { get; set; }
        public bool HideThreadsFromNonMembers { get; set; }
        public bool DisableAnonDownloads { get; set; }

        public string? FrontPageHtml
        {
            get => EncryptionLayer.DecryptString(_frontPageHtml);
            set => _frontPageHtml = EncryptionLayer.Encrypt(value);
        }
        public string? GlobalCss
        {
            get => EncryptionLayer.DecryptString(_globalCss);
            set => _globalCss = EncryptionLayer.Encrypt(value);
        }
        public string? ForumName
        {
            get => EncryptionLayer.DecryptString(_forumName);
            set => _forumName = EncryptionLayer.Encrypt(value);
        }
        public string? Theme
        {
            get => EncryptionLayer.DecryptString(_theme);
            set => _theme = EncryptionLayer.Encrypt(value);
        }

        public int ThreadLimit { get; set; }
        public int CommentLimit { get; set; }
        public int CommentEditLimit { get; set; }
        public int ThreadEditLimit { get; set; }
        public int AttachmentLimit { get; set; }

        public int ProfileChangeLimit { get; set; }
    }
}
