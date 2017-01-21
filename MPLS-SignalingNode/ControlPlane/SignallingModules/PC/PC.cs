using MPLS_SignalingNode;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters.Binary;


//Tutaj musze zrobić wysyłanie pakietów 
namespace ControlPlane
{

    class PC
    {
        #region Delegates
        private delegate void Delegate_SendInsideMessage(SignalMessage message);
        private delegate void Delegate_ReceiveInsideMessage(SignalMessage message);

        private delegate void Delegate_ReceiveOutsideMessage(SignalMessage message);
        #endregion

        #region Network_Variables
        private Socket _pcSocket;
        private IPEndPoint _pcIpEndPoint;

        private string _pcIpAddress;
        private int _pcPort;

        private IPEndPoint _receivingIPEndPoint;
        private EndPoint _receivingEndPoint;

        private byte[] _buffer;
        #endregion

        #region Other_Variables
        private string _configurationFilePath;

        private CC _moduleCC;
        private RC _moduleRC;
        private LRM _moduleLRM;
        #endregion

        #region Properties
        #endregion


        #region Main_Methodes
        public PC(string configurationFilePath, CC ccPointer, RC rcPointer, LRM lrmPointer)
        {
            InitialiseVariables(configurationFilePath);
            InitializeSocket();

            _moduleCC = ccPointer;
            _moduleRC = rcPointer;
            _moduleLRM = lrmPointer;
        }
        private void InitialiseVariables(string configurationFilePath)
        {
            _configurationFilePath = configurationFilePath;

            PC_XmlSchame tmp = new PC_XmlSchame();
            tmp = PC_LoadingXmlFile.Deserialization(_configurationFilePath);

            _pcIpAddress = tmp.XML_myIPAddress;
            _pcPort = tmp.XML_myPortNumber;
        }
        #endregion


        #region Network_Methodes
        private void InitializeSocket()
        {
            try
            {
                //tworzymy gniazdo i przypisujemy mu numer portu i IP zgodne z plikiem konfig
                _pcSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _pcIpEndPoint = new IPEndPoint((IPAddress.Parse(_pcIpAddress)), _pcPort);
                _pcSocket.Bind(_pcIpEndPoint);
            }
            catch
            {
                //LOG
                SignallingNodeDeviceClass.MakeSignallingLog("PC", "ERROR - Incorrect IP address or port number or these values are already in use.");
                //_cloud.StopWorking("Incorrect IP address or port number or these values are already in use.");
            }

            //LOG
            SignallingNodeDeviceClass.MakeSignallingLog("PC", "INFO - PC Socket: IP:" + _pcIpAddress + " Port:" + _pcPort);

            //tworzymy punkt końcowy, z którego będziemy odbierali dane (z jakiegokolwiek adresu IP na porcie sygnalizacyjnym _pcPort)
            _receivingIPEndPoint = new IPEndPoint(IPAddress.Any, _pcPort);
            _receivingEndPoint = (EndPoint)_receivingIPEndPoint;

            //tworzymy bufor nasłuchujący
            _buffer = new byte[_pcSocket.ReceiveBufferSize];

            //nasłuchujemy
            _pcSocket.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref _receivingEndPoint, new AsyncCallback(ReceivedPacketCallback), null);

            //LOG
            SignallingNodeDeviceClass.MakeSignallingLog("PC", "INFO - Start Listening.");
        }
        private void ReceivedPacketCallback(IAsyncResult res)
        {
            int size;
            try
            {
                //kończę odbieranie danych
                size = _pcSocket.EndReceiveFrom(res, ref _receivingEndPoint);
            }
            catch
            {
                IPEndPoint unreachableHost = _receivingEndPoint as IPEndPoint;
                SignallingNodeDeviceClass.MakeSignallingLog("PC", "ERROR - Cannnot send packet to: IP:" + unreachableHost.Address + " Port: " + unreachableHost.Port + ". Destination unreachable (Port unreachable)");

                //ustawiam odpowiedni recivingEndPoint
                _receivingIPEndPoint = new IPEndPoint(IPAddress.Any, _pcPort);
                _receivingEndPoint = (EndPoint)_receivingIPEndPoint;

                //tworzymy bufor nasłuchujący
                _buffer = new byte[_pcSocket.ReceiveBufferSize];

                //uruchamiam ponowne nasłuchiwanie
                _pcSocket.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref _receivingEndPoint, new AsyncCallback(ReceivedPacketCallback), null);

                return;
            }

