// #! csharp
using System;
using System.IO;
using Rhino.PlugIns;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;

namespace Daxs
{
    public static class Utils
    {
        public static string GetPackageFolderPath()
        {

            PlugInInfo packageInfo = GetInfo();
            var pth = Path.GetDirectoryName(packageInfo.FileName);
            RhinoApp.WriteLine(pth);
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
            return System.IO.Path.Combine(folder, fileName);
        }
    }
}