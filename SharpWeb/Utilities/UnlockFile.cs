using System;
using System.IO;
using System.Collections.Generic;

using System.Runtime.InteropServices;

namespace SharpWeb.Utilities
{
    internal class UnlockFile
    {
        //add from to https://github.com/qwqdanchun/Pillager/blob/main/Pillager/Helper/LockedFile.cs
        public static byte[] ReadLockedFile(string fileName)
        {
            try
            {
                int pid = GetProcessIDByFileName(fileName)[0];
                IntPtr hfile = DuplicateHandleByFileName(pid, fileName);
                var oldFilePointer = Natives.SetFilePointer(hfile, 0, 0, 1);
                int size = Natives.SetFilePointer(hfile, 0, 0, 2);
                byte[] fileBuffer = new byte[size];
                IntPtr hProcess = Natives.OpenProcess(Enums.PROCESS_ACCESS_FLAGS.PROCESS_SUSPEND_RESUME, false, pid);
                Natives.NtSuspendProcess(hProcess);
                Natives.SetFilePointer(hfile, 0, 0, 0);
                Natives.ReadFile(hfile, fileBuffer, (uint)size, out _, IntPtr.Zero);
                Natives.SetFilePointer(hfile, oldFilePointer, 0, 0);
                Natives.CloseHandle(hfile);
                Natives.NtResumeProcess(hProcess);
                Natives.CloseHandle(hProcess);
                return fileBuffer;
            }
            catch { return null; }
        }

