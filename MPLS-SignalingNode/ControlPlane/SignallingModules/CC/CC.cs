
namespace ControlPlane
{
    class CC
    {
        #region Variables
        private string _configurationFilePath;
        #endregion


        #region Main_Methodes
        public CC(string configurationFilePath)
        {
            InitialiseVariables(configurationFilePath);
        }
        private void InitialiseVariables(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;

            CC_XmlSchame tmp = new CC_XmlSchame();
            tmp = CC_LoadingXmlFile.Deserialization(_configurationFilePath);

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
