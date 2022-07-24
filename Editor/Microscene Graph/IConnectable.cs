using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

namespace Microscenes.Editor
{
    public interface IConnectable
    {
        public Port input  { get; }
        public Port output { get; }

        public Edge ConnectInputTo(Port p);
        public Edge ConnectOutputTo(Port p);

        public Edge ConnectInputTo(IConnectable connectable);
        public Edge ConnectOutputTo(IConnectable connectable);

        public IEnumerable<Edge> OutputEdges();
    }
}
