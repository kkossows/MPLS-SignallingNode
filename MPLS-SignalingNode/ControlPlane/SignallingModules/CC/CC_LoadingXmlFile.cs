using MPLS_SignalingNode;
using System;
using System.IO;
using System.Xml.Serialization;

namespace ControlPlane
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
                SignallingNodeDeviceClass.MakeSignallingLog("CC", "ERROR - Deserialization cannot be complited.");
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
                SignallingNodeDeviceClass.MakeSignallingLog("CC", "ERROR - Serialization cannot be complited.");
            }
        }
    }
}
