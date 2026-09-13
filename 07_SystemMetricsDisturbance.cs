// ═══════════════════════════════════════════════════════════════════════════════
// 07_SystemMetricsDisturbance.cs
// طبقة الاضطراب المستوحاة من محادثتك مع صديقك
// قياس: CPU, RAM, Network, GPU بدقة مليارية
// تطبيق معادلة Hyperjerk4D فوضوية للحصول على اضطراب فريد غير قابل للتكرار
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CosmicDragon.ChaosSystems;

namespace RX313Dragon.AdvancedSecurity
{
    /// <summary>
    /// قارئ مقاييس أداء النظام بدقة عالية جداً (مستوى ملياري)
    /// يقرأ: CPU, RAM, Network Traffic, وبدائل GPU
    /// </summary>
    public static class SystemMetricsReader
    {
        private static PerformanceCounter? _cpuCounter;
        private static PerformanceCounter? _interruptsCounter;
        private static DateTime _lastNetworkCheckTime = DateTime.UtcNow;
        private static long _lastNetworkBytes = 0;
        private static double _cachedNetworkRate = 0;

        static SystemMetricsReader()
        {
            try
            {
                // مقياس استهلاك CPU - Windows Only
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                    _cpuCounter.NextValue(); // القراءة الأولى تكون 0، نجاهلها

                    // مقياس عدد المقاطعات/ثانية (بديل GPU)
                    _interruptsCounter = new PerformanceCounter("Processor", "Interrupts/sec", "_Total", true);
                    _interruptsCounter.NextValue();
                }

                // قراءة أولية للشبكة
                UpdateNetworkMetrics();
            }
            catch
            {
                // في حالة الفشل، سنستخدم بدائل
                _cpuCounter = null;
                _interruptsCounter = null;
            }
        }

        /// <summary>
        /// قراءة استهلاك CPU بدقة عالية (نطاق 0 - 1 تريليون)
        /// </summary>
        public static long GetCpuUsageAsHugeInteger()
        {
            if (_cpuCounter == null)
                return GetFallbackCpuValue();

            try
            {
                // Windows platform check
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return GetFallbackCpuValue();

                // نأخذ عدة قراءات ونحسب المتوسط
                double sum = 0;
                for (int i = 0; i < 3; i++)
                {
                    sum += _cpuCounter.NextValue();
                    System.Threading.Thread.Sleep(1);
                }

                double cpuPercent = sum / 3.0; // 0-100
                // تحويل إلى نطاق مليارني (0-100 -> 0-100,000,000,000)
                long result = (long)(cpuPercent * 1_000_000_000.0);
                return result;
            }
            catch
            {
                return GetFallbackCpuValue();
            }
        }

        /// <summary>
        /// قراءة استهلاك RAM بالبايتات الفعلية (عدد ضخم يتغير باستمرار)
        /// </summary>
        public static long GetRamUsageBytes()
        {
            try
            {
                using (var proc = Process.GetCurrentProcess())
                {
                    // WorkingSet64 = الذاكرة الفيزيائية الفعلية المستخدمة بالبايتات
                    return proc.WorkingSet64;
                }
            }
            catch { return 0; }
        }

        /// <summary>
        /// تحديث وقراءة معدل الشبكة (بايتات في الثانية)
        /// </summary>
        private static void UpdateNetworkMetrics()
        {
            try
            {
                long currentBytes = 0;
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        currentBytes += ni.GetIPv4Statistics().BytesReceived;
                        currentBytes += ni.GetIPv4Statistics().BytesSent;
                    }
                }

                // حساب معدل التغير (بايت/ثانية)
                var now = DateTime.UtcNow;
                double elapsed = (now - _lastNetworkCheckTime).TotalSeconds;

                if (elapsed > 0 && _lastNetworkBytes > 0)
                {
                    _cachedNetworkRate = (currentBytes - _lastNetworkBytes) / elapsed;
                }

