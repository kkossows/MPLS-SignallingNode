using System.Xml.Serialization;

namespace ControlPlane
{
    [XmlRoot("LRM_Configuration")]
    public class LRM_XmlSchame
    {
        [XmlElement("localPcIpAddress")]
        public string XML_localPcIpAddress { get; set; }

        [XmlElement("routingAreaName")]
        public string XML_routingAreaName { get; set; }

        [XmlElement("administrativeAreaName")]
        public string XML_administrativeAreaName { get; set; }


    }
}
