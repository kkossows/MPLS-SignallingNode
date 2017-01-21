
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


        #region Incomming_Methodes_From_Standardization
        private void ConnectionRequestFromNcc(int connectionID, string callingIpAddress, string calledIpAddress, int callingCapacity)
        {
            //dodajemy wpis w tablicy dla danego connectionID
            _connectionsList.Add(
                new ConnectionTableRecord
                {
                    ConnectionID = connectionID,
                    AllocatedCapacity = callingCapacity,
                    AllocatedSnps = new List<List<SNP>>(),
                    AllocatedSnpAreaName = new List<string>(),
                    Status = "inProgress"
                });
            
            //dodajemy wpis w słowniku dzięki czemu będziemy wiedzieć, że to connection ID odpowieada temu indexowi w liście
            _indexInListOfConnection.Add(connectionID, _connectionsList.Capacity - 1);

            //wysyłamy wiadomosć RouteQuery do RC
            RouteQuery(connectionID, callingIpAddress, calledIpAddress, callingCapacity);
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

                //połączenie wewnątrzdomenowe
                if (includedSnppIdPairs.Count == 1)
                {
                    //zmieniamy typ połączenia w tablicy na nie międzydomenowy
                    record.IsInterdomain = false;

                    //odpytujemy RC o szczegóły połączenia pomiędzy zadną parą SNPP
                    RouteQuery(
                        connectionID,
                        new SignalMessage.Pair() { first = includedSnppIdPairs[0].first, second = includedSnppIdPairs[0].second },
                        callingCapacity);

                    //wyjdź z metody
                    return;
                }

                //połączenie międzycdomenowe
                else
                {
                    //zmieniamy typ połączenia w tablicy na nie międzydomenowy
                    record.IsInterdomain = true;
                    record.Status = "establishingInterdomainLink";

                    //tutaj musimy wysłac druga parę do własnego LRM aby otrzymać etykietę niezbędną do PeerCoordination
                    LinkConnectionRequest(
                        connectionID,
                        new SignalMessage.Pair() { first = includedSnppIdPairs[1].first, second = includedSnppIdPairs[1].second },
                        callingCapacity);

                    //wyjdź z metody
                    return;
                }
            }
            #endregion

            #region Full_AreaName
            else
            {
                //tworzymy liste nazw i o tym samym id wskaznik na liste SNP
                for (int i = 0; i < includedAreaNames.Count; i++)
                {
                    record.AllocatedSnpAreaName.Add(includedAreaNames[i]);
                    record.AllocatedSnps.Add(new List<SNP>());
                }

                //uzupełniamy liste par snpp do zaalokowania
                for (int i = 0; i < includedSnppIdPairs.Count; i++)
                    record.SnppIdPairToAllocate.Add(includedSnppIdPairs[i]);

                //wysyłamy pierwszą parę do LRM aby zaalokował (nastepne będą wysyłane jak dostaniemy odpowiedź)
                LinkConnectionRequest(connectionID, record.SnppIdPairToAllocate[0], callingCapacity);

                //usuwamy parę z listy
                record.SnppIdPairToAllocate.RemoveAt(0);
            }
            #endregion
        }

        private void LinkConnectionResponse(int connectionID, bool isAccepted, List<SNP> receivedSnps, List<string> receivedSnpsAreaNames)
        {
            //wyszukujemy wskaźnik na odpowiednią tablicę
            ConnectionTableRecord record = _connectionsList[_indexInListOfConnection[connectionID]];

            #region Step_1-Sprawdz_status_odpowiedzi
            if(isAccepted)
            {
                //odpowiedź jest pozytywna - udało się zaalokować łącze
                for(int i=0; i< receivedSnpsAreaNames.Count; i++)
                {
                    int index = record.AllocatedSnpAreaName.IndexOf(receivedSnpsAreaNames[i]);
                    record.AllocatedSnps[i].Add(receivedSnps[i]);
                }
            }
            else
            {
                //odpowiedź jest negatywna - nie udało się zaalowkować łącza
                SignallingNodeDeviceClass.MakeSignallingLog("CC", "ERROR - LinkConnectionRequest is not accepted");
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("CC", "ERROR - LinkConnectionRequest is not accepted");


                //dopisać kod, który wykonuje sie w sytuacji odmowy alokacji
                //...
                //...
                //...?
            }
            #endregion


            #region Step_2-Sprawdź_status_rekordu
            if (record.IsInterdomain && record.Status == "establishingInterdomainLink")
            {
                
                //Wyslij do RC pierwsza parę złożoną ze zmiennych boudaryFirst boundarySecond znajdującej się w rekordzie
                SignalMessage message = new SignalMessage()
                {
                    General_SignalMessageType = SignalMessage.SignalType.RouteQuery,
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
            else
            {
                //sprawdź, czy mamy jeszcze jakieś łacza lokalnie do alokowania
                if (record.SnppIdPairToAllocate.Count != 0)
                {
                    SignalMessage.Pair snppPairToAllocate = record.SnppIdPairToAllocate[0];
                    LinkConnectionRequest(connectionID, snppPairToAllocate, record.AllocatedCapacity);

                    //jeżeli mamy to usuń to łącze
                    record.SnppIdPairToAllocate.Remove(snppPairToAllocate);

                    //wyjdź z metody
                    return;
                }
                else
                {
                    //ustaw nowy status rekordu oznaczający, że wszystkie lokalne linki zostały ustawione
                    record.Status = "localLinksAllocated";

                    //sprawdź, czy nasze połaczenie przechodzi przez jakieś podsieci
                    if(record.AllocatedSnpAreaName.Count > 1)   //zawsze będzie nasza podsieć
                    {

                    }
                    else
                    {
                        //jak nie ma już żadnej podsieci to znaczy, że zakończyliśmy działania tego CC i wysyłamy ConnectionResponse
                        //ConnectionResponse(connectionID, true, )
                    }
                }

            }
            #endregion
        }
        #endregion


        #region Outcomming_Methodes_From_Standardization
        private void RouteQuery(int connectionID, string callingIpAddress, string calledIpAddress, int callingCapacity)
        {
            //wysyłamy wiadomośc do RC z prośbą o wyliczenie ścieżki
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.RouteQuery,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "CC",
                General_DestinationModule = "RC",

                ConnnectionID = connectionID,
                CallingIpAddress = callingIpAddress,
                CalledIpAddress = calledIpAddress,
                CallingCapacity = callingCapacity
            };

            //wysyłamy żądanie do RC
            _pc.SendSignallingMessage(message);
        }
        private void RouteQuery(int connectionID, SignalMessage.Pair snppIdPair, int callingCapacity)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.RouteQuery,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "CC",
                General_DestinationModule = "RC",

                ConnnectionID = connectionID,
                SnppIdPair = snppIdPair,
                CallingCapacity = callingCapacity
            };

            //wysyłam wiadomość
            _pc.SendSignallingMessage(message);
        }

        private void LinkConnectionRequest(int connectionID, SignalMessage.Pair connectionSnppIdPair, int callingCapacity)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.LinkConnectionRequest,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "CC",
                General_DestinationModule = "LRM",

                ConnnectionID = connectionID,
                SnppIdPair = connectionSnppIdPair,
                CallingCapacity = callingCapacity
            };

            //wysyłamy żądanie do RC
            _pc.SendSignallingMessage(message);
        }




        #endregion

        #region Other_Methodes
        private void MakeNewFibRecords(int interfaceIn, int labelIn, int interfaceOut, int labelOut, string operation)
        {
            //gdzies to przekaż albo coś UZUPEŁNIć !!
        }
        #endregion
    }
}