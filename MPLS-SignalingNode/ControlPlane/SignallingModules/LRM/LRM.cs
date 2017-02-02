using MPLS_SignalingNode;
using System;
using System.Collections.Generic;
using DTO.ControlPlane;
using System.Threading;

namespace ControlPlane
{
    class LRM
    {

        #region Variables
        private string _configurationFilePath;

        private string _localPcIpAddress;
        private string _areaName;
        private Dictionary<string, string> _lrmToSubnetworksDictionary;    //słownik zawierający nazwę podsieci wraz z przypisanej do niej adresem agenta LRM
        private List<SNPP> _snppList;

        private PC _pc;
        private Dictionary<int, bool> _isSnpNegotiationAnswerBack;
        private Dictionary<int, SignalMessage> _snpNegotiationAnswerBack;
        private Dictionary<int, string> _snpNegotiatinAnswerBackAreaName;

        private Dictionary<int, bool> _isSnpRealiseAnswerBack;
        private Dictionary<int, bool> _snpRealiseAnswerBack;

        public static readonly object SyncObject = new object();
        #endregion

        #region Properties
        public PC LocalPC { set { _pc = value; } }
        #endregion


        #region Main_Methodes
        public LRM(string configurationFilePath)
        {
            InitialiseVariables(configurationFilePath);
        }
        private void InitialiseVariables(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;

            LRM_XmlSchame schema = new LRM_XmlSchame();
            schema = LRM_LoadingXmlFile.Deserialization(_configurationFilePath);

            //miejsce na przypisanie zmiennych
            _localPcIpAddress = schema.XML_localPcIpAddress;
            _areaName = schema.XML_areaName;

            //tworzenie słownika ze struktur
            _lrmToSubnetworksDictionary = new Dictionary<string, string>();
            foreach (LrmDescription element in schema.XML_LrmList)
            {
                _lrmToSubnetworksDictionary.Add(element.areaName, element.ipAddress);
            }

            //tworzę słownik SNPP
            _snppList = schema.XML_SnppList;

            //alokacja słowników
            _isSnpNegotiationAnswerBack = new Dictionary<int, bool>();
            _snpNegotiationAnswerBack = new Dictionary<int, SignalMessage>();
            _snpNegotiatinAnswerBackAreaName = new Dictionary<int, string>();

            _isSnpRealiseAnswerBack = new Dictionary<int, bool>();
            _snpRealiseAnswerBack = new Dictionary<int, bool>();
        }
        #endregion


        #region Properties
        #endregion

        #region PC_Cooperation_Methodes
        private void SendMessageToPC(SignalMessage message)
        {
            Console.WriteLine
                        ("|SIGNALLING|LRM| - SendMessageToPC - |" + message.General_SignalMessageType + "|");

            _pc.SendSignallingMessage(message);
            //SignallingNodeDeviceClass.MakeSignallingLog("LRM", "INFO - Signalling message send to PC module");
        }
        public void ReceiveMessageFromPC(SignalMessage message)
        {
            Console.WriteLine
                      ("|SIGNALLING|LRM| - ReceiveMessageFromPC - |" + message.General_SignalMessageType + "|");

            Thread.Sleep(10);

            switch (message.General_SignalMessageType)
            {
                case SignalMessage.SignalType.LinkConnectionRequest:
                    LinkConnectionRequest(message.ConnnectionID, message.SnppIdPair, message.CallingCapacity, message.LinkConnection_ID);
                    break;
                case SignalMessage.SignalType.LinkConnectionDealocation:
                    LinkConnectionDealocation(message.ConnnectionID, message.SnppIdPair);
                    break;

                case SignalMessage.SignalType.SNPNegotiation:
                     SNPNegotiation_Analyse(message.Negotiation_ID, message.Negotiation_ConnectionID, message.Negotiation_Label, message.Negotiation_SnppID, message.Negotiation_Capacity, message.General_SourceIpAddress);
                    break;
                case SignalMessage.SignalType.SNPNegotiationResponse:
                    SNPNegotiationResponse_Analyse(message.Negotiation_ID, message.IsAccepted, message.Negotiation_AllocatedSNP);
                    break;            

                case SignalMessage.SignalType.SNPRealise:
                    SNPRealise(message.Negotiation_ID, message.Negotiation_SnppID, message.Negotiation_ConnectionID, message.General_SourceIpAddress);
                    break;
                case SignalMessage.SignalType.SNPRealiseResponse:
                    SNPRealiseResponse_Analyse(message.Negotiation_ID, message.IsAccepted);
                    break;
                default:
                    break;
            }
        }
        #endregion

