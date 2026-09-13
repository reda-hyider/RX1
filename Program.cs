using System;
using System.IO;
using System.Diagnostics;
using RX313Dragon.AdvancedSecurity;

namespace RX313Dragon
{
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // اسم الملف النهائي الذي نريد اختباره
            string outputFile = "finall_improve.bin";
            long sizeInMB = 1;  // 1 MB
            long bytesToGenerate = sizeInMB * 1024 * 1024;

            Console.WriteLine($"🌌 Generating random file using UltimateHybridCryptoSystem");
            Console.WriteLine($"📁 File: {outputFile}");
            Console.WriteLine($"📦 Size: {sizeInMB} MB ({bytesToGenerate:N0} bytes)");
            Console.WriteLine();

            // بذرة عشوائية
            byte[] seed = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(seed);
            }
            Console.WriteLine($"🔑 Seed (Base64): {Convert.ToBase64String(seed)}");
            Console.WriteLine();

            var sw = Stopwatch.StartNew();

            // إنشاء النظام الهجين الحقيقي
            using (var hybrid = new UltimateHybridCryptoSystem(seed))
            {
                byte[] buffer = new byte[256 * 1024]; // 256 KB buffer
                long written = 0;

                using (FileStream fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
                {
                    while (written < bytesToGenerate)
                    {
                        int toWrite = (int)Math.Min(buffer.Length, bytesToGenerate - written);
                        hybrid.GenerateBytes(buffer, 0, toWrite);
                        fs.Write(buffer, 0, toWrite);
                        written += toWrite;

                        // إظهار التقدم
                        double percent = (double)written / bytesToGenerate * 100;
                        Console.Write($"\rProgress: {percent:F1}% ({written / 1024 / 1024} MB / {sizeInMB} MB)");
                    }
                }
            }

            sw.Stop();

            Console.WriteLine($"\n✅ File successfully generated: {outputFile}");
            Console.WriteLine($"⏱️ Time elapsed: {sw.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"💾 Entropy: {(new FileInfo(outputFile).Length / (1024.0 * 1024.0)):F2} MB written");
            Console.WriteLine("\n📊 Next steps:");
            Console.WriteLine("1. Copy the file to your NIST STS directory.");
            Console.WriteLine("2. Run: ./assess 1000000");
            Console.WriteLine("3. Use the file as binary stream.");
            Console.WriteLine("4. Verify that all tests pass (p-value > 0.01 and proportion >= minimum).");
        }
    }
}