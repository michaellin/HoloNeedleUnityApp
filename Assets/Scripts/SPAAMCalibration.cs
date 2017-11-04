using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.Input;
using UnityEngine.UI;


public class SPAAMCalibration : MonoBehaviour
{
    public float speed = 0.3f;             // speed for moving the camera
    public float distFwd = 0.4f;           // 0.4 meters in front of the user

    // *** User Interface *** //
    //public Text textStatus;

    //public GameObject HoloLensMarker;      // This is the virtual object that will be moved around
    public GameObject NeedleMarker;                // This marker will be the one attached to the needle
    public GameObject HoloLensMarker;              // This marker will be the one attached to the HoloLens
    public GameObject CalibrationObject;
    public GameObject TestObject;
    public GameObject Needle;
    private Transform CalibrationObjectTransform;  // This is the object that will move around w/r to the HoloLens for calibration

    private bool initialAlignmentDone = false;
    public bool completedAlignment = false;
    Matrix4x4 T_H;

    Quaternion qC_D;
    Vector3 rC_Co_Do;

    float x_off;
    float y_off;
    float z_off;

    // *** Calibration Parameters *** //
    public const int SPAAMPoints = 12;
    private double[,] AlignmentPoints;
    private double[,] CalibrationPoints =
        { { 0, 0, 0.5 },
          { 0, 0.04, 0.5 },
          { 0.025, 0, 0.55 },
          { 0.025, -0.04, 0.5 },
          { -0.04, 0, 0.55  },
          { -0.025, 0.025, 0.5 },
          { -0.04, 0.04, 0.55 },
          { -0.025, 0, 0.5 },
          { -0.025, -0.04, 0.55 },
          { 0.04, 0, 0.5 },
          { 0.045, 0.02, 0.55 },
          { -0.025, 0, 0.5 }
        };
    private double[,] MeasuredCalibrationPoints;
    private int currStep = 0;
    private bool debounceCalib = false;
    private bool tapDetected = false;
    GestureRecognizer recognizer;

