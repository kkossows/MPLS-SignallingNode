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

        #region Properties
        public bool isWorking { get; private set; }
        #endregion

        public NodeDeviceClass(string configurationFolderPath)
        {
            _configurationFolderPath = configurationFolderPath;

            _pc = new PC(_configurationFolderPath + "/PC_config.xml");
            _cc = new CC(_configurationFolderPath + "/CC_config.xml");
            _rc = new RC(_configurationFolderPath + "/RC_config.xml");
            _lrm = new LRM(_configurationFolderPath + "/LRM_config.xml");
        }

        private void InitializeSignallingModules()
        {
            DeviceClass.MakeLog("|SIGNALLINg| NodeDevise is waking up...");
            DeviceClass.MakeConsoleLog("|SIGNALLINg| NodeDevise is working...");
        }


        #region Start_Stop_Methodes
        private void StartWorking()
        {
            isWorking = true;
            DeviceClass.MakeLog("|SIGNALLINg| NodeDevise is working...");
            DeviceClass.MakeConsoleLog("|SIGNALLINg| NodeDevise is working...");
        }
        private void StopWorking(string reason)
        {
            Console.WriteLine();
            Console.WriteLine(reason);
            Console.WriteLine("Click 'enter' to close the application...");
            Console.ReadLine();

            //LOG
            DeviceClass.MakeLog("INFO - Stop working.");
            DeviceClass.MakeConsoleLog("INFO - Stop working.");

            //wyłącz konsolę i zwolnij calą pamięć alokowaną
            Environment.Exit(0);
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
