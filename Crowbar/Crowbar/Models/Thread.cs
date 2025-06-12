using Crowbar.Encryption;

namespace Crowbar.Models
{
    public class Thread
    {
        private string? _title;
        private string? _content;
        private string? _creator;
        private string? _category;
        private string[]? _likes;
        private string[]? _dislikes;

        public int Id { get; set; }
        public DateTime Published { get; set; }
        public int[]? Attachments { get; set; }
        public string[]? Dislikes
        {
            get => EncryptionLayer.DecryptStringList(_dislikes);
            set => _dislikes = EncryptionLayer.Encrypt(value);
        }
        public string[]? Likes {
            get => EncryptionLayer.DecryptStringList(_likes);
            set => _likes = EncryptionLayer.Encrypt(value);
        }
        public string? Title
        {
            get => EncryptionLayer.DecryptString(_title);
            set => _title = EncryptionLayer.Encrypt(value);
        }
        public string? Content
        {
            get => EncryptionLayer.DecryptString(_content);
            set => _content = EncryptionLayer.Encrypt(value);
        }
        public string? Creator
        {
            get => EncryptionLayer.DecryptString(_creator);
            set => _creator = EncryptionLayer.Encrypt(value);
        }
        public string? Category
        {
            get => EncryptionLayer.DecryptString(_category);
            set => _category = EncryptionLayer.Encrypt(value);
        }
    }
}
