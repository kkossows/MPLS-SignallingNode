using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ControlPlane
{
    public class SNP
    {
        #region Variables
        [XmlElement("SnpID")]
        public int _snpID;

        [XmlElement("SnppID")]
        public int _snppID;
        [XmlElement("ConnectionID")]
        public int _connectionID;

        [XmlElement("AllocatedLabel")]
        public int _allocatedLabel;
        [XmlElement("AllocatedCapacity")]
        public int _allocatedCapacity;
        #endregion

        #region Methodes
        public SNP()
        {

        }
        #endregion
    }
}
