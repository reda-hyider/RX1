using RX313Dragon.AdvancedSecurity;

namespace RX313Dragon.Cryptography
{
    /// <summary>
    /// يغلف UltimateHybridCryptoSystem ليعمل كمصدر عشوائي متوافق مع NIST.
    /// </summary>
    public sealed class RX313QuantumGenerator : IQuantumGenerator
    {
        private readonly UltimateHybridCryptoSystem _rng;

        public RX313QuantumGenerator(byte[] seed)
        {
            _rng = new UltimateHybridCryptoSystem(seed);
        }

        public byte[] GenerateRandom(int size)
        {
            byte[] result = new byte[size];
            _rng.GenerateBytes(result, 0, result.Length);
            return result;
        }
    }
}
