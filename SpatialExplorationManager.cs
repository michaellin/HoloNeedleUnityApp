/// <summary>
/// Manager for spatial exploration user study.
/// 
/// Author: A. Siu
/// September 26, 2017
/// </summary>
using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using RecData;		// For storing data
using System.Linq;

public class SpatialExplorationManager : MonoBehaviour
{

    #region study vars
    public bool practiceTrial = false;
    public bool staticCondition = false;

    enum studyState_t { Waiting4StudyContent, Waiting4Trial, DuringTrial, TargetReached };
	studyState_t currentState = studyState_t.Waiting4StudyContent;

    private bool studyComplete = false;

    private List<GameObject> allObjects;
    private List<GameObject> mapObjects;  // for storing all map objects
    private List<GameObject> targetObjects;

	public int numOfMaps = 4;
	private int currMapIndex = 0;

    public int numOfTargets = 4;
    private int currTargetIndex = 0;

    private const float minTargetDistance = 150.0f / 1000.0f; //[m] //250.0f/1000.0f; //[m]
    private const float maxTargetDistance = 600.0f/1000.0f; //[m]
    private const float minOtherObjectsDistance = 150.0f / 1000.0f; //[m]
    #endregion study vars

    #region workspace limits
    float maxX = 0.747f;
    float minX = 0.128f;
    float maxY = 0.376f;
    float minY = 0.122f;
    #endregion workspace limits

    #region record data vars
    // the recording objects
    private RecordData mapRecorder;
	private RecordData posRecorder;
	private RecordData mapAnswersRecorder;

	// num of columns for each file
	private int mapRecorderCols = 5;
	private int posRecorderCols = 3;
	private int mapAnswersRecorderCols = 4;

    // variables to store data
    private List<Vector2> trialPos;
    private float trialStartTime;
    private float trialElapsedTime;
    public float trialRecordingRate = 0.05f;
    private float trialRecordingCounter = 0.0f; //[s]
    
    // file names
    public string filename = "spatial_exp";
    public int mapNum;
	private string userFolderName;
    #endregion record data vars

    #region UI elements
    // dropdown menu for answers
    public Dropdown targetSelection;
    List<string> options;
    public Text studyState;
    public Text studyAction;
    public Text mapNumText;
    public Text trialNumText;
    public Text currentTargetText;
    #endregion UI elements

    public GameObject WorkspaceObject;
	private TableRenderVive workspace;

    public GameObject ShapeDisplayObject;
    private ShapeCast ShapeCastObject;
    public GameObject ShapeDisplayPosObject;

    public float footprint = 0.026f;

    public bool debug = false;

    // Use this for initialization
    void Start() {

        ShapeCastObject = ShapeDisplayObject.GetComponent<ShapeCast>();
        
        workspace = WorkspaceObject.GetComponent<TableRenderVive> ();
        
        // Generate map objects and targets
        GetMapObjectsAndTargets();

        // Set all recognition objects as inactive
        //Debug.Log("Non-targets are: ");
        foreach (GameObject obj in mapObjects)
        {
            obj.transform.rotation = workspace.transform.rotation;
            obj.transform.Rotate(new Vector3(90, 0, 0));
            obj.transform.parent = workspace.transform;
            //Debug.Log(obj.name);
        }
        //Debug.Log("Target objects are: ");
        foreach (GameObject obj in targetObjects)
        {
            obj.transform.rotation = workspace.transform.rotation;
            obj.transform.Rotate(new Vector3(90, 0, 0));
            obj.transform.parent = workspace.transform;
        }
        
        // Create folder directories to save data into for this study
        string baseFilePath = Directory.GetCurrentDirectory() + "/Assets/Data/";
        string userFolderName = filename + "_" + "mapNum_" + mapNum;
        string userFolder = baseFilePath + userFolderName;
        // Determine whether the directory exists.
        if (Directory.Exists(userFolder))
        {
            Debug.Log("WARNING! User folder exists already. Did you update subject number?");
        }
        else
        {
            Directory.CreateDirectory(userFolder);
        }

        filename = userFolderName + "/" + filename;

        // Init object selection list
        targetSelection.ClearOptions();
        options = new List<string>();
        foreach (GameObject obj in allObjects)
        {
            options.Add(obj.name);
        }
        targetSelection.AddOptions(options);
        targetSelection.gameObject.SetActive(false);


    }

