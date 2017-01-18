using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlPlane
{
    public class SignalMessage
    {
        #region General
        public SignalType SignalMessageType { get; set; }
        public string DestinationIpAddress { get; set; }    //adres docelowego PC
        public string SourceIpAddress { get; set; }         //adres źródłowego PC

        public string DestinationModule { get; set; }       //nazwa modułu docelowego
        public string SourceModule { get; set; }            //nazwa modułu źródłowego
        #endregion

        #region Call
        public int CallID { get; set; }
        public string CallingID { get; set; }
        public string CalledID { get; set; }
        public int CallingCapacity { get; set; }
        #endregion

        #region Connection
        public int ConnnectionID { get; set; }
        public int LabelIN { get; set; }
        public int LabelOUT { get; set; }
        public int ModificationID { get; set; }
        public Pair SnppIdPair { get; set; }
        #endregion

        



        public enum ModuleType
        {
            RC, CC, LRM, NCC, CPCC
        };

        public enum SignalType
        {
            //CPCC
            CallRequest, CallAccept,
            
            //LRM
            LinkConnectionRequest, SNPNegotiation, SNPNegotiationAccept, LinkConnectionResponse
        };

        public struct Pair
        {
            int first;
            int second;
        };

        public SignalMessage()
        {

        }
    }


}
