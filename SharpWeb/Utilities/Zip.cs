using System;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;

namespace SharpWeb.Utilities
{
    class Zip
    {
        public static void saveZip()
        {
            string name = "out";
            string sourceFolder = String.Format("{0}\\{1}",System.IO.Directory.GetCurrentDirectory(),name);
            string zipFile = String.Format("{0}\\{1}.zip", System.IO.Directory.GetCurrentDirectory(), name);
            CompressFolder(sourceFolder, zipFile);
        }
        public static void CompressFolder(string sourceFolder, string zipFile)
        {
            using (FileStream fsOut = File.Create(zipFile))
            using (ZipOutputStream zipStream = new ZipOutputStream(fsOut))
            {
                zipStream.SetLevel(9);

                CompressFolder(sourceFolder, sourceFolder, zipStream);

                zipStream.IsStreamOwner = true;
                zipStream.Close();
            }
        }

        public static void CompressFolder(string rootFolder, string currentFolder, ZipOutputStream zipStream)
        {
            string[] files = Directory.GetFiles(currentFolder);
            foreach (string file in files)
            {
                string relativePath = GetRelativePath(rootFolder, file);
                ZipEntry entry = new ZipEntry(relativePath);
                zipStream.PutNextEntry(entry);

                using (FileStream fs = File.OpenRead(file))
                {
                    byte[] buffer = new byte[4096];
                    StreamUtils.Copy(fs, zipStream, buffer);
                }

                zipStream.CloseEntry();
            }

            string[] subFolders = Directory.GetDirectories(currentFolder);
            foreach (string folder in subFolders)
            {
                CompressFolder(rootFolder, folder, zipStream);
            }
        }

        public static string GetRelativePath(string fromPath, string toPath)
        {
            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme)
            {
                return toPath;
            }

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            return Uri.UnescapeDataString(relativeUri.ToString());
        }
    }
}
