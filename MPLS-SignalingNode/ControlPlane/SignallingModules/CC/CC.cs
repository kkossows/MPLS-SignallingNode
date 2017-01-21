
using MPLS_SignalingNode;
using System.Collections.Generic;

namespace ControlPlane
{
    class CC
    {
        #region Variables
        private string _configurationFilePath;
        private string _localPcIpAddress;
        private string _areaName;
        private bool _isInLsrSubnetwork;

        private PC _pc;
        private List<ConnectionTableRecord> _connectionsList;
        private Dictionary<int, int> _indexInListOfConnection;
        #endregion

        #region Properties
        public PC LocalPC { set { _pc = value; } }
        #endregion


        #region Main_Methodes
        public CC(string configurationFilePath)
        {
            InitialiseVariables(configurationFilePath);
        }
        private void InitialiseVariables(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;

            CC_XmlSchema schema = new CC_XmlSchema();
            schema = CC_LoadingXmlFile.Deserialization(_configurationFilePath);

            //miejsce na przypisanie zmiennych
            _localPcIpAddress = schema.XML_localPcIpAddress;
            _areaName = schema.XML_areaName;
            _isInLsrSubnetwork = schema.XML_IsInLsrSubnetwork;

            _connectionsList = new List<ConnectionTableRecord>();
            _indexInListOfConnection = new Dictionary<int, int>();
        }
        #endregion


        #region PC_Cooperation_Methodes
        private void SendMessageToPC(SignalMessage message)
        {
            _pc.SendSignallingMessage(message);
            SignallingNodeDeviceClass.MakeSignallingLog("LRM", "INFO - Signalling message send to PC module");
        }
        public void ReceiveMessageFromPC(SignalMessage message)
        {
            switch (message.General_SignalMessageType)
            {
                case SignalMessage.SignalType.ConnectionRequest:
                    if (message.General_SourceModule == "NCC")
                        ConnectionRequestFromNcc(message.ConnnectionID, message.CallingIpAddress, message.CalledIpAddress, message.CallingCapacity);
                    else
                        ConnectionRequest(message.ConnnectionID, message.SnpIn, message.SnpOut);
                    break;
                case SignalMessage.SignalType.RouteQueryResponse:
                    RouteQueryResponse(message.ConnnectionID, message.IncludedSnppIdPairs, message.IncludedAreaNames);
                    break;
                case SignalMessage.SignalType.LinkConnectionResponse:
                    LinkConnectionResponse(message.ConnnectionID,message.IsAccepted, message.LinkConnection_AllocatedSnpList, message.LinkConnection_AllocatedSnpAreaNameList);
                    break;
            }
        }
        #endregion


        #region Methodes_From_Standardization
        private void ConnectionRequestFromNcc(int connectionID, string callingIpAddress, string calledIpAddress, int callingCapacity)
        {
            //wysyłamy wiadomośc do RC z prośbą o wyliczenie ścieżki
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.RouteQueryRequest,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "CC",
                General_DestinationModule = "RC",

                ConnnectionID = connectionID,
                CallingIpAddress = callingIpAddress,
                CalledIpAddress = calledIpAddress,
                CallingCapacity = callingCapacity

            };

            //dodajemy wpis w tablicy dla danego connectionID
            _connectionsList.Add(
                new ConnectionTableRecord
                {
                    ConnectionID = connectionID,
                    AllocatedCapacity = callingCapacity,
                    AllocatedSnps = new List<Snp_Pair>(),
                    AllocatedSnpPairsAreaName = new List<string>(),
                    Status = "inProgress"
                });
            
            //dodajemy wpis w słowniku dzięki czemu będziemy wiedzieć, że to connection ID odpowieada temu indexowi w liście
            _indexInListOfConnection.Add(connectionID, _connectionsList.Capacity - 1);

            //wysyłamy żądanie do RC
            _pc.SendSignallingMessage(message);

        }
        private void ConnectionRequest(int connectionID, SNP snpIn, SNP snpOut)
        {
            if (!_isInLsrSubnetwork)
            {
                
            }
            else
            {
                //tutaj wywołaj funkcję, żeby robić odpowiednie wpisyw do fiba
                MakeNewFibRecords(
                    snpIn._snppID, snpIn._allocatedLabel,
                    snpOut._snppID, snpOut._allocatedLabel,
                    "swap");
            }
        }
        
