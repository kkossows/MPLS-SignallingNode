using System.Collections.Generic;
using System.Xml.Serialization;

namespace ControlPlane
{
    [XmlRoot("CC_Configuration")]
    public class CC_XmlSchema
    {
        [XmlElement("localPcIpAddress")]
        public string XML_localPcIpAddress { get; set; }

        [XmlElement("areaName")]
        public string XML_areaName { get; set; }

        [XmlElement("higherAreaName")]
        public string XML_higherAreaName { get; set; }

        [XmlElement("isInLsrSubnetwork")]
        public bool XML_IsInLsrSubnetwork { get; set; }

        [XmlArray("CC-List")]
        [XmlArrayItem("Record", typeof(CCDescription))]
        public List<CCDescription> XML_CCList { get; set; }

        [XmlArray("FIB-List")]
        [XmlArrayItem("Record", typeof(FIBDescription))]
        public List<FIBDescription> XML_FIBList { get; internal set; }

        public CC_XmlSchema()
        {
            XML_CCList = new List<CCDescription>();
            XML_FIBList = new List<FIBDescription>();
        }
    }

    public struct CCDescription
    {
        [XmlElement("areaName")]
        public string areaName;
        [XmlElement("ipAddress")]
        public string ipAddress;
    }

    public struct FIBDescription
    {
        [XmlElement("connectionId")]
        public int connectionId;
        [XmlElement("fibIndex")]
        public int fibIndex;
    }
}
