using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlPlane
{
    class SNPP
    {
        private int _localID;
        private string _areaName;
        private int _areaID;

        private int[] _availableLabels;
        private int _availableCapacity;

        private List<SNP> _allocatedSNP;
    }
}
