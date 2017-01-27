using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTO.ControlPlane;

namespace ControlPlane
{
    class ConnectionTableRecord
    {
        //zmienne docelowe
        public int ConnectionID { get; set; }
        public int AllocatedCapacity { get; set; }
        public int LocalBoundaryFirstSnppID { get; set; }
        public int LocalBoundarySecondSnppID { get; set; }
        public List<List<SNP>> AllocatedSnps { get; set; }
        public List<string> AllocatedSnpAreaName { get; set; }
        public bool IsInterdomain { get; set; }
        public string Status { get; set; }

        //zmienne pomocnicze używane w procesie zestawiania 
        public int NumberOfRequest { get; set; }
        public int NumberOfResponse { get; set; }

        public ConnectionTableRecord()
        {
            AllocatedSnps = new List<List<SNP>>();
            AllocatedSnpAreaName = new List<string>();
        }

    }
}
