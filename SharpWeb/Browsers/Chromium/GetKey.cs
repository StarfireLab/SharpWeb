using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.InteropServices;
using static SharpWeb.Utilities.Natives;
using static SharpWeb.Utilities.Struct;

namespace SharpWeb.Browsers
{
    //adapted from https://github.com/0xfd3/Chrome-Password-Recovery/blob/master/Chromium.cs#L229-L294
    class GetKey
    {
        public static byte[] GetMasterKey(string filePath)
        {
            //Key saved in Local State file

            byte[] masterKey = new byte[] { };

            if (File.Exists(filePath) == false)
                return null;

            //Get key with regex.
            var pattern = new System.Text.RegularExpressions.Regex("\"encrypted_key\":\"(.*?)\"", System.Text.RegularExpressions.RegexOptions.Compiled).Matches(File.ReadAllText(filePath));

            foreach (System.Text.RegularExpressions.Match prof in pattern)
            {
                if (prof.Success)
                    masterKey = Convert.FromBase64String((prof.Groups[1].Value)); //Decode base64
            }

            //Trim first 5 bytes. Its signature "DPAPI"
            byte[] temp = new byte[masterKey.Length - 5];
            Array.Copy(masterKey, 5, temp, 0, masterKey.Length - 5);

            try
            {
                return ProtectedData.Unprotect(temp, null, DataProtectionScope.CurrentUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        public static string DecryptWithKey(byte[] encryptedData, byte[] MasterKey)
        {
            byte[] iv = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // IV 12 bytes

            //trim first 3 bytes(signature "v10") and take 12 bytes after signature.
            Array.Copy(encryptedData, 3, iv, 0, 12);

            try
            {
                //encryptedData without IV
                byte[] Buffer = new byte[encryptedData.Length - 15];
                Array.Copy(encryptedData, 15, Buffer, 0, encryptedData.Length - 15);

                byte[] tag = new byte[16]; //AuthTag
                byte[] data = new byte[Buffer.Length - tag.Length]; //Encrypted Data

                //Last 16 bytes for tag
                Array.Copy(Buffer, Buffer.Length - 16, tag, 0, 16);

                //encrypted password
                Array.Copy(Buffer, 0, data, 0, Buffer.Length - tag.Length);

                AesGcm aesDecryptor = new AesGcm();
                var result = Encoding.UTF8.GetString(aesDecryptor.Decrypt(MasterKey, iv, null, data, tag));

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }
    }

    //AES GCM from https://github.com/dvsekhvalnov/jose-jwt
    class AesGcm
    {
        public byte[] Decrypt(byte[] key, byte[] iv, byte[] aad, byte[] cipherText, byte[] authTag)
        {
            IntPtr hAlg = OpenAlgorithmProvider(BCRYPT_AES_ALGORITHM, MS_PRIMITIVE_PROVIDER, BCRYPT_CHAIN_MODE_GCM);
            IntPtr hKey, keyDataBuffer = ImportKey(hAlg, key, out hKey);

            byte[] plainText;

            var authInfo = new BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO(iv, aad, authTag);
            using (authInfo)
            {
                byte[] ivData = new byte[MaxAuthTagSize(hAlg)];

                int plainTextSize = 0;

                uint status = BCryptDecrypt(hKey, cipherText, cipherText.Length, ref authInfo, ivData, ivData.Length, null, 0, ref plainTextSize, 0x0);

                if (status != ERROR_SUCCESS)
                    throw new CryptographicException(string.Format("BCryptDecrypt() (get size) failed with status code: {0}", status));

                plainText = new byte[plainTextSize];

                status = BCryptDecrypt(hKey, cipherText, cipherText.Length, ref authInfo, ivData, ivData.Length, plainText, plainText.Length, ref plainTextSize, 0x0);

                if (status == STATUS_AUTH_TAG_MISMATCH)
                    throw new CryptographicException("BCryptDecrypt(): authentication tag mismatch");

                if (status != ERROR_SUCCESS)
                    throw new CryptographicException(string.Format("BCryptDecrypt() failed with status code:{0}", status));
            }

            BCryptDestroyKey(hKey);
            Marshal.FreeHGlobal(keyDataBuffer);
            BCryptCloseAlgorithmProvider(hAlg, 0x0);

            return plainText;
        }

        private int MaxAuthTagSize(IntPtr hAlg)
        {
            byte[] tagLengthsValue = GetProperty(hAlg, BCRYPT_AUTH_TAG_LENGTH);

            return BitConverter.ToInt32(new[] { tagLengthsValue[4], tagLengthsValue[5], tagLengthsValue[6], tagLengthsValue[7] }, 0);
        }

        private IntPtr OpenAlgorithmProvider(string alg, string provider, string chainingMode)
        {
            IntPtr hAlg = IntPtr.Zero;

            uint status = BCryptOpenAlgorithmProvider(out hAlg, alg, provider, 0x0);

            if (status != ERROR_SUCCESS)
                throw new CryptographicException(string.Format("BCryptOpenAlgorithmProvider() failed with status code:{0}", status));

            byte[] chainMode = Encoding.Unicode.GetBytes(chainingMode);
            status = BCryptSetAlgorithmProperty(hAlg, BCRYPT_CHAINING_MODE, chainMode, chainMode.Length, 0x0);

            if (status != ERROR_SUCCESS)
                throw new CryptographicException(string.Format("BCryptSetAlgorithmProperty(BCRYPT_CHAINING_MODE, BCRYPT_CHAIN_MODE_GCM) failed with status code:{0}", status));

            return hAlg;
        }

        private IntPtr ImportKey(IntPtr hAlg, byte[] key, out IntPtr hKey)
        {
            byte[] objLength = GetProperty(hAlg, BCRYPT_OBJECT_LENGTH);

            int keyDataSize = BitConverter.ToInt32(objLength, 0);

            IntPtr keyDataBuffer = Marshal.AllocHGlobal(keyDataSize);

            byte[] keyBlob = Concat(BCRYPT_KEY_DATA_BLOB_MAGIC, BitConverter.GetBytes(0x1), BitConverter.GetBytes(key.Length), key);

            uint status = BCryptImportKey(hAlg, IntPtr.Zero, BCRYPT_KEY_DATA_BLOB, out hKey, keyDataBuffer, keyDataSize, keyBlob, keyBlob.Length, 0x0);

            if (status != ERROR_SUCCESS)
                throw new CryptographicException(string.Format("BCryptImportKey() failed with status code:{0}", status));

            return keyDataBuffer;
        }

        private byte[] GetProperty(IntPtr hAlg, string name)
        {
            int size = 0;

            uint status = BCryptGetProperty(hAlg, name, null, 0, ref size, 0x0);

            if (status != ERROR_SUCCESS)
                throw new CryptographicException(string.Format("BCryptGetProperty() (get size) failed with status code:{0}", status));

            byte[] value = new byte[size];

            status = BCryptGetProperty(hAlg, name, value, value.Length, ref size, 0x0);

            if (status != ERROR_SUCCESS)
                throw new CryptographicException(string.Format("BCryptGetProperty() failed with status code:{0}", status));

            return value;
        }

        public byte[] Concat(params byte[][] arrays)
        {
            int len = 0;

            foreach (byte[] array in arrays)
            {
                if (array == null)
                    continue;
                len += array.Length;
            }

            byte[] result = new byte[len - 1 + 1];
            int offset = 0;

            foreach (byte[] array in arrays)
            {
                if (array == null)
                    continue;
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }

            return result;
        }
    }
}
