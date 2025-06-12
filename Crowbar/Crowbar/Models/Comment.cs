using Crowbar.Encryption;

namespace Crowbar.Models
{
    public class Comment
    {
        private string? _creator;
        private string? _content;

        public int Id { get; set; }
        public int For {  get; set; }
        public string? Creator
        {
            get => EncryptionLayer.DecryptString(_creator);
            set => _creator = EncryptionLayer.Encrypt(value);
        }
        public string? Content
        {
            get => EncryptionLayer.DecryptString(_content);
            set => _content = EncryptionLayer.Encrypt(value);
        }
        public int ReplyTo { get; set; }
        public DateTime Published { get; set; }
    }
}
