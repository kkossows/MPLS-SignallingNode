using System;
using MPLS_SignalingNode;
using System.Collections.Generic;
using DTO.ControlPlane;

namespace ControlPlane
{
    class RC
    {
        #region Variables
        private string _configurationFilePath;
        private string _localPcIpAddress;
        private string _myAreaName;
        private Dictionary<int, int> interdomainLinks = new Dictionary<int, int>();
        private Dictionary<string, int> IPTOIDDictionary = new Dictionary<string, int>();
        private Graph graph = new Graph();
        private List<Lrm> LRMs = new List<Lrm>();
        private PC _pc;
        #endregion

        #region Properties
        public PC LocalPC { set { _pc = value; } }
        #endregion

        public RC()
        {

        }
        #region Main_Methodes
        public RC(string configurationFilePath)
        {
            InitialiseVariables(configurationFilePath);
        }
        private void InitialiseVariables(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;

            RC_XmlSchame tmp = new RC_XmlSchame();
            Topology temp = new Topology();
            tmp = RC_LoadingXmlFile.Deserialization(_configurationFilePath);
            _localPcIpAddress = tmp.XML_myIPAddress;
            _myAreaName = tmp.XMP_myAreaName;
            if (tmp.Dictionary != null)
            {
                foreach (var v in tmp.Dictionary)
                {
                    IPTOIDDictionary.Add(v.IP, v.ID);
                }
            }
            foreach (var v in tmp.LocalTopology)
            {
                if (_myAreaName.Equals("Dom_1"))
                {
                    List<int> otherDomain = v.reachableID.FindAll(x => x > 199);
                    if (otherDomain != null)
                        NetworkTopology(v.ID, otherDomain);
                    LocalTopology(v.ID, v.capacity, v.reachableID, v.areaName);
                }
                else if (_myAreaName.Equals("Dom_2"))
                {
                    List<int> otherDomain = v.reachableID.FindAll(x => x < 101);
                    if (otherDomain != null)
                        NetworkTopology(v.ID, otherDomain);
                    LocalTopology(v.ID, v.capacity, v.reachableID, v.areaName);
                }
                else
                    LocalTopology(v.ID, v.capacity, v.reachableID, v.areaName);
            }
            if (_myAreaName != null)
            {
                if (_myAreaName.Equals("Dom_1"))
                {
                    graph.Edges.Find(x => x.Id.Equals("13_21")).Weight = 15;
                    graph.Edges.Find(x => x.Id.Equals("14_22")).Weight = 15;
                    graph.Edges.Find(x => x.Id.Equals("21_13")).Weight = 15;
                    graph.Edges.Find(x => x.Id.Equals("22_14")).Weight = 15;
                }
            }
        }
        #endregion


        #region PC_Cooperation_Methodes
        private void SendMessageToPC(SignalMessage message)
        {
            _pc.SendSignallingMessage(message);
            //SignallingNodeDeviceClass.MakeSignallingLog("RC", "INFO - Signalling message send to PC module");
        }
        public void ReceiveMessageFromPC(SignalMessage message)
        {
            switch (message.General_SignalMessageType)
            {
                case SignalMessage.SignalType.RouteQuery:
                    if (message.CallingIpAddress != null)
                    {
                        RouteQuery(message.ConnnectionID, message.CallingIpAddress, message.CalledIpAddress, message.CallingCapacity); // Wewnatrzdomenowa wiad nr 1 i miedzydomenowa nr 1
                    }else if(message.CalledIpAddress != null)
                    {
                        RouteQuery(message.ConnnectionID, message.SnppInId, message.CalledIpAddress, message.CallingCapacity); // Miedzydomenowa wiad nr 3
                    }else
                    {
                        RouteQuery(message.ConnnectionID, message.SnppIdPair, message.CallingCapacity); // wewnatrzdomenowa wiad nr 2 i miedzydomenowa nr 2
                    }
                       
                    break;

                case SignalMessage.SignalType.IsUp:
                    IsUp(message.IsUpKeepAlive_areaName);

                    break;

                case SignalMessage.SignalType.KeepAlive:
                    KeepAlive(message.IsUpKeepAlive_areaName);
                    break;




                case SignalMessage.SignalType.LocalTopology:
                    
                    if (_myAreaName.Equals("Domena_1"))
                    {
                        List<int> otherDomain = message.LocalTopology_reachableSnppIdList.FindAll(x => x > 199);
                        message.LocalTopology_reachableSnppIdList.RemoveAll(x => x > 199);
                        if (otherDomain != null)
                            NetworkTopology(message.LocalTopology_SnppID, otherDomain);
                        LocalTopology(message.LocalTopology_SnppID, message.LocalTopology_availibleCapacity, message.LocalTopology_reachableSnppIdList, message.LocalTopology_areaName);
                    }
                    else if (_myAreaName.Equals("Domena_2"))
                    {
                        List<int> otherDomain = message.LocalTopology_reachableSnppIdList.FindAll(x => x < 100);
                        message.LocalTopology_reachableSnppIdList.RemoveAll(x => x < 100);
                        if (otherDomain != null)
                            NetworkTopology(message.LocalTopology_SnppID, otherDomain);
                        LocalTopology(message.LocalTopology_SnppID, message.LocalTopology_availibleCapacity, message.LocalTopology_reachableSnppIdList, message.LocalTopology_areaName);
                    }
                    else
                                        
                        LocalTopology(message.LocalTopology_SnppID, message.LocalTopology_availibleCapacity, message.LocalTopology_reachableSnppIdList, message.LocalTopology_areaName);
                    break;
            }
        }


