using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.Input;
using System.Globalization;

#if NETFX_CORE   // Need this so that these libraries are only imported in the HoloLens
using Windows;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Networking;
using Windows.Foundation;
#endif

#if UNITY_EDITOR
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text;
using System;
#endif

namespace ShapeSensingNeedle {
    /// <summary>
    /// Class for establishing remote tcp connection and streaming data.
    /// </summary>
    public class TcpClientManager : MonoBehaviour
    {
        // Set the IP address of the server
        public string ServerIP;

        // Port that you want to use
        public int ConnectionPort = 20602;

        /// <summary>
        /// variables used to store interpolated polynomial coefficients
        /// </summary>
        public float ax = 0, bx = 0;
        public float ay = 0, by = 0;

        public int interlock = 0;  // This variable is used to protect read wright issues. However, might be unnecessary

        // code that should be in macro
        private Socket _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private byte[] _receiveBuffer = new byte[16];

        public void Start()
        {
            SetupServer();
        }

        private void SetupServer()
        {
            try
            {
                _clientSocket.Connect(new IPEndPoint(IPAddress.Parse(ServerIP), ConnectionPort));
            }
            catch (SocketException ex)
            {
                Debug.Log(ex.Message);
            }

            _clientSocket.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);

        }

        private void ReceiveCallback(IAsyncResult AR)
        {
            //Check how much bytes are recieved and call EndRecieve to finalize handshake
            int received = _clientSocket.EndReceive(AR);

            if (received <= 0)
                return;

            //Copy the received data into new buffer to avoid null bytes
            byte[] recData = new byte[received];
            Buffer.BlockCopy(_receiveBuffer, 0, recData, 0, received);

            //Process data here the way you want , all your bytes will be stored in recData
            if (interlock <= 0)
            {
                ax = BitConverter.ToSingle(_receiveBuffer, 0);
                bx = BitConverter.ToSingle(_receiveBuffer, 4);
                ay = BitConverter.ToSingle(_receiveBuffer, 8);
                by = BitConverter.ToSingle(_receiveBuffer, 12);
                interlock++;
            }

            //Start receiving again
            _clientSocket.BeginReceive(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
        }

        void OnApplicationQuit()
        {
            Debug.Log("Closing TCP socket");
            if (_clientSocket.Connected)
            {
                _clientSocket.Disconnect(false);
            }
            _clientSocket.Close();
        }

#if NETFX_CORE
        /// <summary>
        /// Tracks the network connection to the remote machine were the server runs.
        /// </summary>
        private StreamSocket socketConnection;

        /// <summary>
        /// Flag to indicate whether socket is open for streaming data
        /// </summary>
        private bool socketOpen = false;

        /// <summary>
        /// If socket is open this will be the object used to send data
        /// </summary>
        private DataWriter socketDataWriter;

        /// <summary>
        /// If socket is open this will be the object used to receive data
        /// </summary>
        private DataReader socketDataReader;

        /// <summary>
        /// Flag to indicate whether a connection attempt has been deferred
        /// </summary>
        private bool deferredConnection;

        public void Start()
        {

            OpenConnection();
            ax = 1; bx = 0;
            ay = 1; by = 0;
        }

        public void Update()
        {
            if (deferredConnection)
            {
                deferredConnection = false;
                Invoke("OpenConnection", 2); // Will call OpenConnection again after 2 seconds
            }
        }

        /// <summary>
        /// opens a socket connection as client.
        /// </summary>
        private void OpenConnection()
        {
            // Setup a connection to the server.
            HostName networkHost = new HostName(ServerIP.Trim());
            socketConnection = new StreamSocket();

            Debug.Log("Attempting to connect");

            // Connections are asynchronous. This callback function 'SocketOpenedHandler' will be called when the connection is established
            IAsyncAction outstandingAction = socketConnection.ConnectAsync(networkHost, ConnectionPort.ToString());
            AsyncActionCompletedHandler aach = new AsyncActionCompletedHandler(SocketOpenedHandler);
            outstandingAction.Completed = aach;
        }

        /// <summary>
        /// Called when a connection attempt complete, successfully or not.  
        /// </summary>
        /// <param name="asyncInfo">Data about the async operation.</param>
        /// <param name="status">The status of the operation.</param>
        public void SocketOpenedHandler(IAsyncAction asyncInfo, AsyncStatus status)
        {
            // Status completed is successful.
            if (status == AsyncStatus.Completed)
            {
                socketOpen = true;
                Debug.Log("Connected! Ready to send and receive data");
                socketDataWriter = new DataWriter(socketConnection.OutputStream);
                socketDataReader = new DataReader(socketConnection.InputStream);
                socketDataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
                socketDataReader.ByteOrder = ByteOrder.LittleEndian;

                //Begin reading data in the input stream async
                DataReaderLoadOperation outstandingRead = socketDataReader.LoadAsync(4*4); // Try to load 4 floating points
                AsyncOperationCompletedHandler<uint> aoch = new AsyncOperationCompletedHandler<uint>(DataReadHandler);
                outstandingRead.Completed = aoch;
            }
            else
            {
                Debug.Log("Failed to establish connection. Error Code: " + asyncInfo.ErrorCode);
                // In the failure case we'll requeue the data and wait before trying again.
                socketConnection.Dispose();
                // Setup a callback function that will retry connection after 2 seconds
                if (!socketOpen) // Redundant but to be safe
                {
                    deferredConnection = true; // Defer the connection attempt
                }
            }
        }

        /// <summary>
        /// Called when receive data has completed.
        /// </summary>
        /// <param name="operation">Data about the async operation.</param>
        /// <param name="status">The status of the operation.</param>
        public void DataReadHandler(IAsyncOperation<uint> operation, AsyncStatus status)
        {
            // If we failed, requeue the data and set the deferral time.
            if (status == AsyncStatus.Error)
            {
                // didn't load data
                Debug.Log("Failed to load new data");
            }
            else
            {

                ax = socketDataReader.ReadSingle();              // Substract the offset of 1 and scale from mm to meters
                bx = socketDataReader.ReadSingle();
                //cx = socketDataReader.ReadSingle();
                ay = socketDataReader.ReadSingle();
                by = socketDataReader.ReadSingle();
                //cy = socketDataReader.ReadSingle();
                //Debug.Log("ax " + ax + " bx " + bx + " cx " + cx + " ay " + ay + " by " + by + " cy " + cy);
                interlock = 1;

        
                //restart reading data in the input stream async
                DataReaderLoadOperation outstandingRead = socketDataReader.LoadAsync(4*4);
                AsyncOperationCompletedHandler<uint> aoch = new AsyncOperationCompletedHandler<uint>(DataReadHandler);
                outstandingRead.Completed = aoch;
            }
        }
#endif
    }
}