using System;
using System.Runtime.InteropServices;
using static SharpWeb.Utilities.Struct;

namespace SharpWeb.Utilities
{
    class Natives
    {
        public const uint ERROR_SUCCESS = 0x00000000;
        public static readonly byte[] BCRYPT_KEY_DATA_BLOB_MAGIC = BitConverter.GetBytes(0x4d42444b);
        public static readonly string BCRYPT_OBJECT_LENGTH = "ObjectLength";
        public static readonly string BCRYPT_CHAIN_MODE_GCM = "ChainingModeGCM";
        public static readonly string BCRYPT_AUTH_TAG_LENGTH = "AuthTagLength";
        public static readonly string BCRYPT_CHAINING_MODE = "ChainingMode";
        public static readonly string BCRYPT_KEY_DATA_BLOB = "KeyDataBlob";
        public static readonly string BCRYPT_AES_ALGORITHM = "AES";
        public static readonly string MS_PRIMITIVE_PROVIDER = "Microsoft Primitive Provider";
        public static readonly int BCRYPT_AUTH_MODE_CHAIN_CALLS_FLAG = 0x00000001;
        public static readonly int BCRYPT_INIT_AUTH_MODE_INFO_VERSION = 0x00000001;
        public static readonly uint STATUS_AUTH_TAG_MISMATCH = 0xC000A002;

        [DllImport("bcrypt.dll")]
        public static extern uint BCryptOpenAlgorithmProvider(out IntPtr phAlgorithm,
                                                              [MarshalAs(UnmanagedType.LPWStr)] string pszAlgId,
                                                              [MarshalAs(UnmanagedType.LPWStr)] string pszImplementation,
                                                              uint dwFlags);

        [DllImport("bcrypt.dll")]
        public static extern uint BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, uint flags);

        [DllImport("bcrypt.dll", EntryPoint = "BCryptGetProperty")]
        public static extern uint BCryptGetProperty(IntPtr hObject, [MarshalAs(UnmanagedType.LPWStr)] string pszProperty, byte[] pbOutput, int cbOutput, ref int pcbResult, uint flags);

        [DllImport("bcrypt.dll", EntryPoint = "BCryptSetProperty")]
        internal static extern uint BCryptSetAlgorithmProperty(IntPtr hObject, [MarshalAs(UnmanagedType.LPWStr)] string pszProperty, byte[] pbInput, int cbInput, int dwFlags);

        [DllImport("bcrypt.dll")]
        public static extern uint BCryptImportKey(IntPtr hAlgorithm,
                                                         IntPtr hImportKey,
                                                         [MarshalAs(UnmanagedType.LPWStr)] string pszBlobType,
                                                         out IntPtr phKey,
                                                         IntPtr pbKeyObject,
                                                         int cbKeyObject,
                                                         byte[] pbInput, //blob of type BCRYPT_KEY_DATA_BLOB + raw key data = (dwMagic (4 bytes) | uint dwVersion (4 bytes) | cbKeyData (4 bytes) | data)
                                                         int cbInput,
                                                         uint dwFlags);

        [DllImport("bcrypt.dll")]
        public static extern uint BCryptDestroyKey(IntPtr hKey);

        [DllImport("bcrypt.dll")]
        internal static extern uint BCryptDecrypt(IntPtr hKey,
                                                  byte[] pbInput,
                                                  int cbInput,
                                                  ref BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO pPaddingInfo,
                                                  byte[] pbIV,
                                                  int cbIV,
                                                  byte[] pbOutput,
                                                  int cbOutput,
                                                  ref int pcbResult,
                                                  int dwFlags);

        [DllImport("vaultcli.dll")]
        public extern static Int32 VaultOpenVault(ref Guid vaultGuid, UInt32 offset, ref IntPtr vaultHandle);


        [DllImport("vaultcli.dll")]
        public extern static Int32 VaultEnumerateVaults(Int32 offset, ref Int32 vaultCount, ref IntPtr vaultGuid);

        [DllImport("vaultcli.dll")]
        public extern static Int32 VaultEnumerateItems(IntPtr vaultHandle, Int32 chunkSize, ref Int32 vaultItemCount, ref IntPtr vaultItem);

        [DllImport("vaultcli.dll", EntryPoint = "VaultGetItem")]
        public extern static Int32 VaultGetItem_WIN8(IntPtr vaultHandle, ref Guid schemaId, IntPtr pResourceElement, IntPtr pIdentityElement, IntPtr pPackageSid, IntPtr zero, Int32 arg6, ref IntPtr passwordVaultPtr);

        [DllImport("vaultcli.dll", EntryPoint = "VaultGetItem")]
        public extern static Int32 VaultGetItem_WIN7(IntPtr vaultHandle, ref Guid schemaId, IntPtr pResourceElement, IntPtr pIdentityElement, IntPtr zero, Int32 arg5, ref IntPtr passwordVaultPtr);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CopyFile(string lpExistingFileName, string lpNewFileName, bool bFailIfExists);
    }


}