        #region Incomming_Methodes_From_Standardization
        private void LinkConnectionRequest(int connectionID, SignalMessage.Pair snpp_id_pair, int connectionCapacity, int LinkConnection_ID)   //DOKOŃCZYC
        {
            #region Odnajdywanie_SNPP_i_sprawdzenie_czy_istnieją
            //odnajdz pierwszy i drugi SNPP
            SNPP first = null;
            SNPP second = null;

            //wyszukuje pierwszą wartość w liście
            for (int i = 0; i < _snppList.Count; i++)
                if (_snppList[i]._localID == snpp_id_pair.first)
                {
                    first = _snppList[i];
                    break;
                }
            //jeżeli nie ma takiego elementu to zwróć bład
            if (first == null)
            {
                SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - Cannot find the SNPP with ID equals " + snpp_id_pair.first);
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - Cannot find the SNPP with ID equals " + snpp_id_pair.first);
            }
            else
            {
                //wyszukuje drugą wartość w liście
                for (int i = 0; i < _snppList.Count; i++)
                    if (_snppList[i]._localID == snpp_id_pair.second)
                    {
                        second = _snppList[i];
                        break;
                    } 

                //jeżeli nie ma takiego elementu to zwróć bład
                if (second == null)
                {
                    SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - Cannot find the SNPP with ID equals " + snpp_id_pair.first);
                    SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - Cannot find the SNPP with ID equals " + snpp_id_pair.first);
                }
            }

            if (first == null || second == null)
            {
                //wyślij wiadomośc negatywną
                LinkConnectionResponse(connectionID, false, null, null, LinkConnection_ID);

                //zakończ działanie metody
                return;
            }
            #endregion

            #region Sprawdzenie_czy_nie_ma_jakiegos_SNP_zwiazanego_z_tym_connectionID
            //Sprawdz, czy nie ma jakiegoś SNP związanego z tym connectionID
            int labelToForward = -1;
            for (int i = 0; i < first._allocatedSNP.Count; i++)
                if (first._allocatedSNP[i]._connectionID == connectionID)
                {
                    labelToForward = first._allocatedSNP[i]._allocatedLabel;
                    break;
                }

            // Jeżeli nie ma, to sprawdz w drugim
            if (labelToForward == -1)
                for (int i = 0; i < second._allocatedSNP.Count; i++)
                    if (second._allocatedSNP[i]._connectionID == connectionID)
                    {
                        labelToForward = second._allocatedSNP[i]._allocatedLabel;
                        break;
                    }
            #endregion

            #region Ewentualne_losowanie_i_negocjacja_wybranej_etykiety
            int numberOfIterations = 0;
            bool accept = false;
            List<int> negotiationIDList = new List<int>();

            while (!accept)
            {
                numberOfIterations++;

                //wylosuj następną etykiete 
                if (labelToForward == -1)
                    labelToForward = GetNextFreeLabel(first, second, numberOfIterations - 1);

                //nie ma wolnej etykiety
                if (labelToForward == -1)
                {
                    //wyślij wiadomośc negatywną
                    LinkConnectionResponse(connectionID, false, null, null, LinkConnection_ID);
                }

                else
                {
                    #region Send_local_snp_negotiation

                    //trzeba zablokować ten fragment kodu
                    lock (SyncObject)
                    {
                        //dodaj nowe id (po kolei dodajemy, isSnpNegotiation jest ogólną listą, a negotiationIdList lokalną)
                        negotiationIDList.Add(_isSnpNegotiationAnswerBack.Count);

                        //zainicjuj słowniki (na razie do lokalnego LRM)
                        _isSnpNegotiationAnswerBack.Add(negotiationIDList[0], false);
                        _snpNegotiatinAnswerBackAreaName.Add(negotiationIDList[0], _areaName);
                    }

                    //wysyłamy SNMNegotiation do lokalnego LRM
                    SNPNegotiation(
                        negotiationIDList[0],
                        connectionID,
                        labelToForward,
                        second._localID,
                        connectionCapacity);
                    #endregion

                    #region Send_snp_negotiation_to_other_areas
                    //pierwszy SNPP
                    if (first._areaName != _areaName)
                    {
                        //trzeba zablokować ten fragment kodu
                        lock (SyncObject)
                        {
                            //dodaj nowe id
                            negotiationIDList.Add(_isSnpNegotiationAnswerBack.Count);

                            //utwórz wpisy w słownikach
                            _isSnpNegotiationAnswerBack.Add(negotiationIDList[negotiationIDList.Count - 1], false);
                            _snpNegotiatinAnswerBackAreaName.Add(negotiationIDList[negotiationIDList.Count - 1], first._areaName);
                        }
                        //znajdź adres PC odpowiedzialnego za dany LRM w sieci first._areaName
                        string destinationIpAddr = _lrmToSubnetworksDictionary[first._areaName];

                        //wyslij wiadomość
                        SNPNegotiation(
                            negotiationIDList[negotiationIDList.Count - 1],
                            connectionID,
                            labelToForward,
                            first._areaNameSnppID,
                            connectionCapacity,
                            destinationIpAddr);
                    }

                    //drugiSNPP
                    if (second._areaName != _areaName)
                    {
                        //trzeba zablokować ten fragment kodu
                        lock (SyncObject)
                        {
                            //dodaj nowe id
                            negotiationIDList.Add(_isSnpNegotiationAnswerBack.Count);

                            //utwórz wpisy w słownikach
                            _isSnpNegotiationAnswerBack.Add(negotiationIDList[negotiationIDList.Count - 1], false);
                            _snpNegotiatinAnswerBackAreaName.Add(negotiationIDList[negotiationIDList.Count - 1], second._areaName);
                        }

                        //znajdź adres PC odpowiedzialnego za dany LRM w sieci second._areaName
                        string destinationIpAddr = _lrmToSubnetworksDictionary[second._areaName];

                        //wyslij wiadomość
                        SNPNegotiation(
                            negotiationIDList[negotiationIDList.Count - 1],
                            connectionID,
                            labelToForward,
                            second._areaNameSnppID,
                            connectionCapacity,
                            destinationIpAddr);
                    }
                    #endregion

                    #region Wait_until_all_response_comes
                    int waitingAnswerstToGet = negotiationIDList.Count;
                    while (waitingAnswerstToGet != 0)
                    {
                        //przy każdej iteracji zaczynaj sprawdzanie od nowa
                        waitingAnswerstToGet = negotiationIDList.Count;

                        //tutaj można troche przysnąc
                        Thread.Sleep(50);

                        //możemy miec tylko 1 lub max 3 wiadomosci snpNegotiation
                        switch (waitingAnswerstToGet)
                        {
                            case 1:
                                for (int i = 0; i < negotiationIDList.Count; i++)
                                    if (_isSnpNegotiationAnswerBack[(negotiationIDList[0])] == true)
                                        waitingAnswerstToGet = 0;
                                break;
                            case 2:
                                for (int i = 0; i < negotiationIDList.Count; i++)
                                    if (_isSnpNegotiationAnswerBack[(negotiationIDList[0])] == true)
                                        if (_isSnpNegotiationAnswerBack[(negotiationIDList[1])] == true)
                                                waitingAnswerstToGet = 0;
                                break;
                            case 3:
                                for (int i = 0; i < negotiationIDList.Count; i++)
                                    if (_isSnpNegotiationAnswerBack[(negotiationIDList[0])] == true)
                                        if (_isSnpNegotiationAnswerBack[(negotiationIDList[1])] == true)
                                            if (_isSnpNegotiationAnswerBack[(negotiationIDList[2])] == true)
                                                waitingAnswerstToGet = 0;
                                break;
                        }     
                    }
                    #endregion

                    Thread.Sleep(50);


                    #region Check_wheater_all_request_are_correct
                    //sprawdzamy, czy wszystkie odpowiedzi były prawidłowe
                    int numberOfAcceptAnswers = 0;
                    for (int i = 0; i < negotiationIDList.Count; i++)
                        if (_snpNegotiationAnswerBack[negotiationIDList[i]].IsAccepted == true)
                            numberOfAcceptAnswers++;

                    if (numberOfAcceptAnswers == negotiationIDList.Count)
                        accept = true;
                    else
                    {
                        accept = false;
                        //tutaj trzeba namierzyć tą która jest zła i wysłać Realise i ponownie spróbować losować jakąć czy coś
                    }
                    #endregion
                }
            }
            #endregion

            #region Alocate_first_snp
            //trzeba zablokować ten fragment kodu
            int firstSnpID = -1;
            lock (SyncObject)
            {
                // alokujemy SNP lokalnie (tylko pierwsza, bo druga mam w odpowiedzi)
                firstSnpID = first._allocatedSNP.Count;
                first._allocatedSNP.Add(new SNP
                {
                    _snpID = firstSnpID,
                    _snppID = first._localID,
                    _connectionID = connectionID,
                    _allocatedCapacity = connectionCapacity,
                    _allocatedLabel = labelToForward
                });
            }
            #endregion

            #region Make_and_sent_response_message
            // tworzymy odpowiedz 
            // odpowiedz skłąda sie z listy SNP zaalokowanych oraz w takiej samej kolejności wypisanych areaName w drugij liscie
            List<SNP> receivedSnps = new List<SNP>();
            List<string> receivedSnpsAreaNames = new List<string>();

            //dodaj swój lokalnie dodany SNP do listy
            receivedSnps.Add(first._allocatedSNP[firstSnpID]);
            receivedSnpsAreaNames.Add(_areaName);

            //dodaj resztę wpisów
            for (int i = 0; i < negotiationIDList.Count; i++)
            {
                receivedSnps.Add(_snpNegotiationAnswerBack[(negotiationIDList[i])].Negotiation_AllocatedSNP);
                receivedSnpsAreaNames.Add(_snpNegotiatinAnswerBackAreaName[(negotiationIDList[i])]);
            }

            //wyślij wiadomość potwierdzającą
            LinkConnectionResponse(connectionID, true, receivedSnps, receivedSnpsAreaNames, LinkConnection_ID);
            #endregion
        }
        private void LinkConnectionDealocation(int connectionID, SignalMessage.Pair snpp_id_pair)
        {
            #region Odnajdywanie_SNPP_i_sprawdzenie_czy_istnieją
            //odnajdz pierwszy i drugi SNPP
            SNPP first = null;
            SNPP second = null;

            //wyszukuje pierwszą wartość w liście
            for (int i = 0; i < _snppList.Count; i++)
                if (_snppList[i]._localID == snpp_id_pair.first)
                {
                    first = _snppList[i];
                    break;
                }
            //jeżeli nie ma takiego elementu to zwróć bład
            if (first == null)
            {
                SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - Cannot find the SNPP with ID equals " + snpp_id_pair.first);
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - Cannot find the SNPP with ID equals " + snpp_id_pair.first);
            }
            else
            {
                //wyszukuje drugą wartość w liście
                for (int i = 0; i < _snppList.Count; i++)
                    if (_snppList[i]._localID == snpp_id_pair.second)
                    {
                        second = _snppList[i];
                        break;
                    }

                //jeżeli nie ma takiego elementu to zwróć bład
                if (second == null)
                {
                    SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - Cannot find the SNPP with ID equals " + snpp_id_pair.first);
                    SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - Cannot find the SNPP with ID equals " + snpp_id_pair.first);
                }
            }

            if (first == null || second == null)
            {
                SignalMessage rejectedResponse = new SignalMessage()
                {
                    General_SignalMessageType = SignalMessage.SignalType.LinkConnectionDealocationResponse,
                    General_DestinationIpAddress = _localPcIpAddress,
                    General_SourceIpAddress = _localPcIpAddress,
                    General_SourceModule = "LRM",
                    General_DestinationModule = "CC",

                    IsAccepted = false,
                };
                SendMessageToPC(rejectedResponse);

                //zakończ działanie metody
                return;
            }
            #endregion

            #region Znajdź_SNP_związane_z_tym_connectionID
            SNP localSNP = null;
            for (int i = 0; i < first._allocatedSNP.Count; i++)
                if (first._allocatedSNP[i]._connectionID == connectionID)
                {
                    localSNP = first._allocatedSNP[i];
                    break;
                }

            if (localSNP == null)
            {
                SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - Cannot find the SNP with connectionID equals " + snpp_id_pair.first + " in SNPP with ID: " + first._localID);
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - Cannot find the SNP with connectionID equals " + snpp_id_pair.first + " in SNPP with ID: " + first._localID);

                //wyślij wiadomość odmowną
                SignalMessage rejectedResponse = new SignalMessage()
                {
                    General_SignalMessageType = SignalMessage.SignalType.LinkConnectionDealocationResponse,
                    General_DestinationIpAddress = _localPcIpAddress,
                    General_SourceIpAddress = _localPcIpAddress,
                    General_SourceModule = "LRM",
                    General_DestinationModule = "CC",

                    IsAccepted = false
                };
                SendMessageToPC(rejectedResponse);

                //zakończ działanie metody
                return;
            }
            #endregion

            #region Wyslij_lokalne_i_zewnętrzne_snpRealise_i_czekaj_na_odp
            bool accept = false;
            List<int> dealocationIDList = new List<int>();
            Random rnd = new Random();

            while (!accept)
            {
                //losuje identyfikator negocjacji
                dealocationIDList.Add(rnd.Next(0,20));
                while (_isSnpRealiseAnswerBack.ContainsKey(dealocationIDList[0]))
                    dealocationIDList[0] = rnd.Next();

                //utwórz wpis w słowniku z danym ID
                _isSnpRealiseAnswerBack.Add(dealocationIDList[0], false);

                //wysyłamy SNPRealise do lokalnego LRM
                SignalMessage snmInsideNegotiation = new SignalMessage()
                {
                    General_SignalMessageType = SignalMessage.SignalType.SNPRealise,
                    General_DestinationIpAddress = _localPcIpAddress,
                    General_SourceIpAddress = _localPcIpAddress,
                    General_SourceModule = "LRM",
                    General_DestinationModule = "LRM",

                    Negotiation_ID = dealocationIDList[0],
                    Negotiation_SnppID = second._localID,
                    Negotiation_ConnectionID = connectionID
                };
                SendMessageToPC(snmInsideNegotiation);


                //wysyłamy SNPRealise do pozostałych podsieci jeżeli jest taka konieczność
                if (first._areaName != _areaName)
                {
                    dealocationIDList.Add(rnd.Next(20,40));
                    while (_isSnpRealiseAnswerBack.ContainsKey(dealocationIDList[dealocationIDList.Count - 1]))
                        dealocationIDList[dealocationIDList.Count - 1] = rnd.Next();

                    //utwórz wpisy w słownikach
                    _isSnpRealiseAnswerBack.Add(dealocationIDList[dealocationIDList.Count - 1], false);

                    //znajdź adres PC odpowiedzialnego za dany LRM w sieci first._areaName
                    string destinationIpAddr = _lrmToSubnetworksDictionary[first._areaName];

                    SignalMessage message = new SignalMessage()
                    {
                        General_SignalMessageType = SignalMessage.SignalType.SNPRealise,
                        General_SourceIpAddress = _localPcIpAddress,
                        General_DestinationIpAddress = destinationIpAddr,
                        General_SourceModule = "LRM",
                        General_DestinationModule = "LRM",

                        Negotiation_ID = dealocationIDList[dealocationIDList.Count - 1],
                        Negotiation_SnppID = first._areaNameSnppID,
                        Negotiation_ConnectionID = connectionID
                    };
                    SendMessageToPC(message);
                }

                if (second._areaName != _areaName)
                {
                    dealocationIDList.Add(rnd.Next(20,40));
                    while (_isSnpRealiseAnswerBack.ContainsKey(dealocationIDList[dealocationIDList.Count - 1]))
                        dealocationIDList[dealocationIDList.Count - 1] = rnd.Next();

                    //utwórz wpisy w słownikach
                    _isSnpRealiseAnswerBack.Add(dealocationIDList[dealocationIDList.Count - 1], false);

                    //znajdź adres PC odpowiedzialnego za dany LRM w sieci first._areaName
                    string destinationIpAddr = _lrmToSubnetworksDictionary[second._areaName];

                    SignalMessage message = new SignalMessage()
                    {
                        General_SignalMessageType = SignalMessage.SignalType.SNPRealise,
                        General_SourceIpAddress = _localPcIpAddress,
                        General_DestinationIpAddress = destinationIpAddr,
                        General_SourceModule = "LRM",
                        General_DestinationModule = "LRM",

                        Negotiation_ID = dealocationIDList[dealocationIDList.Count - 1],
                        Negotiation_SnppID = second._areaNameSnppID,
                        Negotiation_ConnectionID = connectionID
                    };
                    SendMessageToPC(message);
                }

                //czekamy biernie na odpowiedzi wszystkie (min 1 max 3)
                int waitingAnswerstToGet = dealocationIDList.Count;
                while (waitingAnswerstToGet != 0)
                {
                    for (int i = 0; i < dealocationIDList.Count; i++)
                        if (_isSnpRealiseAnswerBack[(dealocationIDList[i])] == true)
                            waitingAnswerstToGet--;
                }

                //sprawdzamy, czy wszystkie odpowiedzi były prawidłowe
                int numberOfAcceptAnswers = 0;
                for (int i = 0; i < dealocationIDList.Count; i++)
                    if (_snpRealiseAnswerBack[dealocationIDList[i]] == true)
                        numberOfAcceptAnswers++;

                if (numberOfAcceptAnswers == dealocationIDList.Count)
                    accept = true;
                else
                {
                    accept = false;
                    //tutaj trzeba namierzyć tą która jest zła i wysłać Realise i ponownie spróbować losować jakąć czy coś
                }
            }//end while(!accept)
            #endregion

            #region Zwalnianie_etykiety_i_usuwanie_localSNP
            //zwalnianie etykiety
            int freeLabel = localSNP._allocatedLabel;
            first._availableLabels.Add(freeLabel);

            //usuwanie local SNP
            first._allocatedSNP.Remove(localSNP);
            #endregion

            #region Wysyłanie_wiadomości_wstecznej
            SignalMessage acceptedMessage = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.LinkConnectionDealocationResponse,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceIpAddress = _localPcIpAddress,
                General_SourceModule = "LRM",
                General_DestinationModule = "CC",

                IsAccepted = true,
            };
            SendMessageToPC(acceptedMessage);
            #endregion
        }

