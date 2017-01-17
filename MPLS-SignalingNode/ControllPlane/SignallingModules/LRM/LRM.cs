using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlPlane
{
    class LRM
    {

        #region Variables
        private string _areaName;   //nazwa domeny lub podsieci do której należy dany LRM 
        private PC _packetController;   //agent służący do komunikacji węzlów sterowania 
        private Dictionary<string, string> _lrmToSubnetworksDictionary;    //słownik zawierający nazwę podsieci wraz z przypisanej do niej adresem agenta LRM
        #endregion


        #region Properties

        #endregion

        public LRM()
        {

        }


        public void LocalTopologyMethod()
        {

        }


        #region PC_Cooperation_Methodes
        private void SendMessageToPC(SignalMessage message)
        {

        }
        public static void ReceiveMessageFromPC(SignalMessage message)
        {

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
