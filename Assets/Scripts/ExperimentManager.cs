﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using RecData;


public class ExperimentManager : MonoBehaviour
{
    // *** User Interface *** //
    //public Text textStatus;

    #region externalObjects
    public GameObject NeedleMarker;                // This marker will be the one attached to the needle
    public GameObject HoloLensMarker;              // This marker will be the one attached to the HoloLens
    public GameObject PhantomMarker;               // This marker will be attached to the phantom
    public GameObject CalibrationBoxMarker;        // This marker will be attached to the calibration box
    public GameObject Needle;                      // This object represents the needle
    public GameObject NeedleRenderer;              // This object has ProcNeedle attached
    public GameObject Phantom;                     // This object is linked to the phantom
    public GameObject Target;                      // This object is linked to the target within the phantom
    public GameObject CalibrationBox;              // This object is linked to the calibration box
    #endregion

    // *** Relative Transform Vars *** //
    Quaternion qC_D;
    Vector3 rC_Co_Do;
    Quaternion qN1_N2;
    Vector3 rN1_No1_No2;

    // *** Experiment Vars/Const *** //
    #region experimentVars
    public int subjNum;
    private string userFolder;
    public int trialNo;
    private const int trialsPerCondition = 24;
	public int conditionNo;
	private ExpStates[] conditionOrder;
    private bool startRecording;
    public float recordPeriod;
    private float recordCounter;

    ExpStates currState;
    private bool lockPhantom;

    // *** Experiment Measured Stuff ** //
    private Vector3 phantomOffset;
    private float phantomBrimOffset;
    private float phantomSkinOffset;

    // States for the experiment state machine
    enum ExpStates
    {
        InitCalib,
        KeyboardFB,
        Straight,
        Shape,
        Projection,
        ProjectHit
    };
    #endregion

    #region record data vars
    // the recording objects
    private RecordData phantomMkrRecorder;
    private RecordData needleMkrRecorder;
    private RecordData hlMkrRecorder;
    private RecordData MkrsRecorder;
    private RecordData offsetRecorder;
    // num of columns for each file
    private int phantomMkrRecorderCols = 7;
    private int needleMkrRecorderCols = 7;
    private int hlMkrRecorderCols = 7;
    private int offsetRecorderCols = 3;
    #endregion

    // *** Misc Vars *** //
    #region miscVars
    float x_off;
    float y_off;
    float z_off;
    float step_off = 0.05f;
    private bool debounceCalib = false;
    //private bool tapDetected = false;
    //UnityEngine.XR.WSA.Input.GestureRecognizer recognizer;
    #endregion

