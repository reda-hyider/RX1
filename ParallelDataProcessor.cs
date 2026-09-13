using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using RX313Dragon.Processing;

namespace RX313Dragon.Cryptography
{
    public class ParallelDataProcessor
    {
        private readonly IQuantumGenerator _rng;
        private readonly int _numCores;

        public ParallelDataProcessor(IQuantumGenerator rng, int cores = 0)
        {
            _rng = rng;
            _numCores = cores > 0 ? cores : Environment.ProcessorCount;
        }

        public byte[] EncryptParallel(byte[] data, byte[] key)
        {
            int chunkSize = Math.Max(4096, data.Length / _numCores);
            var chunks = SplitData(data, chunkSize);
            var results = new ConcurrentBag<(int index, byte[] encrypted)>();

            Parallel.For(0, chunks.Length, i =>
            {
                var proc = new SecureStreamProcessor(_rng);
                byte[] encrypted = proc.Process(chunks[i], key, ProcessingMode.Encrypt, i);
                results.Add((i, encrypted));
            });

            var ordered = results.OrderBy(x => x.index).Select(x => x.encrypted).ToArray();
            return CombineChunks(ordered);
        }

        public byte[] DecryptParallel(byte[] encryptedData, byte[] key)
        {
            // نفس المنطق مع تغيير mode للفك
            int chunkSize = Math.Max(4096, encryptedData.Length / _numCores);
            var chunks = SplitData(encryptedData, chunkSize);
            var results = new ConcurrentBag<(int index, byte[] decrypted)>();

            Parallel.For(0, chunks.Length, i =>
            {
                var proc = new SecureStreamProcessor(_rng);
                byte[] decrypted = proc.Process(chunks[i], key, ProcessingMode.Decrypt, i);
                results.Add((i, decrypted));
            });

            var ordered = results.OrderBy(x => x.index).Select(x => x.decrypted).ToArray();
            return CombineChunks(ordered);
        }

        private byte[][] SplitData(byte[] data, int chunkSize)
        {
            int chunkCount = (int)Math.Ceiling((double)data.Length / chunkSize);
            var chunks = new byte[chunkCount][];
            for (int i = 0; i < chunkCount; i++)
            {
                int offset = i * chunkSize;
                int len = Math.Min(chunkSize, data.Length - offset);
                chunks[i] = new byte[len];
                Array.Copy(data, offset, chunks[i], 0, len);
            }
            return chunks;
        }

        private byte[] CombineChunks(byte[][] chunks)
        {
            int total = chunks.Sum(c => c.Length);
            byte[] result = new byte[total];
            int offset = 0;
            foreach (var ch in chunks)
            {
                Buffer.BlockCopy(ch, 0, result, offset, ch.Length);
                offset += ch.Length;
            }
            return result;
        }
    }
}
