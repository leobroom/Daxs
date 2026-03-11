using System.Diagnostics;

namespace PackagePublisher
{
    internal class Program
    {

        //Check https://developer.rhino3d.com/guides/yak/pushing-a-package-to-the-server/
        static void Main(string[] args)
        {
            string yakExe = @"C:\Program Files\Rhino 8\System\Yak.exe";
            string yakFolder = @"C:\Git\Daxs\VSPlugin\bin\Release";
            string source = "https://yak.rhino3d.com";
            //string source = "https://test.yak.rhino3d.com";

            Console.WriteLine("yak Folder: " + yakFolder);
            Console.WriteLine("yak Server: " + source + "\n");

            if (!File.Exists(yakExe))
            {
                Console.WriteLine("Yak.exe not found.");
                return;
            }

            if (!Directory.Exists(yakFolder))
            {
                Console.WriteLine("Yak folder not found.");
                return;
            }

            var yakFiles = Directory.GetFiles(yakFolder, "*.yak");

            if (yakFiles.Length == 0)
            {
                Console.WriteLine("No .yak files found.");
                return;
            }

            Console.WriteLine("Found yak files:");

            for (int i = 0; i < yakFiles.Length; i++)
                Console.WriteLine($"{i + 1}: {Path.GetFileName(yakFiles[i])}");

            string yakFile = yakFiles.First();

            Console.WriteLine();
            Console.WriteLine($"Upload {Path.GetFileName(yakFile)} ? (y/n)");

            if (Console.ReadLine()?.ToLower() != "y")
                return;

            Console.WriteLine("Are you sure? (y/n)");

            if (Console.ReadLine()?.ToLower() != "y")
                return;

            Console.WriteLine("Uploading...");

            var process = new Process();
            process.StartInfo.FileName = yakExe;
            process.StartInfo.Arguments = $"push --source {source} \"{yakFile}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            process.Start();

            Console.WriteLine(process.StandardOutput.ReadToEnd());
            Console.WriteLine(process.StandardError.ReadToEnd());

            process.WaitForExit();

            Console.WriteLine("Done.");
        }
    }
}
