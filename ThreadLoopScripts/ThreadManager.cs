using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using thrThreadLoop;

public class ThreadManager : MonoBehaviour
{

    // Set to false for minimal print statements
    public bool debug = false;

    #region serial port settings
    public string comPort;
    public int baudRate = 115200;
    public int readTimeout = 10;
    public int writeTimeout = 1;
    #endregion serial port settings

    // Declare any threads to create here
    private SerialThreadLoop _ShapeDispSerialThread;

    // Data sending variables
    // Display automatic refresh rate
    public float refreshRate = 0.05f; //[s]

    // Enable sending data to hw at fixed rate
    private bool _AutoSending = false;
    private float _Counter;
    private float _StartTime;
    private bool _SetupSlaves = false;
    private bool _RefreshDisplay = false;    //true if there is pos data to send
    private bool _ResetDisplay = false;
    private bool _StopDisplay = false;
    private List<float> _Zmap = new List<float>(); //array to send

    // shape display object in unity
    private ShapeCast _ShapeRenderer;
    public GameObject renderingPlane;

    #region shape display msg commands
    // Message commands
    private const int DATA_CMD = 127;
    private const int ZERO_CMD = 126;
    private const int STOP_CMD = 125;
    private const int SETUP_CMD = 124;
    // slave setup commands
    private const int SET_KP = 253;
    private const int SET_KI = 247;
    private const int SET_KD = 246;
    private const int SET_MAXSPEED = 249;
    private const int SET_MINSPEED = 248;
    private const int DISABLE_PIN = 245;
    #endregion shape display msg commands

    public string SlaveParametersFolderName;

    // conversion factor
    private const float CONV_M_TO_MM = 1000; // [mm/m]

    #region Unity updates
    // Use this for initialization
    void Start()
    {

        // Get the shape display object
        _ShapeRenderer = renderingPlane.GetComponent<ShapeCast>();

        // Initially send slave setup data
        _SetupSlaves = true;

        // Initialize thread
        _ShapeDispSerialThread = new SerialThreadLoop(
            "shapeDisplayThread", true, System.Threading.ThreadPriority.Lowest, 5, 4,
            comPort, baudRate, readTimeout, writeTimeout);

        // Start the thread
        _ShapeDispSerialThread.Start();

        // Register for a notification of the SerialDataReceivedEvent
        //SerialThreadLoop.SerialReceivedDataEvent +=
        //    new SerialThreadLoop.SerialReceivedDataEventHandler(SerialReceivedEvent);
    }

    // Update is called once per frame
    void Update()
    {

        // check keystrokes and set boolean flags here
        // Press 'S' to send data to Teensy continuously
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Pressed S");
            _AutoSending = !_AutoSending;
            _Counter = refreshRate;
            _StartTime = Time.time;
            if (debug)
            {
                if (_AutoSending)
                {
                    Debug.Log("shape sending on");
                }
                else
                {
                    Debug.Log("shape sending off");
                }
            }
        }

