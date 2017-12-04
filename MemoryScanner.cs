using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ProcessMemoryScanner
{
    public class MemoryScanner
    {
        private IntPtr handle;
        
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public MemoryScanner(Func<Process, bool> filter)
        {
            var process = Process.GetProcesses().SingleOrDefault(filter);
            if(process == null)
            {
                throw new InvalidOperationException($"Cannot find process!");
            }
            var access = Kernel32.ProcessAccessType.PROCESS_QUERY_INFORMATION |
                                 Kernel32.ProcessAccessType.PROCESS_VM_READ |
                                 Kernel32.ProcessAccessType.PROCESS_VM_WRITE |
                                 Kernel32.ProcessAccessType.PROCESS_VM_OPERATION;
            this.handle = Kernel32.OpenProcess((uint)access, 1, (uint)process.Id);
            if (this.handle == null)
            {
                throw new InvalidOperationException("Cannot open process");
            }
        }
        
        public byte[] ReadMemory(IntPtr address, uint byteArrayLength)
        {
            var lpNumberOfBytesRead = IntPtr.Zero;
            var buffer = new byte[byteArrayLength];
            Kernel32.ReadProcessMemory(this.handle, address, buffer, byteArrayLength, out lpNumberOfBytesRead);
            return buffer;
        }

        public void WriteMemory(IntPtr address, byte[] data)
        {
            var length = data.Length;
            var lpNumberOfBytesWritten = IntPtr.Zero;
            Kernel32.WriteProcessMemory(this.handle, address, data, (uint)data.Length, out lpNumberOfBytesWritten);
        }

        public List<MEMORY_BASIC_INFORMATION> FindMemoryRegion(Func<MEMORY_BASIC_INFORMATION, bool> filter)
        {
            var result = new List<MEMORY_BASIC_INFORMATION>();
            Kernel32.GetSystemInfo(out var systemInformation);
            var minAddress = (long)systemInformation.minimumApplicationAddress;
            var maxAddress = (long)systemInformation.maximumApplicationAddress;
            var memoryDump = new MEMORY_BASIC_INFORMATION();
            var currentAddress = minAddress;
            while (currentAddress < maxAddress)
            {
                var readResult = Kernel32.VirtualQueryEx(this.handle, new IntPtr(currentAddress), out memoryDump, (uint)Marshal.SizeOf(memoryDump));
                if (readResult != 0)
                {
                    if (filter(memoryDump))
                    {
                        result.Add(memoryDump);
                    }
                }
                else
                {
                    break;
                }
                currentAddress += memoryDump.RegionSize.ToInt64();
            }
            return result;
        }

        public IntPtr FindByAoB(byte[] AoBSignature, MEMORY_BASIC_INFORMATION memoryRegion)
        {
            var memory = this.ReadMemory(memoryRegion.BaseAddress, (uint)memoryRegion.RegionSize);
            var memoryOffset = memory.IndexOf(AoBSignature);
            if (memoryOffset == -1)
            {
                return IntPtr.Zero;
            }
            return IntPtr.Add(memoryRegion.BaseAddress, memoryOffset);
        }

        public void ReplaceByAoB(byte[] AoBSignature, byte[] AoBToReplace, MEMORY_BASIC_INFORMATION memoryRegion)
        {
            this.WriteMemory(this.FindByAoB(AoBSignature, memoryRegion), AoBToReplace);
        }
    }
}
