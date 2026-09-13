using System;
using System.Text;

namespace RX313Dragon.Cryptography
{
    public class QuantumPasswordService
    {
        private readonly IQuantumGenerator _rng;

        public QuantumPasswordService(IQuantumGenerator rng)
        {
            _rng = rng;
        }

        public string GenerateSecurePassword(int length, bool useSymbols = true)
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string symbols = "!@#$%^&*()-_=+[]{}|;:,.<>?/";

            string chars = lower + upper + digits;
            if (useSymbols) chars += symbols;

            byte[] random = _rng.GenerateRandom(length);
            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(chars[random[i] % chars.Length]);

            return sb.ToString();
        }
    }
}
