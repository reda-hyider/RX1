using System;
using System.Security.Cryptography;

namespace RX313Dragon.Cryptography
{
    public class KeyDerivationService
    {
        private readonly IQuantumGenerator _rng;

        public KeyDerivationService(IQuantumGenerator rng)
        {
            _rng = rng;
        }

        public byte[] GenerateSalt(int length = 32) => _rng.GenerateRandom(length);

        public byte[] DeriveKey(string password, byte[] salt, int keyLength = 32, int iterations = 100000)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA512);
            return pbkdf2.GetBytes(keyLength);
        }
    }
}
