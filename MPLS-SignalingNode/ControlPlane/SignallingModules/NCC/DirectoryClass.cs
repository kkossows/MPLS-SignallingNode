using MPLS_SignalingNode;
using System.Collections.Generic;

namespace ControlPlane
{
    class DirectoryClass
    {
        public List<string> CallingIdList { get; set; }
        public List<string> CallingIpAddressList { get; set;}
        public Dictionary<string, string> DirectoryDictionary { get; set;}
        public DirectoryClass()
            {

            }

        public dynamic CheckDirectory(string callingId, string calledId )
        {
            MakeDictionary(CallingIdList, CallingIpAddressList);


            string callingIpAddress = DirectoryDictionary[callingId];
            string calledIpAddress = DirectoryDictionary[calledId];
         //   DeviceClass.MakeLog("|SIGNALLING|RC| - Directory has been checked");
            DeviceClass.MakeConsoleLog("|SIGNALLING|RC| Directory has been checked");
            var DirectoryOutput = new { callingIpAddress, calledIpAddress};
            return DirectoryOutput;

        }

        public string CheckDirectory(string _idToTranslate)
        {
            MakeDictionary(CallingIdList, CallingIpAddressList);
            //MakeDictionary(CallingIdList, CallingIpAddressList);
            string _ipAd = DirectoryDictionary[_idToTranslate];
                return _ipAd;
        }

        public void MakeDictionary(List<string> CallingIdList, List<string>CallingIpAddressList)
        {
            DirectoryDictionary = new Dictionary<string, string>();
            for(int i = 0; i < CallingIdList.Count; i++)

            {
                DirectoryDictionary.Add(CallingIdList[i], CallingIpAddressList[i]);
            }

        }

    }
}
