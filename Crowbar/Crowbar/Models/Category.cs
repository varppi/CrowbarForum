using Crowbar.Encryption;

namespace Crowbar.Models
{
    public class Category
    {
        private string? _name;
        private string? _description;

        public int Id { get; set; }
        public string? Name {
            get => EncryptionLayer.DecryptString(_name);
            set => _name = EncryptionLayer.Encrypt(value);
        }
        public string? Description
        {
            get => EncryptionLayer.DecryptString(_description);
            set => _description = EncryptionLayer.Encrypt(value);
        }
    }
}
