using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ControlPlane
{
    [XmlRoot("PC_Configuration")]
    public class PC_XmlSchame
    {
        [XmlElement("myIPAddress")]
        public string XML_myIPAddress { get; set; }

        [XmlElement("myPortNumber")]
        public int XML_myPortNumber { get; set; }

        public PC_XmlSchame()
        {
        }

    }
}
