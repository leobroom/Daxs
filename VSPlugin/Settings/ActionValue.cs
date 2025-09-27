using System;
using System.Collections.Generic;
using System.Linq;

namespace Daxs
{
    internal class ActionValue : IValue
    {

        public string Name { get; private set; } = "unset";

        public event EventHandler<double>? ValueChanged;

        object[] _args = null;

        public object[] Args
        {
            get => _args;
            set
            {
                _args = value;
                //OnValueChanged(_value);
            }
        }

        public GButton Button { get; internal set; }
        public AProperty ActionName { get; internal set; }

        public ActionValue(GButton button, AProperty actionName, params object[] args)
        {
            List<object> objs = new List<object>();
            objs.Add(button);
            objs.Add(actionName);
            objs.AddRange(args);
            _args = args.ToArray();
            Name = button.ToString();
            Button = button;
            ActionName = actionName;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }
    }
}