        public static List<Struct.SYSTEM_HANDLE_INFORMATION> GetHandles(int pid)
        {
            List<Struct.SYSTEM_HANDLE_INFORMATION> aHandles = new List<Struct.SYSTEM_HANDLE_INFORMATION>();
            int handle_info_size = Marshal.SizeOf(new Struct.SYSTEM_HANDLE_INFORMATION()) * 20000;
            IntPtr ptrHandleData = IntPtr.Zero;
            try
            {
                ptrHandleData = Marshal.AllocHGlobal(handle_info_size);
                int nLength = 0;

                while (Natives.NtQuerySystemInformation(Natives.CNST_SYSTEM_HANDLE_INFORMATION, ptrHandleData, handle_info_size, ref nLength) == Natives.STATUS_INFO_LENGTH_MISMATCH)
                {
                    handle_info_size = nLength;
                    Marshal.FreeHGlobal(ptrHandleData);
                    ptrHandleData = Marshal.AllocHGlobal(nLength);
                }
                if (IntPtr.Size == 8)
                {
                    int handle_count = Marshal.ReadIntPtr(ptrHandleData).ToInt32();
                    IntPtr ptrHandleItem = new IntPtr(ptrHandleData.ToInt64() + IntPtr.Size);

                    for (long lIndex = 0; lIndex < handle_count; lIndex++)
                    {
                        Struct.SYSTEM_HANDLE_INFORMATION oSystemHandleInfo = new Struct.SYSTEM_HANDLE_INFORMATION();
                        oSystemHandleInfo = (Struct.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ptrHandleItem, oSystemHandleInfo.GetType());
                        ptrHandleItem = new IntPtr(ptrHandleItem.ToInt64() + Marshal.SizeOf(oSystemHandleInfo.GetType()));
                        if (oSystemHandleInfo.ProcessID != pid) { continue; }
                        aHandles.Add(oSystemHandleInfo);
                    }
                }
                else
                {
                    int handle_count = Marshal.ReadIntPtr(ptrHandleData).ToInt32();
                    IntPtr ptrHandleItem = new IntPtr(ptrHandleData.ToInt32() + IntPtr.Size);

                    for (long lIndex = 0; lIndex < handle_count; lIndex++)
                    {
                        Struct.SYSTEM_HANDLE_INFORMATION oSystemHandleInfo = new Struct.SYSTEM_HANDLE_INFORMATION();
                        oSystemHandleInfo = (Struct.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(ptrHandleItem, oSystemHandleInfo.GetType());
                        ptrHandleItem = new IntPtr(ptrHandleItem.ToInt32() + Marshal.SizeOf(new Struct.SYSTEM_HANDLE_INFORMATION()));
                        if (oSystemHandleInfo.ProcessID != pid) { continue; }
                        aHandles.Add(oSystemHandleInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Marshal.FreeHGlobal(ptrHandleData);
            }
            return aHandles;
        }

        private static string TryGetName(IntPtr Handle)
        {
            Struct.IO_STATUS_BLOCK status = new Struct.IO_STATUS_BLOCK();
            uint bufferSize = 1024;
            var bufferPtr = Marshal.AllocHGlobal((int)bufferSize);
            Natives.NtQueryInformationFile(Handle, ref status, bufferPtr, bufferSize, Enums.FILE_INFORMATION_CLASS.FileNameInformation);
            var nameInfo = (Struct.FileNameInformation)Marshal.PtrToStructure(bufferPtr, typeof(Struct.FileNameInformation));
            if (nameInfo.NameLength > bufferSize || nameInfo.NameLength <= 0)
            {
                return null;
            }
            return Marshal.PtrToStringUni(new IntPtr((IntPtr.Size == 8 ? bufferPtr.ToInt64() : bufferPtr.ToInt32()) + 4), nameInfo.NameLength / 2);
        }

        public static IntPtr FindHandleByFileName(Struct.SYSTEM_HANDLE_INFORMATION systemHandleInformation, string filename, IntPtr processHandle)
        {
            IntPtr openProcessHandle = processHandle;
            try
            {
                if (!Natives.DuplicateHandle(openProcessHandle, new IntPtr(systemHandleInformation.Handle), Natives.GetCurrentProcess(), out var ipHandle, 0, false, Natives.DUPLICATE_SAME_ACCESS))
                {
                    return IntPtr.Zero;
                }
                int objectTypeInfoSize = 0x1000;
                IntPtr objectTypeInfo = Marshal.AllocHGlobal(objectTypeInfoSize);
                try
                {
                    int returnLength = 0;
                    if (Natives.NtQueryObject(ipHandle, (int)Enums.OBJECT_INFORMATION_CLASS.ObjectTypeInformation, objectTypeInfo, objectTypeInfoSize, ref returnLength) != 0)
                    {
                        return IntPtr.Zero;
                    }
                    var objectTypeInfoStruct = (Struct.OBJECT_TYPE_INFORMATION)Marshal.PtrToStructure(objectTypeInfo, typeof(Struct.OBJECT_TYPE_INFORMATION));
                    string typeName = objectTypeInfoStruct.Name.ToString();
                    if (typeName == "File")
                    {
                        string name = TryGetName(ipHandle);
                        if (name == filename.Substring(2, filename.Length - 2))
                            return ipHandle;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(objectTypeInfo);
                }
            }
            catch { }

            return IntPtr.Zero;
        }

        private static IntPtr DuplicateHandleByFileName(int pid, string fileName)
        {
            IntPtr handle = IntPtr.Zero;
            List<Struct.SYSTEM_HANDLE_INFORMATION> syshInfos = GetHandles(pid);
            IntPtr processHandle = GetProcessHandle(pid);

            foreach (var t in syshInfos)
            {
                handle = FindHandleByFileName(t, fileName, processHandle);
                if (handle != IntPtr.Zero)
                {
                    Natives.CloseHandle(processHandle);
                    return handle;
                }
            }
            Natives.CloseHandle(processHandle);
            return handle;
        }

        private static List<int> GetProcessIDByFileName(string path)
        {
            List<int> result = new List<int>();
            var bufferPtr = IntPtr.Zero;
            var statusBlock = new Struct.IO_STATUS_BLOCK();

            try
            {
                var handle = GetFileHandle(path);
                uint bufferSize = 0x4000;
                bufferPtr = Marshal.AllocHGlobal((int)bufferSize);

                uint status;
                while ((status = Natives.NtQueryInformationFile(handle,
                    ref statusBlock, bufferPtr, bufferSize,
                    Enums.FILE_INFORMATION_CLASS.FileProcessIdsUsingFileInformation))
                    == Natives.STATUS_INFO_LENGTH_MISMATCH)
                {
                    Marshal.FreeHGlobal(bufferPtr);
                    bufferPtr = IntPtr.Zero;
                    bufferSize *= 2;
                    bufferPtr = Marshal.AllocHGlobal((int)bufferSize);
                }

                Natives.CloseHandle(handle);

                if (status != Natives.STATUS_SUCCESS)
                {
                    return result;
                }

                IntPtr readBuffer = bufferPtr;
                int numEntries = Marshal.ReadInt32(readBuffer); // NumberOfProcessIdsInList
                readBuffer = IntPtr.Size == 8 ? new IntPtr(readBuffer.ToInt64() + IntPtr.Size) : new IntPtr(readBuffer.ToInt32() + IntPtr.Size);
                for (int i = 0; i < numEntries; i++)
                {
                    IntPtr processId = Marshal.ReadIntPtr(readBuffer); // A single ProcessIdList[] element
                    result.Add(processId.ToInt32());
                    readBuffer = IntPtr.Size == 8 ? new IntPtr(readBuffer.ToInt64() + IntPtr.Size) : new IntPtr(readBuffer.ToInt32() + IntPtr.Size);
                }
            }
            catch { return result; }
            finally
            {
                if (bufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(bufferPtr);
                }
            }
            return result;
        }

        private static IntPtr GetFileHandle(string name)
        {
            return Natives.CreateFile(name,
                0,
                FileShare.Read | FileShare.Write | FileShare.Delete,
                IntPtr.Zero,
                FileMode.Open,
                (int)FileAttributes.Normal,
                IntPtr.Zero);
        }

        public static IntPtr GetProcessHandle(int pid)
        {
            return Natives.OpenProcess(Enums.PROCESS_ACCESS_FLAGS.PROCESS_DUP_HANDLE | Enums.PROCESS_ACCESS_FLAGS.PROCESS_VM_READ, false, pid);
        }
    }
}