        private void SNPNegotiation_Analyse(int negotiationID, int connectionID, int label, int snppID, int connectionCapacity, string sourcePcIpAddress)
        {
            #region Odszukuje_obiekt_SNPP_o_zadanym_id
            SNPP requestedSnpp = null;
            for (int i = 0; i < _snppList.Count; i++)
                if (_snppList[i]._localID == snppID)
                {
                    requestedSnpp = _snppList[i];
                    break;
                }

            //jeżeli nie ma takiego obiektu to zwróć błąd
            if (requestedSnpp == null)
            {
                //jest jakiś błąd i trzeba to zakomunikować
                SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR (SnppNegotiation)- Cannot find the SNPP with ID equals " + snppID);
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR (SnppNegotiation)- Cannot find the SNPP with ID equals " + snppID);

                //zwróć odpowiedź negatywną
                SNPNegotiationResponse(negotiationID, false, null, sourcePcIpAddress);
                return;
            }
            #endregion

            #region Sprawdzam_czy_etykieta_jest_wolna_i_wyslij_odpowiednia_wiadomosc
            if( requestedSnpp._availableLabels.Contains(label))
            {
                SNP allocatedSnp = null;
                lock (SyncObject)
                {
                    //etykieta jest wolna
                    int index = requestedSnpp._allocatedSNP.Count;
                    allocatedSnp = new SNP
                    {
                        _snpID = index,
                        _snppID = requestedSnpp._localID,
                        _allocatedCapacity = connectionCapacity,
                        _allocatedLabel = label,
                        _connectionID = connectionID
                    };
                    requestedSnpp._allocatedSNP.Add(allocatedSnp);
                }

                //wyślij odpowiedź zawierającą zaalokowane SNP
                SNPNegotiationResponse(negotiationID, true, allocatedSnp, sourcePcIpAddress);
            }
            else
            {
                //etykieta jest zajęta
                SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR (SnppNegotiation)- Lable already in use: " + label);
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR (SnppNegotiation)- Lable already in use: " + label);

                //zwróć odpowiedź negatywną
                SNPNegotiationResponse(negotiationID, false, null, sourcePcIpAddress);
                return;
            }
            #endregion
        }
        private void SNPNegotiationResponse_Analyse(int negotiationID, bool isAccepted, SNP allocatedSnp)
        {
            //ocznaczam, że otrzymałem wiadomość powrotną
            _isSnpNegotiationAnswerBack[negotiationID] = true;

            //wrzucam wiadomość zwrotną do słownika skojarzoną z tym samym negotiationID co reszta zmiennych w innych słownikach
            _snpNegotiationAnswerBack.Add(negotiationID, new SignalMessage { IsAccepted = isAccepted, Negotiation_AllocatedSNP = allocatedSnp });
        }

