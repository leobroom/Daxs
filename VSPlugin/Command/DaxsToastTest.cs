using Rhino;
using Rhino.Commands;
using System;
using System.Runtime.InteropServices;

namespace Daxs
{
    public class DaxsToastTestCmd : Command
    {



[DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    private static float GetWindowsScale()
    {
        var hwnd = RhinoApp.MainWindowHandle();
        if (hwnd == IntPtr.Zero)
            return 1f;

        uint dpi = GetDpiForWindow(hwnd);
        return dpi / 96f;
    }
    public DaxsToastTestCmd() => Instance = this;

        public static DaxsToastTestCmd Instance { get; private set; }

        public override string EnglishName => "Daxs_ToastTest";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            RhinoApp.WriteLine(GetWindowsScale().ToString());
            OverlayRenderer.Instance.SetText("🎮", "DaxsToastTestCmd", 5000);

            return Result.Success;
        }
    }
}