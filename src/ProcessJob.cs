using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CodexQuotaLite
{
    // Keep any descendants of our short-lived query inside an owned kill-on-close job.
    internal sealed class ProcessJob : IDisposable
    {
        private IntPtr handle;
        private ProcessJob(IntPtr handle) { this.handle = handle; }
        public static ProcessJob TryAttach(Process process)
        {
            IntPtr job = CreateJobObject(IntPtr.Zero, null);
            if (job == IntPtr.Zero) return null;
            var info = new ExtendedInfo();
            info.BasicLimitInformation.LimitFlags = 0x2000;
            int size = Marshal.SizeOf(typeof(ExtendedInfo));
            IntPtr memory = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(info, memory, false);
                if (!SetInformationJobObject(job, 9, memory, (uint)size) || !AssignProcessToJobObject(job, process.Handle))
                { CloseHandle(job); return null; }
                return new ProcessJob(job);
            }
            finally { Marshal.FreeHGlobal(memory); }
        }
        public void Dispose() { if (handle != IntPtr.Zero) { CloseHandle(handle); handle = IntPtr.Zero; } }

        [StructLayout(LayoutKind.Sequential)] private struct BasicInfo
        {
            public long PerProcessUserTimeLimit, PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize, MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass, SchedulingClass;
        }
        [StructLayout(LayoutKind.Sequential)] private struct IoCounters
        { public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount; }
        [StructLayout(LayoutKind.Sequential)] private struct ExtendedInfo
        {
            public BasicInfo BasicLimitInformation;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit, JobMemoryLimit, PeakProcessMemoryUsed, PeakJobMemoryUsed;
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr CreateJobObject(IntPtr attributes, string name);
        [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetInformationJobObject(IntPtr job, int informationClass, IntPtr info, uint length);
        [DllImport("kernel32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
        [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseHandle(IntPtr handle);
    }
}