    // Use this for initialization
    void Start()
    {
        T_H = Matrix4x4.identity;    // Initialize the alignment matrix as identity

        // Calibration results
        qC_D = new Quaternion(0.058282339452415f, -0.020470290730742f, -0.000699217278049f, 0.998089999549415f);
        rC_Co_Do = new Vector3(-0.003902248347851f, 0.008639295309357f, 0.008261943033732f);

        CalibrationObjectTransform = CalibrationObject.transform; // obtain the transform of the container for the calibration object
        if (completedAlignment == false)
        {
            CalibrationObject.SetActive( false );
        } else
        {
            TestObject.SetActive( false );
        }

        AlignmentPoints = new double[SPAAMPoints, 7];             // Initialize 2D array of numData and 7 which is 3 for position and 4 for orientation
        MeasuredCalibrationPoints = new double[SPAAMPoints, 3];   // Array to store the calibration points in world coordinates

        CalibrationObjectTransform.localPosition = new Vector3( (float)CalibrationPoints[0,0], (float)CalibrationPoints[0, 1], (float)CalibrationPoints[0, 2] );  // Set the first position

        //textStatus.text = "Step " + (currStep + 1);

        // Testing space
        //GetCalibration2( toHomogeneous( 12, 3, CalibrationPoints ), toHomogeneous( 12, 3, CalibrationPoints ) );
        recognizer = new GestureRecognizer();
        recognizer.TappedEvent += ( source, tapCount, ray ) =>
        {
            tapDetected = true;
        };
        recognizer.StartCapturingGestures();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (completedAlignment)
        {
            // ** 0.00340	-0.04503	-0.03366	0.00316
            //    0.00268    0.02992    0.01796     0.00146
            //    0.05821    0.51140    0.35615    -0.00126
            //    0.07317    0.63595    0.44050     1.00000
            //T_H.m03 = x_off; T_H.m13 = y_off; T_H.m23 = z_off;
            Vector3 relativePos = HoloLensMarker.transform.InverseTransformVector(NeedleMarker.transform.position - HoloLensMarker.transform.position);
            Quaternion relativeRot = Quaternion.Inverse( HoloLensMarker.transform.rotation ) * NeedleMarker.transform.rotation;
            TestObject.transform.localRotation = qC_D * relativeRot;
            TestObject.transform.localPosition = rC_Co_Do + qC_D*relativePos;
        }

        CalibrationObjectTransform.localPosition = new Vector3( (float)CalibrationPoints[currStep, 0], (float)CalibrationPoints[currStep, 1], (float)CalibrationPoints[currStep, 2] );  // Set next position
        //CalibrationObjectTransform.transform.up = Vector3.up;

        float step_off = 0.05f;
        if (Input.GetKeyDown( KeyCode.T ) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.2f);
            if (completedAlignment == true)
            {
                completedAlignment = false;
                CalibrationObject.SetActive(true);
                TestObject.SetActive(false);
                T_H = Matrix4x4.identity;
            } else
            {
                completedAlignment = true;
                CalibrationObject.SetActive(false);
                TestObject.SetActive(true);
                //T_H.m00 = 0.3318f; T_H.m01 = -0.0052f; T_H.m02 = 0.04f; T_H.m03 = -0.0088f;
                //T_H.m10 = -0.0298f; T_H.m11 = 0.3186f; T_H.m12 = -0.0633f; T_H.m13 = 0.019f;
                //T_H.m20 = -0.4631f; T_H.m21 = 0.2223f; T_H.m22 = -0.4134f; T_H.m23 = 0.365f;
                //T_H.m30 = -0.7778f; T_H.m31 = 0.2727f; T_H.m32 = -1.3637f; T_H.m33 = 1.0f;
            }
            
        }
        else if (Input.GetKeyDown(KeyCode.W) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            //rC_Co_Do += new Vector3(step_off, 0, 0);
            qC_D = qC_D * Quaternion.Euler(new Vector3(step_off, 0, 0));
            Debug.Log(qC_D);
        }
        else if (Input.GetKeyDown(KeyCode.S) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            //rC_Co_Do -= new Vector3(step_off, 0, 0);
            qC_D = qC_D * Quaternion.Euler(new Vector3(-step_off, 0, 0));
            Debug.Log(qC_D);
        }
        else if (Input.GetKeyDown(KeyCode.A) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            //rC_Co_Do += new Vector3(0, step_off, 0);
            qC_D = qC_D * Quaternion.Euler(new Vector3(0, step_off, 0));
            Debug.Log(qC_D);
        }
        else if (Input.GetKeyDown(KeyCode.D) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            //rC_Co_Do -= new Vector3(0, step_off, 0);
            qC_D = qC_D * Quaternion.Euler(new Vector3(0, -step_off, 0));
            Debug.Log(qC_D);
        }
        else if (Input.GetKeyDown(KeyCode.Q) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            //rC_Co_Do += new Vector3(0, 0, step_off);
            qC_D = qC_D * Quaternion.Euler(new Vector3(0, 0, step_off));
            Debug.Log(qC_D);
        }
        else if (Input.GetKeyDown(KeyCode.E) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            //rC_Co_Do -= new Vector3(0, 0, step_off);
            qC_D = qC_D * Quaternion.Euler(new Vector3(0, 0, -step_off));
            Debug.Log(qC_D);
        }

