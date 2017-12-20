
using ProcessMemoryScanner;
using System;
using System.Diagnostics;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var memory = new MemoryScanner(p => p.ProcessName == "Tutorial-x86_64");
            var a = memory.ReadMemory<int>(new IntPtr(0x034F5060));
            memory.WriteMemory(new IntPtr(0x034F5060), 200);
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
