using System;
using System.Collections.Generic;
using DTO.ControlPlane;

namespace ControlPlane
{
    class NCC
    {
        private string _configurationFilePath {
            get; set; }
        public string _localPcIpAddress { get; set; }
        public string _callingIpAddress { get; set; }
        public string _calledIpAddress { get; set; }
        PC _pc;
        PolicyClass policyClass;
        DirectoryClass dirClass;
        SignalMessage sm;
        List<String> DomainList { get; set; }
        public Dictionary<string, string> DomainDictionary { get; set; }
        #region Main_Methodes

        #region Properties
        public PC LocalPC { set { _pc = value; } }
        #endregion


        public NCC(string configurationFilePath)
        {
            policyClass = new PolicyClass();
            dirClass = new DirectoryClass();
            InitialiseVariables(configurationFilePath);

        }


        private void InitialiseVariables(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;

            NCC_XmlSchame tmp = new NCC_XmlSchame();
            tmp = NCC_LoadingXmlFile.Deserialization(_configurationFilePath);

            _localPcIpAddress = tmp.XML_localPcIpAddress;


            //Inicjalizacja parametrów klasy Directory
            dirClass.CallingIdList = tmp.XML_CallingIdList;
            dirClass.CallingIpAddressList = tmp.XML_CallingIpAddressList;
            DomainList = tmp.XML_DomainList;

        }



        #endregion
        #region Methodes_From_Standarization
        public void CallIndication(int _callID, string _callingID, string _calledID, int _callingcapacity)
        {
            sm = new SignalMessage();
            //sm.LinkConnection_AllocatedSnpAreaNameList.Add("roe");

            sm.General_SignalMessageType = SignalMessage.SignalType.CallIndication;
            sm.CalledID = _calledID;
            sm.General_DestinationIpAddress = dirClass.CheckDirectory(_calledID);
            sm.General_SourceIpAddress = _localPcIpAddress; ;
            sm.General_SourceModule = "NCC";
            sm.General_DestinationModule = "CPCC";
            
            sm.CallID = _callID;
            sm.CallingID = _callingID;
            sm.CalledID = _calledID;
            sm.CallingCapacity = _callingcapacity;
            SendMessageToPC(sm);

        }

        public void CallCoordination(int _callID, string _callingID, string _calledID, int _callingcapacity)
        {
            sm = new SignalMessage();
            //sm.LinkConnection_AllocatedSnpAreaNameList.Add("roe");

            sm.General_SignalMessageType = SignalMessage.SignalType.CallCoordination;

            sm.General_DestinationIpAddress = NCCForwarding(_localPcIpAddress);
            sm.General_SourceIpAddress = _localPcIpAddress; ;
            sm.General_SourceModule = "NCC";
            sm.General_DestinationModule = "NCC";

            sm.CallID = _callID;
            sm.CallingID = _callingID;
            sm.CalledID = _calledID;
            sm.CallingCapacity = _callingcapacity;
            SendMessageToPC(sm);

        }

        public void ConnectionRequest(int _callID, string _callingIpAddress, string _calledIpAddress, int _callingCapacity)
        {
            sm = new SignalMessage();
            //sm.LinkConnection_AllocatedSnpAreaNameList.Add("roe");

            sm.General_SignalMessageType = SignalMessage.SignalType.ConnectionRequest;
            sm.General_DestinationIpAddress = _localPcIpAddress;
            sm.General_SourceIpAddress = _localPcIpAddress;
            sm.General_SourceModule = "NCC";
            sm.General_DestinationModule = "CC";
            sm.CallID = _callID;
            sm.CallingIpAddress = _callingIpAddress;
            sm.CalledIpAddress = _calledIpAddress;
            sm.CallingCapacity = _callingCapacity;
            SendMessageToPC(sm);
        }

        public void CallAccept(int _callID, bool _confirmation, int _labelIN, int _callingcapacity)
        {
            sm = new SignalMessage();
            //sm.LinkConnection_AllocatedSnpAreaNameList.Add("roe");

            sm.General_SignalMessageType = SignalMessage.SignalType.CallAccept;
            sm.General_DestinationIpAddress = NCCForwarding(_localPcIpAddress);
            sm.General_SourceIpAddress = _localPcIpAddress;
            sm.General_SourceModule = "NCC";
            sm.General_DestinationModule = "NCC";
            sm.CallID = _callID;
            
            sm.Confirmation = _confirmation;
            sm.LabelIN = _labelIN;
            sm.CallingCapacity = _callingcapacity;
            SendMessageToPC(sm);
        }
        public string CheckDomain(string ipAddReceiver)
        {
            return ipAddReceiver;
        }
        #endregion


