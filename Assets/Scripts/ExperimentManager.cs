using System;
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
    private bool needleUpright = true;

    // Practice variables
    public int practiceTrialNo;
    private const int practiceTrialsPerCondition = 4;
    private int practiceConditionNo;

    private bool startRecording;
    public float recordPeriod;
    private float recordCounter;

    ExpStates currState;
    private bool lockPhantom;

    // *** Experiment Measured Stuff ** //
    private static Vector3 phantomOffset;
    private static float phantomBrimOffset = 0.000764584f + 0.050f;
    public float measuredPhantomSkinOffset;
    private static float phantomSkinOffset;

    // States for the experiment state machine
    enum ExpStates
    {
        InitEnterSubjectInfo,
        InitCalib,
        WaitingForKeyboardFB,
        KeyboardFB,
        WaitingForPractice,
        PracticeStraight,
        PracticeShape,
        PracticeProjection,
        WaitingForExp,
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
    private RecordData PracticeMkrsRecorder;
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
    private static bool debounceCalib = false;
    private static float debounceTime = 0.04f;
    public Vector3 currTargetPos;
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

        // Initialize experiment parameters
		trialNo = 0;
		conditionNo = 0;

        CalibrationBox.SetActive(false);
        Phantom.SetActive(false);
        NeedleRenderer.SetActive(false);

        currState = ExpStates.InitEnterSubjectInfo;
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
            case ExpStates.WaitingForKeyboardFB:
                if (Input.GetKeyDown(KeyCode.RightArrow) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    CalibrationBox.SetActive(false);
                    Phantom.SetActive(false);
                    NeedleRenderer.SetActive(true);
                    currState = ExpStates.KeyboardFB;
                    NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateTip();
                    Debug.Log("current state: keyboard FB");

                    // start recorder
                    string filename = "/offset";
                    string pathname = userFolder + "/offset";
                    offsetRecorder = new RecordData(pathname, filename, offsetRecorderCols, 1);
                }
                break;
            case ExpStates.KeyboardFB:
                relativePos = HoloLensMarker.transform.InverseTransformVector(NeedleMarker.transform.position - HoloLensMarker.transform.position);
                relativeRot = Quaternion.Inverse(HoloLensMarker.transform.rotation) * NeedleMarker.transform.rotation;
                Needle.transform.localRotation = qC_D * relativeRot;
                Needle.transform.localPosition = rC_Co_Do + qC_D * relativePos;

                if (Input.GetKeyDown(KeyCode.Keypad7) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    if (needleUpright)
                    {
                        NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().upXOffset();
                    } else
                    {
                        NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().dnYOffset();
                    }
                    //rC_Co_Do += new Vector3(step_off, 0, 0);
                    //qC_D = qC_D * Quaternion.Euler(new Vector3(step_off, 0, 0));
                    //Debug.Log(qC_D);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad9) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    if (needleUpright)
                    {
                        NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().dnXOffset();
                    } else
                    {
                        NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().upYOffset();
                    }
                    //rC_Co_Do -= new Vector3(step_off, 0, 0);
                    //qC_D = qC_D * Quaternion.Euler(new Vector3(-step_off, 0, 0));
                    //Debug.Log(qC_D);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad6) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().dnZOffset();
                    //rC_Co_Do += new Vector3(0, step_off, 0);
                    //qC_D = qC_D * Quaternion.Euler(new Vector3(0, step_off, 0));
                    //Debug.Log(qC_D);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad4) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().upZOffset();
                    
                    //rC_Co_Do -= new Vector3(0, step_off, 0);
                    //qC_D = qC_D * Quaternion.Euler(new Vector3(0, -step_off, 0));
                    //Debug.Log(qC_D);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad2) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    if (needleUpright)
                    {
                        NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().dnYOffset();
                    } else
                    {
                        NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().dnXOffset();
                    }
                    //rC_Co_Do += new Vector3(0, 0, step_off);
                    //qC_D = qC_D * Quaternion.Euler(new Vector3(0, 0, step_off));
                    //Debug.Log(qC_D);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad8) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    if (needleUpright)
                    {
                        NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().upYOffset();
                    } else
                    {
                        NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().upXOffset();
                    }
                    //rC_Co_Do -= new Vector3(0, 0, step_off);
                    //qC_D = qC_D * Quaternion.Euler(new Vector3(0, 0, -step_off));
                    //Debug.Log(qC_D);
                }
                else if (Input.GetKeyDown(KeyCode.KeypadEnter) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    Vector3 offsetVec = NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().offset;
                    offsetRecorder.addData(offsetVec.x.ToString(), offsetVec.y.ToString(), offsetVec.z.ToString());
                    Debug.Log("Recorded: " + offsetVec.x.ToString() + ", " + offsetVec.y.ToString() + ", " + offsetVec.z.ToString());
                }
                else if (Input.GetKeyDown(KeyCode.Keypad0) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().resetOffset();
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    needleUpright ^= true;
                    if (needleUpright)
                    {
                        Debug.Log("needle upright");
                    } else
                    {
                        Debug.Log("needle sideways");
                    }
                }

                break;

            case ExpStates.InitEnterSubjectInfo:
                if (Input.GetKeyDown(KeyCode.KeypadEnter) && (debounceCalib == false))
                {
                    if (subjNum == 0)
                    {
                        Debug.Log("WARNING: did you forget to update subject number?");
                    } else
                    {
                        conditionOrder = allConditionOrders[subjNum % 6];
                    }
                    if (measuredPhantomSkinOffset == 0)
                    {
                        Debug.Log("WARNING: did you forget to measure the skin offset?");
                    }
                    else
                    {
                        phantomSkinOffset = phantomBrimOffset - (measuredPhantomSkinOffset/1000); // Set the Z of the phantom skin offset
                        Debug.Log("Phantom skin offset " + phantomSkinOffset);
                    }
                    
                    if (subjNum != 0 && measuredPhantomSkinOffset != 0)
                    {

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
                        
                        CalibrationBox.SetActive(true);
                        Phantom.SetActive(false);
                        NeedleRenderer.SetActive(false);
                        currState = ExpStates.InitCalib;
                    }
                   
                }
                   
                break;
            case ExpStates.InitCalib:
                // Set Needle inactive and set calibration cube active
                relativePos = HoloLensMarker.transform.InverseTransformVector(CalibrationBoxMarker.transform.position - HoloLensMarker.transform.position);
                relativeRot = Quaternion.Inverse(HoloLensMarker.transform.rotation) * CalibrationBoxMarker.transform.rotation;
                CalibrationBox.transform.localRotation = qC_D * relativeRot;
                CalibrationBox.transform.localPosition = rC_Co_Do + qC_D * relativePos;
                if (Input.GetKeyDown(KeyCode.RightArrow) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    Debug.Log("current state: waiting to start practice");
                    currState = ExpStates.WaitingForPractice;
                    
                } 
                else if (Input.GetKeyDown(KeyCode.UpArrow) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    Debug.Log("current state: waiting to start experiment");
                    currState = ExpStates.WaitingForExp;

                }
                else if (Input.GetKeyDown(KeyCode.DownArrow) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    Debug.Log("current state: waiting to start keyboard FB");
                    currState = ExpStates.WaitingForKeyboardFB;

                }
                break;
            case ExpStates.WaitingForPractice:
                if (Input.GetKeyDown(KeyCode.RightArrow) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    CalibrationBox.SetActive(false);
                    NeedleRenderer.SetActive(true);
                    Phantom.SetActive(true);

                    int targetPosIdx = practiceTargetOrder[practiceTrialNo];
                    Target.transform.localPosition = phantomOffset + targetLoc[targetPosIdx] + new Vector3(0, 0, 0.03f);

                    currTargetPos = targetLoc[targetPosIdx];

                    practiceConditionNo = 0;
                    currState = ExpStates.PracticeStraight;
                    NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateStraight();
                    Debug.Log("current state: practice straight");

                    startRecording = true;
                    Debug.Log("started recording practice trial " + practiceTrialNo);
                    recordCounter = recordPeriod;
                    string filename = "/Practice_Trial_" + practiceTrialNo + "_Condition_" + allConditionOrders[0][practiceConditionNo % 3];
                    string pathname;
                    pathname = userFolder + filename;
                    PracticeMkrsRecorder = new RecordData(pathname, filename, needleMkrRecorderCols + hlMkrRecorderCols + phantomMkrRecorderCols + 1);
                }
                break;

            case ExpStates.PracticeProjection:
            case ExpStates.PracticeShape:
            case ExpStates.PracticeStraight:
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
                        PracticeMkrsRecorder.addData(Time.realtimeSinceStartup.ToString(),
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

                if (Input.GetKeyDown(KeyCode.RightArrow) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);

                    startRecording = false;
					if (!PracticeMkrsRecorder.isClosed()) PracticeMkrsRecorder.closeRecorder();

                    if (practiceTrialNo < 3*practiceTrialsPerCondition - 1 )
                    {
                        practiceTrialNo += 1;
                        int targetPosIdx = practiceTargetOrder[practiceTrialNo];
                        Target.transform.localPosition = phantomOffset + targetLoc[targetPosIdx] + new Vector3(0, 0, 0.03f);
                        currTargetPos = targetLoc[targetPosIdx];

                        practiceConditionNo = (int)Math.Floor((double)(practiceTrialNo / practiceTrialsPerCondition));
                            
                        if (practiceConditionNo == 0)
                        {
                            currState = ExpStates.PracticeStraight;
                            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateStraight();
                            Debug.Log("current state: practice straight");

                        }
                        else if (practiceConditionNo == 1)
                        {
                            currState = ExpStates.PracticeShape;
                            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateShape();
                            Debug.Log("current state: practice shape");
                        }
                        else if (practiceConditionNo == 2)
                        {
                            currState = ExpStates.PracticeProjection;
                            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateProjection();
                            Debug.Log("current state: practice project");
                        }

						startRecording = true;
                        Debug.Log("started recording practice trial " + practiceTrialNo);
                        recordCounter = recordPeriod;
                        string filename = "/Practice_Trial_" + practiceTrialNo + "_Condition_" + allConditionOrders[0][practiceConditionNo % 3];
                        string pathname;
                        pathname = userFolder + filename;
                        PracticeMkrsRecorder = new RecordData(pathname, filename, needleMkrRecorderCols + hlMkrRecorderCols + phantomMkrRecorderCols + 1);

                    }
                    else
                    {
                        Debug.Log("Practice done. Waiting to start experiment.");
                        currState = ExpStates.WaitingForExp;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);

                    startRecording = false;
				    if (!PracticeMkrsRecorder.isClosed()) PracticeMkrsRecorder.closeRecorder();

                    if (practiceTrialNo > 0)
                    {
                        practiceTrialNo -= 1;
                        int targetPosIdx = practiceTargetOrder[practiceTrialNo];
                        Target.transform.localPosition = phantomOffset + targetLoc[targetPosIdx] + new Vector3(0, 0, 0.03f);
                        currTargetPos = targetLoc[targetPosIdx];

                        practiceConditionNo = (int) Math.Floor((double)(practiceTrialNo / practiceTrialsPerCondition));

                        if (practiceConditionNo == 0)
                        {
                            currState = ExpStates.PracticeStraight;
                            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateStraight();
                            Debug.Log("current state: practice straight");

                        }
                        else if (practiceConditionNo == 1)
                        {
                            currState = ExpStates.PracticeShape;
                            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateShape();
                            Debug.Log("current state: practice shape");
                        }
                        else if (practiceConditionNo == 2)
                        {
                            currState = ExpStates.PracticeProjection;
                            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateProjection();
                            Debug.Log("current state: practice project");
                        }

						startRecording = true;
						Debug.Log("started recording practice trial " + practiceTrialNo);
						recordCounter = recordPeriod;
                        string filename = "/Practice_Trial_" + practiceTrialNo + "_Condition_" + allConditionOrders[0][practiceConditionNo % 3];
                        string pathname;
                        pathname = userFolder + filename;
						PracticeMkrsRecorder = new RecordData(pathname, filename, needleMkrRecorderCols + hlMkrRecorderCols + phantomMkrRecorderCols + 1);

                    }
                }
                break;
            case ExpStates.WaitingForExp:
                if (Input.GetKeyDown(KeyCode.RightArrow) && (debounceCalib == false))
                {
                    debounceCalib = true;
                    Invoke("recoverDebounce", debounceTime);
                    CalibrationBox.SetActive(false);
                    NeedleRenderer.SetActive(true);
                    Phantom.SetActive(true);

                    int targetPosIdx = targetOrder[trialNo];
                    Target.transform.localPosition = phantomOffset + targetLoc[targetPosIdx] + new Vector3(0, 0, phantomSkinOffset);

                    currTargetPos = targetLoc[targetPosIdx];

                    conditionNo = (int)Math.Floor((double)(trialNo / trialsPerCondition));
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

                    // start the recording stuff
                    startRecording = true;
                    Debug.Log("started recording trial " + trialNo);
                    recordCounter = recordPeriod;
                    string filename = "/Exp_Trial_" + trialNo + "_Condition_" + conditionOrder[conditionNo % 3];
                    string pathname;
                    pathname = userFolder + filename;
                    MkrsRecorder = new RecordData(pathname, filename, needleMkrRecorderCols + hlMkrRecorderCols + phantomMkrRecorderCols + 1);
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

                #region old stuff
                // If space pressed then start recording
                //if (Input.GetKeyDown(KeyCode.Space) && (debounceCalib == false))
                //{
                //    debounceCalib = true;
                //    Invoke("recoverDebounce", debounceTime);
                //    startRecording ^= true; // toggle
                //    if (startRecording)
                //    {
                //        // reset the counter
                //        Debug.Log("started recording trial " + trialNo);
                //        recordCounter = recordPeriod;

                //        string filename = "/Mkrs_Trial_" + trialNo + "_Condition_" + conditionOrder[conditionNo % 3];
                //        string pathname;
                //        pathname = userFolder + "/Mkrs_Trial_" + trialNo + "_Condition_" + conditionOrder[conditionNo%3];
                //        MkrsRecorder = new RecordData(pathname, filename, needleMkrRecorderCols + hlMkrRecorderCols + phantomMkrRecorderCols + 1);
                //    }
                //    else
                //    {
                //        MkrsRecorder.closeRecorder();
                //    }
                //}
#endregion

                if (Input.GetKeyDown(KeyCode.RightArrow) && (debounceCalib == false))
                {
                    startRecording = false;
                    if (!MkrsRecorder.isClosed()) MkrsRecorder.closeRecorder();

                    if (trialNo < 3 * trialsPerCondition - 1)
                    {
                        debounceCalib = true;
                        Invoke("recoverDebounce", debounceTime);

                        trialNo += 1;
                        if (trialNo % trialsPerCondition == 0)
                        {
                            Debug.Log("Take a break. Waiting to continue.");
                            currState = ExpStates.InitCalib;
                            break;
                        }
                        int targetPosIdx = targetOrder[trialNo];
                        Target.transform.localPosition = phantomOffset + targetLoc[targetPosIdx] + new Vector3(0, 0, phantomSkinOffset);
                        currTargetPos = targetLoc[targetPosIdx];

                        conditionNo = (int)Math.Floor((double)(trialNo / trialsPerCondition));
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
                        
                        startRecording = true;
                        Debug.Log("started recording trial " + trialNo);
                        recordCounter = recordPeriod;
                        string filename = "/Exp_Trial_" + trialNo + "_Condition_" + conditionOrder[conditionNo % 3];
                        string pathname;
                        pathname = userFolder + filename;
                        MkrsRecorder = new RecordData(pathname, filename, needleMkrRecorderCols + hlMkrRecorderCols + phantomMkrRecorderCols + 1);
                    }
                    else
                    {
                        Debug.Log("Experiment done. Waiting for keyboard feedback experiment.");
                        currState = ExpStates.WaitingForKeyboardFB;
                    }
                }
                else if (Input.GetKeyDown(KeyCode.LeftArrow) && (debounceCalib == false))
                {
                    startRecording = false;
                    if (!MkrsRecorder.isClosed()) MkrsRecorder.closeRecorder();

                    if (trialNo > 0)
                    {
                        debounceCalib = true;
                        Invoke("recoverDebounce", debounceTime);
                        trialNo -= 1;
                        int targetPosIdx = targetOrder[trialNo];
                        Target.transform.localPosition = phantomOffset + targetLoc[targetPosIdx] + new Vector3(0, 0, phantomSkinOffset);
                        currTargetPos = targetLoc[targetPosIdx];

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

                        startRecording = true;
                        Debug.Log("started recording trial " + trialNo);
                        recordCounter = recordPeriod;
                        string filename = "/Exp_Trial_" + trialNo + "_Condition_" + conditionOrder[conditionNo % 3];
                        string pathname;
                        pathname = userFolder + filename;
                        MkrsRecorder = new RecordData(pathname, filename, needleMkrRecorderCols + hlMkrRecorderCols + phantomMkrRecorderCols + 1);
                    }
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow) && (debounceCalib == false))
                {
                    startRecording = false;
                    if (!MkrsRecorder.isClosed()) MkrsRecorder.closeRecorder();

                    if (trialNo > 0 && trialNo < 3 * trialsPerCondition - 1)
                    {
                        debounceCalib = true;
                        Invoke("recoverDebounce", debounceTime);
 
                        int targetPosIdx = targetOrder[trialNo];
                        Target.transform.localPosition = phantomOffset + targetLoc[targetPosIdx] + new Vector3(0, 0, phantomSkinOffset);
                        currTargetPos = targetLoc[targetPosIdx];

                        conditionNo = (int)Math.Floor((double)(trialNo / trialsPerCondition));
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

                        startRecording = true;
                        Debug.Log("started recording trial " + trialNo);
                        recordCounter = recordPeriod;
                        string filename = "/Exp_Trial_" + trialNo + "_Condition_" + conditionOrder[conditionNo % 3];
                        string pathname;
                        pathname = userFolder + filename;
                        MkrsRecorder = new RecordData(pathname, filename, needleMkrRecorderCols + hlMkrRecorderCols + phantomMkrRecorderCols + 1);
                    }
                }
                break;
        }


        /*** Mode changing and other key listeners ***/
        if (Input.GetKeyDown(KeyCode.Alpha0) && (currState != ExpStates.InitEnterSubjectInfo) && (debounceCalib == false)) // Go straight to Calibration with black box
        {
            debounceCalib = true;
            Invoke("recoverDebounce", debounceTime);
            currState = ExpStates.InitCalib;
            CalibrationBox.SetActive(true);
            Phantom.SetActive(false);
            NeedleRenderer.SetActive(false);
            Debug.Log("current state: init calib");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1) && (currState != ExpStates.InitEnterSubjectInfo) && (debounceCalib == false)) // Go straight to experiment
        {
            debounceCalib = true;
            Invoke("recoverDebounce", debounceTime);
            CalibrationBox.SetActive(false);
            Phantom.SetActive(true);
            NeedleRenderer.SetActive(true);

            int targetPosIdx = targetOrder[trialNo];
            Target.transform.localPosition = phantomOffset + targetLoc[targetPosIdx] + new Vector3(0, 0, phantomSkinOffset);
            currTargetPos = targetLoc[targetPosIdx];

            conditionNo = (int)Math.Floor((double)(trialNo / trialsPerCondition));
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
        else if (Input.GetKeyDown(KeyCode.Alpha2) && (currState != ExpStates.InitEnterSubjectInfo) && (debounceCalib == false)) // Go straight to Keyboard thing
        {
            debounceCalib = true;
            Invoke("recoverDebounce", debounceTime);
            CalibrationBox.SetActive(false);
            Phantom.SetActive(false);
            NeedleRenderer.SetActive(true);
            currState = ExpStates.KeyboardFB;
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateTip();
            Debug.Log("current state: keyboard FB");
        }
        // ******************//
        else if (Input.GetKeyDown(KeyCode.Alpha6) && (currState != ExpStates.InitEnterSubjectInfo) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", debounceTime);
            currState = ExpStates.Straight;
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateStraight();
            Debug.Log("current state: straight");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7) && (currState != ExpStates.InitEnterSubjectInfo) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", debounceTime);
            currState = ExpStates.Shape;
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateShape();
            Debug.Log("current state: shape");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8) && (currState != ExpStates.InitEnterSubjectInfo) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", debounceTime);
            currState = ExpStates.Projection;
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateProjection();
            Debug.Log("current state: project");
        }
        else if (Input.GetKeyDown(KeyCode.L) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", debounceTime);
            lockPhantom ^= true;
        }
    }

    private static int[] practiceTargetOrder = new int[]
    {
        0, 20, 31, 27,   // for condition 1
	    10, 4, 15, 25,   // for condition 2
    	24, 16, 9, 13,   // for conditino 3
    };

    private static int[] targetOrder = new int[]
    {
		  58, 60, 52, 0, 46, 20, 28, 56, 34, 8, 2, 68, 31, 27, 21, 71, 17, 65, 49, 69, 19, 1, 55, 67,   // for condition 1
	    64, 10, 50, 54, 4, 22, 44, 70, 66, 62, 38, 26, 15, 25, 35, 47, 63, 45, 57, 11, 33, 61, 7, 53, // for condition 2
    	42, 24, 16, 18, 32, 12, 36, 6, 30, 48, 14, 40, 39, 59, 9, 37, 29, 43, 13, 5, 51, 23, 41, 3    // for conditino 3
    };
    //private static int[] targetOrder = new int[]
    //{
    //    1, 5, 17, 13, 3, 7, 11, 15, 9, 10, 2, 6, 14, 8, 12, 16, 4, 0
    //};

    private static Vector3[] targetLoc = new Vector3[] {
    new Vector3( 0.1033f, 0.06523f, -0.03f ),
    new Vector3( 0.0933f, 0.06023f, -0.07f ),
    new Vector3( 0.1033f, 0.037338f, -0.03f ),
    new Vector3( 0.0933f, 0.037338f, -0.07f ),
    new Vector3( 0.1033f, 0.012446f, -0.03f ),
    new Vector3( 0.0933f, 0.012446f, -0.07f ),
    new Vector3( 0.062f, 0.06523f, -0.03f ),
    new Vector3( 0.052f, 0.06023f, -0.07f ),
    new Vector3( 0.062f, 0.037338f, -0.03f ),
    new Vector3( 0.052f, 0.037338f, -0.07f ),
    new Vector3( 0.062f, 0.012446f, -0.03f ),
    new Vector3( 0.052f, 0.017446f, -0.07f ),
    new Vector3( 0.020666f, 0.06523f, -0.03f ),
    new Vector3( 0.010666f, 0.06023f, -0.07f ),
    new Vector3( 0.020666f, 0.037338f, -0.03f ),
    new Vector3( 0.010666f, 0.037338f, -0.07f ),
    new Vector3( 0.020666f, 0.012446f, -0.03f ),
    new Vector3( 0.010666f, 0.017446f, -0.07f ),
    new Vector3( -0.1033f, 0.06523f, -0.03f ),
    new Vector3( -0.0933f, 0.06023f, -0.07f ),
    new Vector3( -0.1033f, 0.037338f, -0.03f ),
    new Vector3( -0.0933f, 0.037338f, -0.07f ),
    new Vector3( -0.1033f, 0.012446f, -0.03f ),
    new Vector3( -0.0933f, 0.012446f, -0.07f ),
    new Vector3( -0.062f, 0.06523f, -0.03f ),
    new Vector3( -0.052f, 0.06023f, -0.07f ),
    new Vector3( -0.062f, 0.037338f, -0.03f ),
    new Vector3( -0.052f, 0.037338f, -0.07f ),
    new Vector3( -0.062f, 0.012446f, -0.03f ),
    new Vector3( -0.052f, 0.017446f, -0.07f ),
    new Vector3( -0.020666f, 0.06523f, -0.03f ),
    new Vector3( -0.010666f, 0.06023f, -0.07f ),
    new Vector3( -0.020666f, 0.037338f, -0.03f ),
    new Vector3( -0.010666f, 0.037338f, -0.07f ),
    new Vector3( -0.020666f, 0.012446f, -0.03f ),
    new Vector3( -0.010666f, 0.017446f, -0.07f ),
    new Vector3( 0.1033f, -0.06523f, -0.03f ),
    new Vector3( 0.0933f, -0.06023f, -0.07f ),
    new Vector3( 0.1033f, -0.037338f, -0.03f ),
    new Vector3( 0.0933f, -0.037338f, -0.07f ),
    new Vector3( 0.1033f, -0.012446f, -0.03f ),
    new Vector3( 0.0933f, -0.012446f, -0.07f ),
    new Vector3( 0.062f, -0.06523f, -0.03f ),
    new Vector3( 0.052f, -0.06023f, -0.07f ),
    new Vector3( 0.062f, -0.037338f, -0.03f ),
    new Vector3( 0.052f, -0.037338f, -0.07f ),
    new Vector3( 0.062f, -0.012446f, -0.03f ),
    new Vector3( 0.052f, -0.017446f, -0.07f ),
    new Vector3( 0.020666f, -0.06523f, -0.03f ),
    new Vector3( 0.010666f, -0.06023f, -0.07f ),
    new Vector3( 0.020666f, -0.037338f, -0.03f ),
    new Vector3( 0.010666f, -0.037338f, -0.07f ),
    new Vector3( 0.020666f, -0.012446f, -0.03f ),
    new Vector3( 0.010666f, -0.017446f, -0.07f ),
    new Vector3( -0.1033f, -0.06523f, -0.03f ),
    new Vector3( -0.0933f, -0.06023f, -0.07f ),
    new Vector3( -0.1033f, -0.037338f, -0.03f ),
    new Vector3( -0.0933f, -0.037338f, -0.07f ),
    new Vector3( -0.1033f, -0.012446f, -0.03f ),
    new Vector3( -0.0933f, -0.012446f, -0.07f ),
    new Vector3( -0.062f, -0.06523f, -0.03f ),
    new Vector3( -0.052f, -0.06023f, -0.07f ),
    new Vector3( -0.062f, -0.037338f, -0.03f ),
    new Vector3( -0.052f, -0.037338f, -0.07f ),
    new Vector3( -0.062f, -0.012446f, -0.03f ),
    new Vector3( -0.052f, -0.017446f, -0.07f ),
    new Vector3( -0.020666f, -0.06523f, -0.03f ),
    new Vector3( -0.010666f, -0.06023f, -0.07f ),
    new Vector3( -0.020666f, -0.037338f, -0.03f ),
    new Vector3( -0.010666f, -0.037338f, -0.07f ),
    new Vector3( -0.020666f, -0.012446f, -0.03f ),
    new Vector3( -0.010666f, -0.017446f, -0.07f ),
    };

    private static ExpStates[][] allConditionOrders = new ExpStates[][]
    {
        new ExpStates[] { ExpStates.Straight, ExpStates.Shape, ExpStates.Projection},
        new ExpStates[] { ExpStates.Shape, ExpStates.Projection, ExpStates.Straight},
        new ExpStates[] { ExpStates.Projection, ExpStates.Straight, ExpStates.Shape},
        new ExpStates[] { ExpStates.Straight, ExpStates.Projection, ExpStates.Shape},
        new ExpStates[] { ExpStates.Shape, ExpStates.Straight, ExpStates.Projection},
        new ExpStates[] { ExpStates.Projection, ExpStates.Shape, ExpStates.Straight}
};

    /// <summary>
    /// Clean up the thread and close the port on application close event.
    /// </summary>
    void OnApplicationQuit()
    {
        Debug.Log("Closing all recording objects");
        if (!(MkrsRecorder == null) && !MkrsRecorder.isClosed()) MkrsRecorder.closeRecorder();
        if (!(offsetRecorder == null) && !offsetRecorder.isClosed()) offsetRecorder.closeRecorder();
        if (!(PracticeMkrsRecorder == null) && !PracticeMkrsRecorder.isClosed()) PracticeMkrsRecorder.closeRecorder();
    }

    /// <summary>
    /// This function is called when the MonoBehaviour will be destroyed.
    /// OnDestroy will only be called on game objects that have previously
    /// been active.
    /// </summary>
    void OnDestroy()
    {
        Debug.Log("Closing all recording objects");
        if (!(MkrsRecorder == null) && !MkrsRecorder.isClosed()) MkrsRecorder.closeRecorder();
        if (!(offsetRecorder == null) && !offsetRecorder.isClosed()) offsetRecorder.closeRecorder();
        if (!(PracticeMkrsRecorder == null) && !PracticeMkrsRecorder.isClosed()) PracticeMkrsRecorder.closeRecorder();
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
