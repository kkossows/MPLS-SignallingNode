using MPLS_SignalingNode;
using System.Collections.Generic;
using DTO.ControlPlane;
using System;
using System.Threading;

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

        public static readonly object SyncObject = new object();
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
            Console.WriteLine
                        ("|SIGNALLING|CC| - SendMessageToPC - |" + message.General_SignalMessageType + "|");

            _pc.SendSignallingMessage(message);
            //SignallingNodeDeviceClass.MakeSignallingLog("LRM", "INFO - Signalling message send to PC module");
        }
        public void ReceiveMessageFromPC(SignalMessage message)
        {
            Console.WriteLine
                        ("|SIGNALLING|CC| - ReceiveMessageFromPC - |" + message.General_SignalMessageType + "|");

            Thread.Sleep(10);

            switch (message.General_SignalMessageType)
            {
                case SignalMessage.SignalType.ConnectionRequest:
                    if (message.General_SourceModule == "NCC")
                        ConnectionRequest_Analyse(message.ConnnectionID, message.CallingIpAddress, message.CalledIpAddress, message.CallingCapacity);
                    else
                        ConnectionRequest_Analyse(message.ConnnectionID, message.SnpIn, message.SnpOut, message.CallingCapacity);
                    break;
                case SignalMessage.SignalType.ConnectionResponse:
                    ConnectionResponse_Analyse(message.ConnnectionID, message.IsAccepted);
                    break;
                case SignalMessage.SignalType.PeerCoordination:
                    PeerCoordination_Analyse(message.ConnnectionID, message.SnppInId, message.CalledIpAddress, message.CallingCapacity, message.General_SourceIpAddress);
                    break;
                case SignalMessage.SignalType.PeerCoordinationResponse:
                    PeerCoordinationResponse_Analyse(message.ConnnectionID, message.IsAccepted, message.LabelOUT);
                    break;
                case SignalMessage.SignalType.RouteQueryResponse:
                    RouteQueryResponse(message.ConnnectionID, message.IncludedSnppIdPairs, message.IncludedAreaNames);
                    break;
                case SignalMessage.SignalType.LinkConnectionResponse:
                    LinkConnectionResponse(message.ConnnectionID, message.IsAccepted, message.LinkConnection_AllocatedSnpList, message.LinkConnection_AllocatedSnpAreaNameList, message.LinkConnection_ID);
                    break;
                case SignalMessage.SignalType.ConnectionRealise:
                    ConnectionRealise_Analyse(message.ConnnectionID);
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
                    Status = "inProgress",
                    RequestFrom = "NCC",
                    CallingIpAddress = callingIpAddress,
                    CalledIpAddress = calledIpAddress
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
                        Status = "inProgress",
                        RequestFrom = "CC"
                    });

                //dodajemy wpis w słowniku dzięki czemu będziemy wiedzieć, że to connection ID odpowieada temu indexowi w liście
                _indexInListOfConnection.Add(connectionID, _connectionsList.Count - 1);

                //tutaj trzeba dopisac do listy swoje sieci
                ConnectionTableRecord record = _connectionsList[_indexInListOfConnection[connectionID]];
                record.AllocatedSnpAreaName.Add(_areaName);
                record.AllocatedSnps.Add(new List<SNP>());

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
            #region Find_right_record_and_increment_number_of_response
            ConnectionTableRecord record = _connectionsList[_indexInListOfConnection[connectionID]];
            record.NumberOfResponse++;
            #endregion

            #region CheckedIfConfirmed
            if (isAccepted == true)
                record.NumberOfConfirmationResponses++;
            #endregion


            #region Number_of_received_message_==_number_of_send_requests
            //jeżeli jest miedzydomenowe to zawsze numberofrequest bedzie wieksza o 1 bo przypisujemy jej wartosc numberooFAreaNamesAllocated a tam jest tez domena 2 ktorej nie chcemy tutaj
            if (
                ( record.NumberOfResponse == record.NumberOfRequest && record.NumberOfConfirmationResponses == record.NumberOfRequest && record.IsInterdomain == false )
                || ( (record.NumberOfResponse == record.NumberOfRequest -1) && (record.NumberOfConfirmationResponses == record.NumberOfRequest -1) &&(record.IsInterdomain == true ))
                )
            {
                //zmień status na established
                record.Status = "established";

                //wyczyść zmienne w rekordzie
                record.NumberOfRequest = 0;
                record.NumberOfResponse = 0;

                //jak nie ma już żadnej podsieci to znaczy, że zakończyliśmy działania tego CC
                string destinationIpAddress = null;

                //wybieramy odpowiednii moduł docelowy, który czeka na odpowiedź
                switch (record.RequestFrom)
                {
                    case "NCC":
                        #region Connection_From_NCC
                        //znajdz etykiete wejsciową
                        for (int i = 0; i < record.AllocatedSnps[0].Count; i++)
                        {
                            if (record.AllocatedSnps[0][i]._connectionID == connectionID)
                                if (record.AllocatedSnps[0][i]._snppID == record.LocalBoundaryFirstSnppID)
                                    record.LabelIn = record.AllocatedSnps[0][i]._allocatedLabel;
                        }

                        //jeżeli połączenie byłoo wewnątrzdomeny to znajdz etykiete wyjściową
                        if (!record.IsInterdomain)
                        {
                            for (int i = 0; i < record.AllocatedSnps[0].Count; i++)
                            {
                                if (record.AllocatedSnps[0][i]._connectionID == connectionID)
                                    if (record.AllocatedSnps[0][i]._snppID == record.LocalBoundarySecondSnppID)
                                        record.LabelOut = record.AllocatedSnps[0][i]._allocatedLabel;
                            }
                        }

                        //wyślij wiadomośc do NCC
                        ConnectionResponse(connectionID, true, record.LabelIn, record.LabelOut);
                        #endregion
                        break;
                    case "CC":
                        #region Connection_From_HigherCC
                        //znajdź adres docelowy PC obsługującego CC wyższego rzędu związanego z daną areaname
                        destinationIpAddress = _connectedCcDestinationAddrress[_higherAreaName];

                        //wyślij wiadomość do CC wyższego
                        ConnectionResponse(connectionID, true, destinationIpAddress);

                        //zmień status na established
                        record.Status = "established";
                        #endregion
                        break;
                    case "CC-peer":
                        #region PeerCoordination_Connection
                        //znajdź adres docelowy PC obsługującego CC, który wywołał PeerCoordination
                        destinationIpAddress = record.DestOrSourIp;

                        //znajdujemy etykiete końcową
                        //musze przesujkać listę zaalokowanych SNP w swojej domenie (zerowa lista) i znależć ten, który ma to samo borderSnppIDSecond co snppID
                        for (int i = 0; i < record.AllocatedSnps[0].Count; i++)
                        {
                            if (record.AllocatedSnps[0][i]._connectionID == connectionID)
                                if (record.AllocatedSnps[0][i]._snppID == record.LocalBoundarySecondSnppID)
                                    record.LabelOut = record.AllocatedSnps[0][i]._allocatedLabel;
                        }

                        //wyślij wiadomość do CC wyższego
                        PeerCoordinationResponse(connectionID, true, record.LabelOut, destinationIpAddress);

                        //zmień status na established
                        record.Status = "established";

                        #endregion
                        break;
                }
            }
            else if (
                (record.NumberOfResponse == record.NumberOfRequest && record.NumberOfConfirmationResponses != record.NumberOfRequest && record.IsInterdomain == false)
                || ((record.NumberOfResponse == record.NumberOfRequest - 1) && (record.NumberOfConfirmationResponses != record.NumberOfRequest - 1) && (record.IsInterdomain == true))
                )
            {
                //tutaj trzeba obsłużyć to, że dostaliśy jakiegoś falssa
            }
            #endregion

            #region Number_of_received_message!=_number_of_send_requests
            //jak nie to nic nie rób, czekaj dalej
            #endregion
        }

        private void RouteQueryResponse(int connectionID, List<SignalMessage.Pair> includedSnppIdPairs, List<string> includedAreaNames)
        {
            #region Find_right_record_and_get_capacity
            ConnectionTableRecord record = _connectionsList[_indexInListOfConnection[connectionID]];
            int callingCapacity = record.AllocatedCapacity;
            #endregion

            #region First_insideDomain_message
            //jeżeli mamy pustą listę uwikłanych podsieci to znaczy, że jest to pierwsza wiadomość dotycząca połączenia wewnątrzdomenowego (dostaliśmy graniczne SNPP in i out)
            if (includedAreaNames == null)
            {
                //zapisz do tablicy graniczne zbiory SNPP
                record.LocalBoundaryFirstSnppID = includedSnppIdPairs[0].first;
                record.LocalBoundarySecondSnppID = includedSnppIdPairs[0].second;

                //zmieniamy typ połączenia w tablicy na nie międzydomenowy
                record.IsInterdomain = false;

                //uzupełniam tabele o podsieć do której należe
                record.AllocatedSnpAreaName.Add(_areaName);
                record.AllocatedSnps.Add(new List<SNP>());

                //odpytujemy RC o szczegóły połączenia pomiędzy zadną parą SNPP
                RouteQuery(
                    connectionID,
                    new SignalMessage.Pair()
                    {
                        first = record.LocalBoundaryFirstSnppID,
                        second = record.LocalBoundarySecondSnppID
                    },
                    callingCapacity);

                //wyjdź z metody
                return;
            }
            #endregion

            #region First_interdomain_message
            else if (includedAreaNames[0].StartsWith("Dom"))
            {
                //zmieniamy typ połączenia w tablicy na międzydomenowy
                record.IsInterdomain = true;
                //zmieniam status na ustawianie połączenia międzydomenowego
                record.Status = "establishingInterdomainLink";
                //dodaje nazwę uwikłanej domeny
                record.NextDomainName = includedAreaNames[0];

                //zapisz do tablicy graniczne zbiory SNPP
                record.LocalBoundaryFirstSnppID = includedSnppIdPairs[0].first;
                record.LocalBoundarySecondSnppID = includedSnppIdPairs[0].second;

                //uzupełniam tabele o podsieć do której należe
                record.AllocatedSnpAreaName.Add(_areaName);
                record.AllocatedSnps.Add(new List<SNP>());

                //uzupełniam tabele o nazwy uwikłanej domeny
                record.AllocatedSnpAreaName.Add(includedAreaNames[0]);
                record.AllocatedSnps.Add(new List<SNP>());

                //ustawiam liczbę wiadomości wysłanych na 1 a odebranych na 0
                record.NumberOfRequest = 1;
                record.NumberOfResponse = 0;

                //wyzeruj licznik linkConnectionRequest
                record.LinkConnectionIdToProceed = 0;

                //tutaj musimy wysłac druga parę aby druga domena zaalokowała u sibie SNP i miała wybraną już etykiete
                LinkConnectionRequest(
                    connectionID,
                    new SignalMessage.Pair()
                    {
                        first = includedSnppIdPairs[1].first,
                        second = includedSnppIdPairs[1].second
                    },
                    callingCapacity, 0);

                //wyjdź z metody - oczekuj na wiadomość LinkConnectionRequest z connectionID równym 222
                return;
            }
            #endregion

            #region Detail_Route_Response
            else
            {
                #region Step_1-uzupełnij_tabele_i_wyslij_LinkConnectionRequest_do_wszystkich
                //uzupełniam tabele o nazwy uwikłanych podsieci oraz alokuje listy na SNP związane z nimi
                for (int i = 0; i < includedAreaNames.Count; i++)
                {
                    record.AllocatedSnpAreaName.Add(includedAreaNames[i]);
                    record.AllocatedSnps.Add(new List<SNP>());
                }

                //ustawiamy zmienną, mówiącą o ilości wysyłanych wiaodmości LinkConnectionRequest
                int numberOfPairs = includedSnppIdPairs.Count;
                record.NumberOfRequest = numberOfPairs;

                //wyzeruj liczbę otrzymanych wiadomości zwrotnych
                record.NumberOfResponse = 0;

                //ustalamy wartosc aktualnie odebranego LinkConnectionResponsa na 0
                record.LinkConnectionIdToProceed = 0;

                //wysyłamy wszystkie requesty na raz
                for (int i = 0; i < numberOfPairs; i++)
                    LinkConnectionRequest(connectionID, includedSnppIdPairs[i], callingCapacity, i);
                #endregion

                #region Step_2-Sprawdź_czy_odebrałem_wszystkie_odpowiedzi_czy_mam_czekac_dalej
                //poczekaj na wszystkie odpowiedzi
                while (record.NumberOfResponse != record.NumberOfRequest)
                {
                    //busyWaiting poczekaj na wszystkie odpowiedzi
                }

                //otrzymałem wszystkie odpowiedzi
                #region Normaln_respones
                //ustaw nowy status rekordu oznaczający, że wszystkie lokalne linki zostały ustawione
                record.Status = "localLinksAllocated";

                //sprawdź, czy nasze połaczenie przechodzi przez jakieś podsieci
                //zawsze będzie nasza podsieć i zawsze będzie jakaś inna podsieć bo jak jest CC ostatnim to nie wysyła nic do LRM wiec nie bedzie tego kodu

                //ustaw nowe wartości Response i Request
                record.NumberOfRequest = record.AllocatedSnpAreaName.Count - 1;
                record.NumberOfResponse = 0;

                Thread.Sleep(20);
                record.NumberOfConfirmationResponses = 0;

                lock (SyncObject)
                {
                    //wyślij do każdego areaName != localAreaName wiadomosć ConnectionRequest
                    //pierwszy wiersz dotyczy lokalnej sytuacji więc go omijamy
                    for (int i = 1; i < record.NumberOfRequest + 1; i++)
                    {
                        if (record.AllocatedSnpAreaName[i].StartsWith("Dom"))
                            i++;    //omiń wpis zwiazany z domena sąsiednia
                        SNP snpIn = record.AllocatedSnps[i][0];
                        SNP snpOut = record.AllocatedSnps[i][1];

                        //znajdź adres docelowy PC obsługującego CC niższego rzędu związanego z daną areaname
                        string destinationIpAddress = _connectedCcDestinationAddrress[record.AllocatedSnpAreaName[i]];

                        //wyślij do CC niższego rzędu wiadomośc connectionRequest
                        ConnectionRequest(connectionID, snpIn, snpOut, record.AllocatedCapacity, destinationIpAddress);
                    }
                }
                #endregion
                #endregion
            }
            #endregion
        }

        private void LinkConnectionResponse(int connectionID, bool isAccepted, List<SNP> receivedSnps, List<string> receivedSnpsAreaNames, int LinkConnectionID)
        {
            #region Find_right_record
            ConnectionTableRecord record = _connectionsList[_indexInListOfConnection[connectionID]];
            #endregion

            #region Step_1-Sprawdz_status_odpowiedzi
            if (isAccepted)
            {
                //odpowiedź jest pozytywna - udało się zaalokować łącze
                //uzupełniam odpowiednie listy danymi zawartymi w odpowiedzi
                //trzeba sprawdzić kolejność


                while (record.LinkConnectionIdToProceed != LinkConnectionID)
                {
                    //busyWaiting (inaczej beda nie po kolei dodawac sie do list...
                }
                lock (SyncObject)
                {
                    for (int i = 0; i < receivedSnpsAreaNames.Count; i++)
                    {
                        int index = record.AllocatedSnpAreaName.IndexOf(receivedSnpsAreaNames[i]);
                        record.AllocatedSnps[index].Add(receivedSnps[i]);
                    }

                    //zwiększ licznik
                    record.LinkConnectionIdToProceed++;
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

            #region Increment_number_of_responses
            record.NumberOfResponse++;
            #endregion

            #region InterDomain_Response
            //tylko w przypadku połaczenia międzydomenowego i pierwszego przypadku - alokacja lacza miedzydomenowego
            if (record.IsInterdomain && record.Status == "establishingInterdomainLink")
            {
                //zmien status
                record.Status = "interdomainLinkEstablished";

                //poczekaj na wszystkie odpowiedzi
                while (record.NumberOfResponse != record.NumberOfRequest)
                {
                    //busyWaiting poczekaj na wszystkie odpowiedzi
                }

                //wyzeruj wartości Response i Request
                record.NumberOfRequest = 0;
                record.NumberOfResponse = 0;

                //znajdz snppIN uwikładnej domeny
                int index = record.AllocatedSnpAreaName.IndexOf(record.NextDomainName);
                int snppIn = record.AllocatedSnps[index][0]._snppID;

                //znajdz adres domeny uwikłanej
                string destinationIpAddresss = _connectedCcDestinationAddrress[record.NextDomainName];

                //wyślij do CC z domeny uwikłanej PeerCoordination
                PeerCoordination(
                    connectionID,
                    snppIn,
                    record.CalledIpAddress,
                    record.AllocatedCapacity,
                    destinationIpAddresss);

                //zakończ metodę i oczekuj aż domena ustawi u siebie połączenie (czekaj na wiadomośc PeerCoordinationRequest)
                return;
            }
            #endregion
        }


        private void PeerCoordination_Analyse(int connectionID, int snppInId, string ipAddressOut, int callingCapacity, string sourceIpAddress)
        {
            //dodajemy wpis w tablicy dla danego connectionID
            _connectionsList.Add(
                new ConnectionTableRecord
                {
                    ConnectionID = connectionID,
                    AllocatedCapacity = callingCapacity,
                    AllocatedSnps = new List<List<SNP>>(),
                    AllocatedSnpAreaName = new List<string>(),
                    Status = "inProgress",
                    RequestFrom = "CC-peer",
                    CalledIpAddress = ipAddressOut,
                    DestOrSourIp = sourceIpAddress  //dodaj adres źródła aby można było mu odpowiedzieć póxniej
                });

            //dodajemy wpis w słowniku dzięki czemu będziemy wiedzieć, że to connection ID odpowieada temu indexowi w liście
            _indexInListOfConnection.Add(connectionID, _connectionsList.Count - 1);

            //wysyłam RouteRequest odpowiedniego typu
            RouteQuery(connectionID, snppInId, ipAddressOut, callingCapacity);

            //czekam na odebranie odpowiedzi od RC
        }
        private void PeerCoordinationResponse_Analyse(int connectionID, bool isAccepted, int labelOut)
        {
            #region Find_right_record
            ConnectionTableRecord record = _connectionsList[_indexInListOfConnection[connectionID]];
            #endregion

            #region Analyse_Response_Status
            if(isAccepted)
            {
                #region Confirmed_Response
                //dodaj do wpisu etykiete końcową
                record.LabelOut = labelOut;

                //zmień status rekordu na normalne postępowanie wewnątrz domenowe
                record.Status = "inProgress";

                //wyślij wiadomość do RC o szczegółowe połaczenie w naszej domenie
                RouteQuery(
                    connectionID,
                    new SignalMessage.Pair()
                    {
                        first = record.LocalBoundaryFirstSnppID,
                        second = record.LocalBoundarySecondSnppID
                    },
                    record.AllocatedCapacity);

                //wyjdź z metody
                return;
                #endregion
            }
            else
            {
                #region Rejected_Response

                #endregion
            }
            #endregion
        }

        private void ConnectionRealise_Analyse(int connectionID)
        {
            //odszukaj rekord zwiazany z połaczneniem
            ConnectionTableRecord record = _connectionsList[_indexInListOfConnection[connectionID]];

            //sprawdz, czy połaczenie jest międzydomenowe czy wewnątrzdomenowe
            if(record.IsInterdomain)
            {
                //międzydomenowe
            }
            else
            {
                //wewnatrzdomenowe

                //zwolnij wszystkie SNP zaalokowane lokalnie 



                //wyślij do wszystkich CC uwikłanych wiaodmosc ConnectionRealise - pierwsza uwikłana podsiec to moja podsiec wiec nie wysyłam
                for(int i=1; i<record.AllocatedSnpAreaName.Count; i++)
                {
                    ConnectionRealise(connectionID, _connectedCcDestinationAddrress[record.AllocatedSnpAreaName[i]]);
                }
            }
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
            SendMessageToPC(message);
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
            SendMessageToPC(message);
        }
        private void RouteQuery(int connectionID, int snppInId, string calledIpAddress, int callingCapacity)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.RouteQuery,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "CC",
                General_DestinationModule = "RC",

                ConnnectionID = connectionID,
                SnppInId = snppInId,
                CalledIpAddress = calledIpAddress,
                CallingCapacity = callingCapacity
            };

            //wysyłam wiadomość
            SendMessageToPC(message);
        }

        private void LinkConnectionRequest(int connectionID, SignalMessage.Pair connectionSnppIdPair, int callingCapacity, int linkConnectionRequestID)
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
                CallingCapacity = callingCapacity,

                LinkConnection_ID = linkConnectionRequestID   //zmienna pomocnicza aby ta sama kolejnosc byla
            };

            //wysyłamy żądanie do RC
            SendMessageToPC(message);
        }

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
            SendMessageToPC(message);
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
            SendMessageToPC(message);
        }
        private void ConnectionRequest(int connectionID, SNP snpIn, SNP snpOut, int callingCapacity, string destinationIpAddress)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.ConnectionRequest,
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
            SendMessageToPC(message);
        }

        private void PeerCoordination(int connectionID, int snppInId, string ipAddressOut, int callingCapacity, string destinationIpAddress)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.PeerCoordination,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = destinationIpAddress,
                General_SourceModule = "CC",
                General_DestinationModule = "CC",

                ConnnectionID = connectionID,
                SnppInId = snppInId,
                CalledIpAddress = ipAddressOut,
                CallingCapacity = callingCapacity
            };

            //wysyłamy żądanie do RC
            SendMessageToPC(message);
        }
        private void PeerCoordinationResponse(int connectionID, bool isAccepted, int labelOut, string destinationIpAddress)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.PeerCoordinationResponse,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = destinationIpAddress,
                General_SourceModule = "CC",
                General_DestinationModule = "CC",

                ConnnectionID = connectionID,
                IsAccepted = isAccepted,
                LabelOUT = labelOut,    //taka etykieta idzie do klienta docelowego
            };

            //wysyłamy żądanie do RC
            SendMessageToPC(message);
        }

        private void ConnectionRealise(int connectionID, string destinationIpAddress)
        {
            SignalMessage message = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.ConnectionRealise,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = destinationIpAddress,
                General_SourceModule = "CC",
                General_DestinationModule = "CC",

                ConnnectionID = connectionID
            };

            //wysyłamy żądanie do RC
            SendMessageToPC(message);
        }
        #endregion

        #region Other_Methodes
        //do poprawy, metoda tymczasowa
        private int MakeNewFibRecords(int interfaceIn, int labelIn, int interfaceOut, int labelOut, string operation)
        {
            string newEntry = (interfaceIn + "-" + labelIn + "-" + interfaceOut + "-" + labelOut + "-" + operation);
            Console.WriteLine
                        ("|SIGNALLING|CC| - MakeNewFibRecords - |" + newEntry + "|");


            //gdzies to przekaż albo coś UZUPEŁNIć !!
            return 1;
        }
        #endregion
    }
}