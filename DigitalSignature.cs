using System;
using System.Security.Cryptography;

namespace RX313Dragon.Cryptography
{
    public static class DigitalSignature
    {
        public static (byte[] privateKey, byte[] publicKey) GenerateKeyPair()
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP521);
            byte[] priv = ecdsa.ExportECPrivateKey();
            byte[] pub = ecdsa.ExportSubjectPublicKeyInfo();
            return (priv, pub);
        }

        public static byte[] Sign(byte[] data, byte[] privateKey)
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(privateKey, out _);
            return ecdsa.SignData(data, HashAlgorithmName.SHA512);
        }

        public static bool Verify(byte[] data, byte[] signature, byte[] publicKey)
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(publicKey, out _);
            return ecdsa.VerifyData(data, signature, HashAlgorithmName.SHA512);
        }
    }
}
