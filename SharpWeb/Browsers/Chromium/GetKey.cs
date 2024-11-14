using System;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;

namespace SharpWeb.Browsers
{
    //adapted from https://github.com/0xfd3/Chrome-Password-Recovery/blob/master/Chromium.cs#L229-L294
    class GetKey
    {
        public static byte[] GetMasterKey(string filePath)
        {
            byte[] masterKey = new byte[] { };
            if (!File.Exists(filePath))
                return null;
            var pattern = new System.Text.RegularExpressions.Regex("\"encrypted_key\":\"(.*?)\"", System.Text.RegularExpressions.RegexOptions.Compiled).Matches(File.ReadAllText(filePath).Replace(" ", ""));
            foreach (System.Text.RegularExpressions.Match prof in pattern)
            {
                if (prof.Success)
                    masterKey = Convert.FromBase64String((prof.Groups[1].Value));
            }
            byte[] temp = new byte[masterKey.Length - 5];
            Array.Copy(masterKey, 5, temp, 0, masterKey.Length - 5);
            try
            {
                return ProtectedData.Unprotect(temp, null, DataProtectionScope.CurrentUser);
            }
            catch
            {
                return null;
            }
        }


        public static string DecryptData(byte[] buffer, byte[] MasterKey)
        {
            byte[] decryptedData = null;
            if (MasterKey is null) 
                return null;
            try
            {
                string bufferString = Encoding.UTF8.GetString(buffer);
                if (bufferString.StartsWith("v10") || bufferString.StartsWith("v11"))
                {
                    byte[] iv = new byte[12];
                    Array.Copy(buffer, 3, iv, 0, 12);
                    byte[] cipherText = new byte[buffer.Length - 15];
                    Array.Copy(buffer, 15, cipherText, 0, buffer.Length - 15);
                    byte[] tag = new byte[16];
                    Array.Copy(cipherText, cipherText.Length - 16, tag, 0, 16);
                    byte[] data = new byte[cipherText.Length - tag.Length];
                    Array.Copy(cipherText, 0, data, 0, cipherText.Length - tag.Length);
                    decryptedData = new AesGcm().Decrypt(MasterKey, iv, null, data, tag);
                }
                else
                {
                    decryptedData = ProtectedData.Unprotect(buffer, null, DataProtectionScope.CurrentUser);
                }
            }
            catch 
            {
                return null;
            }

            var result = Encoding.UTF8.GetString(decryptedData);

            return result;
        }

        public static byte[] RemoveAppBPrefix(byte[] encryptedKey)
        {
            if (encryptedKey.Length >= 4 && encryptedKey[0] == 0x41 && encryptedKey[1] == 0x50 && encryptedKey[2] == 0x50 && encryptedKey[3] == 0x42)
            {
                byte[] result = new byte[encryptedKey.Length - 4];
                Array.Copy(encryptedKey, 4, result, 0, encryptedKey.Length - 4);
                return result;
            }
            else
            {
                return null;
            }
        }


        public static byte[] DecryptWithSystemDPAPI(string fullPath)
        {
            try
            {
                byte[] base64EncryptedKey = null;
                var pattern = new System.Text.RegularExpressions.Regex("\"app_bound_encrypted_key\":\"(.*?)\"", System.Text.RegularExpressions.RegexOptions.Compiled).Matches(File.ReadAllText(fullPath).Replace(" ", ""));
                foreach (System.Text.RegularExpressions.Match prof in pattern)
                {
                    if (prof.Success)
                        base64EncryptedKey = Convert.FromBase64String((prof.Groups[1].Value));
                }
                byte[] Key = ProtectedData.Unprotect(RemoveAppBPrefix(base64EncryptedKey), null, DataProtectionScope.LocalMachine);
                return Key;
            }
            catch
            {
                return null;
            }
        }
        //add from to https://github.com/runassu/chrome_v20_decryption/blob/main/decrypt_chrome_v20_cookie.py
        public static byte[] DecryptWithUserDPAPI(byte[] SystemKey, string file)
        {
            try
            {
                byte[] Key1 = ProtectedData.Unprotect(SystemKey, null, DataProtectionScope.CurrentUser);
                byte[] Key2 = Key1.Skip(Math.Max(0, Key1.Length - 61)).ToArray();
                byte[] decryptedData = null;
                if (file.Contains("Google"))
                {
                    string aesKeyBase64 = "sxxuJBrIRnKNqcH6xJNmUc/7lE0UOrgWJ2vMbaAoR4c=";
                    byte[] aesKey = Convert.FromBase64String(aesKeyBase64);
                    byte[] iv = Key2.Skip(1).Take(12).ToArray();
                    byte[] ciphertext = Key2.Skip(13).Take(32).ToArray();
                    byte[] tag = Key2.Skip(45).Take(16).ToArray();
                    decryptedData = new AesGcm().Decrypt(aesKey, iv, null, ciphertext, tag);
                }
                else
                {
                    byte[] key = new byte[32];
                    Array.Copy(Key1, Key1.Length - 32, key, 0, 32);
                    decryptedData = key;
                }
                return decryptedData;
            }
            catch
            {
                return null;
            }
        }
    }
}
