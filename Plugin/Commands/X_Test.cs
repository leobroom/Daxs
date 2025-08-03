// #! csharp

#r "nuget: SharpDX.XInput, 4.2.0"
#r "nuget: SharpDX, 4.2.0"


using System;
using System.IO;
using Rhino.PlugIns;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;
using Daxs;
using Rhino.UI;
using System.Linq;

// Guid id = PlugIn.IdFromName("Daxs");
// PlugInInfo packageInfo = PlugIn.GetPlugInInfo(id);
// PersistentSettings settings = PlugIn.GetPluginSettings(id,true);
// settings.SetString("TestValue", "HelloSettings");

ControllerManager.Instance.SetLayout("Walk");


// Rhino.Display.DisplayPipeline.DrawForeground -= DrawText;   


//         void DrawText(object sender, DrawEventArgs e)
//         {
//             var activeView = RhinoDoc.ActiveDoc.Views.ActiveView;
 

//             var screenPoint = new Point2d(50, 50);
//             e.Display.Draw2dText("test", System.Drawing.Color.Black, screenPoint, false, 25);
//         }

//int count =RhinoEtoApp.MainWindow.Children;

// for(int i =0;RhinoEtoApp.MainWindow.Children.Count )
// {}


//RhinoApp.RunScript("X_Settings", false);
// PlugIn.SavePluginSettings(id);

 