        #endregion



        #region Incomming_Methodes_From_Standardization

        private void RouteQuery(int connectionID, string callingIpAddress, string calledIpAddress, int callingCapacity)
        {
            SignalMessage.Pair SNPPPair = new SignalMessage.Pair();
            SNPPPair.first = IPTOIDDictionary[callingIpAddress];
            SNPPPair.second = IPTOIDDictionary[calledIpAddress];
            

            //SignalMessage signalMessage = new SignalMessage();

            Vertex begin = graph.Vertices.Find(x => x.Id == SNPPPair.first);
            Vertex end = graph.Vertices.Find(x => x.Id == SNPPPair.second);

            List<string> areaNames = new List<string>();
            List<SignalMessage.Pair> snppPairs = new List<SignalMessage.Pair>();

            SNPPPair.second = interdomainLinks[end.Id];

            if (begin.AreaName.Equals(end.AreaName))
            {
                areaNames = null;
                snppPairs.Add(SNPPPair);
                RouteQueryResponse(connectionID, snppPairs, areaNames);
            }else
            {
                areaNames.Add(end.AreaName);
                snppPairs.Add(SNPPPair);
                SignalMessage.Pair interdomainPair = new SignalMessage.Pair();
                interdomainPair.first = interdomainLinks[end.Id];
                interdomainPair.second = end.Id;
                snppPairs.Add(interdomainPair);
                RouteQueryResponse(connectionID, snppPairs, areaNames);
            }
        }

        public void RouteQuery(int connectionID, int snppInId, string calledIpAddress, int callingCapacity)
        {

            //SignalMessage signalMessage = new SignalMessage();

            SignalMessage.Pair SNPPPair = new SignalMessage.Pair();
            SNPPPair.first = snppInId;
            SNPPPair.second = IPTOIDDictionary[calledIpAddress];

            List<SignalMessage.Pair> localSnppPairs = new List<SignalMessage.Pair>();
            localSnppPairs.Add(SNPPPair);

            Vertex begin = graph.Vertices.Find(x => x.Id == SNPPPair.first);
            Vertex end = graph.Vertices.Find(x => x.Id == SNPPPair.second);

            List<String> areaNames = new List<String>();
            areaNames.Add(end.AreaName);

            if (begin.AreaName.Equals(end.AreaName))
            {
                RouteQueryResponse(connectionID, localSnppPairs, null);
            }else
            {
                RouteQueryResponse(connectionID, localSnppPairs, areaNames);
            }
        }