        if ((Input.GetKeyDown( KeyCode.Space ) || tapDetected) && (currStep < SPAAMPoints) && (debounceCalib == false))
        {
            if (!initialAlignmentDone)
            {
                initialAlignmentDone = true;
            }
            if (tapDetected)
            {
                tapDetected = false;
            }
            Vector3 relativePos = HoloLensMarker.transform.InverseTransformVector(NeedleMarker.transform.position - HoloLensMarker.transform.position); // in the Camera coordinate system
            Quaternion relativeRot = Quaternion.Inverse( HoloLensMarker.transform.rotation ) * NeedleMarker.transform.rotation;                      // rotation from HMD to object

            AlignmentPoints[currStep, 0] = relativePos.x;
            AlignmentPoints[currStep, 1] = relativePos.y;
            AlignmentPoints[currStep, 2] = relativePos.z;
            AlignmentPoints[currStep, 3] = relativeRot.w;
            AlignmentPoints[currStep, 4] = relativeRot.x;
            AlignmentPoints[currStep, 5] = relativeRot.y;
            AlignmentPoints[currStep, 6] = relativeRot.z;

            MeasuredCalibrationPoints[currStep, 0] = CalibrationObjectTransform.position.x;
            MeasuredCalibrationPoints[currStep, 1] = CalibrationObjectTransform.position.y;
            MeasuredCalibrationPoints[currStep, 2] = CalibrationObjectTransform.position.z;

            Debug.Log( "posx " + AlignmentPoints[currStep, 0].ToString( "F4" ) +
                       " posy " + AlignmentPoints[currStep, 1].ToString( "F4" ) +
                       " posz " + AlignmentPoints[currStep, 2].ToString( "F4" ) );

            if (currStep == SPAAMPoints - 1)
            {
                //textStatus.text = "Done.";
                Debug.Log( "Done" );
                completedAlignment = true;
                //Matrix4x4 T_H = GetCalibration2( toHomogeneous(12, 3, CalibrationPoints), toHomogeneous(12, 3, AlignmentPoints) );
                Debug.Log( T_H );

                CalibrationObject.SetActive( false );
                TestObject.SetActive( true );

                // Figure out what to do next
                completedAlignment = true;
            }
            else
            {
                currStep++;
                //textStatus.text = "Step " + (currStep + 1);
                Debug.Log( "Step " + (currStep + 1) );
            }
            debounceCalib = true;
            Invoke( "recoverDebounce", 0.2f );
        }
        else if (Input.GetKeyDown( KeyCode.F ))
        {
            Debug.Log("posx " + TestObject.transform.localPosition.x.ToString("F4") +
                       " posy " + TestObject.transform.localPosition.y.ToString("F4") +
                       " posz " + TestObject.transform.localPosition.z.ToString("F4"));
        }
        else if (Input.GetKeyDown( KeyCode.P ))
        {
            for (int i = 0; i < currStep; i++)
            {
                Debug.Log( "posx " + AlignmentPoints[i, 0].ToString( "F4" ) +
                            " posy " + AlignmentPoints[i, 1].ToString( "F4" ) +
                            " posz " + AlignmentPoints[i, 2].ToString( "F4" ) +
                            " rotw " + AlignmentPoints[i, 3].ToString( "F4" ) +
                            " rotx " + AlignmentPoints[i, 4].ToString( "F4" ) +
                            " roty " + AlignmentPoints[i, 5].ToString( "F4" ) +
                            " rotz " + AlignmentPoints[i, 6].ToString( "F4" ) );
            }
        }
        else if (Input.GetKeyDown( KeyCode.R ))
        {
            completedAlignment = false;
            CalibrationObject.SetActive( true );
            TestObject.SetActive( false );
            currStep = 0;
            CalibrationObjectTransform.position = new Vector3( (float)CalibrationPoints[0, 0], (float)CalibrationPoints[0, 1], (float)CalibrationPoints[0, 2] );  // Set the first position
        }

        //float xAxisValue = speed * Input.GetAxis( "Horizontal" );
        //float zAxisValue = speed * Input.GetAxis( "Vertical" );
        //if (Camera.current != null)
        //{
        //    Camera.current.transform.Translate( new Vector3( xAxisValue, 0.0f, zAxisValue ) );
        //}

