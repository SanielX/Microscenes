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

        public Edge ConnectOutputTo(IConnectable connectable);

        public IEnumerable<Edge> OutputEdges();
        
        public IEnumerable<IConnectable> Children => new[] { this };
    }
}
