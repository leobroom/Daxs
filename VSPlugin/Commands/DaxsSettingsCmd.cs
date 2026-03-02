using Daxs.Settings;
using Rhino;
using Rhino.Commands;
using Rhino.UI;

namespace Daxs.Commands
{
    public class DaxsSettingsCmd : Command
    {
        public DaxsSettingsCmd()=> Instance = this;
        public static DaxsSettingsCmd Instance { get; private set; }

        public override string EnglishName => "Daxs_Settings";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var dSettings = new DaxsSettings();
            EtoExtensions.UseRhinoStyle(dSettings);

            var result = dSettings.ShowSemiModal(RhinoDoc.ActiveDoc, RhinoEtoApp.MainWindow);
            if (!result)
                return Result.Cancel;

            //foreach (var nv in Daxs.Settings.Instance)
            //    RhinoApp.WriteLine(nv.ToString());

            return Result.Success;
        }
    }
}