        //transform.position = Camera.main.transform.position + Camera.main.transform.forward * distFwd;
        //if (!initialAlignment)
        //{
        //    transform.rotation = Quaternion.AngleAxis( Camera.main.transform.rotation.eulerAngles.y, Vector3.up );
        //}
    }

    /// <summary>
    /// Recover from debouncing state for space bar
    /// </summary>
    private void recoverDebounce()
    {
        debounceCalib = false;
    }

    /// <summary>
    /// Get Calibration calculates the projection matrix from points in displayed to points in measured. The problem is defined
    /// as qi = T*pi. Where pi are measured and qi are defined points. If we rework the matrix we can get A*t = 0. The reworked 
    /// matrix is of the following form:
    /// A = [-q1 -q2 -q3 -1   0   0   0  0   0   0   0  0 p1q1 p1q2 p1q3;
    ///        0   0   0  0 -q1 -q2 -q3 -1   0   0   0  0 p2q1 p2q2 p2q3;
    ///        0   0   0  0   0   0   0  0 -q1 -q2 -q3 -1 p3q1 p3q2 p3q3;
    ///        ...
    ///        ]
    /// and t is of the form:
    /// t = [t11; t12; t13; t14; t21; t22; t23; t24; t31; t32; t33; t34; t41; t42; t43]
    /// We solve for t using SVD decomposition.
    /// </summary>
    /// <param name="pi"></param>
    /// <param name="qi"></param>
    /// <returns></returns>
    //private Matrix4x4 GetCalibration( double[,] pi, double[,] qi )
    //{
    //    const int doubleSize = 8;
    //    const int matrixWidth = 16;
    //    const int matrixHeight = SPAAMPoints*3;

    //    var A = new double[matrixHeight, matrixWidth];
    //    for (int p = 0; p < SPAAMPoints; p++)
    //    {
    //        double[] row1 = { -qi[p, 0], -qi[p, 1], -qi[p, 2], -1, 0, 0, 0, 0, 0, 0, 0, 0, pi[p, 0] * qi[p, 0], pi[p, 0] * qi[p, 1], pi[p, 0] * qi[p, 2], pi[p, 0] };
    //        Buffer.BlockCopy( row1, 0, A, doubleSize * matrixWidth * ( 3 * p), doubleSize * matrixWidth );
    //        double[] row2 = { 0, 0, 0, 0, -qi[p, 0], -qi[p, 1], -qi[p, 2], -1, 0, 0, 0, 0, pi[p, 1] * qi[p, 0], pi[p, 1] * qi[p, 1], pi[p, 1] * qi[p, 2], pi[p, 1] };
    //        Buffer.BlockCopy( row2, 0, A, doubleSize * matrixWidth * (3 * p + 1), doubleSize * matrixWidth );
    //        double[] row3 = { 0, 0, 0, 0, 0, 0, 0, 0, -qi[p, 0], -qi[p, 1], -qi[p, 2], -1, pi[p, 2] * qi[p, 0], pi[p, 2] * qi[p, 1], pi[p, 2] * qi[p, 2], pi[p, 2] };
    //        Buffer.BlockCopy( row3, 0, A, doubleSize * matrixWidth * (3 * p + 2), doubleSize * matrixWidth );
    //    }
        
    //    // Prepare matrices to get SVD results
    //    double[] W = new double[matrixWidth];
    //    double[,] U = new double[matrixHeight, matrixWidth];
    //    double[,] VT = new double[matrixWidth, matrixWidth];

    //    alglib.svd.rmatrixsvd( A, matrixHeight, matrixWidth, 0, 1, 2, ref W, ref U, ref VT ); // SVD with alglib

    //    double[] coeffs = new double[matrixWidth];

    //    Buffer.BlockCopy( VT, doubleSize * matrixWidth * (matrixWidth - 1), coeffs, 0, doubleSize * matrixWidth ); // Last row of VT contains the parameters of the transform matrix.
    //    printMatrix( coeffs );

    //    Matrix4x4 Tresult = new Matrix4x4();
    //    Tresult.m00 = (float)coeffs[0];
    //    Tresult.m01 = (float)coeffs[1];
    //    Tresult.m02 = (float)coeffs[2];
    //    Tresult.m03 = (float)coeffs[3];
    //    Tresult.m10 = (float)coeffs[4];
    //    Tresult.m11 = (float)coeffs[5];
    //    Tresult.m12 = (float)coeffs[6];
    //    Tresult.m13 = (float)coeffs[7];
    //    Tresult.m20 = (float)coeffs[8];
    //    Tresult.m21 = (float)coeffs[9];
    //    Tresult.m22 = (float)coeffs[10];
    //    Tresult.m23 = (float)coeffs[11];
    //    Tresult.m30 = (float)coeffs[12];
    //    Tresult.m31 = (float)coeffs[13];
    //    Tresult.m32 = (float)coeffs[14];
    //    Tresult.m33 = (float)coeffs[15];

    //    return Tresult;
    //}

    /// <summary>
    ///  Calibrates two sets of homogeneous coordinates using least squares
    /// </summary>
    /// <param name="P">defined</param>
    /// <param name="Q">measured</param>
    /// <returns></returns>
    //private Matrix4x4 GetCalibration2( double[,] P, double[,] Q )
    //{
    //    double[,] Q_star = new double[4, 12];
    //    transposeMat( 12, 4, Q, ref Q_star );

    //    double[,] Q_temp = new double[4, 4];
    //    alglib.rmatrixgemm( 4, 4, 12, 1, Q_star, 0, 0, 0, Q, 0, 0, 0, 0, ref Q_temp, 0, 0 );

    //    int success;
    //    alglib.matinvreport rep;
    //    alglib.rmatrixinverse( ref Q_temp, out success, out rep );

    //    double[,] Q_pinv = new double[4, 12];
    //    alglib.rmatrixgemm( 4, 12, 4, 1, Q_temp, 0, 0, 0, Q_star, 0, 0, 0, 0, ref Q_pinv, 0, 0 );

    //    double[,] result = new double[4, 4]; // result should be the transpose of this
    //    alglib.rmatrixgemm( 4, 4, 12, 1, Q_pinv, 0, 0, 0, P, 0, 0, 0, 0, ref result, 0, 0 );

    //    Matrix4x4 outMat = new Matrix4x4();
    //    outMat.m00 = (float) result[0, 0];
    //    outMat.m01 = (float)result[1, 0];
    //    outMat.m02 = (float)result[2, 0];
    //    outMat.m03 = (float)result[3, 0];
    //    outMat.m10 = (float)result[0, 1];
    //    outMat.m11 = (float)result[1, 1];
    //    outMat.m12 = (float)result[2, 1];
    //    outMat.m13 = (float)result[3, 1];
    //    outMat.m20 = (float)result[0, 2];
    //    outMat.m21 = (float)result[1, 2];
    //    outMat.m22 = (float)result[2, 2];
    //    outMat.m23 = (float)result[3, 2];
    //    outMat.m30 = (float)result[0, 3];
    //    outMat.m31 = (float)result[1, 3];
    //    outMat.m32 = (float)result[2, 3];
    //    outMat.m33 = (float)result[3, 3];
    //    return outMat;
    //}

    /// <summary>
    /// Computes the transpose of a matrix
    /// </summary>
    /// <param name="m">height</param>
    /// <param name="n">width</param>
    /// <param name="inMat"></param>
    /// <param name="outMat"></param>
    private void transposeMat( int m, int n, double[,] inMat, ref double[,] outMat )
    {
        for ( int k = 0; k < m; k++ )
        {
            for ( int j = 0; j < n; j ++ )
            {
                outMat[j, k] = inMat[k, j];
            }
        }
    }

    /// <summary>
    /// Makes an array of points into Homogeneous coordinate
    /// </summary>
    /// <param name="m"></param>
    /// <param name="n"></param>
    /// <param name="inMat"></param>
    /// <param name="outMat"></param>
    private double[,] toHomogeneous( int m, int n, double[,] inMat )
    {
        double[,] result = new double[m, n + 1];
        for (int k = 0; k < m; k++)
        {
            for (int j = 0; j < n; j++)
            {
                result[k, j] = inMat[k, j];
            }
            result[k, n] = 1.0f;
        }
        return result;
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