        private void SNPRealise(int dealocationID, int snppID, int connectionID, string sourcePcIpAddress)
        {
            #region Odszukuje_obiekt_SNPP_o_zadanym_id
            SNPP requestedSnpp = null;
            for (int i = 0; i < _snppList.Count; i++)
                if (_snppList[i]._localID == snppID)
                {
                    requestedSnpp = _snppList[i];
                    break;
                }

            //jeżeli nie ma takiego obiektu to zwróć błąd
            if (requestedSnpp == null)
            {
                //jest jakiś błąd i trzeba to zakomunikować
                SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR (SnppRealise)- Cannot find the SNPP with ID equals " + snppID);
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR (SnppRealise)- Cannot find the SNPP with ID equals " + snppID);

                //zwróć odpowiedź negatywną
                SignalMessage rejectedMessage = new SignalMessage()
                {
                    General_SignalMessageType = SignalMessage.SignalType.SNPRealiseResponse,
                    General_DestinationIpAddress = sourcePcIpAddress,
                    General_SourceIpAddress = _localPcIpAddress,
                    General_SourceModule = "LRM",
                    General_DestinationModule = "LRM",

                    Negotiation_ID = dealocationID,
                    IsAccepted = false
                };
                //wyslij wiadomość i wyjdź z metody
                SendMessageToPC(rejectedMessage);
                return;
            }
            #endregion

            #region Znajduję_SNP_związane_z_connectionID
            SNP localSNP = null;
            for (int i = 0; i < requestedSnpp._allocatedSNP.Count; i++)
                if (requestedSnpp._allocatedSNP[i]._connectionID == connectionID)
                {
                    localSNP = requestedSnpp._allocatedSNP[i];
                    break;
                }

            //jeżeli nie ma to zwróc negatywną odpowiedź
            if (localSNP == null)
            {
                SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - Cannot find the SNP with connectionID equals " + connectionID + " in SNPP with ID: " + requestedSnpp._localID);
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - Cannot find the SNP with connectionID equals " + connectionID + " in SNPP with ID: " + requestedSnpp._localID);

                //wyślij wiadomość odmowną
                SignalMessage message = new SignalMessage()
                {
                    General_SignalMessageType = SignalMessage.SignalType.SNPRealiseResponse,
                    General_DestinationIpAddress = sourcePcIpAddress,
                    General_SourceIpAddress = _localPcIpAddress,
                    General_SourceModule = "LRM",
                    General_DestinationModule = "LRM",

                    Negotiation_ID = dealocationID,
                    IsAccepted = false
                };
                SendMessageToPC(message);

                //zakończ działanie metody
                return;
            }
            #endregion

            #region Zwolnij_etykiete_i_usun_localSNP
            int freeLabel = localSNP._allocatedLabel;
            requestedSnpp._availableLabels.Add(freeLabel);

            requestedSnpp._allocatedSNP.Remove(localSNP);
            #endregion

            #region Wyslij_odpowiedź
            SignalMessage acceptedMessage = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.SNPRealiseResponse,
                General_DestinationIpAddress = sourcePcIpAddress,
                General_SourceIpAddress = _localPcIpAddress,
                General_SourceModule = "LRM",
                General_DestinationModule = "LRM",

                Negotiation_ID = dealocationID,
                IsAccepted = true
            };
            SendMessageToPC(acceptedMessage);
            #endregion
        }
        private void SNPRealiseResponse_Analyse(int dealocationID, bool isAccepted)
        {
            //ocznaczam, że otrzymałem wiadomość powrotną
            _isSnpRealiseAnswerBack[dealocationID] = true;

            //wrzucam wiadomość zwrotną do słownika skojarzoną z tym samym negotiationID co reszta zmiennych w innych słownikach
            _snpRealiseAnswerBack.Add(dealocationID, isAccepted);
        }

        private void LocalTopology()
        {

        }
        #endregion


        #region Outcomming_Methodes_From_Standardization
        private void LinkConnectionResponse(int connectionID, bool isAccepted, List<SNP> allocatedSnpList, List<string> allocatedSnpAreaName, int linkConnection_ID)
        {
            SignalMessage rejectedResponse = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.LinkConnectionResponse,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceIpAddress = _localPcIpAddress,
                General_SourceModule = "LRM",
                General_DestinationModule = "CC",

                ConnnectionID = connectionID,
                IsAccepted = isAccepted,
                LinkConnection_AllocatedSnpList = allocatedSnpList,
                LinkConnection_AllocatedSnpAreaNameList = allocatedSnpAreaName,

                LinkConnection_ID = linkConnection_ID
            };
            SendMessageToPC(rejectedResponse);
        }
        private void LinkConnectionResponse(int connectionID, bool isAccepted, List<SNP> allocatedSnpList, List<string> allocatedSnpAreaName, string destinationIpAddress)
        {
            SignalMessage rejectedResponse = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.LinkConnectionResponse,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceIpAddress = destinationIpAddress,
                General_SourceModule = "LRM",
                General_DestinationModule = "CC",

                IsAccepted = isAccepted,
                LinkConnection_AllocatedSnpList = allocatedSnpList,
                LinkConnection_AllocatedSnpAreaNameList = allocatedSnpAreaName

            };
            SendMessageToPC(rejectedResponse);
        }

        private void SNPNegotiation(int negotiationId, int connectionId, int label, int snppId, int capacity)
        {
            SignalMessage snmOutSecondNegotiation = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.SNPNegotiation,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = _localPcIpAddress,
                General_SourceModule = "LRM",
                General_DestinationModule = "LRM",

                Negotiation_ID = negotiationId,
                Negotiation_ConnectionID = connectionId,
                Negotiation_Label = label,
                Negotiation_SnppID = snppId,
                Negotiation_Capacity = capacity
            };
            SendMessageToPC(snmOutSecondNegotiation);
        }
        private void SNPNegotiation(int negotiationId, int connectionId, int label, int snppId, int capacity, string destinationIpAddress)
        {
            SignalMessage snmOutSecondNegotiation = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.SNPNegotiation,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = destinationIpAddress,
                General_SourceModule = "LRM",
                General_DestinationModule = "LRM",

                Negotiation_ID = negotiationId,
                Negotiation_ConnectionID = connectionId,
                Negotiation_Label = label,
                Negotiation_SnppID = snppId,
                Negotiation_Capacity = capacity
            };
            SendMessageToPC(snmOutSecondNegotiation);
        }

        private void SNPNegotiationResponse(int negotiationID, bool isAccepted, SNP allocatedSnp, string destinationIpAddress)
        {
            SignalMessage snmOutSecondNegotiation = new SignalMessage()
            {
                General_SignalMessageType = SignalMessage.SignalType.SNPNegotiationResponse,
                General_SourceIpAddress = _localPcIpAddress,
                General_DestinationIpAddress = destinationIpAddress,
                General_SourceModule = "LRM",
                General_DestinationModule = "LRM",

                Negotiation_ID = negotiationID,
                IsAccepted = isAccepted,
                Negotiation_AllocatedSNP = allocatedSnp
            };
            SendMessageToPC(snmOutSecondNegotiation);
        }
        #endregion


        #region Other_Methodes
        private int GetNextFreeLabel(SNPP first, SNPP second, int index_in_list)
        {
            bool sendRejectedResponse = false;

            //sprawdzamy, czy wgl jest jakas wolna lambda
            if (first._availableLabels.Count == 0)
            {
                SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - No free label available in SNPP with id " + first._localID);
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - No free label available in SNPP with id " + first._localID);
                sendRejectedResponse = true;
            }
            else if (second._availableLabels.Count == 0)
            {
                SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - No free label available in SNPP with id " + second._localID);
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - No free label available in SNPP with id " + second._localID);
                sendRejectedResponse = true;
            }

            if (sendRejectedResponse)
                return -1;
            else
            {
                if (first._availableLabels.Count < index_in_list)
                {
                    SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - No more free label available in SNPP with id " + first._localID);
                    SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - No more free label available in SNPP with id " + first._localID);
                    return -1;
                }
                else
                    return first._availableLabels[index_in_list];
            }
        }
        #endregion
    }
}


/*
        * OGÓLNY SCHEMAT DZIAŁANIA
        * 1. otrzymuje parę SNPP
        * 2. Wyszukuję pierwszą w słowniku
        * 3. Sprawdz, czy nie ma jakiegoś SNP związanego z tym connectionID
        * 4. Jeżeli się znajduje to odczytaj jego etykietę i przejdź do kroku 9
        * 5. Jeżeli nie ma, to wylosuj etykiete z możliwych availableLabes
        * 6. Sprawdzam czy SNPP nalezy do mojego areaName czy nie
        * 7. Jeżeli nie należy to wyszukuje jego LRM'a i wysyłam do niego SNPNegotiation z zadaną etykietą
        * 8. Alokuje u siebie SNP związany z pierwszą SNPP
        * 9. Dla 2 SNPP postępuje analogicznie
        * 10. W odpowiedzi zwracam Zbiór zewnętrzych id SNP (id tych SNP które sie utworzyły w podsieciach)
        *       
        * 
        */
