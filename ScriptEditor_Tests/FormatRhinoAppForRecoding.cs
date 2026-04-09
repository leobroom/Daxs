// #! csharp
using System;
using Rhino;

int marginW = 22;
int marginH = 11;

int width = 2560/2;
int height = 1440;

Rhino.RhinoApp.RunScript($"SetMainframeSize {width+ marginW} {height + marginH}", false);
RhinoApp.RunScript("_WindowLayout restore", false);