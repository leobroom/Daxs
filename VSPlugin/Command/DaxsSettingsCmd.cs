using Rhino;
using Rhino.Commands;
using Rhino.UI;


namespace Daxs
{
    public class DaxsSettingsCmd : Command
    {
        public DaxsSettingsCmd(){Instance = this;}
        public static DaxsSettingsCmd Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "Daxs_Settings";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var dSettings = new DaxsSettings();
            EtoExtensions.UseRhinoStyle(dSettings);

            ControllerManager.Instance.SetLayout("Menu");

            var result = dSettings.ShowSemiModal(RhinoDoc.ActiveDoc, RhinoEtoApp.MainWindow);

            if (!result)
            {
                ControllerManager.Instance.SetLayout("Fly");

                return Result.Cancel;
            }

            foreach (var nv in Daxs.Settings.Instance.AllValues)
                RhinoApp.WriteLine($"{nv.Name}: {nv.Value}");

            ControllerManager.Instance.SetLayout("Fly");
            return Result.Success;
        }
    }
}