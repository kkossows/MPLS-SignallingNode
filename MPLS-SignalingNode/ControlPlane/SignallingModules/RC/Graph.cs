using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPLS_SignalingNode.ControlPlane.SignallingModules.RC
{
    class Graph
    {
        private Vertex[] vertices;
        public Vertex[] Vertices
        {
            get { return vertices; }
        }

        private Edge[] edges;
        public Edge[] Edges
        {
            get { return edges; }
        }

        public Graph()
        {

        }
    }
}
