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
        public string CallingIpAddress { get; set; } //wywołujący (opcjonalny parametr)
        public string CalledIpAddress { get; set; } //wywoływany (opcjonalny parametr)
        public int AllocatedCapacity { get; set; }

        public int LocalBoundaryFirstSnppID { get; set; }
        public int LocalBoundarySecondSnppID { get; set; }

        public List<List<SNP>> AllocatedSnps { get; set; }
        public List<string> AllocatedSnpAreaName { get; set; }

        public bool IsInterdomain { get; set; }
        public string NextDomainName { get; set; }
        public string RequestFrom { get; set; }
        public string Status { get; set; }

        public int LabelIn { get; set; }
        public int LabelOut { get; set; }

        //zmienne pomocnicze używane w procesie zestawiania 
        public int NumberOfRequest { get; set; }
        public int NumberOfResponse { get; set; }
        public string DestOrSourIp { get; set; }
        public int LinkConnectionIdToProceed { get; set; }
        public int NumberOfConfirmationResponses { get; set; }

        public ConnectionTableRecord()
        {
            AllocatedSnps = new List<List<SNP>>();
            AllocatedSnpAreaName = new List<string>();
        }

    }
}