    // Update is called once per frame
    void Update() {

        if (!studyComplete)
        {
            switch (currentState)
            {

                case studyState_t.Waiting4StudyContent:

                    
                    studyState.text = "Study State: waiting for map study to start.";
                    studyAction.text = "Press 'space' to generate maps and setup study.";
                    mapNumText.text = "Map Num: " + currMapIndex.ToString();
                    trialNumText.text = "Trial Num: " + currTargetIndex.ToString();
                    currentTargetText.text = "Target: ";

                    // turn off mouse
                    if (staticCondition)
                    {
                        ShapeCastObject.useMouse = false;
                    }

                    // if space is pressed then get ready to start study
                    if (Input.GetKeyDown(KeyCode.Space))
                    {

                        // Create data recorder element
                        mapRecorder = new RecordData(filename + "_mapFile" + "_mapNo_" + mapNum, mapRecorderCols);

                        // generate a map
                        GenerateMap();

                        // write map to file
                        foreach (GameObject obj in allObjects)
                        {

                            Vector3 objInWorkspacePos = workspace.transform.InverseTransformPoint(obj.transform.position);

                            mapRecorder.addData(currMapIndex.ToString(), obj.name,
                                               ( (objInWorkspacePos.x * 1000.0f) + 26.0f ).ToString(),
                                               ( (objInWorkspacePos.y * 1000.0f) + 26.0f).ToString(),
                                               ( objInWorkspacePos.z * 1000.0f ).ToString());
                        }
                        mapRecorder.WriteToFile();

                        // switch to waiting for trial
                        currentState = studyState_t.Waiting4Trial;

                        // reset trial counter
                        currTargetIndex = 0;

                        // increment map index counter
                        currMapIndex++;

                    }
                    break;

                case studyState_t.Waiting4Trial:

                    currentTargetText.text = "";
                    studyState.text = "Study State: waiting for trial to start.";
                    studyAction.text = "Press 'space' to initiate trial.";

                    //Debug.Log("Curr map index: " + currMapIndex + " | curr target index: " + currTargetIndex);

                    // if space bar then get ready to start trial
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        // start timers
                        trialStartTime = Time.time;
                        trialRecordingCounter = 0.0f;
                        // start trial recording list and file
                        trialPos = new List<Vector2>();
                        posRecorder = new RecordData( filename + "_trialPosData" + "_mapNo_" + mapNum + "_trialNo_" 
                                                      + currTargetIndex, posRecorderCols);
                        // start the map answers recorder
                        mapAnswersRecorder = new RecordData(Directory.GetCurrentDirectory() + "/Assets/Data/" +
                                                            filename + "_mapAnswers" + "_mapNo_" + mapNum +
                                                            ".txt", mapAnswersRecorderCols);

                        // set mouse on
                        if ( staticCondition )
                        {
                            ShapeCastObject.useMouse = true;
                        }

                        // Set the target text
                        currentTargetText.text = "Target: " + targetObjects[currTargetIndex].name;
                        trialNumText.text = "Trial Num: " + currTargetIndex.ToString();
                        // increment index for object
                        currTargetIndex++;
                        // change states to during trial
                        currentState = studyState_t.DuringTrial;
                    }
                    break;

                case studyState_t.DuringTrial:

                    studyState.text = "Study State: trial in progress.";
                    studyAction.text = "Press 'space' when target is reached.";

                    // decrement recording counter
                    trialRecordingCounter -= Time.deltaTime;
                    // if timer is over for recording
                    if (trialRecordingCounter < 0.0f)
                    {
                        // record position data in mm
                        Vector3 shapeInWorkspacePos = workspace.transform.InverseTransformPoint(ShapeDisplayPosObject.transform.position);
                        trialPos.Add( new Vector2( shapeInWorkspacePos.x * 1000.0f,
                                                   shapeInWorkspacePos.y * 1000.0f ) );
                    }
                    // if space bar, then target has been reached
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        // change states to TargetReached
                        currentState = studyState_t.TargetReached;

                        // turn off mouse
                        if (staticCondition)
                        {
                            ShapeCastObject.useMouse = false;
                        }

                    }
                    break;

                case studyState_t.TargetReached:

                    trialElapsedTime = Time.time - trialStartTime;
                    studyState.text = "Study State: target reached. End of trial.";
                    studyAction.text = "Press 'space' when answer selection has been made.";

                    // show dropdown
                    // targetSelection.gameObject.SetActive(true);
                    // if dropdown selection has been made
                    if (Input.GetKeyDown(KeyCode.Space))
                    {

                        currentTargetText.text = "";

                        Debug.Log("Please wait. Writing data to file.");

                        // save data for position and trial answer
                        foreach ( Vector2 pos in trialPos )
                        {
                            posRecorder.addData(currMapIndex.ToString(), pos.x.ToString(), pos.y.ToString());
                        }
                        mapAnswersRecorder.addData( (currMapIndex - 1).ToString(),
                                                    targetObjects[currTargetIndex - 1].name,
                                                    options[targetSelection.value],
                                                    trialElapsedTime.ToString() );
                        // write to file for position and for answer
                        posRecorder.WriteToFile();
                        mapAnswersRecorder.WriteToFileName();

                        // if there are more trials
                        if (currTargetIndex < targetObjects.Count)
                        {
                            // go back to waiting for trial
                            currentState = studyState_t.Waiting4Trial;
                        }
                        // if there are more maps to see and trials are over
                        else if ( currMapIndex < numOfMaps && currTargetIndex >= targetObjects.Count)
                        {
                            // go back to beginning of study
                            currentState = studyState_t.Waiting4StudyContent;
                        }
                        // else if its the last map
                        else if (currMapIndex >= numOfMaps)
                        {
                            // mark end of study
                            studyComplete = true;
                        }
                        // set the dropdown back to inactive
                        targetSelection.gameObject.SetActive(false);
                    }
                    break;
            } //endswitch
        } else
        {
            studyState.text = "Study State: end of study.";
            studyAction.text = "";
        }
    }
    

    /// <summary>
    /// Generates list of map objects and targets.
    /// </summary>
    void GetMapObjectsAndTargets() {

        targetObjects = new List<GameObject>();

        if ( practiceTrial )
        {
            numOfMaps = 1;
            allObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("practiceObj"));
            numOfTargets = 2;
            mapObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("practiceObj"));
            targetObjects.Add(mapObjects[0]);
            mapObjects.RemoveAt(0);
            targetObjects = targetObjects.Concat(targetObjects).ToList();
        } else
        {
            allObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("recogObj"));
            mapObjects = new List<GameObject>(GameObject.FindGameObjectsWithTag("recogObj"));

            int randInd;
            for (int i = 0; i < numOfTargets; i++)
            {
                randInd = Random.Range( 0, mapObjects.Count );
                targetObjects.Add( mapObjects[randInd] );
                mapObjects.RemoveAt( randInd );
            }
            targetObjects = targetObjects.Concat(targetObjects).ToList();

            Debug.Log("-------------------------------------------------------");
            for ( int i = 0; i < targetObjects.Count / 2; i++ )
            {
                Debug.Log("Target " + i + ": " + targetObjects[i].name);
            }
            Debug.Log("-------------------------------------------------------");
                for (int i = 0; i < mapObjects.Count; i++)
            {
                Debug.Log("Non-target " + i + ": " + mapObjects[i].name);
            }
            Debug.Log("-------------------------------------------------------");
                

        }

    }

	void GenerateMap () {

        //minX = 1.0f * footprint;
        //maxX = workspace.tableLength - 3.5f * footprint;
        //maxY = workspace.tableWidth - 3.5f * footprint;
        //minY = minX;

        // Generate positions for the target objects first
        for (int currObjInd = 0; currObjInd < numOfTargets; currObjInd++)
        {
            GameObject currObj = targetObjects[currObjInd];
            Vector3 randPos = Vector3.zero;
            //Debug.Log("current object starting: " + currObjInd + " " + currObj.name);

            bool goodDistance = false;
            while (!goodDistance)
            {
                goodDistance = true;
                randPos = new Vector3(Random.Range(minX, maxX),
                                      Random.Range(minY, maxY),
                                      0);
                for (int prevObjInd = 0; prevObjInd < currObjInd; prevObjInd++)
                {
                    GameObject prevObj = targetObjects[prevObjInd];
                    Vector2 obj1 = new Vector2(randPos.x, randPos.y);
                    Vector2 obj2 = new Vector2(prevObj.transform.localPosition.x, prevObj.transform.localPosition.y);

                    float distance = Vector2.Distance(obj1, obj2);
                    // Objects need to be within a min and max distance of each other
                    if ((distance > minTargetDistance)) //&& (distance < maxTargetDistance)  )
                    {
                        //Debug.Log("good distance | distance between " + currObj.name + " and " + prevObj.name + " is " + Vector3.Distance(randPos, prevObj.transform.localPosition));
                    }
                    else
                    {
                        goodDistance = false;
                        //Debug.Log("bad distance | distance between " + currObj.name + " and " + prevObj.name + " is " + Vector3.Distance(randPos, prevObj.transform.localPosition));
                    }
                }
            }
            currObj.transform.localPosition = randPos;
            currObj.transform.position = new Vector3(currObj.transform.position.x, ShapeDisplayObject.transform.position.y, currObj.transform.position.z);
            currObj.transform.localPosition = new Vector3(currObj.transform.localPosition.x, currObj.transform.localPosition.y, 0.0f);
            //Debug.Log("-------------");
        }
        
        // Generate positions for the rest of the objects 
        for (int currObjInd = 0; currObjInd < mapObjects.Count; currObjInd++)
        {
            GameObject currObj = mapObjects[currObjInd];
            Vector3 randPos = Vector3.zero;

            // Create a temporary list that contains all objects that have
            // already been assigned a position
            List<GameObject> templist = new List<GameObject>();
            templist.AddRange( targetObjects.GetRange( 0, numOfTargets ) );
            if ( currObjInd > 0 )
            {
                templist.AddRange(mapObjects.GetRange(0, currObjInd));
            }

            bool goodDistance = false;
            // Keep changing positions until we find a position
            // within a good distance range of the rest of the objects
            while (!goodDistance)
            {
                goodDistance = true;
                randPos = new Vector3(Random.Range(minX, maxX),
                                      Random.Range(minY, maxY),
                                      0);

                for (int prevObjInd = 0; prevObjInd < templist.Count; prevObjInd++)
                {
                    GameObject prevObj = templist[prevObjInd];
                    Vector2 obj1 = new Vector2(randPos.x, randPos.y);
                    Vector2 obj2 = new Vector2(prevObj.transform.localPosition.x, prevObj.transform.localPosition.y);

                    float distance = Vector2.Distance(obj1, obj2);
                    // Objects need to be within a min and max distance of each other
                    if ((distance > minOtherObjectsDistance)) //&& (distance < maxTargetDistance)  )
                    {
                        //Debug.Log("good distance | distance between " + currObj.name + " and " + prevObj.name + " is " + Vector3.Distance(randPos, prevObj.transform.localPosition));
                    }
                    else
                    {
                        goodDistance = false;
                        //Debug.Log("bad distance | distance between " + currObj.name + " and " + prevObj.name + " is " + Vector3.Distance(randPos, prevObj.transform.localPosition));
                    }
                }
            }
            currObj.transform.localPosition = randPos;
            currObj.transform.position = new Vector3(currObj.transform.position.x, ShapeDisplayObject.transform.position.y, currObj.transform.position.z);
            currObj.transform.localPosition = new Vector3(currObj.transform.localPosition.x, currObj.transform.localPosition.y, 0.0f);
            //Debug.Log("-------------");
        }

        //float totalD = 0.0f;
        //for (int currObjInd = 1; currObjInd < allObjects.Count; currObjInd++)
        //{
        //    GameObject prevObj = allObjects[currObjInd - 1];
        //    GameObject currObj = allObjects[currObjInd];
        //    Vector2 obj1 = new Vector2(prevObj.transform.localPosition.x, prevObj.transform.localPosition.y);
        //    Vector2 obj2 = new Vector2(currObj.transform.localPosition.x, currObj.transform.localPosition.y);
        //    totalD += Vector2.Distance(obj1, obj2);
        //}
        //Debug.Log("total dist: " + totalD);

    }


    //private void WritePosDataToFile()
    //{
    //    // convert stored data to string
    //    string textToWrite = "";
    //    foreach (Vector2 data in posList)
    //    {
    //        textToWrite = textToWrite + data.x + "\t" + data.y + "\n";
    //    }

    //    // if no file name is given, use default name
    //    if (filename == "")
    //    {
    //        filename = "default_data";
    //    }

    //    // save file to Data directory
    //    filename = Directory.GetCurrentDirectory() + "/Data/" + filename;

    //    // check to see file name is not already in use so
    //    // we don't override existing data
    //    string temp = filename + ".txt";
    //    int i = 0;
    //    while (File.Exists(temp))
    //    {
    //        Debug.Log("File name already in use. Finding an alternative.");
    //        temp = filename + "_" + i + ".txt";
    //        i++;
    //    }
    //    filename = temp;

    //    // write to the file
    //    File.AppendAllText(filename, textToWrite);
    //    Debug.Log("Saved data to file: " + filename);
    //}

}