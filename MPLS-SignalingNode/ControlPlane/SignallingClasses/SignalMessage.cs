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
        public SignalType General_SignalMessageType { get; set; }
        public string General_DestinationIpAddress { get; set; }    //adres docelowego PC
        public string General_SourceIpAddress { get; set; }         //adres źródłowego PC

        public string General_DestinationModule { get; set; }       //nazwa modułu docelowego
        public string General_SourceModule { get; set; }            //nazwa modułu źródłowego
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


        #region LinkConnectionRequest_LinkConnectionResponse
        public bool LinkConnection_IsAccepted { get; set; }
        public List<SNP> LinkConnection_AllocatedSnpList { get; set; }
        public List<string> LinkConnection_AllocatedSnpAreaNameList { get; set; }
        #endregion

        #region SnppNegotiation_SnppNegotiationResponse
        public int Negotiation_ID { get; set; }
        public int Negotiation_ConnectionID { get; set; }
        public int Negotiation_SnppID  { get; set; }
        public int Negotiation_Label { get; set; }
        public int Negotiation_Capacity { get; set; }

        public bool Negotiation_isAccepted { get; set; }
        public SNP Negotiation_AllocatedSNP { get; set; }
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
            public int first;
            public int second;
        };

        public SignalMessage()
        {

        }
    }


}