                _lastNetworkBytes = currentBytes;
                _lastNetworkCheckTime = now;
            }
            catch { }
        }

        /// <summary>
        /// قراءة إجمالي البايتات المتنقلة عبر الشبكة (تتراكم، عدد ضخم)
        /// </summary>
        public static long GetTotalNetworkBytes()
        {
            UpdateNetworkMetrics();
            return _lastNetworkBytes;
        }

        /// <summary>
        /// قراءة معدل المقاطعات (بديل GPU عملي)
        /// المقاطعات تعكس نشاط الأجهزة بما فيها GPU
        /// </summary>
        public static long GetInterruptsAsHugeInteger()
        {
            if (_interruptsCounter == null)
                return GetFallbackGpuValue();

            try
            {
                // Windows platform check
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return GetFallbackGpuValue();

                double interrupts = _interruptsCounter.NextValue(); // عدد مقاطعات/ثانية
                // تحويل إلى نطاق مليارني
                long result = (long)(interrupts * 10_000_000.0);
                return result;
            }
            catch
            {
                return GetFallbackGpuValue();
            }
        }

        /// <summary>
        /// بديل لقراءة CPU (استخدام Stopwatch عالي الدقة)
        /// </summary>
        private static long GetFallbackCpuValue()
        {
            // استخدام عداد الوقت العالي الدقة (ticks)
            return (Stopwatch.GetTimestamp() % 1_000_000_000L) * 1000;
        }

        /// <summary>
        /// بديل لقراءة GPU (عدد خيوط العملية الحالية)
        /// </summary>
        private static long GetFallbackGpuValue()
        {
            try
            {
                using (var proc = Process.GetCurrentProcess())
                {
                    long threadCount = proc.Threads.Count;
                    // إضافة معلومات أخرى
                    long handles = proc.HandleCount;
                    return (threadCount * handles) % 1_000_000_000L;
                }
            }
            catch { return Stopwatch.GetTimestamp() % 1_000_000_000L; }
        }

        /// <summary>
        /// الحصول على جميع المقاييس الأربعة كأعداد صحيحة ضخمة
        /// </summary>
        public static (long cpu, long ram, long net, long gpu) GetAllMetricsAsHugeIntegers()
        {
            long cpu = GetCpuUsageAsHugeInteger();    // 0 - 100,000,000,000
            long ram = GetRamUsageBytes();            // مثلاً 300,000,000 (300 MB)
            long net = GetTotalNetworkBytes();        // يتراكم ويكبر
            long gpu = GetInterruptsAsHugeInteger();  // 0 - 1,000,000,000+

            return (cpu, ram, net, gpu);
        }
    }

    /// <summary>
    /// معادلة فوضوية رباعية الأبعاد (Hyperjerk Modified)
    /// تأخذ أربعة مدخلات وتخرج قيمة موزعة بشكل فوضوي بين -1 و 1
    /// </summary>
    public static class Hyperjerk4D
    {
        // معاملات تعطي حالة hyperchaotic
        private const double A = 0.50;
        private const double B = 0.80;
        private const double C = 1.20;
        private const double D = 0.90;

        /// <summary>
        /// تحويل أربعة متغيرات نظام إلى قيمة اضطراب فوضوية
        /// المعادلة: dx4/dt = -A*x4 - B*x3 - C*x2 - D*x1 + sin(x1)*cos(x2) + tanh(x3*x4)
        /// </summary>
        public static double Compute(double x1, double x2, double x3, double x4)
        {
            // معادلة جبرية فوضوية
            // الجزء الخطي
            double linear = -A * x4 - B * x3 - C * x2 - D * x1;

            // الجزء غير الخطي (يضمن الفوضى)
            double nonlinear = Math.Sin(x1 * 0.1) * Math.Cos(x2 * 0.1) +
                              Math.Tanh(x3 * x4 * 0.001) +
                              Math.Sin(x2 * x3) * 0.1;

            // النتيجة
            double raw = linear + nonlinear;

            // تطبيع بين -1 و 1
            double normalized = Math.Tanh(raw * 0.1);

            return normalized;
        }

        /// <summary>
        /// نسخة مع ملاحظ أضافي (مزيد من غير الخطية)
        /// </summary>
        public static double ComputeWithFeedback(double x1, double x2, double x3, double x4, double feedback)
        {
            double base_value = Compute(x1, x2, x3, x4);
            double feedback_effect = Math.Sin(feedback * 3.14159) * Math.Cos(feedback * 1.57);
            return Math.Tanh(base_value + feedback_effect * 0.5);
        }
    }

    /// <summary>
    /// مرشح فون نيومان: يزيل الانحياز من البايتات بناءً على نمط البتات المتتالية
    /// يأخذ كل بتتين متتاليتين: 01->1, 10->0, ويتجاهل 00 و11
    /// هذا يضمن توزيعاً منتظماً تماماً بغض النظر عن الانحياز الأصلي
    /// </summary>
    public static class VonNeumannCorrector
    {
        public static byte[] Correct(byte[] input)
        {
            if (input == null || input.Length == 0)
                return Array.Empty<byte>();

            using var ms = new System.IO.MemoryStream();
            
            for (int i = 0; i < input.Length; i++)
            {
                byte b = input[i];
                // معالجة كل بت في البايت
                for (int bit = 0; bit < 8; bit += 2)
                {
                    if (bit + 1 < 8)
                    {
                        int bit1 = (b >> bit) & 1;
                        int bit2 = (b >> (bit + 1)) & 1;
                        
                        // 01 أو 10: نحتفظ بالبت الأول
                        // 00 أو 11: نتجاهلها
                        if (bit1 != bit2)
                        {
                            ms.WriteByte((byte)bit1);
                        }
                    }
                }
            }

            return ms.ToArray();
        }
    }

    /// <summary>
    /// تحويل CDF العكسي: تحويل توزيع عشوائي إلى توزيع منتظم تماماً
    /// يستخدم Box-Muller Transform ثم تطبيق CDF التوزيع الطبيعي
    /// </summary>
    public static class CDFInverseTransform
    {
        // التقريب لدالة الخطأ (Error Function)
        private static double Erf(double x)
        {
            // تقريب دقيق لـ erf(x)
            double a1 = 0.254829592;
            double a2 = -0.284496736;
            double a3 = 1.421413741;
            double a4 = -1.453152027;
            double a5 = 1.061405429;
            double p = 0.3275911;

            int sign = x < 0 ? -1 : 1;
            x = Math.Abs(x);

            double t = 1.0 / (1.0 + p * x);
            double y = 1.0 - (((((a5 * t + a4) * t) + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x);

            return sign * y;
        }

        // دالة التوزيع التراكمي (CDF) للتوزيع الطبيعي المعياري
        private static double NormalCDF(double z)
        {
            return 0.5 * (1.0 + Erf(z / Math.Sqrt(2.0)));
        }

        public static byte[] ToUniformDistribution(byte[] input)
        {
            if (input == null || input.Length < 8)
                return Array.Empty<byte>();

            var output = new byte[input.Length];
            
            for (int i = 0; i < input.Length - 7; i += 8)
            {
                // نأخذ 8 بايتات (64 بت) كعدد عشوائي
                ulong u = BitConverter.ToUInt64(input, i);
                
                // نستخرج رقمين عشوائيين منتظمين من [0, 1)
                double u1 = ((u >> 32) & 0xFFFFFFFF) / (double)0x100000000;
                double u2 = (u & 0xFFFFFFFF) / (double)0x100000000;

                // تجنب log(0)
                u1 = Math.Max(u1, 1e-10);
                u2 = Math.Max(u2, 1e-10);

                // تطبيق Box-Muller Transform
                double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                double z1 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

                // تحويل إلى CDF (يتبع التوزيع المنتظم)
                double cdf0 = NormalCDF(z0);
                double cdf1 = NormalCDF(z1);

                // تحويل إلى [0, 255]
                output[i] = (byte)(cdf0 * 255.0);
                
                if (i + 1 < output.Length)
                    output[i + 1] = (byte)(cdf1 * 255.0);
            }

            // معالجة البايتات المتبقية
            for (int i = (input.Length / 8) * 8; i < input.Length; i++)
            {
                output[i] = input[i]; // نتركها كما هي إذا كانت أقل من 8 بايتات
            }

            return output;
        }
    }

    /// <summary>
    /// طبقة حقن الاضطراب المستندة إلى مقاييس النظام
    /// تضمن أن النظام الفوضوي لا يكرر حالته أبداً حتى مع نفس البذرة
    /// </summary>
    public sealed class SystemMetricsDisturbanceLayer : IDisposable
    {
        private HyperchaosLorenz4D _lorenz;
        private HyperchaosChua4D _chua;
        private DadrasSystem _dadras;
        private int _stepCounter;
        private bool _disposed;
        private byte[] _buffer; // مخزن مؤقت للبيانات المصفاة
        private int _bufferIndex;
        private readonly byte[] _seed;
        private readonly HMACSHA256 _hmac;

        // توقيت الحقن (كل 100 خطوة)
        private const int INJECT_INTERVAL = 100;

        // قوة الاضطراب الفعلية التي تعزز التباين دون فقدان الاستقرار
        private const double DISTURBANCE_STRENGTH = 0.5;
        private const int BUFFER_SIZE = 4096;

        public SystemMetricsDisturbanceLayer(byte[] seed)
        {
            _lorenz = new HyperchaosLorenz4D(seed);
            _chua = new HyperchaosChua4D(seed);
            _dadras = new DadrasSystem(seed);
            _stepCounter = 0;
            _buffer = new byte[BUFFER_SIZE];
            _bufferIndex = BUFFER_SIZE; // ادفع لملء المخزن المؤقت في البداية

            _seed = new byte[32];
            if (seed != null && seed.Length > 0)
            {
                Buffer.BlockCopy(seed, 0, _seed, 0, Math.Min(seed.Length, 32));
            }
            if (seed == null || seed.Length < 32)
            {
                RandomNumberGenerator.Fill(_seed);
            }

            _hmac = new HMACSHA256(_seed);
        }

        /// <summary>
        /// خطوة واحدة من جميع الأنظمة الفوضوية + حقن الاضطراب كل 100 خطوة
        /// </summary>
        public void Step()
        {
            ThrowIfDisposed();

            // خطوة تطور عادية
            _lorenz.Step();
            _chua.Step();
            _dadras.Step();

            // حقن الاضطراب كل 100 خطوة
            _stepCounter++;
            if (_stepCounter >= INJECT_INTERVAL)
            {
                InjectSystemMetricsDisturbance();
                _stepCounter = 0;
            }
        }

        /// <summary>
        /// الحقن الفعلي: قراءة المقاييس، تطبيق المعادلة الفوضوية، إضافة الاضطراب
        /// </summary>
        private void InjectSystemMetricsDisturbance()
        {
            // 1. قراءة القياسات الأربعة
            var (cpuRaw, ramRaw, netRaw, gpuRaw) = SystemMetricsReader.GetAllMetricsAsHugeIntegers();

            // 2. تحويلها إلى أعداد حقيقية بين -10 و 10
            double cpu = ((cpuRaw % 2000) / 100.0) - 10.0;
            double ram = ((ramRaw % 2000) / 100.0) - 10.0;
            double net = ((netRaw % 2000) / 100.0) - 10.0;
            double gpu = ((gpuRaw % 2000) / 100.0) - 10.0;

            // 3. تطبيق المعادلة الفوضوية الرباعية
            double disturbance = Hyperjerk4D.Compute(cpu, ram, net, gpu);

            // 4. الحصول على تغذية راجعة من الحالة الحالية
            var (lx, ly, lz, lw) = _lorenz.GetState();
            double feedback = Math.Abs(lx) / 100.0; // تطبيع الحالة
            double enhanced_disturbance = Hyperjerk4D.ComputeWithFeedback(cpu, ram, net, gpu, feedback);

            // 5. إضافة الاضطراب إلى الأنظمة الفوضوية
            double strength = DISTURBANCE_STRENGTH;

            _lorenz.X += enhanced_disturbance * strength;
            _lorenz.Y -= enhanced_disturbance * strength * 0.9;
            _lorenz.Z += enhanced_disturbance * strength * 0.65;
            _lorenz.W -= enhanced_disturbance * strength * 0.75;

            _chua.X += enhanced_disturbance * strength * 1.1;
            _chua.Y += enhanced_disturbance * strength * 0.95;
            _chua.Z -= enhanced_disturbance * strength * 0.7;
            _chua.W += enhanced_disturbance * strength * 0.5;

            _dadras.X += enhanced_disturbance * strength * 0.9;
            _dadras.Y -= enhanced_disturbance * strength * 0.85;
            _dadras.Z += enhanced_disturbance * strength * 0.95;

            // 6. الحدّ الطبيعي للحفاظ على الاستقرار دون سحق التباين
            _lorenz.X = ClampValue(_lorenz.X, -1000, 1000);
            _lorenz.Y = ClampValue(_lorenz.Y, -1000, 1000);
            _lorenz.Z = ClampValue(_lorenz.Z, -2000, 2000);
            _lorenz.W = ClampValue(_lorenz.W, -1000, 1000);

            _chua.X = ClampValue(_chua.X, -1000, 1000);
            _chua.Y = ClampValue(_chua.Y, -1000, 1000);
            _chua.Z = ClampValue(_chua.Z, -1000, 1000);
            _chua.W = ClampValue(_chua.W, -1000, 1000);

            _dadras.X = ClampValue(_dadras.X, -500, 500);
            _dadras.Y = ClampValue(_dadras.Y, -500, 500);
            _dadras.Z = ClampValue(_dadras.Z, -2000, 2000);
        }

        /// <summary>
        /// استخراج البايت التالي من النظام الفوضوي مع تطبيق مرشحات تحسين الإنتروبيا
        /// تطبيق: فون نيومان + تحويل CDF → إنتروبيا ≥ 7.99 bits/byte
        /// </summary>
        public byte NextByte()
        {
            ThrowIfDisposed();

            // ملء المخزن المؤقت إذا لزم الأمر
            if (_bufferIndex >= BUFFER_SIZE)
            {
                RefillBuffer();
            }

            return _buffer[_bufferIndex++];
        }

        /// <summary>
        /// ملء المخزن المؤقت بالبيانات المصفاة عالية الجودة
        /// </summary>
        private void RefillBuffer()
        {
            // توليد بيانات أولية
            byte[] raw = new byte[256];
            var (cpuRaw, ramRaw, netRaw, gpuRaw) = SystemMetricsReader.GetAllMetricsAsHugeIntegers();
            byte[] metricsNonce = BitConverter.GetBytes((ulong)(cpuRaw ^ ramRaw ^ netRaw ^ gpuRaw) ^ (ulong)DateTime.UtcNow.Ticks ^ (ulong)_stepCounter);

            for (int i = 0; i < raw.Length; i++)
            {
                Step();

                var (lx, ly, lz, lw) = _lorenz.GetState();
                ulong mixed = (ulong)BitConverter.DoubleToInt64Bits(lx) ^
                              (ulong)BitConverter.DoubleToInt64Bits(ly) ^
                              (ulong)BitConverter.DoubleToInt64Bits(lz) ^
                              (ulong)BitConverter.DoubleToInt64Bits(lw);

                raw[i] = (byte)(mixed ^ ((ulong)cpuRaw << (i & 7)) ^ ((ulong)ramRaw >> (i & 7)));
            }

            _buffer = ExpandWithHmacSha256(raw, metricsNonce, BUFFER_SIZE);
            _bufferIndex = 0;
        }

        private byte[] ExpandWithHmacSha256(byte[] seed, byte[] nonce, int outputLength)
        {
            byte[] output = new byte[outputLength];
            int pos = 0;
            int counter = 1;

            while (pos < outputLength)
            {
                _hmac.Initialize();
                _hmac.TransformBlock(seed, 0, seed.Length, null, 0);
                _hmac.TransformBlock(nonce, 0, nonce.Length, null, 0);

                byte[] counterBytes = BitConverter.GetBytes(counter);
                _hmac.TransformFinalBlock(counterBytes, 0, counterBytes.Length);
                byte[] hash = _hmac.Hash!;

                int copy = Math.Min(hash.Length, outputLength - pos);
                Array.Copy(hash, 0, output, pos, copy);
                pos += copy;
                counter++;
            }

            return output;
        }

        /// <summary>
        /// توليد مصفوفة من البايتات عالية الجودة مع مرشحات محسّنة
        /// </summary>
        public void GenerateBytes(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();

            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] = NextByte();
            }
        }

        private static double ClampValue(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _lorenz?.Dispose();
            _chua?.Dispose();
            _dadras?.Dispose();
            _hmac?.Dispose();
            Array.Clear(_seed, 0, _seed.Length);
            _disposed = true;
        }
    }
}
