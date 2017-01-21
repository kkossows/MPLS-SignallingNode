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

        [XmlElement("isInLsrSubnetwork")]
        public bool XML_IsInLsrSubnetwork { get; set; }

        public CC_XmlSchema()
        {

        }
    }
}
