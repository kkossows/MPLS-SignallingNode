using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlPlane;

namespace MPLS_SignalingNode
{
    class Program
    {
        static void Main(string[] args)
        {
            DeviceClass device = new DeviceClass();
            device.StartWorking();
        }
    }
}