            //tworzę tablicę bajtów składającą się jedynie z danych otrzymanych (otrzymany pakiet)
            byte[] receivedPacket = new byte[size];
            Array.Copy(_buffer, receivedPacket, receivedPacket.Length);

            //tworzę tymczasowy LOKALNY punkt końcowy zawierający informacje o nadawcy (jego ip oraz nr portu)
            IPEndPoint _receivedIPEndPoint = (IPEndPoint)_receivingEndPoint;

            //zeruje bufor odbierający
            _buffer = new byte[_pcSocket.ReceiveBufferSize];

            //ustawiam odpowiedni recivingEndPoint
            _receivingIPEndPoint = new IPEndPoint(IPAddress.Any, _pcPort);
            _receivingEndPoint = (EndPoint)_receivingIPEndPoint;

            //tworzę logi
            SignallingNodeDeviceClass.MakeSignallingLog("PC", "INFO - Received packet from: IP:" + _receivedIPEndPoint.Address + " Port: " + _receivedIPEndPoint.Port);

            //uruchamiam ponowne nasłuchiwanie
            _pcSocket.BeginReceiveFrom(_buffer, 0, _buffer.Length, SocketFlags.None, ref _receivingEndPoint, new AsyncCallback(ReceivedPacketCallback), null);

            //przesyłam otrzymaną wiadomość do metody odpowiedzialnej za przetwarzanie
            ReceiveOutsidePacket(receivedPacket);
        }
        private void SendPacketCallback(IAsyncResult res)
        {
            var endPoint = res.AsyncState as IPEndPoint;

            //tworzę logi
           SignallingNodeDeviceClass.MakeSignallingLog("PC", "INFO - Packet send to: IP:" + endPoint.Address + " Port: " + endPoint.Port);
            
            int size = _pcSocket.EndSendTo(res);     
        }
        #endregion


        #region Outside_Message_Methodes
        private void SendOutsidePacket(byte[] myPacket, string destinationIP)
        {
            byte[] packet = myPacket;
            IPEndPoint destinationIpEndPoint = new IPEndPoint(IPAddress.Parse(destinationIP), _pcPort);

            //inicjuje start wysyłania przetworzonego pakietu do nadawcy
            _pcSocket.BeginSendTo(packet, 0, packet.Length, SocketFlags.None, destinationIpEndPoint, new AsyncCallback(SendPacketCallback), null);
        }
        private void ReceiveOutsidePacket(byte[] receivedPacket)
        {
            SignalMessage receivedMessage = ByteToSignalMessage(receivedPacket);
            Delegate_ReceiveOutsideMessage receiveMessage = null;
            switch (receivedMessage.General_DestinationModule)
            {
                case "CC":
                    receiveMessage = new Delegate_ReceiveOutsideMessage(_moduleCC.ReceiveMessageFromPC);
                    break;
                case "RC":
                    receiveMessage = new Delegate_ReceiveOutsideMessage(_moduleRC.ReceiveMessageFromPC);
                    break;
                case "LRM":
                    receiveMessage = new Delegate_ReceiveOutsideMessage(_moduleLRM.ReceiveMessageFromPC);
                    break;
                default:
                    SignallingNodeDeviceClass.MakeSignallingLog("PC", "ERROR - Destination module unknown.");
                    break;
            }
            if (receiveMessage != null)
                receiveMessage.BeginInvoke(receivedMessage, new AsyncCallback(ReceiveOutsideMessageCallback), null);
        }
        private void ReceiveOutsideMessageCallback(IAsyncResult async)
        {
            //tutaj nie chcemy nic robić, po prostu zasoby mają się zwolnić
            //metoda wywoływana po wyjściu z metody ReceiveMessageFromPC
            AsyncResult ar = (AsyncResult)async;
            Delegate_ReceiveOutsideMessage del = (Delegate_ReceiveOutsideMessage)ar.AsyncDelegate;
            del.EndInvoke(async);
        }
        #endregion


        #region Inside_Message_Methodes
        private void SendInsideMessage(SignalMessage message)
        {
            Delegate_ReceiveInsideMessage receiveMessage = new Delegate_ReceiveInsideMessage(ReceiveInsideMessage);
            receiveMessage.BeginInvoke(message, new AsyncCallback(ReceiveInsideMessageCallback), null);
        }
        private static void SendInsideMessageCallback(IAsyncResult async)
        {
            SignallingNodeDeviceClass.MakeSignallingLog("PC", "INFO - Sent inside message");

            //metoda wywoływana po wyjściu z metody SendInsideMessage
            AsyncResult ar = (AsyncResult)async;
            Delegate_SendInsideMessage del = (Delegate_SendInsideMessage)ar.AsyncDelegate;
            del.EndInvoke(async);
        }


        private void ReceiveInsideMessage(SignalMessage message)
        {
            Delegate_ReceiveInsideMessage receiveMessage = null;

            switch (message.General_DestinationModule)
            {
                case "CC":
                    receiveMessage = new Delegate_ReceiveInsideMessage(_moduleCC.ReceiveMessageFromPC);
                    break;
                case "RC":
                    receiveMessage = new Delegate_ReceiveInsideMessage(_moduleRC.ReceiveMessageFromPC);
                    break;
                case "LRM":
                    receiveMessage = new Delegate_ReceiveInsideMessage(_moduleLRM.ReceiveMessageFromPC);
                    break;
                default:
                    SignallingNodeDeviceClass.MakeSignallingLog("PC", "ERROR - Destination module unknown.");
                    break;
            }

            if(receiveMessage != null)
                receiveMessage.BeginInvoke(message, new AsyncCallback(ReceiveInsideMessageCallback), null);
        }
        private static void ReceiveInsideMessageCallback(IAsyncResult async)
        {
            SignallingNodeDeviceClass.MakeSignallingLog("PC", "INFO - Received inside message");

            //metoda wywoływana po wyjściu z metody ReceiveInsideMessage
            AsyncResult ar = (AsyncResult)async;
            Delegate_ReceiveInsideMessage del = (Delegate_ReceiveInsideMessage)ar.AsyncDelegate;
            del.EndInvoke(async);    
        }
        #endregion


        #region Public_methodes_connected_with_sending
        public void SendSignallingMessage(SignalMessage message)
        {
            //trzeba jakoś sprawdzić, czy ma wysyłać wiadomość wewnętrzną, czy zewnętrzną
            bool insideMessage = CheckIfMessageIsInsideOrOutside(message);

            if(insideMessage)
            {
                //inicjalizacja delegata
                Delegate_SendInsideMessage sendMessage = new Delegate_SendInsideMessage(SendInsideMessage);
                sendMessage.BeginInvoke(message, new AsyncCallback(SendInsideMessageCallback), null);
            }
            else
            {
                string destinationIP = message.General_DestinationIpAddress;
                byte[] data = SignalMessageToByte(message);

                SendOutsidePacket(data, destinationIP);
            }
        }
        private bool CheckIfMessageIsInsideOrOutside(SignalMessage message)
        {
            if (_pcIpAddress == message.General_DestinationIpAddress)
                return true;
            else
                return false;
        }
        #endregion


        #region Serialize_Deserialize_Of_SignalMessage
        static byte[] SignalMessageToByte(SignalMessage sm)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, sm);   //Wpisanie w memory streama obiektu
            return ms.ToArray();    //Zwraca ms w formie tablicy bajtów
        }
        private static SignalMessage ByteToSignalMessage(byte[] inData)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();

            ms.Write(inData, 0, inData.Length);
            ms.Seek(0, SeekOrigin.Begin);
            object o = (object)bf.Deserialize(ms);

            return (SignalMessage)o;
        }
        #endregion
    }
}
