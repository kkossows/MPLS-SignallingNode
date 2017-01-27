using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPLS_SignalingNode.ControlPlane.SignallingModules.RC
{
    class Vertex
    {
        private int id;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private Vertex prev;
        public Vertex Prev
        {
            get { return prev; }
            set { prev = value; }
        }

        private double cumulatedWeight;
        public double CumulatedWeight
        {
            get { return cumulatedWeight; }
            set { cumulatedWeight = value; }
        }

        private Edge[] edgesOut;
        public Edge[] EdgesOut
        {
            get { return edgesOut; }
        }

        public void addEdgeOut(Edge edge)
        {
            Edge[] tmp_links = new Edge[edgesOut.Length + 1];
            for (int i = 0; i < edgesOut.Length; i++)
                tmp_links[i] = edgesOut[i];
            tmp_links[edgesOut.Length] = edge;
            edgesOut = tmp_links;
        }

        public Vertex()
        {
            this.id = 0;
            this.edgesOut = new Edge[0];
        }

        public Vertex(int id)
        {
            this.id = id;
            this.edgesOut = new Edge[0];
        }
    
    }
}
