using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SharpWeb.Utilities
{
    class Impersonator
    {
        // Define required constants
        const int SE_DEBUG_PRIVILEGE = 20;
        const int TOKEN_DUPLICATE = 0x0002;
        const int TOKEN_QUERY = 0x0008;
        const int TOKEN_IMPERSONATE = 0x0004;
        const int TOKEN_ALL_ACCESS = 0xF01FF;

        // Import necessary Windows APIs
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, ref IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool DuplicateToken(IntPtr ExistingTokenHandle, int ImpersonationLevel, ref IntPtr DuplicateTokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool RevertToSelf();

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int RtlAdjustPrivilege(int Privilege, bool Enable, bool CurrentThread, ref bool PreviousValue);

        const int PROCESS_QUERY_INFORMATION = 0x0400;

        private IntPtr lsassHandle;
        private IntPtr duplicatedToken;

        public Impersonator()
        {
            this.lsassHandle = IntPtr.Zero;
            this.duplicatedToken = IntPtr.Zero;
        }

        private void EnablePrivilege()
        {
            bool previousValue = false;
            int ret = RtlAdjustPrivilege(SE_DEBUG_PRIVILEGE, true, false, ref previousValue);
            if (ret != 0)
            {
                throw new System.ComponentModel.Win32Exception(ret);
            }
        }

        private void GetLsassHandle()
        {
            Process[] processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                if (process.ProcessName.ToLower() == "lsass" || process.ProcessName.ToLower() == "system")
                {
                    lsassHandle = OpenProcess(PROCESS_QUERY_INFORMATION, false, (uint)process.Id);
                    if (lsassHandle == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("Failed to open SYSTEM process.");
                    }
                    return;
                }
            }
            throw new InvalidOperationException("SYSTEM process not found.");
        }

        private void GetSystemToken()
        {
            IntPtr tokenHandle = IntPtr.Zero;
            if (!OpenProcessToken(lsassHandle, TOKEN_DUPLICATE | TOKEN_QUERY, ref tokenHandle))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            if (!DuplicateToken(tokenHandle, 2, ref duplicatedToken))  // 2 = SecurityImpersonation level
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            CloseHandle(tokenHandle);
        }

        public void Start()
        {
            EnablePrivilege();
            GetLsassHandle();
            GetSystemToken();

            if (!ImpersonateLoggedOnUser(duplicatedToken))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        public void Close()
        {
            if (duplicatedToken != IntPtr.Zero)
            {
                CloseHandle(duplicatedToken);
            }

            if (lsassHandle != IntPtr.Zero)
            {
                CloseHandle(lsassHandle);
            }
            RevertToSelf();
        }
    }

}
