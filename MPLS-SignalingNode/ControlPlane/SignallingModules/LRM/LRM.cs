using MPLS_SignalingNode;
using System;
using System.Collections.Generic;

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
        private Dictionary<int, bool> _isSnppNegotiationAnswerBack;
        private Dictionary<int, SignalMessage> _snppNegotiationAnswerBack;
        private Dictionary<int, string> _snppNegotiatinAnswerBackAreaName;
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
            _lrmToSubnetworksDictionary = new Dictionary<string, string>;
            foreach (LrmDescription element in schema.XML_LrmList)
            {
                _lrmToSubnetworksDictionary.Add(element.areaName, element.ipAddress);
            }

            //tworzę słownik SNPP
            _snppList = schema.XML_SnppList;

            //alokacja słowników
            _isSnppNegotiationAnswerBack = new Dictionary<int, bool>();
            _snppNegotiationAnswerBack = new Dictionary<int, SignalMessage>();
            _snppNegotiatinAnswerBackAreaName = new Dictionary<int, string>();
        }
        #endregion


        #region Properties
        #endregion


        #region Methodes_From_Standardization
        private void LinkConnectionRequest(int connectionID, SignalMessage.Pair snpp_id_pair, int connectionCapacity)   //DOKOŃCZYC
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
            //jeżeli nie ma takiego elementu to zwróć bład i wyjdź
            if (first == null)
            {
                SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - Cannot find the SNPP with ID equals " + snpp_id_pair.first);
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - Cannot find the SNPP with ID equals " + snpp_id_pair.first);

                return;
            }

            //wyszukuje drugą wartość w liście
            for (int i = 0; i < _snppList.Count; i++)
                if (_snppList[i]._localID == snpp_id_pair.second)
                {
                    second = _snppList[i];
                    break;
                }

            //jeżeli nie ma takiego elementu to zwróć bład i wyjdź
            if (second == null)
            {
                SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - Cannot find the SNPP with ID equals " + snpp_id_pair.first);
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - Cannot find the SNPP with ID equals " + snpp_id_pair.first);

                return;
            }
            #endregion

            //------------------------------------------------------------------
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

            //------------------------------------------------------------------
            // Jeżeli nadal nie ma to wylosuj jeden z wolnych
            if (labelToForward == -1)
            {
                //sprawdzamy, czy wgl jest jakas wolna lambda
                if (first._availableLabels.Count == 0)
                {
                    SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - No free label available in SNPP with id " + first._localID);
                    SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - No free label available in SNPP with id " + first._localID);

                    //poinformuj kogoś

                    return;
                }
                else if (second._availableLabels.Count == 0)
                {
                    SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - No free label available in SNPP with id " + second._localID);
                    SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR - No free label available in SNPP with id " + second._localID);

                    //poinformuj kogoś

                    return;
                }

                bool accept = false;
                List<int> negotiationIDList = new List<int>();
                while (!accept)
                {
                    labelToForward = first._availableLabels[n];

                    //losuje identyfikator negocjacji
                    Random rnd = new Random();

                    negotiationIDList.Add(rnd.Next());
                    while (_isSnppNegotiationAnswerBack.ContainsKey(negotiationIDList[0]))
                        negotiationIDList[0] = rnd.Next();

                    _isSnppNegotiationAnswerBack.Add(negotiationIDList[0], false);
                    _snppNegotiatinAnswerBackAreaName.Add(negotiationIDList[0], _areaName);

                    //wysyłamy SNMNegotiation do lokalnego LRM
                    SignalMessage snmInsideNegotiation = new SignalMessage()
                    {
                        Negotiation_ID = negotiationIDList[0],
                        Negotiation_ConnectionID = connectionID,
                        Negotiation_Label = labelToForward,
                        Negotiation_SnppID = second._localID,
                        Negotiation_Capacity = connectionCapacity
                    };
                    SendMessageToPC(snmInsideNegotiation);


                    //wysyłamy SNMNegotiation do pozostałych podsieci jeżeli jest taka konieczność
                    if (first._areaName != _areaName)
                    {
                        negotiationIDList.Add(rnd.Next());
                        while (_isSnppNegotiationAnswerBack.ContainsKey(negotiationIDList[negotiationIDList.Capacity]))
                            negotiationIDList[negotiationIDList.Capacity] = rnd.Next();

                        _isSnppNegotiationAnswerBack.Add(negotiationIDList[negotiationIDList.Capacity], false);
                        _snppNegotiatinAnswerBackAreaName.Add(negotiationIDList[negotiationIDList.Capacity], first._areaName);

                        SignalMessage snmOutFirstNegotiation = new SignalMessage()
                        {
                            Negotiation_ID = negotiationIDList[negotiationIDList.Capacity],
                            Negotiation_ConnectionID = connectionID,
                            Negotiation_Label = labelToForward,
                            Negotiation_SnppID = first._areaNameSnppID,
                            Negotiation_Capacity = connectionCapacity
                        };
                        SendMessageToPC(snmOutFirstNegotiation);
                    }

                    if (second._areaName != _areaName)
                    {
                        negotiationIDList.Add(rnd.Next());
                        while (_isSnppNegotiationAnswerBack.ContainsKey(negotiationIDList[negotiationIDList.Capacity]))
                            negotiationIDList[negotiationIDList.Capacity] = rnd.Next();

                        _isSnppNegotiationAnswerBack.Add(negotiationIDList[negotiationIDList.Capacity], false);
                        _snppNegotiatinAnswerBackAreaName.Add(negotiationIDList[negotiationIDList.Capacity], second._areaName);

                        SignalMessage snmOutSecondNegotiation = new SignalMessage()
                        {
                            Negotiation_ID = negotiationIDList[negotiationIDList.Capacity],
                            Negotiation_ConnectionID = connectionID,
                            Negotiation_Label = labelToForward,
                            Negotiation_SnppID = second._areaNameSnppID,
                            Negotiation_Capacity = connectionCapacity
                        };
                        SendMessageToPC(snmOutSecondNegotiation);
                    }

                    //czekamy biernie na odpowiedzi wszystkie (min 1 max 3)
                    int waitingAnswerstToGet = negotiationIDList.Capacity;
                    while (waitingAnswerstToGet != 0)
                    {
                        for (int i = 0; i < negotiationIDList.Capacity; i++)
                            if (_isSnppNegotiationAnswerBack[(negotiationIDList[i])] == true)
                                waitingAnswerstToGet--;
                    }

                    //sprawdzamy, czy wszystkie odpowiedzi były prawidłowe
                    int numberOfAcceptAnswers = 0;
                    for (int i = 0; i < negotiationIDList.Capacity; i++)
                        if (_snppNegotiationAnswerBack[negotiationIDList[i]].Negotiation_isAccepted == true)
                            numberOfAcceptAnswers++;

                    if (numberOfAcceptAnswers == negotiationIDList.Capacity)
                        accept = true;
                    else
                    {
                        accept = false;
                        //tutaj trzeba namierzyć tą która jest zła i wysłać Realise i ponownie spróbować losować jakąć czy coś
                    }
                }

                //------------------------------------------------------------------
                // alokujemy SNP lokalnie
                int firstSnpID = first._allocatedSNP.Capacity + 1;
                int secondSnpID = second._allocatedSNP.Capacity + 1;
                first._allocatedSNP.Add(new SNP { _snpID = firstSnpID, _snppID = first._localID, _connectionID = connectionID, _allocatedCapacity = connectionCapacity, _allocatedLabel = labelToForward });
                second._allocatedSNP.Add(new SNP { _snpID = secondSnpID, _snppID = second._localID, _connectionID = connectionID, _allocatedCapacity = connectionCapacity, _allocatedLabel = labelToForward });

                //------------------------------------------------------------------
                // tworzymy odpowiedz (odpowiedz skłąda sie z listy SNP zaalokowanych oraz w takiej samej kolejności wypisanych areaName w drugij liscie
                List<SNP> receivedSnps = new List<SNP>();
                List<string> receivedSnpsAreaNames = new List<string>();

                for (int i = 0; i < negotiationIDList.Capacity; i++)
                {
                    receivedSnps.Add(_snppNegotiationAnswerBack[(negotiationIDList[i])].Negotiation_AllocatedSNP);
                    receivedSnpsAreaNames.Add(_snppNegotiatinAnswerBackAreaName[(negotiationIDList[i])]);
                }
            
                SignalMessage response = new SignalMessage()
                {
                    LinkConnection_AllocatedSnpList = receivedSnps,
                    LinkConnection_AllocatedSnpAreaNameList = receivedSnpsAreaNames
                };

                //------------------------------------------------------------------
                // wysyłam odpowiedź
                SendMessageToPC(response);
            }
        }
        private void SNPNegotiation(int negotiationID, int connectionID, int label, int snppID, int connectionCapacity)
        {
            //sprawdzam, czy mam wolny label
            SNPP requestedSnpp = null;
            for (int i = 0; i < _snppList.Capacity; i++)
                if (_snppList[i]._localID == snppID)
                {
                    requestedSnpp = _snppList[i];
                    break;
                }
            if(requestedSnpp == null)
            {
                //jest jakiś błąd i trzeba to zakomunikować
                SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR (SnppNegotiation)- Cannot find the SNPP with ID equals " + snppID);
                SignallingNodeDeviceClass.MakeSignallingConsoleLog("LRM", "ERROR (SnppNegotiation)- Cannot find the SNPP with ID equals " + snppID);

                //zwróć odpowiedź negatywną
                SignalMessage rejectedMessage = new SignalMessage()
                {
                    Negotiation_ID = negotiationID,
                    Negotiation_isAccepted = false
                };
                SendMessageToPC(rejectedMessage);
                return;
            }

            if(requestedSnpp._availableLabels.Contains(label))
            {
                //zwróć odpowiedź negatywną
                SignalMessage rejectedMessage = new SignalMessage()
                {
                    Negotiation_ID = negotiationID,
                    Negotiation_isAccepted = false,
                    Negotiation_AllocatedSNP = null
                };
                SendMessageToPC(rejectedMessage);
            }
            else
            {
                //zwróc odpowiedź pozytywną
                int snpID = requestedSnpp._allocatedSNP.Capacity + 1;
                requestedSnpp._allocatedSNP.Add(new SNP { _snpID = snpID, _snppID = requestedSnpp._localID, _connectionID = connectionID, _allocatedCapacity = connectionCapacity, _allocatedLabel = label });

                //usuwam mozliwą etykiete
                requestedSnpp._availableLabels.Remove(label);

                //tworze i wysyłam wiadookośc zwrotną
                SignalMessage confirmedMessage = new SignalMessage()
                {
                    Negotiation_ID = negotiationID,
                    Negotiation_isAccepted = true,
                    Negotiation_AllocatedSNP = requestedSnpp._allocatedSNP[snpID]
                };
                SendMessageToPC(confirmedMessage);
            }
        }
        private void SNPNegotiationAccept(int negotiationID, bool isAccepted, SNP allocatedSnp)
        {
            _snppNegotiationAnswerBack.Add(negotiationID, new SignalMessage { Negotiation_isAccepted = isAccepted, Negotiation_AllocatedSNP = allocatedSnp });
        }

        private void LocalTopologyMethod()
        {
            
        }
        #endregion

        #region PC_Cooperation_Methodes
        private void SendMessageToPC(SignalMessage message)
        {
            _pc.SendSignallingMessage(message);
            //zrób loga!
        }
        public void ReceiveMessageFromPC(SignalMessage message)
        {
            switch (message.General_SignalMessageType)
            {
                case SignalMessage.SignalType.LinkConnectionRequest:
                    LinkConnectionRequest(message.ConnnectionID, message.SnppIdPair, message.CallingCapacity);
                    break;

                case SignalMessage.SignalType.SNPNegotiation:
                    SNPNegotiation(message.Negotiation_ID, message.Negotiation_ConnectionID, message.Negotiation_Label, message.Negotiation_SnppID, message.Negotiation_Capacity);
                    break;

                case SignalMessage.SignalType.SNPNegotiationAccept:
                    SNPNegotiationAccept(message.Negotiation_ID, message.Negotiation_isAccepted, message.Negotiation_AllocatedSNP);
                    break;

                case SignalMessage.SignalType.LinkConnectionResponse:

                    break;
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
