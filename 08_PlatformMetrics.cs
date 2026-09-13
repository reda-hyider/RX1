// ═══════════════════════════════════════════════════════════════════════════════
// 08_PlatformMetrics.cs
// دعم متعدد المنصات: Windows, Linux, macOS
// قراءة مقاييس النظام بطريقة متوافقة مع جميع الأنظمة الشهيرة
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace RX313Dragon.AdvancedSecurity
{
    /// <summary>
    /// واجهة موحدة لقراءة مقاييس النظام على أنظمة تشغيل مختلفة
    /// </summary>
    public static class PlatformMetrics
    {
        /// <summary>
        /// الحصول على جميع المقاييس بطريقة متعددة المنصات
        /// </summary>
        public static (long cpu, long ram, long net, long gpu) GetAllMetricsAuto()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return WindowsPlatform.GetAllMetrics();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return LinuxPlatform.GetAllMetrics();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return MacOSPlatform.GetAllMetrics();
            else
                return FallbackPlatform.GetAllMetrics();
        }

        /// <summary>
        /// الحصول على اسم نظام التشغيل الحالي
        /// </summary>
        public static string GetOSName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "Windows";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "Linux";
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "macOS";
            else
                return "Unknown";
        }
    }

    /// <summary>
    /// قراءة مقاييس Windows (الطريقة الأصلية)
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class WindowsPlatform
    {
        private static PerformanceCounter? _cpuCounter;
        private static PerformanceCounter? _interruptsCounter;

        static WindowsPlatform()
        {
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                _cpuCounter.NextValue();

                _interruptsCounter = new PerformanceCounter("Processor", "Interrupts/sec", "_Total", true);
                _interruptsCounter.NextValue();
            }
            catch
            {
                _cpuCounter = null;
                _interruptsCounter = null;
            }
        }

        public static (long cpu, long ram, long net, long gpu) GetAllMetrics()
        {
            long cpu = GetCpuUsage();
            long ram = GetRamUsage();
            long net = GetNetworkUsage();
            long gpu = GetGpuUsage();

            return (cpu, ram, net, gpu);
        }

        private static long GetCpuUsage()
        {
            if (_cpuCounter == null)
                return FallbackPlatform.GetCpuUsage();

            try
            {
                double sum = 0;
                for (int i = 0; i < 3; i++)
                {
                    sum += _cpuCounter.NextValue();
                    System.Threading.Thread.Sleep(1);
                }

                double cpuPercent = sum / 3.0;
                return (long)(cpuPercent * 1_000_000_000.0);
            }
            catch
            {
                return FallbackPlatform.GetCpuUsage();
            }
        }

        private static long GetRamUsage()
        {
            try
            {
                using (var proc = Process.GetCurrentProcess())
                {
                    return proc.WorkingSet64;
                }
            }
            catch { return 0; }
        }

        private static long GetNetworkUsage()
        {
            try
            {
                long total = 0;
                foreach (var ni in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                        ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    {
                        total += ni.GetIPv4Statistics().BytesReceived;
                        total += ni.GetIPv4Statistics().BytesSent;
                    }
                }
                return total;
            }
            catch { return 0; }
        }

        private static long GetGpuUsage()
        {
            if (_interruptsCounter == null)
                return FallbackPlatform.GetGpuUsage();

            try
            {
                double interrupts = _interruptsCounter.NextValue();
                return (long)(interrupts * 10_000_000.0);
            }
            catch
            {
                return FallbackPlatform.GetGpuUsage();
            }
        }
    }

    /// <summary>
    /// قراءة مقاييس Linux (من /proc)
    /// </summary>
    internal static class LinuxPlatform
    {
        public static (long cpu, long ram, long net, long gpu) GetAllMetrics()
        {
            long cpu = GetCpuUsage();
            long ram = GetRamUsage();
            long net = GetNetworkUsage();
            long gpu = GetInterrupts();

            return (cpu, ram, net, gpu);
        }

        private static long GetCpuUsage()
        {
            try
            {
                // قراءة /proc/stat
                var lines = File.ReadAllLines("/proc/stat");
                var firstLine = lines[0]; // cpu line

                var parts = firstLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5) return FallbackPlatform.GetCpuUsage();

                long user = long.Parse(parts[1]);
                long system = long.Parse(parts[3]);
                long idle = long.Parse(parts[4]);
                long total = user + system + idle;

                // النسبة المئوية × مليار
                if (total == 0) return FallbackPlatform.GetCpuUsage();
                double cpuPercent = ((user + system) * 100.0) / total;
                return (long)(cpuPercent * 10_000_000.0);
            }
            catch
            {
                return FallbackPlatform.GetCpuUsage();
            }
        }

        private static long GetRamUsage()
        {
            try
            {
                // قراءة /proc/self/status
                var lines = File.ReadAllLines("/proc/self/status");

                foreach (var line in lines)
                {
                    if (line.StartsWith("VmRSS:"))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && long.TryParse(parts[1], out long kb))
                        {
                            return kb * 1024; // تحويل من KB إلى bytes
                        }
                    }
                }

                return FallbackPlatform.GetRamUsage();
            }
            catch
            {
                return FallbackPlatform.GetRamUsage();
            }
        }

        private static long GetNetworkUsage()
        {
            try
            {
                long total = 0;

                // قراءة /proc/net/dev
                var lines = File.ReadAllLines("/proc/net/dev");

                foreach (var line in lines)
                {
                    // تخطي الرؤوس والـ loopback
                    if (line.Contains("lo") || line.Contains("face")) continue;

                    var parts = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        var stats = parts[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (stats.Length >= 10)
                        {
                            if (long.TryParse(stats[0], out long recv) && long.TryParse(stats[8], out long sent))
                            {
                                total += recv + sent;
                            }
                        }
                    }
                }

                return total;
            }
            catch
            {
                return FallbackPlatform.GetNetworkUsage();
            }
        }

        private static long GetInterrupts()
        {
            try
            {
                // قراءة /proc/interrupts أو /proc/stat
                var lines = File.ReadAllLines("/proc/stat");

                foreach (var line in lines)
                {
                    if (line.StartsWith("intr"))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1 && long.TryParse(parts[1], out long interrupts))
                        {
                            return interrupts % 1_000_000_000;
                        }
                    }
                }

                return FallbackPlatform.GetGpuUsage();
            }
            catch
            {
                return FallbackPlatform.GetGpuUsage();
            }
        }
    }

    /// <summary>
    /// قراءة مقاييس macOS
    /// </summary>
    internal static class MacOSPlatform
    {
        public static (long cpu, long ram, long net, long gpu) GetAllMetrics()
        {
            long cpu = GetCpuUsage();
            long ram = GetRamUsage();
            long net = GetNetworkUsage();
            long gpu = GetProcessInfo();

            return (cpu, ram, net, gpu);
        }

        private static long GetCpuUsage()
        {
            try
            {
                // استخدام top أو ps على macOS
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "-c \"ps aux | grep -i 'consoleapp1' | grep -v grep | awk '{print $3}'\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        var output = proc.StandardOutput.ReadToEnd().Trim();
                        if (double.TryParse(output, out double cpuPercent))
                        {
                            return (long)(cpuPercent * 10_000_000.0);
                        }
                    }
                }

                return FallbackPlatform.GetCpuUsage();
            }
            catch
            {
                return FallbackPlatform.GetCpuUsage();
            }
        }

        private static long GetRamUsage()
        {
            try
            {
                using (var proc = Process.GetCurrentProcess())
                {
                    // على macOS، GetTotalMemory قد يكون أكثر دقة
                    return GC.GetTotalMemory(false);
                }
            }
            catch { return FallbackPlatform.GetRamUsage(); }
        }

        private static long GetNetworkUsage()
        {
            try
            {
                // macOS: استخدام netstat
                var psi = new ProcessStartInfo
                {
                    FileName = "/usr/sbin/netstat",
                    Arguments = "-ibnd",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var proc = Process.Start(psi))
                {
                    if (proc != null)
                    {
                        long total = 0;
                        string? line;
                        var reader = proc.StandardOutput;

                        while ((line = reader.ReadLine()) != null)
                        {
                            if (line.Contains("lo")) continue; // تخطي loopback

                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 10)
                            {
                                if (long.TryParse(parts[6], out long ibytes) &&
                                    long.TryParse(parts[9], out long obytes))
                                {
                                    total += ibytes + obytes;
                                }
                            }
                        }

                        return total;
                    }
                }

                return FallbackPlatform.GetNetworkUsage();
            }
            catch
            {
                return FallbackPlatform.GetNetworkUsage();
            }
        }

        private static long GetProcessInfo()
        {
            try
            {
                using (var proc = Process.GetCurrentProcess())
                {
                    long threadCount = proc.Threads.Count;
                    long handles = proc.HandleCount;
                    return (threadCount * handles) % 1_000_000_000L;
                }
            }
            catch { return FallbackPlatform.GetGpuUsage(); }
        }
    }

    /// <summary>
    /// نسخة بديلة: استخدام Stopwatch والعمليات الأساسية
    /// تعمل على أي منصة بدون تبعيات خاصة
    /// </summary>
    internal static class FallbackPlatform
    {
        public static (long cpu, long ram, long net, long gpu) GetAllMetrics()
        {
            long cpu = GetCpuUsage();
            long ram = GetRamUsage();
            long net = GetNetworkUsage();
            long gpu = GetGpuUsage();

            return (cpu, ram, net, gpu);
        }

        public static long GetCpuUsage()
        {
            // استخدام عداد Stopwatch عالي الدقة
            return (Stopwatch.GetTimestamp() % 1_000_000_000L) * 100;
        }

        public static long GetRamUsage()
        {
            return GC.GetTotalMemory(false);
        }

        public static long GetNetworkUsage()
        {
            // استخدام عداد التوقيت كبديل
            return (Stopwatch.GetTimestamp() ^ Environment.TickCount64) % 10_000_000_000L;
        }

        public static long GetGpuUsage()
        {
            try
            {
                using (var proc = Process.GetCurrentProcess())
                {
                    long threadCount = proc.Threads.Count;
                    long handles = proc.HandleCount;
                    return (threadCount * handles) % 1_000_000_000L;
                }
            }
            catch
            {
                return Stopwatch.GetTimestamp() % 1_000_000_000L;
            }
        }
    }
}
