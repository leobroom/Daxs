using System;

namespace Daxs
{
    internal interface IOverlayElement : IDisposable
    {
        string Id { get; }
        bool Enabled { get; }

        /// <summary>Let the element update internal timers/animations.</summary>
        void Tick(long nowMs);

        /// <summary>Draw element into Rhino display.</summary>
        void Draw(Rhino.Display.DisplayPipeline dp, Rhino.Display.RhinoViewport viewport, float uiScale);
    }
}