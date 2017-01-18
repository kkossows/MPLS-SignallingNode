
namespace ControlPlane
{
    class RC
    {
        #region Variables
        private string _configurationFilePath;
        #endregion


        #region Main_Methodes
        public RC(string configurationFilePath)
        {
            InitialiseVariables(configurationFilePath);
        }
        private void InitialiseVariables(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;

            RC_XmlSchame tmp = new RC_XmlSchame();
            tmp = RC_LoadingXmlFile.Deserialization(_configurationFilePath);

            //miejsce na przypisanie zmiennych
        }
        #endregion


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
