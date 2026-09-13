using System;
using System.IO;
using System.Text;
using RX313Dragon.AdvancedSecurity;

namespace RX313Dragon.Cryptography
{
    public class RX313Engine : IDisposable
    {
        private readonly IQuantumGenerator _rng;
        private readonly KeyDerivationService _kdf;
        private readonly ParallelDataProcessor _parallel;
        private readonly QuantumPasswordService _pwdGen;
        private bool _disposed;

        public RX313Engine(byte[] masterSeed)
        {
            _rng = new RX313QuantumGenerator(masterSeed);
            _kdf = new KeyDerivationService(_rng);
            _parallel = new ParallelDataProcessor(_rng);
            _pwdGen = new QuantumPasswordService(_rng);
        }

        public byte[] Encrypt(byte[] plain, byte[] key) => _parallel.EncryptParallel(plain, key);
        public byte[] Decrypt(byte[] cipher, byte[] key) => _parallel.DecryptParallel(cipher, key);

        public byte[] EncryptFile(string inputPath, string outputPath, byte[] key)
        {
            byte[] data = File.ReadAllBytes(inputPath);
            byte[] enc = Encrypt(data, key);
            File.WriteAllBytes(outputPath, enc);
            return enc;
        }

        public byte[] DecryptFile(string inputPath, string outputPath, byte[] key)
        {
            byte[] enc = File.ReadAllBytes(inputPath);
            byte[] dec = Decrypt(enc, key);
            File.WriteAllBytes(outputPath, dec);
            return dec;
        }

        public string GenerateStrongPassword(int length = 16) => _pwdGen.GenerateSecurePassword(length);

        public byte[] GenerateKey(int length = 32) => _rng.GenerateRandom(length);

        public (byte[] privateKey, byte[] publicKey) GenerateSigningKeys() => DigitalSignature.GenerateKeyPair();

        public byte[] SignData(byte[] data, byte[] privateKey) => DigitalSignature.Sign(data, privateKey);
        public bool VerifySignature(byte[] data, byte[] signature, byte[] publicKey) => DigitalSignature.Verify(data, signature, publicKey);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
