using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlPlane
{
    [Serializable]
    class Edge
    {
        private string id;
        public string Id
        {
            get { return id; }
            set { id = value; }
        }
        private List<int> activeConnections = new List<int>();
        public List<int> ActiveConnections
        {
            get { return activeConnections; }
            set { activeConnections = value; }
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
        public Edge(Vertex begin, Vertex end, int capacity, double weight)
        {
            this.id = CreateName(begin, end);
            this.begin = begin;
            this.end = end;
            this.weight = weight;
            this.capacity = capacity;
        }
        public static string CreateName(Vertex begin, Vertex end)
        {
            StringBuilder name = new StringBuilder();
            name.Append(begin.Id.ToString());
            name.Append("_");
            name.Append(end.Id.ToString());
            return name.ToString();
        }
        public static string CreateName(int begin, int end)
        {
            StringBuilder name = new StringBuilder();
            name.Append(begin.ToString());
            name.Append("_");
            name.Append(end.ToString());
            return name.ToString();
        }
    }
}