        private void RouteQuery(int connectionID, SignalMessage.Pair snppIdPair, int callingCapacity)
        {

            Dijkstra dijkstra = new Dijkstra();
            Vertex begin = graph.Vertices.Find(x => x.Id == snppIdPair.first);
            Vertex end = graph.Vertices.Find(x => x.Id == snppIdPair.second);

            List<SignalMessage.Pair> snppIdPairs = dijkstra.runAlgorithm(graph, begin, end, callingCapacity);
   
            List<string> areaNames = new List<string>();

            foreach(SignalMessage.Pair pair in snppIdPairs)
            {
                //Vertex firstVertex = graph.Vertices.Find(x => x.Id == pair.first);
                Vertex secondVertex = graph.Vertices.Find(x => x.Id == pair.second);

                if(!secondVertex.AreaName.Equals(_myAreaName))
                {
                    areaNames.Add(secondVertex.AreaName);
                }
            }

            RouteQueryResponse(connectionID, snppIdPairs, areaNames);

        }
        //klasa, ktora tworzy graf sieci
        //RC wykorzystuje graf do wyznaczania sciezek dla polaczen
        //graf jest aktualizowany z kazda informacja od LRM
        public void IsUp(string areaName)
        {
            var lrm = LRMs.Find(x => x.AreaName.Equals(areaName));
            if (lrm == null)
            {
                Lrm l = new Lrm(areaName, this);
                LRMs.Add(l);
            }
            else
                KeepAlive(areaName);
        }
        public void KeepAlive(string areaName)
        {
            var item = LRMs.Find(x => x.AreaName.Equals(areaName));
            if(item != null)
            {
                LRMs.Find(x => x.AreaName.Equals(areaName)).keepAliveTimer.Stop();
                LRMs.Find(x => x.AreaName.Equals(areaName)).keepAliveTimer.Start();
            }

        }
        public void LocalTopology(int snppId, int availibleCapacity, List<int> reachableSnppIdList, string areaName)
        {
            Console.WriteLine(availibleCapacity);
            var item = graph.Vertices.Find(x => x.Id == snppId);
            //przypadek kiedy wierzcholek jeszcze nie istnieje
            if (item == null)
            {
                Vertex v = new Vertex(snppId, availibleCapacity, areaName);
                foreach (var point in reachableSnppIdList)
                {
                    var res = graph.Vertices.Find(x => x.Id == point);
                    if(res == null)
                    {
                        Vertex uncompleteVertex = new Vertex(point, 0, "unreachable");
                        graph.Vertices.Add(uncompleteVertex);
                        double weight = double.MaxValue;
                        Edge uncompleteEdge = new Edge(v, uncompleteVertex, 0,  weight);
                        v.addEdgeOut(uncompleteEdge);
                        graph.Edges.Add(uncompleteEdge);
                    }
                    else
                    {
                        double weight;
                        int capacity;
                        if (res.AreaName.Equals(areaName))
                        {
                            weight = 0;
                            capacity = int.MaxValue;
                        }
                        else
                        {
                            weight = 1;
                            capacity = Math.Min(v.Capacity, res.Capacity);
                        }
                        Edge edge = new Edge(v, res, capacity, weight);
                        graph.Vertices.Find(x => x.Id == point).addEdgeOut(edge);
                    }
                }
                graph.Vertices.Add(v);
            }
            //wierzcholek juz istnieje
            else
            {
                graph.Vertices.Find(x => x.Id == snppId).Capacity = availibleCapacity;
                graph.Vertices.Find(x => x.Id == snppId).AreaName = areaName;
                foreach (var point in reachableSnppIdList)
                {
                    var res = graph.Vertices.Find(x => x.Id == point);
                    if (res == null)
                    {
                        Vertex uncompleteVertex = new Vertex(point, 0, "unreachable");
                        graph.Vertices.Add(uncompleteVertex);
                        double weight = double.MaxValue;
                        Edge uncompleteEdge = new Edge(graph.Vertices.Find(x => x.Id == snppId), uncompleteVertex, 0, weight);
                        graph.Vertices.Find(x => x.Id == snppId).addEdgeOut(uncompleteEdge);
                        graph.Edges.Add(uncompleteEdge);
                    }
                    else
                    {
                        Edge existingEdge = graph.Edges.Find(x => x.Id.Equals(Edge.CreateName(snppId, res.Id)));
                        if ((existingEdge != null) && !(res.AreaName.Equals("unreachable")))
                        {
                            if (existingEdge.Weight == double.MaxValue)
                            {
                            }
                            else
                            {
                            }
                        }
                        else if (existingEdge == null)
                        {

                            double weight;
                            int capacity;
                            if (res.AreaName.Equals(areaName))
                            {
                                weight = 0;
                                capacity = int.MaxValue;
                            }
                            else
                            {
                                weight = 1;
                                capacity = Math.Min(item.Capacity, res.Capacity);
                            }
                            Edge edge = new Edge(graph.Vertices.Find(x=> x.Id == snppId), graph.Vertices.Find(x=> x.Id == res.Id), capacity, weight);
                            graph.Vertices.Find(x => x.Id == snppId).addEdgeOut(edge);
                            graph.Edges.Add(edge);
                        }
                        else
                        {
                            double weight;
                            int capacity;
                            if (res.AreaName.Equals(areaName))
                            {
                                weight = 0;
                                capacity = int.MaxValue;
                            }
                            else
                            {
                                weight = 1;
                                capacity = Math.Min(item.Capacity, res.Capacity);
                            }
                            graph.Edges.Find(x => x.Id.Equals(Edge.CreateName(snppId, res.Id))).Capacity = capacity;
                            graph.Edges.Find(x => x.Id.Equals(Edge.CreateName(snppId, res.Id))).Weight = weight;

                        }
                    }
                }
            }
            //tworzenie krawedzi pomiedzy wierzcholkami z tego samego SN
            if (!areaName.Equals(_myAreaName))
            {
                List<Vertex> area = new List<Vertex>();
                area = graph.Vertices.FindAll(x => x.AreaName.Equals(areaName));
                if (area != null)
                {
                    foreach (var v in area)
                    {
                        string edgeID = Edge.CreateName(graph.Vertices.Find(x => x.Id == snppId), v);
                        var res = graph.Edges.Find(x => x.Id.Equals(edgeID));
                        if ((res == null) && (v.Id != snppId))
                        {
                            int capacity = int.MaxValue;
                            double weight = 0;
                            Edge edge = new Edge(graph.Vertices.Find(x => x.Id == snppId), graph.Vertices.Find(x => x.Id == v.Id), capacity, weight);
                            graph.Vertices.Find(x => x.Id == snppId).addEdgeOut(edge);
                            graph.Edges.Add(edge);
                        }
                        edgeID = Edge.CreateName(v, graph.Vertices.Find(x => x.Id == snppId));
                        res = graph.Edges.Find(x => x.Id.Equals(edgeID));
                        if ((res == null) && (v.Id != snppId))
                        {
                            int capacity = int.MaxValue;
                            double weight = 0;
                            Edge edge = new Edge(graph.Vertices.Find(x => x.Id == v.Id), graph.Vertices.Find(x => x.Id == snppId), capacity, weight);
                            graph.Vertices.Find(x => x.Id == v.Id).addEdgeOut(edge);
                            graph.Edges.Add(edge);
                        }
                    }
                }
            }
            //sprawdzam czy w kierunku SNPP, ktorego rozgloszenie wlasnie otrzymalismy, nie sa skierowane jakies sciezki widma
            if (item != null)
            {
                foreach (var v in graph.Vertices)
                {
                    string edgeID = Edge.CreateName(v.Id, snppId);
                    var res = graph.Edges.Find(x => x.Id.Equals(edgeID));
                    if (res != null)
                    {
                        if (res.Weight == double.MaxValue)
                        {
                            double weight;
                            int capacity;
                            if (v.AreaName.Equals(areaName))
                            {
                                weight = 0;
                                capacity = int.MaxValue;
                            }
                            else
                            {
                                weight = 1;
                                capacity = Math.Min(graph.Vertices.Find(x => x.Id == snppId).Capacity, res.Capacity);
                            }
                            graph.Edges.Find(x => x.Id.Equals(edgeID)).Capacity = capacity;
                            graph.Edges.Find(x => x.Id.Equals(edgeID)).Weight = weight;
                        }
                    }
                }
            }
        }


