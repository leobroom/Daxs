using System;
using Rhino;
using Rhino.Commands;

namespace Daxs
{
    public class DaxsRunCmdCmd : Command
    {
        public DaxsRunCmdCmd()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static DaxsRunCmdCmd Instance { get; private set; }

        public override string EnglishName => "Daxs_RunCmd";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            using (var helper = new RunScriptHelper(doc.RuntimeSerialNumber))
            {
                //ToDo Save last mode

                helper.RunScript("_ViewCaptureToFile", true);

                LayoutManager.Instance.SetLayout("Walk");

                //if (helper.CommandResult != Result.Success)
                //    return helper.CommandResult;



                //helper.RunScript("_Circle", true);
                //if (helper.CommandResult != Result.Success)
                //    return helper.CommandResult;

                //helper.RunScript("_Arc", true);
                //if (helper.CommandResult != Result.Success)
                //    return helper.CommandResult;
            }

            return Result.Success;
        }
    }
}