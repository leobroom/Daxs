using System;
using System.Drawing;
using Rhino;
using Rhino.Display;

// Rhino 8 ScriptEditor C# script
var doc = __rhino_doc__;

const string viewName = "Daxs Wiki";
const int width = 1024; //1280
const int height = 576; //720

// Placement on screen
var createRect = new Rectangle(100, 100, width, height);

RhinoView targetView = null;

foreach (RhinoView v in doc.Views)
{
    if (v == null ||!v.Floating)
        continue;

    string activeName = v.ActiveViewport?.Name ?? string.Empty;
    string mainName   = v.MainViewport?.Name ?? string.Empty;

    if (string.Equals(activeName, viewName, StringComparison.OrdinalIgnoreCase) || string.Equals(mainName, viewName, StringComparison.OrdinalIgnoreCase))
    {
        targetView = v;
        break;
    }
}

// Create only if no matching floating view exists
if (targetView == null)
{
    targetView = doc.Views.Add( viewName, DefinedViewportProjection.Perspective,createRect, true);

    // Copy camera from the current active view once, on creation
    var sourceView = doc.Views.ActiveView;
    if (sourceView != null && sourceView != targetView)
    {
        var src = sourceView.ActiveViewport;
        var dst = targetView.ActiveViewport;

        dst.SetCameraLocations(src.CameraLocation, src.CameraTarget);
        dst.CameraUp = src.CameraUp;

        if (src.IsParallelProjection)
            dst.ChangeToParallelProjection(true);
        else
            dst.ChangeToPerspectiveProjection(true, src.Camera35mmLensLength);
    }
}

//RESIZE 
if (targetView.ActiveViewport != null)
    targetView.ActiveViewport.Name = viewName;

targetView.Size = new Size(width, height);

// Bring it to the front
doc.Views.ActiveView = targetView;

targetView.Redraw();