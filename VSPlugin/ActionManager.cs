using System;
using System.Collections.Generic;

namespace Daxs
{
    internal class ActionManager
    {
        private static readonly Lazy<ActionManager> _instance = new(() => new ActionManager());
        public static ActionManager Instance => _instance.Value;
  
        private readonly Dictionary<GamepadButton, Tuple<Func<bool>, Action>> bindingTable = new Dictionary<GamepadButton, Tuple<Func<bool>, Action>>();

        public void RegisterBinding(GamepadButton button, DaxsAction dAction)
        {
            
        }
    }
}