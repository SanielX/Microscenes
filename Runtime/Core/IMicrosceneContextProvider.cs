using System;

namespace Microscenes
{
    public interface IMicrosceneContextProvider
    {
        public Type MicrosceneContext { get; }
    }
}
