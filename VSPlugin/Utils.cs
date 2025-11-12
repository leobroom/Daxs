using System;
using System.IO;
using Rhino.PlugIns;

namespace Daxs
{
    public static class Utils
    {
        public static string GetPackageFolderPath()
        {
            PlugInInfo packageInfo = GetInfo();
            var pth = Path.GetDirectoryName(packageInfo.FileName);
            return pth;
        }

        public static PlugInInfo GetInfo()
        {
            Guid id = PlugIn.IdFromName("Daxs");
            return PlugIn.GetPlugInInfo(id);
        }

        public static string GetPackageVersion()
        {
            PlugInInfo packageInfo = GetInfo();
            return packageInfo.Version;
        }

        public static string GetFile(string fileName)
        {
            string folder = GetPackageFolderPath();
            return Path.Combine(folder, fileName);
        }

        public static string GetSharedFile(string fileName)
        {
            string folder = GetPackageFolderPath();
            return Path.Combine(folder,"Shared", fileName);
        }
    }
}