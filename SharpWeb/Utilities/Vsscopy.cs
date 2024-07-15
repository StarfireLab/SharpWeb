using System.Management;

namespace SharpWeb.Utilities
{
    class Vsscopy
    {
        public static string CreateShadow(string volumePath = "C:\\")
        {
            string shadowCopyID = string.Empty;
            try
            {
                ManagementClass shadowCopyClass = new ManagementClass(new ManagementPath("Win32_ShadowCopy"));
                ManagementBaseObject inParams = shadowCopyClass.GetMethodParameters("Create");

                inParams["Volume"] = volumePath;

                ManagementBaseObject outParams = shadowCopyClass.InvokeMethod("Create", inParams, null);
                shadowCopyID = outParams["ShadowID"].ToString();
            }
            catch { }
            return shadowCopyID;
        }

        public static string ListShadow(string shadowCopyID)
        {
            string DeviceObject = string.Empty;
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ShadowCopy WHERE ID='" + shadowCopyID + "'");
                ManagementObjectCollection shadowCopies = searcher.Get();

                foreach (ManagementObject shadowCopy in shadowCopies)
                {
                    DeviceObject = shadowCopy.GetPropertyValue("DeviceObject").ToString();
                }
            }
            catch { }
            return DeviceObject;
        }

        public static void DeleteShadow(string ShadowID)
        {
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ShadowCopy WHERE ID='" + ShadowID + "'");
                ManagementObjectCollection shadowCopies = searcher.Get();

                foreach (ManagementObject shadowCopy in shadowCopies)
                {
                    shadowCopy.Delete();
                }
            }
            catch { }
        }
    }
}
