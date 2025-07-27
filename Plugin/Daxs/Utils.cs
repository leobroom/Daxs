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
            Guid id = PlugIn.IdFromName("Daxs");
            PlugInInfo packageInfo = PlugIn.GetPlugInInfo(id);
            var pth = Path.GetDirectoryName(packageInfo.FileName);
            RhinoApp.WriteLine(pth);
            return pth;
        }  

        public static string GetFile(string fileName)
        {
            string folder = GetPackageFolderPath();
            return System.IO.Path.Combine(folder, fileName);
        }
    }
}