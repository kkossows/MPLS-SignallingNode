using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlPlane
{
    class Path
    {
        private List<Vertex> vertices;
        public List<Vertex> Vertices
        {
            get
            {
                List<Vertex> existingVertices = new List<Vertex>();
                for (int i = 0; i < length; i++)
                {
                    existingVertices[i] = vertices[length - 1 - i];
                }
                return existingVertices;
            }
        }

        private double minWeight;
        public double MinWeight
        {
            get { return minWeight; }
        }

        private double sumWeight;
        public double SumWeight
        {
            get { return sumWeight; }
        }

        private int length;
        public int Length
        {
            get { return length; }
        }

        public void push(Vertex vertex)
        {
            vertices[length++] = vertex;
        }
        public Path()
        {
            vertices = new List<Vertex>();
            length = 0;
            sumWeight = 0;
            minWeight = double.MaxValue;
        }
    }
}
