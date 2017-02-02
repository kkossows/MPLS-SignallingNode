using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ControlPlane
{
    [XmlRoot("NCC_Configuration")]
   public class NCC_XmlSchame
    {
             
            [XmlElement("localPcIpAddress")]
            public string XML_localPcIpAddress { get; set; }


           [XmlArray("CalledId-List")]
           [XmlArrayItem("Record", typeof(string))]
           public List<string> XML_CallingIdList { get; set; }

            [XmlArray("CalledIpAddress-List")]
            [XmlArrayItem("Record", typeof(string))]
            public List<string> XML_CallingIpAddressList { get; set; }

        [XmlArray("Domain-List")]
        [XmlArrayItem("Record", typeof(string))]
        public List<string> XML_DomainList { get; set; }



        public NCC_XmlSchame()
            {
            XML_CallingIdList = new List<string>();
            XML_CallingIpAddressList = new List<string>();
            XML_DomainList = new List<string>();
        }
    
    }
}
