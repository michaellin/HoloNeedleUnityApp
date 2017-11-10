/// <summary>
/// Handles serial threading. Create SerialThreadLoop objects 
/// for threads that handle specifically serial communication.
/// 
/// Author: A. Siu
/// June 30, 2017
/// </summary>
/// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.IO.Ports;

using System.Diagnostics; // for the stopwatch

using UnityEngine;

namespace thrThreadLoop
{
    public class SerialThreadLoop : DataStreamThreadLoop
    {

        // The serial port
        private SerialPort SerialPort;
        
        #region serial port settings
        private string _ComPort;
        private int _BaudRate = 115200;
        private int _ReadTimeout = 10;
        private int _WriteTimeout = 10;
        #endregion serial port settings

        // Vars to store data
        private string _RawData;
        private string[] _ParsedData;

        // The values separator used to create the ParsedData
        private char ValuesSeparator = '\n';

        #region Delegates & Events
        // Define a delegate and event to fire off notifications to registered objects
        // Any number of these events can be created
        // Delegate and event for when a new line is received 
        public delegate void SerialReceivedDataEventHandler(string[] data, string rawData);
        public static event SerialReceivedDataEventHandler SerialReceivedDataEvent;
        #endregion Delegates & Events


        public SerialThreadLoop(string strThreadName, Boolean bolIsBackGround,
            System.Threading.ThreadPriority ePriority, Double numTimerInterval, Double numMaximumLoopSpan,
            string comPort, int baudRate, int readTimeout, int writeTimeout)
            : base(strThreadName, bolIsBackGround, ePriority, numTimerInterval, numMaximumLoopSpan)
        {
            _ComPort = comPort;
            _BaudRate = baudRate;
            _ReadTimeout = readTimeout;
            _WriteTimeout = writeTimeout;

            // open the serial port
            this.OpenStream();
        }

        /// <summary>
        /// Opens the serial port here.
        /// </summary>
        /// <param name="comPort">COM port.</param>
        /// <param name="baudRate">Baud rate.</param>
        /// <param name="readTimeout">Read timeout.</param>
        /// <param name="writeTimeout">Write timeout.</param>
        public override void OpenStream()
        {
            OpenSerialPort();
        }

        public override bool IsOpenStream()
        {
            if (SerialPort == null)
            {
                return false;
            }
            return SerialPort.IsOpen;
        }

        public override void CloseStream()
        {
            CloseSerialPort();
        }

        public override void WriteDataStream(byte[] bytes2send)
        {
            if ( IsOpenStream() )
            {
                SerialPort.Write(bytes2send, 0, bytes2send.Length);
            } else
            {
                this.OpenStream();
            }
        }

        /// <summary>
        /// Reads the data from serial port.
        /// </summary>
        /// <returns><c>true</c>, if data stream was read, <c>false</c> otherwise.</returns>
        public override bool ReadDataStream()
        {
            try
            {
                string rData = SerialPort.ReadLine();
                // If the data is valid then do something with it
                if (rData != null && rData != "")
                {
                    // Store the raw data
                    _RawData = rData;

                    // Split the raw data into chunks via ValueSeparator and store it
                    // into a string array
                    _ParsedData = _RawData.Split(ValuesSeparator);

                    // Parse data
                    ParseSerialData(_ParsedData, _RawData);
                    return true;
                }
            }
            catch (TimeoutException)
            {
                //Debug.Log ("");
            }
            return false;
        }

        /// <summary>
        /// Function to parse data received
        /// </summary>
        /// <param name="data">string of raw data</param>
        private void ParseSerialData(string[] data, string rawData)
        {
            // If received data is valid, fire a notification to all registered objects
            if (data != null && rawData != string.Empty)
            {
                if (SerialReceivedDataEvent != null)
                {
                    SerialReceivedDataEvent(data, rawData);
                }
            }
        }

        #region serial port helpers
        /// <summary>
        /// Opens the serial port.
        /// </summary>
        private void OpenSerialPort()
        {
            try
            {

                if (_ComPort == "")
                {
                    // try opening first port on startup
                    _ComPort = GetDefaultPort();
                    // if it still couln't find a port then return
                    if (_ComPort == "")
                    {
                        UnityEngine.Debug.Log("Error: Couldn't find serial port.");
                        return;
                    }
                }

                // Initialise the serial port
                SerialPort = new SerialPort(_ComPort, _BaudRate);
                
                if (_ReadTimeout == 0)
                {
                    SerialPort.ReadTimeout = 10;
                }
                else
                {
                    SerialPort.ReadTimeout = _ReadTimeout;
                }

                if (_WriteTimeout == 0)
                {
                    SerialPort.WriteTimeout = 10;
                }
                else
                {
                    SerialPort.ReadTimeout = _WriteTimeout;
                }

                // Open the serial port
                SerialPort.Open();

                // clear input buffer from previous garbage
                SerialPort.DiscardInBuffer();

                UnityEngine.Debug.Log("Opening serial port: " + _ComPort);

            }
            catch (Exception ex)
            {
                // Failed to open com port or start serial thread
                UnityEngine.Debug.Log("Error 1: " + ex.Message.ToString());
            }


        }

        /// <summary>
        /// Closes the serial port so that changes can be made or communication
        /// ended.
        /// </summary>
        public void CloseSerialPort()
        {
            try
            {
                // Close the serial port
                SerialPort.Close();
            }
            catch (Exception ex)
            {
                if (SerialPort == null || SerialPort.IsOpen == false)
                {
                    UnityEngine.Debug.Log("Serial port already closed.");
                }
                else
                {
                    // Failed to close the serial port
                    UnityEngine.Debug.Log("Error 2B: " + ex.Message.ToString());
                }
            }

            UnityEngine.Debug.Log("Serial port closed!");

        }

        /// <summary>
        /// Look for available ports and return the first.
        /// </summary>
        /// <returns>The port name.</returns>
        private static string GetDefaultPort()
        {
            string[] portNames;

            switch (Application.platform)
            {

                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXDashboardPlayer:
                case RuntimePlatform.LinuxPlayer:

                    portNames = System.IO.Ports.SerialPort.GetPortNames();

                    if (portNames.Length == 0)
                    {
                        portNames = System.IO.Directory.GetFiles("/dev/");
                    }

                    foreach (string portName in portNames)
                    {
                        if (portName.StartsWith("/dev/tty.usb") || portName.StartsWith("/dev/ttyUSB"))
                            return portName;
                    }
                    return "";

                default: // Windows

                    portNames = System.IO.Ports.SerialPort.GetPortNames();

                    // Defaults to last port in list (most chance to be an Arduino port)
                    if (portNames.Length > 0)
                        return portNames[portNames.Length - 1];
                    else
                        return "";
            }
        }
        #endregion serial port helpers

    }
}