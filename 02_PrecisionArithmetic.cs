// ═══════════════════════════════════════════════════════════════════
// 02_PrecisionArithmetic.cs - جميع أنظمة الحسابات الدقيقة
// دمج FixedPointLorenzSystem + UltraPrecisionFixed
// ═══════════════════════════════════════════════════════════════════

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace RX313Dragon
{
    /// <summary>
    /// حالة النظام الفوضوي (للحفظ والاستعادة)
    /// </summary>
    public struct ChaosSystemState
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    #region UltraPrecisionFixed - نظام الدقة الثابتة 128 بت

    /// <summary>
    /// نظام الدقة الثابتة 128 بت - إصلاح شامل
    /// يستخدم BigInteger للعمليات الحسابية الدقيقة
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct UltraPrecisionFixed : IEquatable<UltraPrecisionFixed>, IComparable<UltraPrecisionFixed>
    {
        private const int FRACTIONAL_BITS = 48;
        private const long SCALE = 1L << FRACTIONAL_BITS; // 2^48
        private const long HALF_SCALE = SCALE >> 1;
        
        private readonly long _value;

        // ثوابت رياضية محسوبة مسبقاً
        private static readonly long PI_RAW = 884279719003555L;
        private static readonly long E_RAW = 765280049042824L;
        private static readonly long ONE_RAW = SCALE;
        private static readonly long HALF_RAW = HALF_SCALE;

        public static readonly UltraPrecisionFixed Zero = new UltraPrecisionFixed(0L, true);
        public static readonly UltraPrecisionFixed One = new UltraPrecisionFixed(ONE_RAW, true);
        public static readonly UltraPrecisionFixed Half = new UltraPrecisionFixed(HALF_RAW, true);
        public static readonly UltraPrecisionFixed Pi = new UltraPrecisionFixed(PI_RAW, true);
        public static readonly UltraPrecisionFixed E = new UltraPrecisionFixed(E_RAW, true);
        public static readonly UltraPrecisionFixed MinValue = new UltraPrecisionFixed(long.MinValue, true);
        public static readonly UltraPrecisionFixed MaxValue = new UltraPrecisionFixed(long.MaxValue, true);

        private UltraPrecisionFixed(long rawValue, bool isRaw) => _value = rawValue;
        public UltraPrecisionFixed(int value) => _value = (long)value << FRACTIONAL_BITS;
        
        public UltraPrecisionFixed(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) { _value = 0; return; }
            const double maxVal = (double)(long.MaxValue >> 1) / SCALE;
            if (value > maxVal) value = maxVal;
            if (value < -maxVal) value = -maxVal;
            _value = (long)(value * SCALE);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UltraPrecisionFixed FromRaw(long rawValue) => new UltraPrecisionFixed(rawValue, true);

        // ======== عمليات حسابية مُصلحة ========

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UltraPrecisionFixed operator +(UltraPrecisionFixed a, UltraPrecisionFixed b)
        {
            long result = a._value + b._value;
            if (((a._value ^ result) & (b._value ^ result)) < 0)
                result = a._value > 0 ? long.MaxValue : long.MinValue;
            return new UltraPrecisionFixed(result, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UltraPrecisionFixed operator -(UltraPrecisionFixed a, UltraPrecisionFixed b)
        {
            long result = a._value - b._value;
            if (((a._value ^ b._value) & (a._value ^ result)) < 0)
                result = a._value > 0 ? long.MaxValue : long.MinValue;
            return new UltraPrecisionFixed(result, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UltraPrecisionFixed operator -(UltraPrecisionFixed a) => 
            a._value == long.MinValue ? MaxValue : new UltraPrecisionFixed(-a._value, true);

        // الضرب المُصلح - ضرب 128 بت صحيح!
        public static UltraPrecisionFixed operator *(UltraPrecisionFixed a, UltraPrecisionFixed b)
        {
            long aVal = a._value;
            long bVal = b._value;
            bool negative = (aVal < 0) != (bVal < 0);
            
            ulong absA = (ulong)Math.Abs(aVal);
            ulong absB = (ulong)Math.Abs(bVal);
            
            ulong aHi = absA >> 32;
            ulong aLo = absA & 0xFFFFFFFF;
            ulong bHi = absB >> 32;
            ulong bLo = absB & 0xFFFFFFFF;
            
            ulong ll = aLo * bLo;
            ulong lh = aLo * bHi;
            ulong hl = aHi * bLo;
            ulong hh = aHi * bHi;
            
            ulong mid = lh + hl;
            bool midOverflow = mid < lh;
            
            ulong lo = ll + (mid << 32);
            bool loOverflow = lo < ll;
            
            ulong hi = hh + (mid >> 32) + (midOverflow ? (1UL << 32) : 0) + (loOverflow ? 1UL : 0);
            
            ulong result128 = (hi << (64 - FRACTIONAL_BITS)) | (lo >> FRACTIONAL_BITS);
            
            if (result128 > (ulong)long.MaxValue)
                return negative ? MinValue : MaxValue;
            
            long result = (long)result128;
            if (negative) result = -result;
            return new UltraPrecisionFixed(result, true);
        }

        public static UltraPrecisionFixed operator /(UltraPrecisionFixed a, UltraPrecisionFixed b)
        {
            if (b._value == 0) return Zero;
            
            long aVal = a._value;
            long bVal = b._value;
            bool negative = (aVal < 0) != (bVal < 0);
            
            BigInteger bigA = new BigInteger(Math.Abs(aVal));
            BigInteger bigB = new BigInteger(Math.Abs(bVal));
            BigInteger bigScale = new BigInteger(SCALE);
            BigInteger result = (bigA * bigScale) / bigB;
            
            if (result > long.MaxValue)
                return negative ? MinValue : MaxValue;
            
            long resultLong = (long)result;
            if (negative) resultLong = -resultLong;
            return new UltraPrecisionFixed(resultLong, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UltraPrecisionFixed operator %(UltraPrecisionFixed a, UltraPrecisionFixed b) =>
            b._value == 0 ? Zero : new UltraPrecisionFixed(a._value % b._value, true);

        // ======== عمليات المقارنة ========
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UltraPrecisionFixed a, UltraPrecisionFixed b) => a._value == b._value;
        public static bool operator !=(UltraPrecisionFixed a, UltraPrecisionFixed b) => a._value != b._value;
        public static bool operator <(UltraPrecisionFixed a, UltraPrecisionFixed b) => a._value < b._value;
        public static bool operator >(UltraPrecisionFixed a, UltraPrecisionFixed b) => a._value > b._value;
        public static bool operator <=(UltraPrecisionFixed a, UltraPrecisionFixed b) => a._value <= b._value;
        public static bool operator >=(UltraPrecisionFixed a, UltraPrecisionFixed b) => a._value >= b._value;

        // ======== دوال رياضية ========

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UltraPrecisionFixed Abs(UltraPrecisionFixed x) =>
            x._value == long.MinValue ? MaxValue : new UltraPrecisionFixed(Math.Abs(x._value), true);

        public static UltraPrecisionFixed Sqrt(UltraPrecisionFixed x)
        {
            if (x._value <= 0) return Zero;
            BigInteger bigVal = new BigInteger(x._value);
            BigInteger bigScale = new BigInteger(SCALE);
            BigInteger scaled = bigVal * bigScale;
            BigInteger guess = BigIntegerSqrt(scaled);
            if (guess > long.MaxValue) return MaxValue;
            return new UltraPrecisionFixed((long)guess, true);
        }

        private static BigInteger BigIntegerSqrt(BigInteger n)
        {
            if (n < 0) return BigInteger.Zero;
            if (n == 0) return BigInteger.Zero;
            if (n < 4) return BigInteger.One;
            
            int bitLength = (int)n.GetBitLength();
            BigInteger guess = BigInteger.One << ((bitLength + 1) / 2);
            BigInteger lastGuess;
            int maxIterations = 100;
            
            do
            {
                lastGuess = guess;
                guess = (guess + n / guess) >> 1;
                maxIterations--;
            } while (guess != lastGuess && maxIterations > 0);
            
            return guess;
        }

        public static UltraPrecisionFixed Floor(UltraPrecisionFixed x)
        {
            long mask = ~(SCALE - 1);
            long result = x._value & mask;
            if (x._value < 0 && (x._value & (SCALE - 1)) != 0)
                result -= SCALE;
            return new UltraPrecisionFixed(result, true);
        }

        public static UltraPrecisionFixed Clamp(UltraPrecisionFixed value, UltraPrecisionFixed min, UltraPrecisionFixed max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        // ======== التحويلات ========

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ToDouble() => (double)_value / SCALE;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToInt() => (int)(_value >> FRACTIONAL_BITS);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetRawValue() => _value;

        public byte[] ToBytes() => BitConverter.GetBytes(_value);

        public static implicit operator UltraPrecisionFixed(int value) => new UltraPrecisionFixed(value);
        public static implicit operator UltraPrecisionFixed(double value) => new UltraPrecisionFixed(value);
        public static explicit operator double(UltraPrecisionFixed value) => value.ToDouble();
        public static explicit operator int(UltraPrecisionFixed value) => value.ToInt();

        public bool Equals(UltraPrecisionFixed other) => _value == other._value;
        public override bool Equals(object? obj) => obj is UltraPrecisionFixed other && Equals(other);
        public override int GetHashCode() => _value.GetHashCode();
        public int CompareTo(UltraPrecisionFixed other) => _value.CompareTo(other._value);
        
        public override string ToString() => ToDouble().ToString("F4");
        public string ToString(string format) => ToDouble().ToString(format);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsZero() => _value == 0;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNegative() => _value < 0;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFinite() => _value != long.MaxValue && _value != long.MinValue;
    }

    #endregion

    #region FixedPointLorenzSystem - نظام لورنز بالحسابات الثابتة

    /// <summary>
    /// نظام لورنز المُصلح - محرك ومحولات دقة في ملف واحد
    /// </summary>
    public class FixedPointLorenzSystem : IDisposable
    {
        // ═══════════════════════════════════════
        // ثوابت الإصلاح
        // ═══════════════════════════════════════
        
        private const double SIGMA = 10.0;
        private const double RHO = 28.0;
        private const double BETA = 8.0 / 3.0;
        private const double DT = 0.005;

        // الحالة الداخلية (double precision - سريع وموثوق)
        private double _x, _y, _z;
        private double _px, _py, _pz;
        private long _mix;
        private int _n;
        private bool _disposed;

        // ═══════════════════════════════════════
        // المُنشئات
        // ═══════════════════════════════════════

        /// <summary>
        /// إنشاء من بذرة عشوائية
        /// </summary>
        public FixedPointLorenzSystem() : this(null)
        {
        }

        /// <summary>
        /// إنشاء من بذرة عشوائية
        /// </summary>
        public FixedPointLorenzSystem(byte[]? seed)
        {
            byte[] safeSeed = MakeSafeSeed(seed ?? new byte[32]);
            InitFromSeed(safeSeed);
            
            // تسخين 500 خطوة
            for (int i = 0; i < 500; i++)
                AdvanceRK4();
        }

        /// <summary>
        /// إنشاء بمعاملات (للتوافقية مع الكود القديم)
        /// </summary>
        public FixedPointLorenzSystem(double sigma = 10.0, double rho = 28.0, double beta = 8.0 / 3.0, double dt = 0.005)
        {
            byte[] seed = new byte[32];
            RandomNumberGenerator.Fill(seed);
            byte[] safeSeed = MakeSafeSeed(seed);
            InitFromSeed(safeSeed);
            
            for (int i = 0; i < 500; i++)
                AdvanceRK4();
        }

        private static byte[] MakeSafeSeed(byte[] seed)
        {
            byte[] result = new byte[32];

            if (seed != null && seed.Length > 0)
            {
                Buffer.BlockCopy(seed, 0, result, 0, Math.Min(seed.Length, 32));
                
                if (seed.Length < 32)
                {
                    long fill = 0x12345678ABCDEF01L;
                    for (int i = 0; i < seed.Length; i++)
                        fill = fill * 31 + seed[i];
                    
                    for (int i = seed.Length; i < 32; i++)
                    {
                        fill = SM64(fill);
                        result[i] = (byte)(fill >> 33);
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

            for (int i = 0; i < 8; i++)
                s1 |= ((long)seed[i]) << (i * 8);
            for (int i = 0; i < 8; i++)
                s2 |= ((long)seed[8 + i]) << (i * 8);
            for (int i = 0; i < 8; i++)
                s3 |= ((long)seed[16 + i]) << (i * 8);

            s1 = SM64(s1);
            s2 = SM64(s2);
            s3 = SM64(s3);

            _mix = s1 ^ s2 ^ s3;

            // حالة أولية بعيدة عن النقاط الثابتة
            _x = 3.0 + ((uint)(s1 & 0xFFFF)) / 4096.0;
            _y = 3.0 + ((uint)(s2 & 0xFFFF)) / 3276.0;
            _z = 5.0 + ((uint)(s3 & 0xFFFF)) / 1820.0;

            if ((s1 & 0x10000) != 0) _x = -_x;
            if ((s2 & 0x10000) != 0) _y = -_y;

            _px = _x; _py = _y; _pz = _z;
            _n = 0;
        }

        // ═══════════════════════════════════════
        // RK4 - الحل العددي الدقيق
        // ═══════════════════════════════════════

        private void AdvanceRK4()
        {
            double k1x = SIGMA * (_y - _x);
            double k1y = _x * (RHO - _z) - _y;
            double k1z = _x * _y - BETA * _z;

            double mx = _x + k1x * DT * 0.5;
            double my = _y + k1y * DT * 0.5;
            double mz = _z + k1z * DT * 0.5;

            double k2x = SIGMA * (my - mx);
            double k2y = mx * (RHO - mz) - my;
            double k2z = mx * my - BETA * mz;

            mx = _x + k2x * DT * 0.5;
            my = _y + k2y * DT * 0.5;
            mz = _z + k2z * DT * 0.5;

            double k3x = SIGMA * (my - mx);
            double k3y = mx * (RHO - mz) - my;
            double k3z = mx * my - BETA * mz;

            mx = _x + k3x * DT;
            my = _y + k3y * DT;
            mz = _z + k3z * DT;

            double k4x = SIGMA * (my - mx);
            double k4y = mx * (RHO - mz) - my;
            double k4z = mx * my - BETA * mz;

            _x += DT / 6.0 * (k1x + 2 * k2x + 2 * k3x + k4x);
            _y += DT / 6.0 * (k1y + 2 * k2y + 2 * k3y + k4y);
            _z += DT / 6.0 * (k1z + 2 * k2z + 2 * k3z + k4z);

            _n++;
            SafetyCheck();
        }

        // ═══════════════════════════════════════
        // حماية من الحالات المرضية
        // ═══════════════════════════════════════

        private void SafetyCheck()
        {
            bool bad = double.IsNaN(_x) || double.IsNaN(_y) || double.IsNaN(_z)
                    || double.IsInfinity(_x) || double.IsInfinity(_y) || double.IsInfinity(_z)
                    || Math.Abs(_x) > 100 || Math.Abs(_y) > 100
                    || _z > 200 || _z < -10;

            if (_n % 50 == 0 && _n > 0)
            {
                if (Math.Abs(_x - _px) < 0.0001 &&
                    Math.Abs(_y - _py) < 0.0001 &&
                    Math.Abs(_z - _pz) < 0.0001)
                    bad = true;
                _px = _x; _py = _y; _pz = _z;
            }

            if (Math.Abs(_x) < 0.5 && Math.Abs(_y) < 0.5 && Math.Abs(_z) < 0.5)
                bad = true;

            if (bad) Kick();
        }

        private void Kick()
        {
            byte[] r = new byte[8];
            RandomNumberGenerator.Fill(r);
            long rv = BitConverter.ToInt64(r, 0);
            rv = SM64(rv);

            _x = 5.0 + ((uint)(rv & 0xFFFF)) / 4369.0;
            _y = 5.0 + ((uint)((rv >> 16) & 0xFFFF)) / 3276.0;
            _z = 10.0 + ((uint)((rv >> 32) & 0xFFFF)) / 1820.0;
            if ((rv & (1L << 48)) != 0) _x = -_x;
            if ((rv & (1L << 49)) != 0) _y = -_y;

            for (int i = 0; i < 200; i++)
            {
                double dx = SIGMA * (_y - _x);
                double dy = _x * (RHO - _z) - _y;
                double dz = _x * _y - BETA * _z;
                _x += dx * DT; _y += dy * DT; _z += dz * DT;
            }
        }

        // ═══════════════════════════════════════
        // الواجهة العامة
        // ═══════════════════════════════════════

        /// <summary>
        /// Step - يُحدّث ويُرجع القيم الجديدة (الإصلاح الأساسي!)
        /// </summary>
        public (UltraPrecisionFixed X, UltraPrecisionFixed Y, UltraPrecisionFixed Z) Step()
        {
            AdvanceRK4();
            return (
                new UltraPrecisionFixed(_x),
                new UltraPrecisionFixed(_y),
                new UltraPrecisionFixed(_z)
            );
        }

        /// <summary>
        /// CoupledStep - نفس Step (للتوافقية)
        /// </summary>
        public (UltraPrecisionFixed X, UltraPrecisionFixed Y, UltraPrecisionFixed Z) CoupledStep()
        {
            return Step();
        }

        /// <summary>
        /// MultiStep - عدة خطوات
        /// </summary>
        public (UltraPrecisionFixed X, UltraPrecisionFixed Y, UltraPrecisionFixed Z) MultiStep(int steps)
        {
            for (int i = 0; i < steps; i++)
                AdvanceRK4();
            return (
                new UltraPrecisionFixed(_x),
                new UltraPrecisionFixed(_y),
                new UltraPrecisionFixed(_z)
            );
        }

        /// <summary>
        /// استخراج بايتات عالية الجودة
        /// </summary>
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

                _mix ^= xb ^ RotL(yb, 17) ^ RotL(zb, 37);
                _mix = SM64(_mix);

                int toCopy = Math.Min(8, count - offset);
                for (int i = 0; i < toCopy; i++)
                    result[offset++] = (byte)(_mix >> (i * 8));
            }

            return result;
        }

        // ═══════════════════════════════════════
        // الخصائص
        // ═══════════════════════════════════════

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

        public UltraPrecisionFixed XFixed => new UltraPrecisionFixed(_x);
        public UltraPrecisionFixed YFixed => new UltraPrecisionFixed(_y);
        public UltraPrecisionFixed ZFixed => new UltraPrecisionFixed(_z);

        public double XDouble => _x;
        public double YDouble => _y;
        public double ZDouble => _z;

        // حفظ واستعادة
        public ChaosSystemState SaveState() => new ChaosSystemState
        {
            X = _x,
            Y = _y,
            Z = _z
        };

        public void RestoreState(ChaosSystemState state)
        {
            _x = state.X;
            _y = state.Y;
            _z = state.Z;
        }

        public void Reset()
        {
            _x = 0.1; _y = 0.1; _z = 0.1;
        }

        // ═══════════════════════════════════════
        // أدوات مساعدة
        // ═══════════════════════════════════════

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long RotL(long v, int b) =>
            (long)(((ulong)v << (b & 63)) | ((ulong)v >> (64 - (b & 63))));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long SM64(long s)
        {
            unchecked
            {
                s += (long)0x9E3779B97F4A7C15UL;
                s ^= (long)((ulong)s >> 30);
                s *= (long)0xBF58476D1CE4E5B9UL;
                s ^= (long)((ulong)s >> 27);
                s *= (long)0x94D049BB133111EBUL;
                s ^= (long)((ulong)s >> 31);
                return s;
            }
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
}
