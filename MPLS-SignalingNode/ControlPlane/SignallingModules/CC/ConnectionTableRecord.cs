using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlPlane
{
    class ConnectionTableRecord
    {
        public int ConnectionID { get; set; }
        public int AllocatedCapacity { get; set; }
        public int LocalBoundaryFirstSnppID { get; set; }
        public int LocalBoundarySecondSnppID { get; set; }
        public List<List<SNP>> AllocatedSnps { get; set; }
        public List<string> AllocatedSnpPairsAreaName { get; set; }
        public string Status { get; set; }
        public bool IsInterdomain { get; set; }

        public List<SignalMessage.Pair> SnppIdPairToAllocate { get; set; }

        public ConnectionTableRecord()
        {
            AllocatedSnps = new List<List<SNP>>();
            AllocatedSnpPairsAreaName = new List<string>();
        }

    }
}
