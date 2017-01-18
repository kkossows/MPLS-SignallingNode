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
    public class PC_LoadingXmlFile
    {
        public static PC_XmlSchame Deserialization( string configFilePath)
        {
            object obj = new object();
            XmlSerializer deserializer = new XmlSerializer(typeof(PC_XmlSchame));
            try
            {
                using (TextReader reader = new StreamReader(configFilePath))
                {
                    obj = deserializer.Deserialize(reader);
                }
                return obj as PC_XmlSchame;
            }
            catch (Exception e)
            {
                NodeDeviceClass.MakeSignallingLog("PC", "ERROR - Deserialization cannot be complited.");
                return null;
            }
        }
        public static void Serialization(string configFilePath, PC_XmlSchame dataSource)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(PC_XmlSchame));
            try
            {
                using (TextWriter writer = new StreamWriter(configFilePath, false))
                {
                    serializer.Serialize(writer, dataSource);
                }
            }
            catch (Exception e)
            {
                NodeDeviceClass.MakeSignallingLog("PC", "ERROR - Serialization cannot be complited.");
            }
        }
    }
}
