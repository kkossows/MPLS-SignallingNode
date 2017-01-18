using System.Collections.Generic;
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


        [XmlArray("LRM-List")]
        [XmlArrayItem("Record", typeof(LrmDescription))]
        public List<LrmDescription> XML_LrmList { get; set; }

        [XmlArray("SNPP-List")]
        [XmlArrayItem("Record", typeof(SNPP))]
        public List<SNPP> XML_SnppList { get; set; }

        public LRM_XmlSchame()
        {
            XML_LrmList = new List<LrmDescription>();
            XML_SnppList = new List<SNPP>();
        }
    }

    public struct LrmDescription
    {
        [XmlElement("areaName")]
        public string areaName;
        [XmlElement("ipAddress")]
        public string ipAddress;
    }
}

/*
 * LRM musi LRM'y z innych domen (po jednym na domene, tylko te główne)
 * oraz LRM'y podsieci z którymi może nawiązać połączenie czyli danej warstwy
 * oraz LRM'y mu podrzędne
 * 
 */

