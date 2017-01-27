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


        private string getDataFromLine(string s, int n)
        {
            string[] stringSeparator = new string[] { " = ", " " };

            return s.Split(stringSeparator, StringSplitOptions.None)[n];
        }


        public void load(List<string> textFile)
        {
            vertices = new Vertex[int.Parse(getDataFromLine(textFile[0], 1))];
            if (vertices.Length == 0) throw new Exception("Zerowa liczba wierzchołków!");
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vertex(i + 1);
            }

            edges = new Edge[int.Parse(getDataFromLine(textFile[1], 1))];
            if (edges.Length == 0) throw new Exception("Zerowa liczba krawędzi!");
            for (int i = 0; i < edges.Length; i++)
            {
                int edge_id = int.Parse(getDataFromLine(textFile[2 + i], 0));
                int begin_id = int.Parse(getDataFromLine(textFile[2 + i], 1));
                int end_id = int.Parse(getDataFromLine(textFile[2 + i], 2));

                edges[i] = new Edge(edge_id, vertices[begin_id - 1], vertices[end_id - 1]);
                edges[i].Begin.addEdgeOut(edges[i]);
            }
        }

        public void randomizeEdgesWeights()
        {
            Random generator = new Random();
            for (int i = 0; i < edges.Length; i++)
            {
                double randomWeight = generator.NextDouble();
                edges[i].Weight = randomWeight;
            }
        }
    }
}