        #region PC_Cooperation_Methodes
        private void SendMessageToPC(SignalMessage message)
        {
            _pc.SendSignallingMessage(message);
        }


        public void ReceiveMessageFromPC(SignalMessage message)
        {
            switch (message.General_SignalMessageType)
            {
                case SignalMessage.SignalType.CallRequest:

                    CallRequestProceeding(message);

                    break;

                case SignalMessage.SignalType.CallAccept:
                    CallAcceptProceeding(message);
                    break;
                //
                case SignalMessage.SignalType.CallCoordination:
                    {
                        var DirectoryOutput = dirClass.CheckDirectory(message.CallingID, message.CalledID);
                        _callingIpAddress = DirectoryOutput.callingIpAddress;
                        _calledIpAddress = DirectoryOutput.calledIpAddress;
                        policyClass.CheckPolicy();
                        CallIndication(message.CallID, message.CallingID, message.CalledID, message.CallingCapacity);
                        break;
                    }
                case SignalMessage.SignalType.CallIndication:
                    CallRequestProceeding(message);
                    // Console.WriteLine("Plecki");
                    break;
                case SignalMessage.SignalType.CallModificationIndication:

                    break;
                case SignalMessage.SignalType.CallModificationAccept:

                    break;


            }
        }
        #endregion

        #region Proceeding_Methods
        public void CallRequestProceeding(SignalMessage message)
        {
            var DirectoryOutput = dirClass.CheckDirectory(message.CallingID, message.CalledID);
            _callingIpAddress = DirectoryOutput.callingIpAddress;
            _calledIpAddress = DirectoryOutput.calledIpAddress;
            policyClass.CheckPolicy();

            if (_localPcIpAddress == ReturnProperDomain(message))

                CallIndication(message.CallID, message.CallingID, message.CalledID, message.CallingCapacity);
            else
                CallCoordination(message.CallID, message.CallingID, message.CalledID, message.CallingCapacity);
        }

        public void CallAcceptProceeding(SignalMessage message)
        {
            //_callingIpAddress = message.CalledIpAddress;

            if (message.General_SourceModule == "CPCC" && (ReturnProperDomain(_callingIpAddress) == _localPcIpAddress)) 
                ConnectionRequest(message.CallID, _calledIpAddress, _callingIpAddress, message.CallingCapacity);
            else if (message.General_SourceModule == "NCC")
                ConnectionRequest(message.CallID, _calledIpAddress, _callingIpAddress, message.CallingCapacity);
            else
                CallAccept(message.CallID, message.Confirmation, message.LabelIN, message.CallingCapacity);
        }
        public void MakeDictionary(List<string> CallingIdList, List<string> CallingIpAddressList)
        {
            DomainDictionary = new Dictionary<string, string>();
            for (int i = 0; i < CallingIdList.Count; i++)

            {
                DomainDictionary.Add(CallingIdList[i], CallingIpAddressList[i]);
            }

        }
        public bool isItMyDomain(SignalMessage sm)
        {
            MakeDictionary(dirClass.CallingIpAddressList, DomainList);
            if (DomainDictionary[_calledIpAddress] == DomainDictionary[_callingIpAddress])
            {

                return true;
            }
            else
                return false;
        }
        public string ReturnProperDomain(SignalMessage sm)
        {
            MakeDictionary(dirClass.CallingIpAddressList, DomainList);
            if (DomainDictionary[_calledIpAddress] == _localPcIpAddress)
            {
                // sm.General_DestinationModule = "CPCC";
                return _localPcIpAddress;
            }
            else
            {
                // sm.General_DestinationModule = "NCC";

                return DomainDictionary[_calledIpAddress];
            }
        }

        public string ReturnProperDomain(string h)
        {
            MakeDictionary(dirClass.CallingIpAddressList, DomainList);
            if (DomainDictionary[h]==_localPcIpAddress)
            {
                // sm.General_DestinationModule = "CPCC";
                h = _localPcIpAddress;
                return h;
            }
            else
            {
                // sm.General_DestinationModule = "NCC";
                h = DomainDictionary[_callingIpAddress];
                return h;
            }
        }
        #endregion

        public string NCCForwarding(string _localIpAdress)
        {
            string AnotherNCC;
            if (_localPcIpAddress == DomainList[0])
                AnotherNCC = DomainList[2];
            else
                AnotherNCC = DomainList[0];
            return AnotherNCC;
        }

    } }
