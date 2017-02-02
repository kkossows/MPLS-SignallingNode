using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ControlPlane
{
    class NCC_LoadingXmlFile
    {
        public static NCC_XmlSchame Deserialization(string configFilePath)
        {
            object obj = new object();
            XmlSerializer deserializer = new XmlSerializer(typeof(NCC_XmlSchame));
            try
            {
                using (TextReader reader = new StreamReader(configFilePath))
                {
                    obj = deserializer.Deserialize(reader);
                }
                return obj as NCC_XmlSchame;
            }
            catch (Exception e)
            {
                //    SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - Deserialization cannot be complited.");
                return null;
            }
        }
        public static void Serialization(string configFilePath, NCC_XmlSchame dataSource)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(NCC_XmlSchame));
            try
            {
                using (TextWriter writer = new StreamWriter(configFilePath, false))
                {
                    serializer.Serialize(writer, dataSource);
                }
            }
            catch (Exception e)
            {
                // SignallingNodeDeviceClass.MakeSignallingLog("LRM", "ERROR - Serialization cannot be complited.");
            }
        }
    }
}