        public void NetworkTopology (int snppId, List<int> reachableSnppIdList)
        {
            foreach (var id in reachableSnppIdList)
                interdomainLinks.Add(id, snppId);
        }
        #endregion



        #region Outcomming_Methodes_From_Standardization

        

        private void RouteQueryResponse(int connectionID, SignalMessage.Pair snppPair, int callingCapacity)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.RouteQueryResponse,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "RC",
                General_DestinationModule = "CC",

                ConnnectionID = connectionID,
                SnppIdPair = snppPair

            };

            _pc.SendSignallingMessage(message);
        }

        private void RouteQueryResponse(int connectionID,List<SignalMessage.Pair> snppPair,List<string> areaName)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.RouteQueryResponse,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "RC",
                General_DestinationModule = "CC",

                ConnnectionID = connectionID,
                IncludedSnppIdPairs = snppPair,
                IncludedAreaNames = areaName

            };

            SendMessageToPC(message);
        }



        #endregion

        #region Other
        public void OnNodeFailure(string areaName)
        {
            List<Vertex> v = graph.Vertices.FindAll(x => x.AreaName.Equals(areaName));
            foreach (var ver in v)
            {
                graph.Edges.RemoveAll(x => x.Begin.Id == ver.Id);
                graph.Edges.RemoveAll(x => x.End.Id == ver.Id);
            }
            graph.Vertices.RemoveAll(x => x.AreaName.Equals(areaName));
        }
        #endregion
    }
}
