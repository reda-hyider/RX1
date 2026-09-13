using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace RX313Dragon.Cryptography
{
    public static class RX313PerformanceTests
    {
        public static void RunLargeFileEncryptionBenchmark(
            string sourcePath,
            string encryptedPath,
            string decryptedPath,
            byte[] key,
            byte[] masterSeed,
            int sizeInMB = 100)
        {
            EnsureTestFile(sourcePath, sizeInMB, masterSeed);

            Console.WriteLine($"\n🔧 Benchmark: تشفير / فك تشفير ملف {sizeInMB} ميجابايت");
            Console.WriteLine($"ملف المصدر: {sourcePath}");
            Console.WriteLine($"ملف مشفر:  {encryptedPath}");
            Console.WriteLine($"ملف مفكك:  {decryptedPath}\n");

            using var engine = new RX313Engine(masterSeed);
            byte[] original = File.ReadAllBytes(sourcePath);

            var sw = Stopwatch.StartNew();
            byte[] encrypted = engine.Encrypt(original, key);
            File.WriteAllBytes(encryptedPath, encrypted);
            sw.Stop();
            Console.WriteLine($"🟢 وقت التشفير: {sw.Elapsed.TotalSeconds:F3} ثانية");
            Console.WriteLine($"   سرعة التشفير: {(original.Length / (1024.0 * 1024.0) / sw.Elapsed.TotalSeconds):F2} MB/s\n");

            sw.Restart();
            byte[] decrypted = engine.Decrypt(encrypted, key);
            File.WriteAllBytes(decryptedPath, decrypted);
            sw.Stop();
            Console.WriteLine($"🟢 وقت فك التشفير: {sw.Elapsed.TotalSeconds:F3} ثانية");
            Console.WriteLine($"   سرعة فك التشفير: {(decrypted.Length / (1024.0 * 1024.0) / sw.Elapsed.TotalSeconds):F2} MB/s\n");

            bool matches = CompareBytes(original, decrypted);
            Console.WriteLine(matches ? "✅ التحقق نجح: الملف المفكوك مطابق للأصل" : "❌ فشل التحقق: الملف المفكوك مختلف");
        }

        private static void EnsureTestFile(string path, int sizeInMB, byte[] seed)
        {
            if (File.Exists(path) && new FileInfo(path).Length >= (long)sizeInMB * 1024 * 1024)
                return;

            Console.WriteLine($"📄 إنشاء ملف اختبار كبير بحجم {sizeInMB} ميجابايت...");
            var rng = new RX313QuantumGenerator(seed);
            using var stream = File.Create(path);
            byte[] buffer = new byte[1024 * 1024];
            long remaining = (long)sizeInMB * 1024 * 1024;

            while (remaining > 0)
            {
                int chunk = (int)Math.Min(buffer.Length, remaining);
                byte[] random = rng.GenerateRandom(chunk);
                stream.Write(random, 0, chunk);
                remaining -= chunk;
            }

            stream.Flush();
            Console.WriteLine("✅ ملف الاختبار تم إنشاؤه.");
        }

        private static bool CompareBytes(byte[] left, byte[] right)
        {
            if (left.Length != right.Length)
                return false;

            for (int i = 0; i < left.Length; i++)
                if (left[i] != right[i])
                    return false;

            return true;
        }
    }
}
