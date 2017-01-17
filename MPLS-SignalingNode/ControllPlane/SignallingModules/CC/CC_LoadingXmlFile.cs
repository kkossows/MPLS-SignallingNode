using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MPLS_SignalingNode.ControllPlane.SignallingModules.CC
{
    class CC_LoadingXmlFile
    {
        public static CC_XmlSchame Deserialization(string configFilePath)
        {
            object obj = new object();
            XmlSerializer deserializer = new XmlSerializer(typeof(CC_XmlSchame));
            try
            {
                using (TextReader reader = new StreamReader(configFilePath))
                {
                    obj = deserializer.Deserialize(reader);
                }
                return obj as CC_XmlSchame;
            }
            catch (Exception e)
            {
                NodeDeviceClass.MakeSignallingLog("CC", "ERROR - Deserialization cannot be complited.");
                return null;
            }
        }
        public static void Serialization(string configFilePath, CC_XmlSchame dataSource)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(CC_XmlSchame));
            try
            {
                using (TextWriter writer = new StreamWriter(configFilePath, false))
                {
                    serializer.Serialize(writer, dataSource);
                }
            }
            catch (Exception e)
            {
                NodeDeviceClass.MakeSignallingLog("CC", "ERROR - Serialization cannot be complited.");
            }
        }
    }
}
