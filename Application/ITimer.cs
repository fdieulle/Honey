using System;

namespace Application
{
    public interface ITimer
    {
        event Action Updated;
    }
}
