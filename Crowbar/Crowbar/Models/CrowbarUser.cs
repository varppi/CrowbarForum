using Crowbar.Encryption;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Crowbar.Models
{
    /// <summary>
    /// The default implementation of <see cref="CrowbarUser{TKey}"/> which uses a string as a primary key.
    /// </summary>
    public class CrowbarUser : IdentityUser
    {
        private string? _userName;
        private string? _profilePicture;
        private string? _description;
        private string[]? _inviteCodes;

        [NotMapped]
        public override string? Email { get; set; } = "none@none.none";
        public override string? UserName
        {
            get => EncryptionLayer.DecryptString(_userName);
            set => _userName = EncryptionLayer.Encrypt(value);
        }
        public byte[]? ProfilePicture
        {
            get => EncryptionLayer.DecryptData(_profilePicture);
            set => _profilePicture = EncryptionLayer.Encrypt(value);
        }
        public string? Description
        {
            get => EncryptionLayer.DecryptString(_description);
            set => _description = EncryptionLayer.Encrypt(value);
        }
        public string[]? InviteCodes { 
            get => EncryptionLayer.DecryptStringList(_inviteCodes);
            set => _inviteCodes = EncryptionLayer.Encrypt(value);
        }
    }
}
