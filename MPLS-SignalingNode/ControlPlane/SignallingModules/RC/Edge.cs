using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPLS_SignalingNode.ControlPlane.SignallingModules.RC
{
    class Edge
    {
        private int id;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private Vertex begin;
        public Vertex Begin
        {
            get { return begin; }
            set { begin = value; }
        }

        private Vertex end;
        public Vertex End
        {
            get { return end; }
            set { end = value; }
        }

        private double weight;
        public double Weight
        {
            get { return weight; }
            set { weight = value; }
        }
        private int capacity;
        public int Capacity
        {
            get { return capacity;}
            set { capacity = value; }
        }
        public Edge(int id, Vertex begin, Vertex end, int capacity, double weight)
        {
            this.id = id;
            this.begin = begin;
            this.end = end;
            this.weight = weight;
            this.capacity = capacity;
        }
    }
}
