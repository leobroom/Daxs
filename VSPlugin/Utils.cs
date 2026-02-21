using System;
using System.IO;
using System.Linq;
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

        public static string GetPackageVersion(string format = "#.#.#")
        {
            PlugInInfo packageInfo = GetInfo();
            if (packageInfo == null || string.IsNullOrWhiteSpace(packageInfo.Version))
                return string.Empty;

            if (!Version.TryParse(packageInfo.Version, out var v))
                return packageInfo.Version; // fallback if parsing fails

            int parts = format.Count(c => c == '#');

            return parts switch
            {
                1 => $"{v.Major}",
                2 => $"{v.Major}.{v.Minor}",
                3 => $"{v.Major}.{v.Minor}.{v.Build}",
                4 => $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}",
                _ => v.ToString()
            };
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