// #! csharp
#r "nuget: SharpDX.XInput, 4.2.0"
#r "nuget: SharpDX, 4.2.0"

using Rhino;
using Rhino.UI;
using Daxs;

var dSettings = new DaxsSettings();
Rhino.UI.EtoExtensions.UseRhinoStyle(dSettings);

ControllerManager.Instance.SetLayout("Menu");

var result = dSettings.ShowSemiModal(RhinoDoc.ActiveDoc, RhinoEtoApp.MainWindow);

if (!result)
{
    ControllerManager.Instance.SetLayout("Fly");
    return;
}
   

foreach (var nv in Settings.Instance.AllValues)
    RhinoApp.WriteLine($"{nv.Name}: {nv.Value}");

ControllerManager.Instance.SetLayout("Fly");