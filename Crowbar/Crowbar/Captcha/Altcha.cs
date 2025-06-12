using Ixnas.AltchaNet;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NuGet.Protocol;
using System.Security.Cryptography;

namespace Crowbar.Captcha
{
    /// <summary>
    /// In memory database for Altcha
    /// </summary>
    public class AltchaInMemoryStore : IAltchaChallengeStore
    {
        private class StoredChallenge
        {
            public string Challenge { get; set; }
            public DateTimeOffset ExpiryUtc { get; set; }
        }

        private readonly List<StoredChallenge> _stored = new List<StoredChallenge>();

        public AltchaInMemoryStore() { }

        public Task Store(string challenge, DateTimeOffset expiryUtc)
        {
            var challengeToStore = new StoredChallenge
            {
                Challenge = challenge,
                ExpiryUtc = expiryUtc
            };
            _stored.Add(challengeToStore);
            return Task.CompletedTask;
        }

        public Task<bool> Exists(string challenge)
        {
            _stored.RemoveAll(storedChallenge => storedChallenge.ExpiryUtc <= DateTimeOffset.UtcNow);
            var exists = _stored.Exists(storedChallenge => storedChallenge.Challenge == challenge);
            return Task.FromResult(exists);
        }
    }

    /// <summary>
    /// Due to not being able to inherit AltchaService directly, I had to
    /// make a wrapper for it.
    /// </summary>
    public class CaptchaContainer
    {
        public AltchaService AltchaServiceReal { get; private set; }
        public CaptchaContainer() {
            var key = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(key);
            var store = new AltchaInMemoryStore();
            AltchaServiceReal = Altcha.CreateServiceBuilder()
                                      .UseSha256(key)
                                      .UseStore(store)
                                      .SetExpiryInSeconds(60)
                                      .SetComplexity(1, 400000)
                                      .Build();
        }
    }
}
