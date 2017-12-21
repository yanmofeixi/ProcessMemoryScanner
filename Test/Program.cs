
using ProcessMemoryScanner;
using System;
using System.Diagnostics;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var memory = new MemoryScanner(p => p.ProcessName == "Zombidle");
            var a = memory.ReadMemory<double>(new IntPtr(0x14B18878 - 0x8));
        }

        static bool IsFlashPlayerProcess(Process process)
        {
            for (var i = 0; i < process.Modules.Count; i++)
            {
                if (!process.HasExited)
                {
                    if (process.Modules[i].ModuleName.Contains("flash"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