    // Use this for initialization
    void Start()
    {

        // Calibration results
        qC_D = new Quaternion(0.058282339452415f, -0.020470290730742f, -0.000699217278049f, 0.998089999549415f);
        rC_Co_Do = new Vector3(-0.003902248347851f, 0.008639295309357f, 0.008261943033732f);

        qN1_N2 = new Quaternion(0.603773723212385f, 0.029050211798263f, 0.796173811526621f, 0.026844705099943f);
        rN1_No1_No2 = new Vector3(-0.064259547303643f, -0.377448931834412f, 0.346321017932784f);

        phantomOffset = new Vector3(-0.0165854f, -0.1127728f, -0.0012354f);

        // Data recording
        string baseFilePath = Directory.GetCurrentDirectory() + "/Assets/Data/";
        string userFolderName = "subject_" + subjNum;
        userFolder = baseFilePath + userFolderName;
        // Determine whether the directory exists.
        if (Directory.Exists(userFolder))
        {
            Debug.Log("WARNING! User folder exists already. Did you update subject number?");
        }
        else
        {
            Directory.CreateDirectory(userFolder);
        }

		// Initialize experiment parameters
		trialNo = 0;
		conditionNo = 0;
		// TODO: Change this to depend on the subject to randomize
		conditionOrder = new ExpStates[] { ExpStates.Straight, ExpStates.Shape, ExpStates.Projection};				

        currState = ExpStates.InitCalib;
        lockPhantom = false;
        startRecording = false;

        // Init all the data recording objects
        string filename;
        //filename = userFolder + "/needleMkr_Trial_" + trialNo;
        //needleMkrRecorder = new RecordData(filename, needleMkrRecorderCols);
        //filename = userFolder + "/hlMkr_Trial_" + trialNo;
        //hlMkrRecorder = new RecordData(filename, hlMkrRecorderCols);
        //filename = userFolder + "/phantomMkr_Trial_" + trialNo;
        //phantomMkrRecorder = new RecordData(filename, phantomMkrRecorderCols, 1);
		filename = userFolder + "/Mkrs_Trial_" + trialNo + "_Condition_" + conditionOrder[0];
        MkrsRecorder = new RecordData(filename, needleMkrRecorderCols + hlMkrRecorderCols + phantomMkrRecorderCols + 1);
        filename = userFolder + "/offset";
        offsetRecorder = new RecordData(filename, offsetRecorderCols, 1);

        CalibrationBox.SetActive(true);
        Phantom.SetActive(false);
        NeedleRenderer.SetActive(false);

        currState = ExpStates.InitCalib;
        lockPhantom = false;
        startRecording = false;

    }

