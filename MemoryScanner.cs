using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ProcessMemoryScanner
{
    public class MemoryScanner : IDisposable
    {
        private IntPtr handle;
        private Process process;
        
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
            this.process = Process.GetProcesses().SingleOrDefault(filter);
            if(this.process == null)
            {
                throw new InvalidOperationException($"Cannot find process!");
            }
            var access = Kernel32.ProcessAccessType.PROCESS_QUERY_INFORMATION |
                                 Kernel32.ProcessAccessType.PROCESS_VM_READ |
                                 Kernel32.ProcessAccessType.PROCESS_VM_WRITE |
                                 Kernel32.ProcessAccessType.PROCESS_VM_OPERATION;
            this.handle = Kernel32.OpenProcess((uint)access, 1, (uint)this.process.Id);
            if (this.handle == null)
            {
                throw new InvalidOperationException("Cannot open process");
            }
        }

        public T ReadMemory<T>(IntPtr address)
        {
            var length = (uint)Marshal.SizeOf(default(T));
            return (T)Convert.ChangeType(BitConverter.ToInt32(this.ReadMemory(address, length), 0), typeof(T));
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

        public void WriteMemory<T>(IntPtr address, T data)
        {
            var size = Marshal.SizeOf(data);
            var bytes = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, false);
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);
            this.WriteMemory(address, bytes);
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

        public IntPtr FindByAoBWithWildCard(byte?[] AoBSignature, MEMORY_BASIC_INFORMATION memoryRegion)
        {
            var memory = (this.ReadMemory(memoryRegion.BaseAddress, (uint)memoryRegion.RegionSize)).ToWildCardByteArray();
            var memoryOffset = memory.IndexOfWithWildCard(AoBSignature);
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

        public void SuspendProcess()
        {
            foreach (ProcessThread pT in this.process.Threads)
            {
                var pOpenThread = Kernel32.OpenThread(Kernel32.ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }
                Kernel32.SuspendThread(pOpenThread);
                Kernel32.CloseHandle(pOpenThread);
            }
        }

        public void ResumeProcess()
        {
            foreach (ProcessThread pT in this.process.Threads)
            {
                var pOpenThread = Kernel32.OpenThread(Kernel32.ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);
                if (pOpenThread == IntPtr.Zero)
                {
                    continue;
                }
                var suspendCount = 0;
                do
                {
                    suspendCount = Kernel32.ResumeThread(pOpenThread);
                } while (suspendCount > 0);
                Kernel32.CloseHandle(pOpenThread);
            }
        }

        public void Dispose()
        {
            Kernel32.CloseHandle(this.handle);
        }
    }
}
