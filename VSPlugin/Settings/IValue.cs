using System;

namespace Daxs
{
    public interface IValue
    {
        string Name { get; }

        void Reset();
    }
}