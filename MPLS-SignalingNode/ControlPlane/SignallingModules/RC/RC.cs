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
        

        private PC _pc;
        #endregion


        #region Main_Methodes
        public RC(string configurationFilePath)
        {
            //InitialiseVariables(configurationFilePath);
        }
        private void InitialiseVariables(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;

            RC_XmlSchame tmp = new RC_XmlSchame();
            tmp = RC_LoadingXmlFile.Deserialization(_configurationFilePath);

            //miejsce na przypisanie zmiennych
        }
        #endregion


        #region Properties
        public PC LocalPC { set { _pc = value; } }
        #endregion


        #region PC_Cooperation_Methodes
        private void SendMessageToPC(SignalMessage message)
        {
            _pc.SendSignallingMessage(message);
            SignallingNodeDeviceClass.MakeSignallingLog("RC", "INFO - Signalling message send to PC module");
        }
        public void ReceiveMessageFromPC(SignalMessage message)
        {
            switch (message.General_SignalMessageType)
            {
                case SignalMessage.SignalType.RouteQuery:
                    if (message.CalledIpAddress != null)    //trzeba sprawdzić, bo jak tu będzie błąd to trzeba w CC ustawiać ten calledIpAddress na nulla
                        RouteQuery(message.ConnnectionID, message.CallingIpAddress, message.CalledIpAddress, message.CallingCapacity);
                    else
                        RouteQuery(message.ConnnectionID, message.SnppIdPair, message.CallingCapacity, message.General_SourceIpAddress);
                    break;
                case SignalMessage.SignalType.LocalTopology:
                    LocalTopology(message.LocalTopology_SnppID, message.LocalTopology_availibleCapacity, message.LocalTopology_reachableSnppIdList, message.LocalTopology_areaName);
                    break;
            }
        }


        #endregion



        #region Incomming_Methodes_From_Standardization
        //sourceIpadrees dodany w celach testowych!!!
        private void RouteQuery(int connectionID, string callingIpAddress, string calledIpAddress, int callingCapacity)
        {
            if (connectionID == 111 && callingIpAddress == "127.0.1.101" && calledIpAddress == "127.0.1.102" && callingCapacity == 1000)
                RouteQueryResponse(
                    111,
                    new List<SignalMessage.Pair>()
                    {
                        new SignalMessage.Pair() { first = 1, second = 2 }
                    },
                    null);

        }
        private void RouteQuery(int connectionID, SignalMessage.Pair snppIdPair, int callingCapacity, string sourceIpAddress)
        {
            if (connectionID == 111 && snppIdPair.first == 1 && snppIdPair.second == 2 && callingCapacity == 1000 && sourceIpAddress == "127.0.1.201")
                RouteQueryResponse(
                    111,
                    new List<SignalMessage.Pair>()
                    {
                        new SignalMessage.Pair() { first = 1, second = 11 },
                        new SignalMessage.Pair() { first = 2, second = 12 }
                    },
                    new List<string>()
                    {
                        "SN_1"
                    });
            if (connectionID == 111 && snppIdPair.first == 1 && snppIdPair.second == 2 && callingCapacity == 1000 && sourceIpAddress == "127.0.1.202")
                RouteQueryResponse(
                    111,
                    new List<SignalMessage.Pair>()
                    {
                        new SignalMessage.Pair() { first = 1, second = 11 },
                        new SignalMessage.Pair() { first = 13, second = 31 },
                        new SignalMessage.Pair() { first = 32, second = 22 },
                        new SignalMessage.Pair() { first = 21, second = 2 }
                    },
                    new List<string>()
                    {
                        "SN_1_1",
                        "SN_1_2",
                        "SN_1_3"
                    });
        }

        public void LocalTopology(int snppId, int availibleCapacity, List<int> reachableSnppIdList, string areaName)
        {

        }

        #endregion



        #region Outcomming_Methodes_From_Standardization
        private void RouteQueryResponse(int connectionID, List<SignalMessage.Pair> includedSnppIdPairs, List<string> includedAreaNames)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.RouteQueryResponse,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "RC",
                General_DestinationModule = "CC",

                ConnnectionID = connectionID,
                IncludedSnppIdPairs = includedSnppIdPairs,
                IncludedAreaNames = includedAreaNames
            };

            //wysyłamy żądanie do RC
            _pc.SendSignallingMessage(message);
        }



        #endregion
    }
}
