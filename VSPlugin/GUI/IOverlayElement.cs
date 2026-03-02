using Rhino.Display;
using System;

namespace Daxs.GUI
{
    internal interface IOverlayElement : IDisposable
    {
        string Id { get; }
        bool Enabled { get; }

        void Tick(long nowMs);

        void Draw(DisplayPipeline dp, RhinoViewport viewport, float uiScale, long nowMs);
    }
}