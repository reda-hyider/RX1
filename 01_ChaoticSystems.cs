// ═══════════════════════════════════════════════════════════════════
// 01_ChaoticSystems.cs - جميع الأنظمة الفوضوية مدمجة
// يشمل: Lorenz 4D, Chua 4D, Dadras 3D
// ═══════════════════════════════════════════════════════════════════

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace CosmicDragon.ChaosSystems
{
    #region HyperchaosLorenz4D - نظام لورنز الفوضوي 4D

    /// <summary>
    /// نظام لورنز فوضوي (4D) - أقوى من 3D
    /// 2 أسس ليابونوف موجبة (hyperchaotic)
    /// </summary>
    public sealed class HyperchaosLorenz4D : IDisposable
    {
        // ثوابت النظام
        private const double A = 10.0;      // σ (sigma)
        private const double B = 8.0 / 3.0; // β (beta)
        private const double C = 28.0;      // ρ (rho)
        private const double D = -1.0;      // معامل البُعد الرابع
        private const double DT = 0.02;    // خطوة زمنية أكبر لتقليل عدد خطوات RK4 لكل بايت

        // الحالة الداخلية (4 متغيرات)
        private double _x, _y, _z, _w;
        private double _px, _py, _pz, _pw;
        private long _mix;
        private int _iterations;
        private bool _disposed;

        public HyperchaosLorenz4D(byte[]? seed = null)
        {
            byte[] safeSeed = MakeSafeSeed(seed);
            InitFromSeed(safeSeed);
            
            // تسخين 500 خطوة
            for (int i = 0; i < 500; i++)
                AdvanceRK4();
        }

        private static byte[] MakeSafeSeed(byte[]? seed)
        {
            byte[] result = new byte[32];
            if (seed != null && seed.Length > 0)
            {
                Buffer.BlockCopy(seed, 0, result, 0, Math.Min(seed.Length, 32));
                if (seed.Length < 32)
                {
                    unchecked
                    {
                        long fill = (long)0xA5A5A5A5A5A5A5A5UL;
                        for (int i = 0; i < seed.Length; i++)
                            fill = fill * 31 + seed[i];
                        for (int i = seed.Length; i < 32; i++)
                        {
                            fill = HyperchaosLorenz4D.SplitMix64(fill);
                            result[i] = (byte)(fill >> 33);
                        }
                    }
                }
            }
            else
            {
                RandomNumberGenerator.Fill(result);
            }
            return result;
        }

        private void InitFromSeed(byte[] seed)
        {
            long s1 = 0, s2 = 0, s3 = 0, s4 = 0;

            for (int i = 0; i < 8 && i < seed.Length; i++)
                s1 |= ((long)(uint)seed[i]) << (i * 8);
            for (int i = 8; i < 16 && i < seed.Length; i++)
                s2 |= ((long)(uint)seed[i]) << ((i - 8) * 8);
            for (int i = 16; i < 24 && i < seed.Length; i++)
                s3 |= ((long)(uint)seed[i]) << ((i - 16) * 8);
            for (int i = 24; i < 32 && i < seed.Length; i++)
                s4 |= ((long)(uint)seed[i]) << ((i - 24) * 8);

            s1 = HyperchaosLorenz4D.SplitMix64(s1);
            s2 = HyperchaosLorenz4D.SplitMix64(s2);
            s3 = HyperchaosLorenz4D.SplitMix64(s3);
            s4 = HyperchaosLorenz4D.SplitMix64(s4);

            _mix = s1 ^ s2 ^ s3 ^ s4;

            // تهيئة الحالة الأولية
            _x = 3.0 + ((uint)(s1 & 0xFFFF)) / 4096.0;
            _y = 3.0 + ((uint)(s2 & 0xFFFF)) / 3276.0;
            _z = 5.0 + ((uint)(s3 & 0xFFFF)) / 1820.0;
            _w = 1.0 + ((uint)(s4 & 0xFFFF)) / 2048.0;

            if ((s1 & 0x10000) != 0) _x = -_x;
            if ((s2 & 0x10000) != 0) _y = -_y;
            if ((s3 & 0x10000) != 0) _w = -_w;

            _px = _x; _py = _y; _pz = _z; _pw = _w;
            _iterations = 0;
        }

        /// <summary>
        /// المعادلات التفاضلية:
        /// dx/dt = A(y - x) + w
        /// dy/dt = Cx - y - xz
        /// dz/dt = xy - Bz
        /// dw/dt = -yz + D*w
        /// </summary>
        private void AdvanceRK4()
        {
            double mw; // Declare mw without initialization

            // K1
            double k1x = A * (_y - _x) + _w;
            double k1y = C * _x - _y - _x * _z;
            double k1z = _x * _y - B * _z;
            double k1w = -_y * _z + D * _w;
            mw = _w + k1w * DT * 0.5; // Assign mw

            // K2
            double mx = _x + k1x * DT * 0.5;
            double my = _y + k1y * DT * 0.5;
            double mz = _z + k1z * DT * 0.5;
            mw = _w + k1w * DT * 0.5;

            double k2x = A * (my - mx) + mw;
            double k2y = C * mx - my - mx * mz;
            double k2z = mx * my - B * mz;
            double k2w = -my * mz + D * mw;

            // K3
            mx = _x + k2x * DT * 0.5;
            my = _y + k2y * DT * 0.5;
            mz = _z + k2z * DT * 0.5;
            mw = _w + k2w * DT * 0.5;

            double k3x = A * (my - mx) + mw;
            double k3y = C * mx - my - mx * mz;
            double k3z = mx * my - B * mz;
            double k3w = -my * mz + D * mw;

            // K4
            mx = _x + k3x * DT;
            my = _y + k3y * DT;
            mz = _z + k3z * DT;
            mw = _w + k3w * DT;

            double k4x = A * (my - mx) + mw;
            double k4y = C * mx - my - mx * mz;
            double k4z = mx * my - B * mz;
            double k4w = -my * mz + D * mw;

            // تحديث
            _x += DT / 6.0 * (k1x + 2 * k2x + 2 * k3x + k4x);
            _y += DT / 6.0 * (k1y + 2 * k2y + 2 * k3y + k4y);
            _z += DT / 6.0 * (k1z + 2 * k2z + 2 * k3z + k4z);
            _w += DT / 6.0 * (k1w + 2 * k2w + 2 * k3w + k4w);

            _iterations++;
            SafetyCheck();
        }

        private void SafetyCheck()
        {
            bool bad = double.IsNaN(_x) || double.IsNaN(_y) || double.IsNaN(_z) || double.IsNaN(_w)
                    || double.IsInfinity(_x) || double.IsInfinity(_y) || double.IsInfinity(_z) || double.IsInfinity(_w)
                    || Math.Abs(_x) > 1e6 || Math.Abs(_y) > 1e6 || Math.Abs(_z) > 1e6 || Math.Abs(_w) > 1e6
                    || _z > 200 || _z < -10;

            if (_iterations % 50 == 0 && _iterations > 0)
            {
                if (Math.Abs(_x - _px) < 0.0001 && Math.Abs(_y - _py) < 0.0001 &&
                    Math.Abs(_z - _pz) < 0.0001 && Math.Abs(_w - _pw) < 0.0001)
                    bad = true;
                _px = _x; _py = _y; _pz = _z; _pw = _w;
            }

            if (bad) Kick();
        }

        private void Kick()
        {
            // توليد قيم حتمية بالكامل من الحالة الحالية
            long rv1 = _mix ^ BitConverter.DoubleToInt64Bits(_x) 
                            ^ BitConverter.DoubleToInt64Bits(_y) 
                            ^ BitConverter.DoubleToInt64Bits(_z) 
                            ^ BitConverter.DoubleToInt64Bits(_w);
            
            // خلط القيم باستخدام SplitMix64
            rv1 = SplitMix64(rv1);
            long rv2 = SplitMix64(rv1);
            rv1 = SplitMix64(rv2);

            // إعادة تهيئة المتغيرات بقيم مشتقة من rv1, rv2
            _x = 5.0 + ((uint)(rv1 & 0xFFFF)) / 4369.0;
            _y = 5.0 + ((uint)((rv1 >> 16) & 0xFFFF)) / 3276.0;
            _z = 10.0 + ((uint)((rv1 >> 32) & 0xFFFF)) / 1820.0;
            _w = 1.0 + ((uint)(rv2 & 0xFFFF)) / 2048.0;
            
            if ((rv1 & (1L << 48)) != 0) _x = -_x;
            if ((rv1 & (1L << 49)) != 0) _y = -_y;
            if ((rv2 & (1L << 48)) != 0) _w = -_w;

            // استقرار سريع (200 خطوة) – حتمي لأن المعادلات حتمية
            for (int i = 0; i < 200; i++)
            {
                double dx = A * (_y - _x) + _w;
                double dy = C * _x - _y - _x * _z;
                double dz = _x * _y - B * _z;
                double dw = -_y * _z + D * _w;
                
                _x += dx * DT;
                _y += dy * DT;
                _z += dz * DT;
                _w += dw * DT;
            }
        }

        public byte[] ExtractBytes(int count)
        {
            byte[] result = new byte[count];
            int offset = 0;

            while (offset < count)
            {
                AdvanceRK4();

                long xb = BitConverter.DoubleToInt64Bits(_x);
                long yb = BitConverter.DoubleToInt64Bits(_y);
                long zb = BitConverter.DoubleToInt64Bits(_z);
                long wb = BitConverter.DoubleToInt64Bits(_w);

                _mix ^= xb ^ RotL(yb, 17) ^ RotL(zb, 37) ^ RotL(wb, 23);
                _mix = SplitMix64(_mix);

                int toCopy = Math.Min(8, count - offset);
                for (int i = 0; i < toCopy; i++)
                    result[offset++] = (byte)(_mix >> (i * 8));
            }

            return result;
        }

        public double X
        {
            get => _x;
            set => _x = value;
        }
        public double Y
        {
            get => _y;
            set => _y = value;
        }
        public double Z
        {
            get => _z;
            set => _z = value;
        }
        public double W
        {
            get => _w;
            set => _w = value;
        }

        /// <summary>
        /// استخراج متغير بناءً على الفهرس
        /// Extract variable by index (0=X, 1=Y, 2=Z, 3=W)
        /// </summary>
        public double ExtractDouble(int index)
        {
            return index switch
            {
                0 => _x,
                1 => _y,
                2 => _z,
                3 => _w,
                _ => _x
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long RotL(long v, int b) =>
            (long)(((ulong)v << (b & 63)) | ((ulong)v >> (64 - (b & 63))));

        internal static long SplitMix64(long x)
        {
            unchecked
            {
                x += (long)0x9E3779B97F4A7C15UL;
                x = (x ^ (x >> 30)) * (long)0xBF58476D1CE4E5B9UL;
                x = x ^ (x >> 27);
                x = (x ^ (x >> 31)) * (long)0x94D049BB133111EBUL;
                return x ^ (x >> 33);
            }
        }

        /// <summary>
        /// الحصول على الحالة الحالية (للعمل مع الطبقات المتقدمة)
        /// </summary>
        public (double X, double Y, double Z, double W) GetState()
        {
            return (_x, _y, _z, _w);
        }

        /// <summary>
        /// تطبيق اضطراب على الحالة (للتغذية العكسية المتشابكة)
        /// </summary>
        public void ApplyPerturbation(double dx, double dy, double dz)
        {
            _x += dx;
            _y += dy;
            _z += dz;
            AdvanceRK4(); // خطوة واحدة للأمام بعد الاضطراب
        }

        /// <summary>
        /// خطوة واحدة للأمام في النظام
        /// </summary>
        public (double X, double Y, double Z, double W) Step()
        {
            AdvanceRK4();
            return (_x, _y, _z, _w);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _x = 0; _y = 0; _z = 0; _w = 0; _mix = 0;
                _disposed = true;
            }
        }
    }

    #endregion

    #region HyperchaosChua4D - نظام Chua الفوضوي 4D

    /// <summary>
    /// نظام Chua المعدّل الفوضوي (4D)
    /// Double-scroll attractor مع غير خطية قطعية
    /// </summary>
    public sealed class HyperchaosChua4D : IDisposable
    {
        // معاملات النظام
        private const double ALPHA = 15.6;   // معامل الاقتران
        public const double BETA = 28.0;    // معامل التأثير
        public const double GAMMA = 0.0;    // معامل اللزوج
        private const double M0 = -0.5;      // منحدر الأول
        private const double M1 = -0.8;      // منحدر الثاني
        private const double DT = 0.01;     // خطوة زمنية أكبر لتقليل تكلفة تكامل RK4

        // الحالة الداخلية
        private double _x, _y, _z, _w;
        private double _px, _py, _pz, _pw;
        private long _mix;
        private int _iterations;
        private bool _disposed;

        public HyperchaosChua4D(byte[]? seed = null)
        {
            byte[] safeSeed = MakeSafeSeed(seed);
            InitFromSeed(safeSeed);
            
            for (int i = 0; i < 500; i++)
                AdvanceRK4();
        }

        private static byte[] MakeSafeSeed(byte[]? seed)
        {
            byte[] result = new byte[32];
            if (seed != null && seed.Length > 0)
            {
                Buffer.BlockCopy(seed, 0, result, 0, Math.Min(seed.Length, 32));
                if (seed.Length < 32)
                {
                    unchecked
                    {
                        long fill = (long)0xDEADBEEFDEADBEEFUL;
                        for (int i = 0; i < seed.Length; i++)
                            fill = fill * 31 + seed[i];
                        for (int i = seed.Length; i < 32; i++)
                        {
                            fill = HyperchaosLorenz4D.SplitMix64(fill);
                            result[i] = (byte)(fill >> 33);
                        }
                    }
                }
            }
            else
            {
                RandomNumberGenerator.Fill(result);
            }
            return result;
        }

        private void InitFromSeed(byte[] seed)
        {
            long s1 = 0, s2 = 0, s3 = 0, s4 = 0;

            for (int i = 0; i < 8 && i < seed.Length; i++)
                s1 |= ((long)(uint)seed[i]) << (i * 8);
            for (int i = 8; i < 16 && i < seed.Length; i++)
                s2 |= ((long)(uint)seed[i]) << ((i - 8) * 8);
            for (int i = 16; i < 24 && i < seed.Length; i++)
                s3 |= ((long)(uint)seed[i]) << ((i - 16) * 8);
            for (int i = 24; i < 32 && i < seed.Length; i++)
                s4 |= ((long)(uint)seed[i]) << ((i - 24) * 8);

            s1 = HyperchaosLorenz4D.SplitMix64(s1);
            s2 = HyperchaosLorenz4D.SplitMix64(s2);
            s3 = HyperchaosLorenz4D.SplitMix64(s3);
            s4 = HyperchaosLorenz4D.SplitMix64(s4);

            _mix = s1 ^ s2 ^ s3 ^ s4;

            _x = 1.0 + ((uint)(s1 & 0xFFFF)) / 8192.0;
            _y = 0.5 + ((uint)(s2 & 0xFFFF)) / 6553.0;
            _z = 0.2 + ((uint)(s3 & 0xFFFF)) / 4096.0;
            _w = 0.1 + ((uint)(s4 & 0xFFFF)) / 8192.0;
            
            if ((s1 & 0x10000) != 0) _x = -_x;
            if ((s2 & 0x10000) != 0) _y = -_y;
            if ((s3 & 0x10000) != 0) _z = -_z;

            _px = _x; _py = _y; _pz = _z; _pw = _w;
            _iterations = 0;
        }

        /// <summary>
        /// دالة Chua اللاخطية القطعية
        /// f(x) = M1*x + 0.5*(M0 - M1)*(|x+1| - |x-1|)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double ChuaNonlinearity(double x)
        {
            double abs_x_p1 = Math.Abs(x + 1.0);
            double abs_x_m1 = Math.Abs(x - 1.0);
            return M1 * x + 0.5 * (M0 - M1) * (abs_x_p1 - abs_x_m1);
        }

        /// <summary>
        /// المعادلات التفاضلية:
        /// dx/dt = α(y - x - f(x)) + w
        /// dy/dt = x - y + z
        /// dz/dt = -β*y
        /// dw/dt = γ*x + δ*w
        /// </summary>
        private void AdvanceRK4()
        {
            double fx = ChuaNonlinearity(_x);

            // K1
            double k1x = ALPHA * (_y - _x - fx) + _w;
            double k1y = _x - _y + _z;
            double k1z = -BETA * _y;
            double k1w = GAMMA * _x - 0.5 * _w;

            // K2
            double mx = _x + k1x * DT * 0.5;
            double my = _y + k1y * DT * 0.5;
            double mz = _z + k1z * DT * 0.5;
            double mw = _w + k1w * DT * 0.5;

            double fmx = ChuaNonlinearity(mx);
            double k2x = ALPHA * (my - mx - fmx) + mw;
            double k2y = mx - my + mz;
            double k2z = -BETA * my;
            double k2w = GAMMA * mx - 0.5 * mw;

            // K3
            mx = _x + k2x * DT * 0.5;
            my = _y + k2y * DT * 0.5;
            mz = _z + k2z * DT * 0.5;
            mw = _w + k2w * DT * 0.5;

            fmx = ChuaNonlinearity(mx);
            double k3x = ALPHA * (my - mx - fmx) + mw;
            double k3y = mx - my + mz;
            double k3z = -BETA * my;
            double k3w = GAMMA * mx - 0.5 * mw;

            // K4
            mx = _x + k3x * DT;
            my = _y + k3y * DT;
            mz = _z + k3z * DT;
            mw = _w + k3w * DT;

            fmx = ChuaNonlinearity(mx);
            double k4x = ALPHA * (my - mx - fmx) + mw;
            double k4y = mx - my + mz;
            double k4z = -BETA * my;
            double k4w = GAMMA * mx - 0.5 * mw;

            // تحديث
            _x += DT / 6.0 * (k1x + 2 * k2x + 2 * k3x + k4x);
            _y += DT / 6.0 * (k1y + 2 * k2y + 2 * k3y + k4y);
            _z += DT / 6.0 * (k1z + 2 * k2z + 2 * k3z + k4z);
            _w += DT / 6.0 * (k1w + 2 * k2w + 2 * k3w + k4w);

            _iterations++;
            SafetyCheck();
        }

        private void SafetyCheck()
        {
            bool bad = double.IsNaN(_x) || double.IsNaN(_y) || double.IsNaN(_z) || double.IsNaN(_w)
                    || double.IsInfinity(_x) || double.IsInfinity(_y) || double.IsInfinity(_z) || double.IsInfinity(_w)
                    || Math.Abs(_x) > 1e6 || Math.Abs(_y) > 1e6 || Math.Abs(_z) > 1e6 || Math.Abs(_w) > 1e6;

            if (_iterations % 50 == 0 && _iterations > 0)
            {
                if (Math.Abs(_x - _px) < 0.00001 && Math.Abs(_y - _py) < 0.00001)
                    bad = true;
                _px = _x; _py = _y; _pz = _z; _pw = _w;
            }

            if (bad) Kick();
        }

        private void Kick()
        {
            // توليد قيم حتمية بالكامل من الحالة الحالية
            long rv1 = _mix ^ BitConverter.DoubleToInt64Bits(_x) 
                            ^ BitConverter.DoubleToInt64Bits(_y) 
                            ^ BitConverter.DoubleToInt64Bits(_z) 
                            ^ BitConverter.DoubleToInt64Bits(_w);
            
            rv1 = HyperchaosLorenz4D.SplitMix64(rv1);
            long rv2 = HyperchaosLorenz4D.SplitMix64(rv1);
            rv1 = HyperchaosLorenz4D.SplitMix64(rv2);

            _x = 2.0 + ((uint)(rv1 & 0xFFFF)) / 4096.0;
            _y = 1.0 + ((uint)((rv1 >> 16) & 0xFFFF)) / 6553.0;
            _z = 0.5 + ((uint)((rv1 >> 32) & 0xFFFF)) / 8192.0;
            _w = 0.1 + ((uint)(rv2 & 0xFFFF)) / 4096.0;
            
            if ((rv1 & (1L << 48)) != 0) _x = -_x;
            if ((rv2 & (1L << 48)) != 0) _y = -_y;
            if ((rv2 & (1L << 49)) != 0) _z = -_z;

            for (int i = 0; i < 200; i++)
                AdvanceRK4();
        }

        public byte[] ExtractBytes(int count)
        {
            byte[] result = new byte[count];
            int offset = 0;

            while (offset < count)
            {
                AdvanceRK4();

                long xb = BitConverter.DoubleToInt64Bits(_x);
                long yb = BitConverter.DoubleToInt64Bits(_y);
                long zb = BitConverter.DoubleToInt64Bits(_z);
                long wb = BitConverter.DoubleToInt64Bits(_w);

                _mix ^= xb ^ RotL(yb, 19) ^ RotL(zb, 41) ^ RotL(wb, 29);
                _mix = HyperchaosLorenz4D.SplitMix64(_mix);

                int toCopy = Math.Min(8, count - offset);
                for (int i = 0; i < toCopy; i++)
                    result[offset++] = (byte)(_mix >> (i * 8));
            }

            return result;
        }

        public double X
        {
            get => _x;
            set => _x = value;
        }
        public double Y
        {
            get => _y;
            set => _y = value;
        }
        public double Z
        {
            get => _z;
            set => _z = value;
        }
        public double W
        {
            get => _w;
            set => _w = value;
        }

        /// <summary>
        /// استخراج متغير بناءً على الفهرس
        /// Extract variable by index (0=X, 1=Y, 2=Z, 3=W)
        /// </summary>
        public double ExtractDouble(int index)
        {
            return index switch
            {
                0 => _x,
                1 => _y,
                2 => _z,
                3 => _w,
                _ => _y
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long RotL(long v, int b) =>
            (long)(((ulong)v << (b & 63)) | ((ulong)v >> (64 - (b & 63))));

        /// <summary>
        /// Get current state (for advanced layers)
        /// </summary>
        public (double X, double Y, double Z, double W) GetState()
        {
            return (_x, _y, _z, _w);
        }

        /// <summary>
        /// Apply perturbation to state (for entangled feedback)
        /// </summary>
        public void ApplyPerturbation(double dx, double dy, double dz)
        {
            _x += dx;
            _y += dy;
            _z += dz;
            AdvanceRK4();
        }

        /// <summary>
        /// Single step forward in the system
        /// </summary>
        public (double X, double Y, double Z, double W) Step()
        {
            AdvanceRK4();
            return (_x, _y, _z, _w);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _x = 0; _y = 0; _z = 0; _w = 0; _mix = 0;
                _disposed = true;
            }
        }
    }

    #endregion

    #region DadrasSystem - نظام Dadras الفوضوي 3D

    /// <summary>
    /// نظام Dadras الفوضوي (3D)
    /// معادلات مشهورة ومثبتة فوضوياً
    /// </summary>
    public sealed class DadrasSystem : IDisposable
    {
        // معاملات Dadras الكلاسيكية
        private const double A = 3.0;
        private const double B = 2.7;
        private const double C = 1.7;
        private const double D = 2.0;
        private const double E = 9.0;
        private const double DT = 0.01;

        // الحالة الداخلية
        private double _x, _y, _z;
        private double _px, _py, _pz;
        private long _mix;
        private int _iterations;
        private bool _disposed;

        public DadrasSystem(byte[]? seed = null)
        {
            byte[] safeSeed = MakeSafeSeed(seed);
            InitFromSeed(safeSeed);
            
            for (int i = 0; i < 300; i++)
                AdvanceRK4();
        }

        private static byte[] MakeSafeSeed(byte[]? seed)
        {
            byte[] result = new byte[32];
            if (seed != null && seed.Length > 0)
            {
                Buffer.BlockCopy(seed, 0, result, 0, Math.Min(seed.Length, 32));
                if (seed.Length < 32)
                {
                    unchecked
                    {
                        long fill = (long)0xDEADBEEFDEADBEEFUL;
                        for (int i = 0; i < seed.Length; i++)
                            fill = fill * 31 + seed[i];
                        for (int i = seed.Length; i < 32; i++)
                        {
                            fill = HyperchaosLorenz4D.SplitMix64(fill);
                            result[i] = (byte)(fill >> 33);
                        }
                    }
                }
            }
            else
            {
                RandomNumberGenerator.Fill(result);
            }
            return result;
        }

        private void InitFromSeed(byte[] seed)
        {
            long s1 = 0, s2 = 0, s3 = 0;

            for (int i = 0; i < 8 && i < seed.Length; i++)
                s1 |= ((long)(uint)seed[i]) << (i * 8);
            for (int i = 8; i < 16 && i < seed.Length; i++)
                s2 |= ((long)(uint)seed[i]) << ((i - 8) * 8);
            for (int i = 16; i < 24 && i < seed.Length; i++)
                s3 |= ((long)(uint)seed[i]) << ((i - 16) * 8);

            s1 = HyperchaosLorenz4D.SplitMix64(s1);
            s2 = HyperchaosLorenz4D.SplitMix64(s2);
            s3 = HyperchaosLorenz4D.SplitMix64(s3);

            _mix = s1 ^ s2 ^ s3;

            _x = 1.0 + ((uint)(s1 & 0xFFFF)) / 4096.0;
            _y = 1.0 + ((uint)(s2 & 0xFFFF)) / 4096.0;
            _z = 1.0 + ((uint)(s3 & 0xFFFF)) / 4096.0;

            if ((s1 & 0x10000) != 0) _x = -_x;
            if ((s2 & 0x10000) != 0) _y = -_y;
            if ((s3 & 0x10000) != 0) _z = -_z;

            _px = _x; _py = _y; _pz = _z;
            _iterations = 0;
        }

        /// <summary>
        /// معادلات Dadras:
        /// dx/dt = y - a*x + b*y*z
        /// dy/dt = c*y - x*z + z
        /// dz/dt = d*x*y - e*z
        /// </summary>
        private void AdvanceRK4()
        {
            // K1
            double k1x = _y - A * _x + B * _y * _z;
            double k1y = C * _y - _x * _z + _z;
            double k1z = D * _x * _y - E * _z;

            // K2
            double mx = _x + k1x * DT * 0.5;
            double my = _y + k1y * DT * 0.5;
            double mz = _z + k1z * DT * 0.5;

            double k2x = my - A * mx + B * my * mz;
            double k2y = mx - my + mz;
            double k2z = D * mx * my - E * mz;

            // K3
            mx = _x + k2x * DT * 0.5;
            my = _y + k2y * DT * 0.5;
            mz = _z + k2z * DT * 0.5;

            double k3x = my - A * mx + B * my * mz;
            double k3y = mx - my + mz;
            double k3z = D * mx * my - E * mz;

            // K4
            mx = _x + k3x * DT;
            my = _y + k3y * DT;
            mz = _z + k3z * DT;

            double k4x = my - A * mx + B * my * mz;
            double k4y = mx - my + mz;
            double k4z = D * mx * my - E * mz;

            // تحديث
            _x += DT / 6.0 * (k1x + 2 * k2x + 2 * k3x + k4x);
            _y += DT / 6.0 * (k1y + 2 * k2y + 2 * k3y + k4y);
            _z += DT / 6.0 * (k1z + 2 * k2z + 2 * k3z + k4z);

            _iterations++;
            SafetyCheck();
        }

        private void SafetyCheck()
        {
            bool bad = double.IsNaN(_x) || double.IsNaN(_y) || double.IsNaN(_z)
                    || double.IsInfinity(_x) || double.IsInfinity(_y) || double.IsInfinity(_z)
                    || Math.Abs(_x) > 1e6 || Math.Abs(_y) > 1e6 || Math.Abs(_z) > 1e6;

            if (_iterations % 50 == 0 && _iterations > 0)
            {
                if (Math.Abs(_x - _px) < 0.0001 && Math.Abs(_y - _py) < 0.0001 && Math.Abs(_z - _pz) < 0.0001)
                    bad = true;
                _px = _x; _py = _y; _pz = _z;
            }

            if (bad) Kick();
        }

        private void Kick()
        {
            long rv = _mix ^ BitConverter.DoubleToInt64Bits(_x) 
                       ^ BitConverter.DoubleToInt64Bits(_y) 
                       ^ BitConverter.DoubleToInt64Bits(_z);
            rv = HyperchaosLorenz4D.SplitMix64(rv);

            _x = 2.0 + ((uint)(rv & 0xFFFF)) / 4096.0;
            _y = 2.0 + ((uint)((rv >> 16) & 0xFFFF)) / 4096.0;
            _z = 2.0 + ((uint)((rv >> 32) & 0xFFFF)) / 4096.0;
            
            if ((rv & (1L << 48)) != 0) _x = -_x;
            if ((rv & (1L << 49)) != 0) _y = -_y;

            for (int i = 0; i < 150; i++)
                AdvanceRK4();
        }

        public byte[] ExtractBytes(int count)
        {
            byte[] result = new byte[count];
            int offset = 0;

            while (offset < count)
            {
                AdvanceRK4();

                long xb = BitConverter.DoubleToInt64Bits(_x);
                long yb = BitConverter.DoubleToInt64Bits(_y);
                long zb = BitConverter.DoubleToInt64Bits(_z);

                _mix ^= xb ^ RotL(yb, 21) ^ RotL(zb, 43);
                _mix = HyperchaosLorenz4D.SplitMix64(_mix);

                int toCopy = Math.Min(8, count - offset);
                for (int i = 0; i < toCopy; i++)
                    result[offset++] = (byte)(_mix >> (i * 8));
            }

            return result;
        }

        public double X
        {
            get => _x;
            set => _x = value;
        }
        public double Y
        {
            get => _y;
            set => _y = value;
        }
        public double Z
        {
            get => _z;
            set => _z = value;
        }

        /// <summary>
        /// استخراج متغير بناءً على الفهرس
        /// Extract variable by index (0=X, 1=Y, 2=Z)
        /// </summary>
        public double ExtractDouble(int index)
        {
            return index switch
            {
                0 => _x,
                1 => _y,
                2 => _z,
                _ => _z
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long RotL(long v, int b) =>
            (long)(((ulong)v << (b & 63)) | ((ulong)v >> (64 - (b & 63))));

        /// <summary>
        /// Get current state (for advanced layers)
        /// </summary>
        public (double X, double Y, double Z) GetState()
        {
            return (_x, _y, _z);
        }

        /// <summary>
        /// Apply perturbation to state (for entangled feedback)
        /// </summary>
        public void ApplyPerturbation(double dx, double dy, double dz)
        {
            _x += dx;
            _y += dy;
            _z += dz;
            AdvanceRK4();
        }

        /// <summary>
        /// Single step forward in the system
        /// </summary>
        public (double X, double Y, double Z) Step()
        {
            AdvanceRK4();
            return (_x, _y, _z);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _x = 0; _y = 0; _z = 0; _mix = 0;
                _disposed = true;
            }
        }
    }

    #endregion

    #region RX313R - نظام موحد Cascaded Fusion

    /// <summary>
    /// RX313R - نظام موحد يدمج Lorenz4D, Chua4D, و Dadras3D
    /// باستخدام Cascaded Fusion لتحسين الفوضى
    /// </summary>
    public sealed class RX313R : IDisposable
    {
        private HyperchaosLorenz4D _lorenz;
        private HyperchaosChua4D _chua;
        private DadrasSystem _dadras;
        private long _mix;
        private bool _disposed;

        public RX313R(byte[]? seed = null)
        {
            _lorenz = new HyperchaosLorenz4D(seed);
            _chua = new HyperchaosChua4D(seed);
            _dadras = new DadrasSystem(seed);

            // تسخين
            for (int i = 0; i < 1000; i++)
            {
                _lorenz.Step();
                _chua.Step();
                _dadras.Step();
                ApplyCascadedFusion();
            }
        }

        /// <summary>
        /// تطبيق دمج متسلسل (Cascaded Fusion) باستخدام المعادلات غير الخطية
        /// </summary>
        private void ApplyCascadedFusion()
        {
            double Lx = _lorenz.X, Ly = _lorenz.Y, Lz = _lorenz.Z, Lw = _lorenz.W;
            double Cx = _chua.X, Cy = _chua.Y, Cz = _chua.Z, Cw = _chua.W;
            double Dx = _dadras.X, Dy = _dadras.Y, Dz = _dadras.Z;

            // تحديث Lorenz
            _lorenz.X = (Lx * 0.9) + (Cy * 0.05) - (Dz * 0.05) + Math.Sin(Lz * Cx);
            _lorenz.Y = (Ly * 0.8) + (Cz * 0.1) + (Dx * 0.1) + Math.Cos(Lw * Dy);
            _lorenz.Z = (Lz * 0.7) + (Cw * 0.15) + (Dy * 0.15) + Math.Tan(Lx * Dx) * 0.1;
            _lorenz.W = (Lw * 0.6) + (Cx * 0.2) + (Dz * 0.2) + (Lx * Ly * 0.01);

            // تحديث Chua
            _chua.X = (Cx * 0.9) + (Ly * 0.05) - (Dx * 0.05) + Math.Exp(Math.Min(Lz * 0.001, 10));
            _chua.Y = (Cy * 0.8) + (Lz * 0.1) + (Dy * 0.1) + Math.Sqrt(Math.Abs(Lw * Cx)) * 0.1;
            _chua.Z = (Cz * 0.7) + (Lw * 0.15) + (Dz * 0.15) + Math.Log(Math.Abs(Lx * Dy) + 1);
            _chua.W = (Cw * 0.6) + (Lx * 0.2) + (Dx * 0.2) + (Cy * Dy * 0.01);

            // تحديث Dadras
            _dadras.X = (Dx * 0.9) + (Lz * 0.05) - (Cw * 0.05) + Math.Sin(Ly * Cy);
            _dadras.Y = (Dy * 0.8) + (Lw * 0.1) + (Cx * 0.1) + Math.Cos(Lz * Cz);
            _dadras.Z = (Dz * 0.7) + (Lx * 0.15) + (Cy * 0.15) + Math.Tan(Ly * Cw) * 0.1;

            // تطبيق الحدود لمنع الانفلات
            _lorenz.X = Clamp(_lorenz.X, -100, 100);
            _lorenz.Y = Clamp(_lorenz.Y, -100, 100);
            _lorenz.Z = Clamp(_lorenz.Z, -200, 200);
            _lorenz.W = Clamp(_lorenz.W, -100, 100);
            
            _chua.X = Clamp(_chua.X, -100, 100);
            _chua.Y = Clamp(_chua.Y, -100, 100);
            _chua.Z = Clamp(_chua.Z, -200, 200);
            _chua.W = Clamp(_chua.W, -50, 50);
            
            _dadras.X = Clamp(_dadras.X, -50, 50);
            _dadras.Y = Clamp(_dadras.Y, -50, 50);
            _dadras.Z = Clamp(_dadras.Z, -50, 50);
        }

        /// <summary>
        /// دالة تحديد القيمة ضمن نطاق محدد
        /// </summary>
        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// تنفيذ خطوة واحدة من النظام الموحد
        /// </summary>
        public void Step()
        {
            _lorenz.Step();
            _chua.Step();
            _dadras.Step();
            ApplyCascadedFusion();
        }

        /// <summary>
        /// استخراج بايتات عشوائية من حالة النظام
        /// </summary>
        public byte[] ExtractBytes(int count)
        {
            byte[] result = new byte[count];
            int offset = 0;

            while (offset < count)
            {
                Step();

                // جمع الحالة من الأنظمة الثلاثة (11 double = 88 bytes)
                byte[] stateBytes = new byte[11 * 8];
                Buffer.BlockCopy(BitConverter.GetBytes(_lorenz.X), 0, stateBytes, 0, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(_lorenz.Y), 0, stateBytes, 8, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(_lorenz.Z), 0, stateBytes, 16, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(_lorenz.W), 0, stateBytes, 24, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(_chua.X), 0, stateBytes, 32, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(_chua.Y), 0, stateBytes, 40, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(_chua.Z), 0, stateBytes, 48, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(_chua.W), 0, stateBytes, 56, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(_dadras.X), 0, stateBytes, 64, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(_dadras.Y), 0, stateBytes, 72, 8);
                Buffer.BlockCopy(BitConverter.GetBytes(_dadras.Z), 0, stateBytes, 80, 8);

                // مزج باستخدام SHA512
                using (var sha = System.Security.Cryptography.SHA512.Create())
                {
                    byte[] hash = sha.ComputeHash(stateBytes);
                    _mix ^= BitConverter.ToInt64(hash, 0) ^ BitConverter.ToInt64(hash, 8);
                    _mix = HyperchaosLorenz4D.SplitMix64(_mix);
                }

                // استخراج البايتات
                int toCopy = Math.Min(8, count - offset);
                for (int i = 0; i < toCopy; i++)
                    result[offset++] = (byte)(_mix >> (i * 8));
            }

            return result;
        }

        public double X => _lorenz.X;
        public double Y => _lorenz.Y;
        public double Z => _lorenz.Z;
        public double W => _lorenz.W;

        /// <summary>
        /// Get current state from primary system (for advanced layers)
        /// </summary>
        public (double X, double Y, double Z, double W) GetState()
        {
            return (_lorenz.X, _lorenz.Y, _lorenz.Z, _lorenz.W);
        }

        /// <summary>
        /// Apply perturbation through primary system (for entangled feedback)
        /// </summary>
        public void ApplyPerturbation(double dx, double dy, double dz)
        {
            _lorenz.ApplyPerturbation(dx, dy, dz);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _lorenz?.Dispose();
                _chua?.Dispose();
                _dadras?.Dispose();
                _mix = 0;
                _disposed = true;
            }
        }
    }

    #endregion
}
