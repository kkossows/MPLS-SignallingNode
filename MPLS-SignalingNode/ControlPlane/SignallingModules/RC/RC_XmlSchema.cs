using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ControlPlane
{
    [XmlRoot("RC_Configuration")]
    public class RC_XmlSchame
    {
        [XmlElement("myIPAddress")]
        public string XML_myIPAddress { get; set; }

        [XmlElement("myAreaName")]
        public string XMP_myAreaName { get; set; }

        public struct IPTOID
        {
            [XmlElement("IP")]
            public string IP { get; set; }
            [XmlElement("ID")]
            public int ID { get; set; }
        }

        [XmlElement("IPTOID")]
        public IPTOID[] Dictionary { get; set; }


        [XmlArray("LocalTopology")]
        [XmlArrayItem("Record", typeof(Topology))]
        public List<Topology> LocalTopology { get; set; }


        public RC_XmlSchame()
        {
            LocalTopology = new List<ControlPlane.Topology>();
        }

    }


    public class Topology
    {
        [XmlElement("ID")]
        public int ID { get; set; }
        [XmlElement("capacity")]
        public int capacity { get; set; }
        [XmlElement("weight")]
        public double weight { get; set; }

        [XmlArray("reachableID-List")]
        [XmlArrayItem("Record", typeof(int))]
        public List<int> reachableID { get; set; }

        [XmlElement("areaName")]
        public string areaName { get; set; }

        public Topology()
        {
            reachableID = new List<int>();
        }
    }
}
