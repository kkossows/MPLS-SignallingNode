using MPLS_SignalingNode;
using System.Collections.Generic;
using DTO.ControlPlane;

namespace ControlPlane
{
    class CC
    {
        #region Variables
        private string _configurationFilePath;
        private string _localPcIpAddress;
        private string _areaName;

        private bool _isInLsrSubnetwork;
        private Dictionary<int, int> _connectionIdToFibIndex;
        private string _higherAreaName; //nazwa wyższej podsieci, do kórej należy

        private PC _pc;
        private List<ConnectionTableRecord> _connectionsList;
        private Dictionary<int, int> _indexInListOfConnection;

        private Dictionary<string, string> _connectedCcDestinationAddrress;
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
            _higherAreaName = schema.XML_higherAreaName;

            _connectionsList = new List<ConnectionTableRecord>();
            _indexInListOfConnection = new Dictionary<int, int>();

            //tworzę słownik składający się z areaName i adresuIP obsługującego go PC
            _connectedCcDestinationAddrress = new Dictionary<string, string>();
            foreach (CCDescription element in schema.XML_CCList)
            {
                _connectedCcDestinationAddrress.Add(element.areaName, element.ipAddress);
            }

            //tworzę słownik nowych indeksów w fibie, zwiazanych z danymi połaczeniami
            _connectionIdToFibIndex = new Dictionary<int, int>();
            foreach (FIBDescription element in schema.XML_FIBList)
            {
                _connectionIdToFibIndex.Add(element.connectionId, element.fibIndex);
            }
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
                        ConnectionRequest_Analyse(message.ConnnectionID, message.CallingIpAddress, message.CalledIpAddress, message.CallingCapacity);
                    else
                        ConnectionRequest_Analyse(message.ConnnectionID, message.SnpIn, message.SnpOut, message.CallingCapacity);
                    break;
                case SignalMessage.SignalType.RouteQueryResponse:
                    RouteQueryResponse(message.ConnnectionID, message.IncludedSnppIdPairs, message.IncludedAreaNames);
                    break;
                case SignalMessage.SignalType.LinkConnectionResponse:
                    LinkConnectionResponse(message.ConnnectionID,message.IsAccepted, message.LinkConnection_AllocatedSnpList, message.LinkConnection_AllocatedSnpAreaNameList);
                    break;
                case SignalMessage.SignalType.ConnectionResponse:
                    ConnectionResponse_Analyse(message.ConnnectionID, message.IsAccepted);
                    break;
            }
        }
        #endregion


        #region Incomming_Methodes_From_Standardization
        private void ConnectionRequest_Analyse(int connectionID, string callingIpAddress, string calledIpAddress, int callingCapacity)
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
            _indexInListOfConnection.Add(connectionID, _connectionsList.Count - 1);

            //wysyłamy wiadomosć RouteQuery do RC
            RouteQuery(connectionID, callingIpAddress, calledIpAddress, callingCapacity);
        }
        private void ConnectionRequest_Analyse(int connectionID, SNP snpIn, SNP snpOut, int callingCapacity)
        {
            #region CC_nie_jest_końcowym_ogniwem(nie_jest_w_routerze)
            if (!_isInLsrSubnetwork)
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
                _indexInListOfConnection.Add(connectionID, _connectionsList.Count - 1);

                //wysyłamy wiadomosć RouteQuery do RC
                RouteQuery(
                    connectionID,
                    new SignalMessage.Pair { first = snpIn._snppID, second = snpOut._snppID },
                    callingCapacity);
            }
            #endregion
            #region CC_jest_końcowym_ogniwem(jest_w_routerze)
            else
            {
                //tutaj wywołaj funkcję, żeby robić odpowiednie wpisyw do fiba
                int indexInFibTable = MakeNewFibRecords(
                    snpIn._snppID, snpIn._allocatedLabel,
                    snpOut._snppID, snpOut._allocatedLabel,
                    "swap");

                //dodaj wpis do słownika, związany z connectionID i indexemWpisuWFibie
                _connectionIdToFibIndex.Add(connectionID, indexInFibTable);

                //znajdź adres wyższego bloku funckyjnego, z którego musiało pochodzic dane żądanie
                string destinationIpAddress = _connectedCcDestinationAddrress[_higherAreaName];

                //wyslij wiadomość potwierdzającą wykonanie zadania
                ConnectionResponse(connectionID, true, destinationIpAddress);
            }
            #endregion
        }
        private void ConnectionResponse_Analyse(int connectionID, bool isAccepted)
        {
            //wyszukujemy wskaźnik na odpowiednią tablicę
            ConnectionTableRecord record = _connectionsList[_indexInListOfConnection[connectionID]];

            //zwiększ liczbe otrzmyanych wiadomości
            record.NumberOfResponse++;

            //sprawdzamy, czy otrzymaliśmy wszystkie odpowiedzi
            if (record.NumberOfResponse == record.NumberOfRequest)
            {
                //zmień status na established
                record.Status = "established";

                //wyczyść zmienne w rekordzie
                record.NumberOfRequest = 0;
                record.NumberOfResponse = 0;

                if (_higherAreaName != "")
                {
                    //trzeba odeslać wiadomość do CC

                    //znajdź adres wyżej w hierarchii węzła sterowania z którego musiało pochodzić pierwotne żądanie
                    string destinationIpAddress = _connectedCcDestinationAddrress[_higherAreaName];

                    //wyslij wiadomość potwierdzającą wykonanie zadania
                    ConnectionResponse(connectionID, true, destinationIpAddress);
                }
                else
                {
                    //trzeba odeslać wiadomośc do NCC
                    //znajdź etykiete wejściową i wyjściową
                    int labelIn = 0;
                    int labelOut = 0;

                    //przeszukaj liste AllocatedSNP związaną z lokal areaName i znajdz SNP skojarzony z localBoundaryFirstSnppID
                    foreach(SNP element in record.AllocatedSnps[0])
                    {
                        if (element._snppID == record.LocalBoundaryFirstSnppID)
                            labelIn = element._allocatedLabel;
                        if (element._snppID == record.LocalBoundarySecondSnppID)
                            labelOut = element._allocatedLabel;
                        if ((labelIn != 0) && (labelOut != 0))
                            break;
                    }

                    //wyślij wiadomośc do NCC
                    ConnectionResponse(connectionID, true, labelIn, labelOut);
                }
            }
            //jak nie to nic nie rób, czekaj dalej
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
                //uzupełniam tabele o podsieć do której należe
                record.AllocatedSnpAreaName.Add(_areaName);
                record.AllocatedSnps.Add(new List<SNP>());

                //uzupełniam tabele o nazwy uwikłanych podsieci oraz alokuje listy na SNP związane z nimi
                for (int i = 1; i < includedAreaNames.Count; i++)
                {
                    record.AllocatedSnpAreaName.Add(includedAreaNames[i]);
                    record.AllocatedSnps.Add(new List<SNP>());
                }

                //ustawiamy zmienną, mówiącą o ilości wysyłanych wiaodmości LinkConnectionRequest
                int numberOfPairs = includedSnppIdPairs.Count;
                record.NumberOfRequest = numberOfPairs;

                //wyzeruj liczbę otrzymanych wiadomości zwrotnych
                record.NumberOfResponse = 0;

                //wysyłamy wszystkie requesty na raz
                for (int i = 0; i < numberOfPairs; i++)
                    LinkConnectionRequest(connectionID, includedSnppIdPairs[i], callingCapacity);
            }
            #endregion
        }
        private void LinkConnectionResponse(int connectionID, bool isAccepted, List<SNP> receivedSnps, List<string> receivedSnpsAreaNames)
        {
            //wyszukujemy wskaźnik na odpowiednią tablicę
            ConnectionTableRecord record = _connectionsList[_indexInListOfConnection[connectionID]];

            //zwiększ liczbę otrzymanych wiadomości
            record.NumberOfResponse++;

            #region Step_1-Sprawdz_status_odpowiedzi
            if (isAccepted)
            {
                //odpowiedź jest pozytywna - udało się zaalokować łącze
                //uzupełniam odpowiednie listy danymi zawartymi w odpowiedzi
                for(int i=0; i< receivedSnpsAreaNames.Count; i++)
                {
                    int index = record.AllocatedSnpAreaName.IndexOf(receivedSnpsAreaNames[i]);
                    record.AllocatedSnps[index].Add(receivedSnps[i]);
                }
            }
            else
            {
                //odpowiedź jest negatywna - nie udało się zaalowkować łącza
                SignallingNodeDeviceClass.MakeSignallingLog("CC", "ERROR - LinkConnectionRequest is not accepted");
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("CC", "ERROR - LinkConnectionRequest is not accepted");

                //dopisać kod, który wykonuje sie w sytuacji odmowy alokacji
                return;
            }
            #endregion

            #region Step_2-Sprawdź_czy_odebrałem_wszystkie_odpowiedzi_czy_mam_czekac_dalej
            if(record.NumberOfResponse == record.NumberOfRequest)
            {
                //otrzymałem wszystkie odpowiedzi
                //ustaw nowy status rekordu oznaczający, że wszystkie lokalne linki zostały ustawione
                    record.Status = "localLinksAllocated";

                //sprawdź, czy nasze połaczenie przechodzi przez jakieś podsieci
                if (record.AllocatedSnpAreaName.Count > 1)   //zawsze będzie nasza podsieć
                {
                    //ustaw nowe wartości Response i Request
                    record.NumberOfRequest = record.AllocatedSnpAreaName.Count - 1;
                    record.NumberOfResponse = 0;

                    //wyślij do każdego areaName != localAreaName wiadomosć ConnectionRequest
                    //pierwszy wiersz dotyczy lokalnej sytuacji więc go omijamy
                    for (int i = 1; i < record.NumberOfRequest + 1; i++)
                    {
                        SNP snpIn = record.AllocatedSnps[i][0];
                        SNP snpOut = record.AllocatedSnps[i][1];

                        //znajdź adres docelowy PC obsługującego CC niższego rzędu związanego z daną areaname
                        string destinationIpAddress = _connectedCcDestinationAddrress[record.AllocatedSnpAreaName[i]];

                        //wyślij do CC niższego rzędu wiadomośc connectionRequest
                        ConnectionRequest(connectionID, snpIn, snpOut, record.AllocatedCapacity, destinationIpAddress);
                    }
                }
                else
                {
                    //jak nie ma już żadnej podsieci to znaczy, że zakończyliśmy działania tego CC i wysyłamy ConnectionResponse do CC wyższego

                    //znajdź adres docelowy PC obsługującego CC wyższego rzędu związanego z daną areaname
                    string destinationIpAddress = _connectedCcDestinationAddrress[_higherAreaName];

                    ConnectionResponse(connectionID, true, destinationIpAddress);

                    //zmień status na established
                    record.Status = "established";
                }
            }
            else
            {
                //nie otrzymałem wszystkich więc czekam na pozostałe
                return;
            }

            #endregion
            
            //if (record.IsInterdomain && record.Status == "establishingInterdomainLink")
            //{

            //    //Wyslij do RC pierwsza parę złożoną ze zmiennych boudaryFirst boundarySecond znajdującej się w rekordzie
            //    SignalMessage message = new SignalMessage()
            //    {
            //        General_SignalMessageType = SignalMessage.SignalType.RouteQuery,
            //        General_SourceIpAddress = _localPcIpAddress,
            //        General_DestinationIpAddress = _localPcIpAddress,
            //        General_SourceModule = "CC",
            //        General_DestinationModule = "RC",

            //        ConnnectionID = record.ConnectionID,
            //        SnppIdPair = new SignalMessage.Pair
            //        {
            //            first = record.LocalBoundaryFirstSnppID,
            //            second = record.LocalBoundarySecondSnppID
            //        },
            //        CallingCapacity = record.AllocatedCapacity
            //    };

            //}
            //else
            //{
            //    //ustaw nowy status rekordu oznaczający, że wszystkie lokalne linki zostały ustawione
            //    record.Status = "localLinksAllocated";

            //    //sprawdź, czy nasze połaczenie przechodzi przez jakieś podsieci
            //    if (record.AllocatedSnpAreaName.Count > 1)   //zawsze będzie nasza podsieć
            //    {
            //        //zaalokuj nowe listy
            //        record.NumberOfCcConnectionRequestToSend = record.AllocatedSnpAreaName.Count;
            //        int
            //            //tworzę listę odpowiedzi
            //            record.IsConnectionResponse = new List<bool>(listCount);
            //        record.ConnectionResponseStatus = new List<bool>(listCount);

            //        //wyślij do każdego areaName != localAreaName wiadomosć ConnectionRequest
            //        //pierwszy wiersz dotyczy lokalnej sytuacji więc go omijamy
            //        for (int i = 1; i < listCount; i++)
            //        {
            //            SNP snpIn = record.AllocatedSnps[i][0];
            //            SNP snpOut = record.AllocatedSnps[i][1];

            //            ConnectionRequest(connectionID, snpIn, snpOut);
            //        }
            //    }
            //    else
            //    {
            //        //jak nie ma już żadnej podsieci to znaczy, że zakończyliśmy działania tego CC i wysyłamy ConnectionResponse
            //        ConnectionResponse(connectionID, true);

            //        //zmień status na established
            //        record.Status = "established";
            //    }
            //}
            //#endregion
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
        //trzeba to dopracować!!
        private void ConnectionResponse(int connectionID, bool isAccepted, string destinationIpAddress)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.ConnectionResponse,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = destinationIpAddress,
                General_SourceModule = "CC",
                General_DestinationModule = "CC",

                ConnnectionID = connectionID,
                IsAccepted = isAccepted
            };

            //wysyłamy żądanie do RC
            _pc.SendSignallingMessage(message);
        }
        private void ConnectionResponse(int connectionID, bool isAccepted,int labelIn,int labelOut)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.ConnectionResponse,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "CC",
                General_DestinationModule = "NCC",

                ConnnectionID = connectionID,
                IsAccepted = isAccepted,
                LabelIN = labelIn,
                LabelOUT = labelOut
            };

            //wysyłamy żądanie do RC
            _pc.SendSignallingMessage(message);
        }
        private void ConnectionRequest(int connectionID, SNP snpIn, SNP snpOut, int callingCapacity, string destinationIpAddress)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.ConnectionResponse,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = destinationIpAddress,
                General_SourceModule = "CC",
                General_DestinationModule = "CC",

                ConnnectionID = connectionID,
                SnpIn = snpIn,
                SnpOut = snpOut,
                CallingCapacity = callingCapacity
            };

            //wysyłamy żądanie do RC
            _pc.SendSignallingMessage(message);
        }
        #endregion

        #region Other_Methodes
        //do poprawy, metoda tymczasowa
        private int MakeNewFibRecords(int interfaceIn, int labelIn, int interfaceOut, int labelOut, string operation)
        {
            //gdzies to przekaż albo coś UZUPEŁNIć !!
            return 1;
        }
        #endregion
    }
}