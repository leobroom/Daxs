using Rhino;
using Rhino.Commands;

namespace Daxs
{
    public class DaxsRunCmdCmd : Command
    {
        public DaxsRunCmdCmd()=>Instance = this;

        public static DaxsRunCmdCmd Instance { get; private set; }

        public override string EnglishName => "Daxs_RunCmd";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            using var helper = new RunScriptHelper(doc.RuntimeSerialNumber);
            LayoutManager.Instance.Set(Layout.Menu);

            helper.RunScript("_ViewCaptureToFile", true);

            LayoutManager.Instance.SetToPreviousLayout();

            return Result.Success;
        }
    }
}