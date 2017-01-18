using System.Collections.Generic;

namespace ControlPlane
{
    class LRM
    {

        #region Variables
        private string _configurationFilePath;

        private string _localPcIpAddress;
        private string _routingAreaName; 
        private string _administrativeAreaName;
        private Dictionary<string, string> _lrmToSubnetworksDictionary;    //słownik zawierający nazwę podsieci wraz z przypisanej do niej adresem agenta LRM
        private List<SNPP> _mySnppList;

        private PC _pc;
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
            _routingAreaName = schema.XML_routingAreaName;
            _administrativeAreaName = schema.XML_administrativeAreaName;

            //tworzenie słownika ze struktur
            _lrmToSubnetworksDictionary = new Dictionary<string, string>;
            foreach (LrmDescription element in schema.XML_LrmList)
            {
                _lrmToSubnetworksDictionary.Add(element.areaName, element.ipAddress);
            }

            //tworzę słownik SNPP


        }
        #endregion


        #region Properties
        #endregion


        #region Methodes_From_Standardization
        private void LinkConnectionRequest(int connectionID, SignalMessage.Pair snpp_id_pair)
        {

        }



        private void LocalTopologyMethod()
        {
            
        }
        #endregion

        #region PC_Cooperation_Methodes
        private void SendMessageToPC(SignalMessage message)
        {

        }
        public void ReceiveMessageFromPC(SignalMessage message)
        {
            switch (message.SignalMessageType)
            {
                case SignalMessage.SignalType.LinkConnectionRequest:
                    LinkConnectionRequest(message.ConnnectionID, message.SnppIdPair);
                    break;

                case SignalMessage.SignalType.SNPNegotiation:

                    break;

                case SignalMessage.SignalType.SNPNegotiationAccept:

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
