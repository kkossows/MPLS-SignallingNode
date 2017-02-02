using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ControlPlane
{
    [Serializable]
    class Graph
    {
        private List<Vertex> vertices;
        public List<Vertex> Vertices
        {
            get { return vertices; }
            set { vertices = value; }
        }

        private List<Edge> edges;
        public List<Edge> Edges
        {
            get { return edges; }
            set { edges = value; }
        }

        public Graph()
        {
            vertices = new List<Vertex>();
            edges = new List<Edge>();
        }

        public Graph(Graph graph)
        {
            Edge[] tmpEdges = new Edge[graph.Edges.Count];
            graph.Edges.CopyTo(tmpEdges);
            edges = new List<Edge>();
            foreach (Edge edge in tmpEdges)
            {
                edges.Add(edge);
            }

            Vertex[] tmpVertices = new Vertex[graph.Vertices.Count];
            graph.Vertices.CopyTo(tmpVertices);
            vertices = new List<Vertex>();
            foreach (Vertex vertex in tmpVertices)
            {
                vertices.Add(vertex);
            }
        }

        public Graph Copy(Graph graph)
        {
            Graph tmpGraph = new Graph();
           // graph.Edges.CopyTo(tmpGraph.Edges);
            tmpGraph.Vertices = graph.Vertices;
            return tmpGraph;
        }
        public Graph Clone()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();

            bf.Serialize(ms, this);

            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();

            return obj as Graph;
        }
    }
}