        private void RouteQueryResponse(int connectionID, List<SignalMessage.Pair> includedSnppIdPairs, List<string> includedAreaNames)
        {
            //wyszukujemy wskaźnik na odpowiednią tablicę
            ConnectionTableRecord record = _connectionsList[_indexInListOfConnection[connectionID]];
            //odczytuje przepustoowość z tablicy

            int callingCapacity = record.AllocatedCapacity;

            #region Empty_AreaName
            //jeżeli includedAreaName jest pusta to znaczy, że jest to pierwsza wiadomość i dostaliśmy ogólne SNPP in i out
            if (includedAreaNames.Count == 0)
            {
                //zapisz do tablicy graniczne zbiory SNPP
                record.LocalBoundaryFirstSnppID = includedSnppIdPairs[0].first;
                record.LocalBoundarySecondSnppID = includedSnppIdPairs[0].second;

                if (includedSnppIdPairs.Count == 1 )
                {
                    //połączenie wewnątrzdomenowe
                    //odpytujemy RC o szczegóły połączenia pomiędzy zadną parą SNPP
                    SignalMessage message = new SignalMessage()
                    {
                        General_SignalMessageType = SignalMessage.SignalType.RouteQueryRequest,
                        General_SourceIpAddress = _localPcIpAddress,
                        General_DestinationIpAddress = _localPcIpAddress,
                        General_SourceModule = "CC",
                        General_DestinationModule = "RC",

                        ConnnectionID = connectionID,
                        SnppIdPair = new SignalMessage.Pair() {
                            first = includedSnppIdPairs[0].first,
                            second = includedSnppIdPairs[0].second
                        },
                        CallingCapacity = callingCapacity
                    };

                    //zmieniamy typ połączenia w tablicy na nie międzydomenowy
                    record.IsInterdomain = false;

                    //wysyłamy żądanie do RC
                    _pc.SendSignallingMessage(message);
                }
                else
                {
                    //połączenie międzydomenowe
                    //tutaj musimy wysłac druga parę do własnego LRM aby otrzymać etykietę niezbędną do PeerCoordination
                    SignalMessage message = new SignalMessage()
                    {
                        General_SignalMessageType = SignalMessage.SignalType.LinkConnectionRequest,
                        General_SourceIpAddress = _localPcIpAddress,
                        General_DestinationIpAddress = _localPcIpAddress,
                        General_SourceModule = "CC",
                        General_DestinationModule = "LRM",

                        ConnnectionID = connectionID,
                        SnppIdPair = new SignalMessage.Pair{
                            first = includedSnppIdPairs[1].first,
                            second = includedSnppIdPairs[1].first
                        },
                        CallingCapacity = callingCapacity
                    };

                    //zmieniamy typ połączenia w tablicy na nie międzydomenowy
                    record.IsInterdomain = true;
                    record.Status = "establishingInterdomainLink";

                    //wysyłamy żądanie do RC
                    _pc.SendSignallingMessage(message);
                }
            }
            #endregion

            #region Full_AreaName
            else
            {
                //tworzymy liste nazw i o tym samym id wskaznik na liste SNP
                for (int i = 0; i < includedAreaNames.Capacity; i++)
                {
                    record.AllocatedSnpPairsAreaName.Add(includedAreaNames[i]);
                    record.AllocatedSnps.Add(new List<SNP>());
                }

                //uzupełniamy liste par snpp do zaalokowania
                for (int i = 0; i < includedSnppIdPairs.Capacity; i++)
                    record.SnppIdPairToAllocate.Add(includedSnppIdPairs[i]);

                //wysyłamy pierwszą parę do LRM aby zaalokował (nastepne będą wysyłane jak dostaniemy odpowiedź)
                SignalMessage message = new SignalMessage()
                {
                    General_SignalMessageType = SignalMessage.SignalType.RouteQueryRequest,
                    General_SourceIpAddress = _localPcIpAddress,
                    General_DestinationIpAddress = _localPcIpAddress,
                    General_SourceModule = "CC",
                    General_DestinationModule = "LRM",

                    ConnnectionID = connectionID,
                    SnppIdPair = record.SnppIdPairToAllocate[0],
                    CallingCapacity = callingCapacity

                };

                //usuwamy parę z listy i wysyłamy
                record.SnppIdPairToAllocate.RemoveAt(0);
                _pc.SendSignallingMessage(message);
            }
            #endregion
        }

        private void LinkConnectionResponse(int connectionID, bool isAccepted, List<SNP> receivedSnps, List<string> receivedSnpsAreaNames)
        {
            //wyszukujemy wskaźnik na odpowiednią tablicę
            ConnectionTableRecord record = _connectionsList[_indexInListOfConnection[connectionID]];

            #region Step_1
            if(isAccepted)
            {
                //odpowiedź jest pozytywna - udało się zaalokować łącze
                for(int i=0; i< receivedSnpsAreaNames.Count; i++)
                {
                    int index = record.AllocatedSnpPairsAreaName.IndexOf(receivedSnpsAreaNames[i]);
                    record.AllocatedSnps[i].Add(receivedSnps[i]);
                }

            }
            else
            {
                //odpowiedź jest negatywna - nie udało się zaalowkować łącza

            }
            #endregion


            #region Step_2
            if (record.IsInterdomain && record.Status == "establishingInterdomainLink")
            {






                //Wyslij do RC pierwsza parę złożoną ze zmiennych boudaryFirst boundarySecond znajdującej się w rekordzie
                SignalMessage message = new SignalMessage()
                {
                    General_SignalMessageType = SignalMessage.SignalType.RouteQueryRequest,
                    General_SourceIpAddress = _localPcIpAddress,
                    General_DestinationIpAddress = _localPcIpAddress,
                    General_SourceModule = "CC",
                    General_DestinationModule = "RC",

                    ConnnectionID = record.ConnectionID,
                    SnppIdPair = new SignalMessage.Pair
                    {
                        first = record.LocalBoundaryFirstSnppID,
                        second = record.LocalBoundarySecondSnppID
                    },
                    CallingCapacity = record.AllocatedCapacity
                };

            }
        }
        #endregion






        #endregion





        #region Other_Methodes
        private void MakeNewFibRecords(int interfaceIn, int labelIn, int interfaceOut, int labelOut, string operation)
        {
            //gdzies to przekaż albo coś UZUPEŁNIć !!
        }
        #endregion
    }
}



/*
 * Wysyłane metody:
 * -> RouteQueryRequest
 * -> ConnectionRequest
 * -> LinkConnectionRequest
 * -> PeerCoordination
 * -> ConnectionRequestOut
 * -> PeerCoordinationOut
 * 
 * Odbierane metody:
 * -> ConnectionRequest
 * -> RouteQueryResponse
 * -> LinkConnectionResponse
 * -> PeerCoordination
 * -> ConnectionRequestOut
 * -> PeerCoordinationOut
 * 

 *