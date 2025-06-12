using Crowbar.Encryption;

namespace Crowbar.Models
{
    public class File
    {
        public int Id { get; set; }

        private string? _fileName;
        private string? _fileContentType;
        private string? _fileData;

        public string? FileName
        {
            get => EncryptionLayer.DecryptString(_fileName);
            set => _fileName = EncryptionLayer.Encrypt(value);
        }
        public string? FileContentType
        {
            get => EncryptionLayer.DecryptString(_fileContentType);
            set => _fileContentType = EncryptionLayer.Encrypt(value);
        }
        public byte[]? FileData
        {
            get => EncryptionLayer.DecryptData(_fileData);
            set => _fileData = EncryptionLayer.Encrypt(value);
        }
    }
}
