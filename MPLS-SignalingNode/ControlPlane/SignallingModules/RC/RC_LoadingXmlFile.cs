using MPLS_SignalingNode;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ControlPlane
{
    class RC_LoadingXmlFile
    {
        public static RC_XmlSchame Deserialization(string configFilePath)
        {
            object obj = new object();
            XmlSerializer deserializer = new XmlSerializer(typeof(RC_XmlSchame));
            try
            {
                using (TextReader reader = new StreamReader(configFilePath))
                {
                    obj = deserializer.Deserialize(reader);
                }
                return obj as RC_XmlSchame;
            }
            catch (Exception e)
            {
                NodeDeviceClass.MakeSignallingLog("RC", "ERROR - Deserialization cannot be complited.");
                return null;
            }
        }
        public static void Serialization(string configFilePath, RC_XmlSchame dataSource)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(RC_XmlSchame));
            try
            {
                using (TextWriter writer = new StreamWriter(configFilePath, false))
                {
                    serializer.Serialize(writer, dataSource);
                }
            }
            catch (Exception e)
            {
                NodeDeviceClass.MakeSignallingLog("RC", "ERROR - Serialization cannot be complited.");
            }
        }
    }
}