    float startTime;
    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 relativePos;
        Quaternion relativeRot;
        Vector3 relativePhPos;
        Quaternion relativePhRot;
        switch (currState)
        {
            case ExpStates.InitCalib:
                // Set Needle inactive and set calibration cube active
                relativePos = HoloLensMarker.transform.InverseTransformVector(CalibrationBoxMarker.transform.position - HoloLensMarker.transform.position);
                relativeRot = Quaternion.Inverse(HoloLensMarker.transform.rotation) * CalibrationBoxMarker.transform.rotation;
                CalibrationBox.transform.localRotation = qC_D * relativeRot;
                CalibrationBox.transform.localPosition = rC_Co_Do + qC_D * relativePos;
                if (Input.GetKeyDown(KeyCode.KeypadEnter) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", 0.05f);
                    CalibrationBox.SetActive(false);
                    NeedleRenderer.SetActive(true);
                    Phantom.SetActive(true);
                    int targetPosIdx = targetOrder[0] - 1;
                    Target.transform.localPosition = phantomOffset + targetLoc[targetPosIdx];
                    NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateStraight();
                    Debug.Log("current state: straight");
                    currState = ExpStates.Straight;
                }
                break;

            case ExpStates.KeyboardFB:
                relativePos = HoloLensMarker.transform.InverseTransformVector(NeedleMarker.transform.position - HoloLensMarker.transform.position);
                relativeRot = Quaternion.Inverse(HoloLensMarker.transform.rotation) * NeedleMarker.transform.rotation;
                Needle.transform.localRotation = qC_D * relativeRot;
                Needle.transform.localPosition = rC_Co_Do + qC_D * relativePos;
                
                if (Input.GetKeyDown(KeyCode.KeypadPlus) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", 0.05f);
                    NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().upXOffset();
                    //rC_Co_Do += new Vector3(step_off, 0, 0);
                    //qC_D = qC_D * Quaternion.Euler(new Vector3(step_off, 0, 0));
                    //Debug.Log(qC_D);
                }
                else if (Input.GetKeyDown(KeyCode.KeypadMinus) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", 0.05f);
                    NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().dnXOffset();
                    //rC_Co_Do -= new Vector3(step_off, 0, 0);
                    //qC_D = qC_D * Quaternion.Euler(new Vector3(-step_off, 0, 0));
                    //Debug.Log(qC_D);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad6) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", 0.05f);
                    NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().dnZOffset();
                    //rC_Co_Do += new Vector3(0, step_off, 0);
                    //qC_D = qC_D * Quaternion.Euler(new Vector3(0, step_off, 0));
                    //Debug.Log(qC_D);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad4) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", 0.05f);
                    NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().upZOffset();
                    //rC_Co_Do -= new Vector3(0, step_off, 0);
                    //qC_D = qC_D * Quaternion.Euler(new Vector3(0, -step_off, 0));
                    //Debug.Log(qC_D);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad2) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", 0.05f);
                    NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().dnYOffset();
                    //rC_Co_Do += new Vector3(0, 0, step_off);
                    //qC_D = qC_D * Quaternion.Euler(new Vector3(0, 0, step_off));
                    //Debug.Log(qC_D);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad8) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", 0.05f);
                    NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().upYOffset();
                    //rC_Co_Do -= new Vector3(0, 0, step_off);
                    //qC_D = qC_D * Quaternion.Euler(new Vector3(0, 0, -step_off));
                    //Debug.Log(qC_D);
                }
                else if (Input.GetKeyDown(KeyCode.KeypadEnter) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", 0.05f);
                    Vector3 offsetVec = NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().offset;
                    offsetRecorder.addData(offsetVec.x.ToString(), offsetVec.y.ToString(), offsetVec.z.ToString());
                }

                break;

            case ExpStates.Projection:
            case ExpStates.Shape:
            case ExpStates.Straight:
            default:
                relativePos = HoloLensMarker.transform.InverseTransformVector(NeedleMarker.transform.position - HoloLensMarker.transform.position);
                relativeRot = Quaternion.Inverse(HoloLensMarker.transform.rotation) * NeedleMarker.transform.rotation;
                Needle.transform.localRotation = qC_D * relativeRot;
                Needle.transform.localPosition = rC_Co_Do + qC_D * relativePos;

                if (lockPhantom == false)
                {
                    relativePhPos = HoloLensMarker.transform.InverseTransformVector(PhantomMarker.transform.position - HoloLensMarker.transform.position);
                    relativePhRot = Quaternion.Inverse(HoloLensMarker.transform.rotation) * PhantomMarker.transform.rotation;
                    Phantom.transform.rotation = Camera.main.transform.rotation * qC_D * relativePhRot;
                    Phantom.transform.position = Camera.main.transform.position + Camera.main.transform.TransformVector(rC_Co_Do + qC_D * relativePhPos);
                }

                if (startRecording)
                {
                    recordCounter -= Time.deltaTime;
                    if (recordCounter < 0)
                    {
                        MkrsRecorder.addData(Time.realtimeSinceStartup.ToString(),
                                                PhantomMarker.transform.position.x.ToString(), PhantomMarker.transform.position.y.ToString(), PhantomMarker.transform.position.z.ToString(),
                                                PhantomMarker.transform.rotation.w.ToString(), PhantomMarker.transform.rotation.x.ToString(),
                                                PhantomMarker.transform.rotation.y.ToString(), PhantomMarker.transform.rotation.z.ToString(),
                                                NeedleMarker.transform.position.x.ToString(), NeedleMarker.transform.position.y.ToString(), NeedleMarker.transform.position.z.ToString(),
                                                NeedleMarker.transform.rotation.w.ToString(), NeedleMarker.transform.rotation.x.ToString(),
                                                NeedleMarker.transform.rotation.y.ToString(), NeedleMarker.transform.rotation.z.ToString(),
                                                HoloLensMarker.transform.position.x.ToString(), HoloLensMarker.transform.position.y.ToString(), HoloLensMarker.transform.position.z.ToString(),
                                                HoloLensMarker.transform.rotation.w.ToString(), HoloLensMarker.transform.rotation.x.ToString(),
                                                HoloLensMarker.transform.rotation.y.ToString(), HoloLensMarker.transform.rotation.z.ToString());
                        recordCounter = recordPeriod; // reset the counter
                    }
                }

                // If space pressed then start recording
                if (Input.GetKeyDown(KeyCode.Space) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", 0.05f);
                    startRecording ^= true; // toggle
                    if (startRecording)
                    {
                        // reset the counter
                        Debug.Log("started recording trial " + trialNo);
                        recordCounter = recordPeriod;
                        string filename;

                        filename = userFolder + "/Mkrs_Trial_" + trialNo + "_Condition_" + conditionOrder[conditionNo%3];
                        MkrsRecorder = new RecordData(filename, needleMkrRecorderCols + hlMkrRecorderCols + phantomMkrRecorderCols + 1);
                    }
                    else
                    {
                        MkrsRecorder.closeRecorder();
                    }
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow) && (debounceCalib == false))
                {
                    if (trialNo < 6 * trialsPerCondition - 1)
                    {
                        debounceCalib = true;
                        Invoke("recoverDebounce", 0.05f);
                        trialNo += 1;
                        int targetPosIdx = targetOrder[trialNo] - 1;
                        Target.transform.localPosition = phantomOffset + targetLoc[targetPosIdx];
                        if ((trialNo % trialsPerCondition) == 0)
                        {
                            conditionNo += 1;
                            ExpStates currCondition = conditionOrder[conditionNo%3];

                            if (currCondition == ExpStates.Straight)
                            {
                                currState = ExpStates.Straight;
                                NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateStraight();
                                Debug.Log("current state: straight");

                            }
                            else if (currCondition == ExpStates.Shape)
                            {
                                currState = ExpStates.Shape;
                                NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateShape();
                                Debug.Log("current state: shape");
                            }
                            else if (currCondition == ExpStates.Projection)
                            {
                                currState = ExpStates.Projection;
                                NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateProjection();
                                Debug.Log("current state: project");
                            }
                        }

                    }
                    else
                    {
                        Debug.Log("Experiment done");
                    }
                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow) && (debounceCalib == false))
                {
                    if (trialNo > 0)
                    {
                        debounceCalib = true;
                        Invoke("recoverDebounce", 0.05f);
                        trialNo -= 1;
                        int targetPosIdx = targetOrder[trialNo] - 1;
                        Target.transform.localPosition = phantomOffset + targetLoc[targetPosIdx];
                        if ((trialNo % trialsPerCondition) == 0)
                        {
                            conditionNo = (int) Math.Floor((double)(trialNo/trialsPerCondition));
                            ExpStates currCondition = conditionOrder[conditionNo];

                            if (currCondition == ExpStates.Straight)
                            {
                                currState = ExpStates.Straight;
                                NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateStraight();
                                Debug.Log("current state: straight");

                            }
                            else if (currCondition == ExpStates.Shape)
                            {
                                currState = ExpStates.Shape;
                                NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateShape();
                                Debug.Log("current state: shape");
                            }
                            else if (currCondition == ExpStates.Projection)
                            {
                                currState = ExpStates.Projection;
                                NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateProjection();
                                Debug.Log("current state: project");
                            }
                        }

                    }
                }
                break;
        }


        /*** Mode changing and other key listeners ***/
        if (Input.GetKeyDown( KeyCode.T ) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.2f);
            
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.05f);
            currState = ExpStates.KeyboardFB;
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateTip();
            Debug.Log("current state: keyboard feedback");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.05f);
            currState = ExpStates.Straight;
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateStraight();
            Debug.Log("current state: straight");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.05f);
            currState = ExpStates.Shape;
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateShape();
            Debug.Log("current state: shape");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.05f);
            currState = ExpStates.Projection;
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateProjection();
            Debug.Log("current state: project");
        }
        else if (Input.GetKeyDown(KeyCode.L) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.05f);
            lockPhantom ^= true;
        }
    }

    private static int[] targetOrder = new int[]
    {
        24, 36, 18, 

        7, 17, 33, 39
        10, 22, 36, 20,   2, 24, 6, 30, 28, 14, 8, 12, 16, 4, 32, 26, 18, 34, 1, 5, 35, 25, 21, 17,

        13, 3, 7, 27, 11, 19, 23, 31, 29, 15, 33, 9,
        46, 58, 72, 56, 38, 60, 42, 66, 64, 50, 44, 48, 52, 40, 68, 62, 54, 70,
        37, 41, 71, 61, 57, 53, 49, 39, 33, 63, 47, 55, 59, 67, 65, 51, 69, 45
        
    };
    //private static int[] targetOrder = new int[]
    //{
    //    1, 5, 17, 13, 3, 7, 11, 15, 9, 10, 2, 6, 14, 8, 12, 16, 4, 0
    //};

    private static Vector3[] targetLoc = new Vector3[] {
    new Vector3( 0.1033f, 0.06223f, -0.0077f ),
    new Vector3( 0.1033f, 0.06223f, phantomSkinOffset-0.03f ),
    new Vector3( 0.1033f, 0.037338f, -0.0077f ),
    new Vector3( 0.1033f, 0.037338f, -0.0477f ),
    new Vector3( 0.1033f, 0.012446f, -0.0077f ),
    new Vector3( 0.1033f, 0.012446f, -0.0477f ),
    new Vector3( 0.062f, 0.06223f, -0.0077f ),
    new Vector3( 0.062f, 0.06223f, -0.0477f ),
    new Vector3( 0.062f, 0.037338f, -0.0077f ),
    new Vector3( 0.062f, 0.037338f, -0.0477f ),
    new Vector3( 0.062f, 0.012446f, -0.0077f ),
    new Vector3( 0.062f, 0.012446f, -0.0477f ),
    new Vector3( 0.020666f, 0.06223f, -0.0077f ),
    new Vector3( 0.020666f, 0.06223f, -0.0477f ),
    new Vector3( 0.020666f, 0.037338f, -0.0077f ),
    new Vector3( 0.020666f, 0.037338f, -0.0477f ),
    new Vector3( 0.020666f, 0.012446f, -0.0077f ),
    new Vector3( 0.020666f, 0.012446f, -0.0477f ),
    new Vector3( -0.1033f, 0.06223f, -0.0077f ),
    new Vector3( -0.1033f, 0.06223f, -0.0477f ),
    new Vector3( -0.1033f, 0.037338f, -0.0077f ),
    new Vector3( -0.1033f, 0.037338f, -0.0477f ),
    new Vector3( -0.1033f, 0.012446f, -0.0077f ),
    new Vector3( -0.1033f, 0.012446f, -0.0477f ),
    new Vector3( -0.062f, 0.06223f, -0.0077f ),
    new Vector3( -0.062f, 0.06223f, -0.0477f ),
    new Vector3( -0.062f, 0.037338f, -0.0077f ),
    new Vector3( -0.062f, 0.037338f, -0.0477f ),
    new Vector3( -0.062f, 0.012446f, -0.0077f ),
    new Vector3( -0.062f, 0.012446f, -0.0477f ),
    new Vector3( -0.020666f, 0.06223f, -0.0077f ),
    new Vector3( -0.020666f, 0.06223f, -0.0477f ),
    new Vector3( -0.020666f, 0.037338f, -0.0077f ),
    new Vector3( -0.020666f, 0.037338f, -0.0477f ),
    new Vector3( -0.020666f, 0.012446f, -0.0077f ),
    new Vector3( -0.020666f, 0.012446f, -0.0477f ),
    new Vector3( 0.1033f, -0.06223f, -0.0077f ),
    new Vector3( 0.1033f, -0.06223f, -0.0477f ),
    new Vector3( 0.1033f, -0.037338f, -0.0077f ),
    new Vector3( 0.1033f, -0.037338f, -0.0477f ),
    new Vector3( 0.1033f, -0.012446f, -0.0077f ),
    new Vector3( 0.1033f, -0.012446f, -0.0477f ),
    new Vector3( 0.062f, -0.06223f, -0.0077f ),
    new Vector3( 0.062f, -0.06223f, -0.0477f ),
    new Vector3( 0.062f, -0.037338f, -0.0077f ),
    new Vector3( 0.062f, -0.037338f, -0.0477f ),
    new Vector3( 0.062f, -0.012446f, -0.0077f ),
    new Vector3( 0.062f, -0.012446f, -0.0477f ),
    new Vector3( 0.020666f, -0.06223f, -0.0077f ),
    new Vector3( 0.020666f, -0.06223f, -0.0477f ),
    new Vector3( 0.020666f, -0.037338f, -0.0077f ),
    new Vector3( 0.020666f, -0.037338f, -0.0477f ),
    new Vector3( 0.020666f, -0.012446f, -0.0077f ),
    new Vector3( 0.020666f, -0.012446f, -0.0477f ),
    new Vector3( -0.1033f, -0.06223f, -0.0077f ),
    new Vector3( -0.1033f, -0.06223f, -0.0477f ),
    new Vector3( -0.1033f, -0.037338f, -0.0077f ),
    new Vector3( -0.1033f, -0.037338f, -0.0477f ),
    new Vector3( -0.1033f, -0.012446f, -0.0077f ),
    new Vector3( -0.1033f, -0.012446f, -0.0477f ),
    new Vector3( -0.062f, -0.06223f, -0.0077f ),
    new Vector3( -0.062f, -0.06223f, -0.0477f ),
    new Vector3( -0.062f, -0.037338f, -0.0077f ),
    new Vector3( -0.062f, -0.037338f, -0.0477f ),
    new Vector3( -0.062f, -0.012446f, -0.0077f ),
    new Vector3( -0.062f, -0.012446f, -0.0477f ),
    new Vector3( -0.020666f, -0.06223f, -0.0077f ),
    new Vector3( -0.020666f, -0.06223f, -0.0477f ),
    new Vector3( -0.020666f, -0.037338f, -0.0077f ),
    new Vector3( -0.020666f, -0.037338f, -0.0477f ),
    new Vector3( -0.020666f, -0.012446f, -0.0077f ),
    new Vector3( -0.020666f, -0.012446f, -0.0477f ),
    };

    /// <summary>
    /// Clean up the thread and close the port on application close event.
    /// </summary>
    void OnApplicationQuit()
    {
        Debug.Log("Closing all recording objects");
        MkrsRecorder.closeRecorder();
        offsetRecorder.closeRecorder();
    }

    /// <summary>
    /// This function is called when the MonoBehaviour will be destroyed.
    /// OnDestroy will only be called on game objects that have previously
    /// been active.
    /// </summary>
    void OnDestroy()
    {
        Debug.Log("Closing all recording objects");
        MkrsRecorder.closeRecorder();
        offsetRecorder.closeRecorder();
    }

    /// <summary>
    /// Recover from debouncing state for space bar
    /// </summary>
    private void recoverDebounce()
    {
        debounceCalib = false;
    }


    static void printMatrix(double[,] M)
    {
        int height = M.GetLength( 0 );
        int width = M.GetLength( 1 );

        string ToPrint = "";
        for (int i = 0; i < height; i ++)
        {
            for (int j = 0; j < width; j++)
            {
                ToPrint += M[i, j].ToString();
                ToPrint += "\t";
            }
            ToPrint += "\n";
        }
        Debug.Log( ToPrint );
    }

    static void printMatrix( double[] arr )
    {
        int len = arr.GetLength(0);
        string ToPrint = "";
        ToPrint += "[";
        for (int i = 0; i < len; i++)
        {
            ToPrint += arr[i].ToString();
            ToPrint += "\t";
        }
        ToPrint += "]";
        Debug.Log( ToPrint );
    }
}