        // Press 'A' to send data to Teensy manually
        else if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("Pressed A");
            // Refresh the display list data 
            _Zmap = _ShapeRenderer.GetZMap();
            // Set flag to send data
            _RefreshDisplay = true;
        }

        // Press 'R' to zero and reset the display
        else if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Pressed R");
            _ResetDisplay = true;
            _RefreshDisplay = false;
            _AutoSending = false;
            if (debug)
            {
                Debug.Log("Reset display");
            }
        }

        // Press 'X' to stop the display
        else if (Input.GetKeyDown(KeyCode.X))
        {
            _StopDisplay = true;
            _RefreshDisplay = false;
            _AutoSending = false;
        }

        // Press 'I' to resend slave parameters
        else if (Input.GetKeyDown(KeyCode.I))
        {
            _SetupSlaves = true;
            _RefreshDisplay = false;
            _AutoSending = false;
        }

        // Press 'P' to print the current map data 
        else if (Input.GetKeyDown(KeyCode.P))
        {
            // Refresh the display list data 
            _Zmap = _ShapeRenderer.GetZMap();
            printZmap();
        }

        // If auto mode enabled
        if (_AutoSending)
        {
            if ( (Time.time - _StartTime) >= refreshRate )
            {
                // Refresh the display list data 
                _Zmap = _ShapeRenderer.GetZMap();
                _RefreshDisplay = true;
                _StartTime = Time.time;
            }
        }

        SendDisplayData();


    }

    /// <summary>
    /// Clean up the thread and close the port on application close event.
    /// </summary>
    void OnApplicationQuit()
    {
        _ShapeDispSerialThread.CloseAtConvenience();
    }

    /// <summary>
    /// This function is called when the MonoBehaviour will be destroyed.
    /// OnDestroy will only be called on game objects that have previously
    /// been active.
    /// </summary>
    void OnDestroy()
    {
        _ShapeDispSerialThread.CloseAtConvenience();
        // Remove event notifiation registration
        //SerialThreadLoop.SerialReceivedDataEvent -= SerialReceivedEvent;
    }
    #endregion Unity updates

    #region Notification Events
    /// <summary>
    /// Data parsed serialport notification event
    /// </summary>
    /// <param name="Data">string</param>
    /// <param name="RawData">string[]</param>
    void SerialReceivedEvent(string[] Data, string RawData)
    {
        if (debug)
        {
            Debug.Log("Data recieved from port: " + RawData);
        }
    }
    #endregion Notification Events


    #region Shape display data parse functions
    /// <summary>
    /// Forwards the display data to the hardware
    /// and resets thread-safe flags. 
    /// </summary>
    private void SendDisplayData()
    {
        if (_ShapeDispSerialThread.IsOpenStream())
        {
            if (_RefreshDisplay)
            {
                RefreshShapeDisplay();
                _RefreshDisplay = false;
            }
            else if (_ResetDisplay)
            {
                ResetShapeDisplay();
                _ResetDisplay = false;
            }
            else if (_StopDisplay)
            {
                StopShapeDisplay();
                _StopDisplay = false;
            }
            else if (_SetupSlaves)
            {
                SetupSlaves();
                _SetupSlaves = false;
            }
        }
    }

    // Send slave initialization parameters
    private void SetupSlaves()
    {

        // Grab directory
        string currDir = Directory.GetCurrentDirectory();
        string path = currDir + "/Assets/Scripts/" + SlaveParametersFolderName;

        // For each file in "SlaveParameters" folder
        foreach (string file in Directory.GetFiles(path, "*.txt"))
        {
            using (StreamReader sr = new StreamReader(file))
            {
                // Read first line (SlaveID)
                string line = sr.ReadLine();
                while ((line == "" || line.StartsWith("#")) && sr.Peek() >= 0) line = sr.ReadLine(); // Handle comments or newlines
                if (sr.EndOfStream) continue;

                string[] separators = new string[] { ":", "," };
                string[] parsedID = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                int ID = int.Parse(parsedID[1].Trim());

                // Read command lines
                while (sr.Peek() >= 0)
                {
                    line = sr.ReadLine();
                    if (line == "" || line.StartsWith("#"))
                    {
                        // Handle comments or new lines
                        continue;
                    }

                    string[] parsedLine = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    int cmd = 0;
                    int pin, val;

                    // Parse command
                    if (parsedLine[0].ToLower() == "kp")
                    {
                        cmd = SET_KP;
                    }
                    else if (parsedLine[0].ToLower() == "ki")
                    {
                        cmd = SET_KI;
                    }
                    else if (parsedLine[0].ToLower() == "kd")
                    {
                        cmd = SET_KD;
                    }
                    else if (parsedLine[0].ToLower() == "maxspeed")
                    {
                        cmd = SET_MAXSPEED;
                    }
                    else if (parsedLine[0].ToLower() == "minspeed")
                    {
                        cmd = SET_MINSPEED;
                    }
                    else if (parsedLine[0].ToLower() == "disable")
                    {
                        cmd = DISABLE_PIN;
                    }
                    else
                    {
                        string err = "Error: Unknown command in Slave ID " + ID.ToString();
                        print(err);
                        continue;
                    }

                    pin = int.Parse(parsedLine[1].Trim());
                    val = int.Parse(parsedLine[2].Trim());

                    // Send message 
                    byte[] msg = { (byte)SETUP_CMD, (byte)ID, (byte)cmd, (byte)pin, (byte)val };
                    _ShapeDispSerialThread.EnqueueBytesToWrite(msg);
                }

            }
        }
    }

    // Send all the zMap data to the master shape display
    private void RefreshShapeDisplay()
    {

        byte[] msg = new byte[_Zmap.Count + 1];
        msg[0] = (byte)DATA_CMD;

        // Now send the rest of the data
        for (int i = 0, j = 1; i < (_Zmap.Count); i++, j++)
        {
            msg[j] = (byte)(_Zmap[i] * CONV_M_TO_MM);
        }

        _ShapeDispSerialThread.EnqueueBytesToWrite(msg);

    }

    // Send command to stop all motors and rendering
    private void ResetShapeDisplay()
    {
        byte[] msg = { (byte)ZERO_CMD };
        _ShapeDispSerialThread.EnqueueBytesToWrite(msg);
    }

    // Send command to stop all motors and rendering
    private void StopShapeDisplay()
    {
        byte[] msg = { (byte)STOP_CMD };
        _ShapeDispSerialThread.EnqueueBytesToWrite(msg);
    }
    #endregion Shape display data parse functions


    // Prints the current pin values, useful for debugging
    private void printZmap()
    {
        int countZeros = 0;
        for (int i = 0; i < (_Zmap.Count); i++)
        {
            // convert from m to mm
            int val = (int)(_Zmap[i] * CONV_M_TO_MM);
            print("[" + i + "]  " + val);
            if (val == 0)
            {
                countZeros++;
            }
        }
        Debug.Log("Real num of pins at zero: " + countZeros);
        Debug.Log("Real num of pins up: " + (_Zmap.Count - countZeros));
    }




}