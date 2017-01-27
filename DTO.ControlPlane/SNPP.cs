using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DTO.ControlPlane
{
    [Serializable]
    public class SNPP
    {
        #region Variables
        [XmlElement("localID")]
        public int _localID;

        [XmlElement("areaName")]
        public string _areaName;

        [XmlElement("areaNameSnppID")]
        public int _areaNameSnppID;

        [XmlArray("FreeLabel-Table")]
        [XmlArrayItem("Record", typeof(int))]
        public List<int> _availableLabels;

        [XmlElement("AvailableCapacity")]
        public int _availableCapacity;

        [XmlArray("AllocatedSNP-List")]
        [XmlArrayItem("Record", typeof(SNP))]
        public List<SNP> _allocatedSNP;
        #endregion


        #region Methodes
        public SNPP()
        {
            _availableLabels = new List<int>();
            _allocatedSNP = new List<SNP>();
        }
        #endregion
    }
}
