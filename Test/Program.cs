
using ProcessMemoryScanner;
using System.Diagnostics;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var memory = new MemoryScanner(p => p.ProcessName == "chrome" && IsFlashPlayerProcess(p));
            var memoryRegion = memory.FindMemoryRegion(m =>
            m.State == 0x01000 &&
            m.Protect == 0x4 &&
            m.Type == 0x20000 &&
            m.RegionSize.ToInt32() > 0x3000000)[0];
            memory.ResumeProcess();
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
