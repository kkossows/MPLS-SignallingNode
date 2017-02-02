using MPLS_SignalingNode;

namespace ControlPlane
{
    class PolicyClass
    {
        public PolicyClass()
        {

        }

        public void CheckPolicy()
        {
           // DeviceClass.MakeLog("|SIGNALLING| Policy Has been checked");
            DeviceClass.MakeConsoleLog("|SIGNALLING| Policy has been checked");
        }
    }
}
