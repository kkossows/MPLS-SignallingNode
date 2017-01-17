using ControlPlane;

namespace MPLS_SignalingNode
{
    class NodeDeviceClass
    {
        #region Variables
        private CC _cc;
        private RC _rc;
        private LRM _lrm;
        private PC _pc;

        private string _configurationFolderPath;
        #endregion


        #region Configuration_Methodes
        public NodeDeviceClass(string configurationFolderPath)
        {
            _configurationFolderPath = configurationFolderPath;
            InitializeSignallingModules();
        }
        private void InitializeSignallingModules()
        {
            DeviceClass.MakeLog("|SIGNALLING| NodeDevise is waking up...");
            DeviceClass.MakeConsoleLog("|SIGNALLING| NodeDevise is waking up...");

            _pc = new PC(_configurationFolderPath + "/PC_config.xml");
            _cc = new CC(_configurationFolderPath + "/CC_config.xml");
            _rc = new RC(_configurationFolderPath + "/RC_config.xml");
            _lrm = new LRM(_configurationFolderPath + "/LRM_config.xml");

            StartWorking();
        }
        #endregion

        #region Start_Stop_Methodes
        private void StartWorking()
        {
            DeviceClass.MakeLog("|SIGNALLING| NodeDevise is working...");
            DeviceClass.MakeConsoleLog("|SIGNALLING| NodeDevise is working...");
        }
        private void StopWorking(string reason)
        {
            DeviceClass.MakeLog("|SIGNALLING| NodeDevise is working...");
        }
        #endregion


        #region Signalling_Log_Methodes
        public static void MakeSignallingLog(string moduleName, string logMessage)
        {
            DeviceClass.MakeLog("|SIGNALLING - " + moduleName+"| " + logMessage);
        }
        public static void MakeSignallingConsoleLog(string moduleName, string logMessage)
        {
            DeviceClass.MakeConsoleLog("|SIGNALLING - " + moduleName + "| " + logMessage);
        }
        #endregion
    }
}
