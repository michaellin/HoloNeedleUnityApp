﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;


public class ExperimentManager : MonoBehaviour
{
    // *** User Interface *** //
    //public Text textStatus;

    public GameObject NeedleMarker;                // This marker will be the one attached to the needle
    public GameObject HoloLensMarker;              // This marker will be the one attached to the HoloLens
    public GameObject PhantomMarker;               // This marker will be attached to the phantom
    public GameObject Needle;                      // This object represents the needle
    public GameObject NeedleRenderer;              // This object has ProcNeedle attached
    public GameObject Phantom;                     // This object is linked to the phantom

    // *** Relative Transform Vars *** //
    Quaternion qC_D;
    Vector3 rC_Co_Do;
    Quaternion qN1_N2;
    Vector3 rN1_No1_No2;

    // *** Experiment Vars *** //
    ExpStates currState;
    private bool lockPhantom;

    // *** Tweak Vars *** //
    float x_off;
    float y_off;
    float z_off;

    // *** Misc Vars *** //
    private bool debounceCalib = false;
    private bool tapDetected = false;
    UnityEngine.XR.WSA.Input.GestureRecognizer recognizer;


    // *** States for the experiment state machine *** //
    enum ExpStates
    {
        InitCalib,
        KeyboardFB,
        Straight,
        Shape,
        Projection,
        ProjectHit
    };

    // Use this for initialization
    void Start()
    {

        // Calibration results
        qC_D = new Quaternion(0.058282339452415f, -0.020470290730742f, -0.000699217278049f, 0.998089999549415f);
        rC_Co_Do = new Vector3(-0.003902248347851f, 0.008639295309357f, 0.008261943033732f);

        qN1_N2 = new Quaternion(0.603773723212385f, 0.029050211798263f, 0.796173811526621f, 0.026844705099943f);
        rN1_No1_No2 = new Vector3(-0.064259547303643f, -0.377448931834412f, 0.346321017932784f);

        recognizer = new UnityEngine.XR.WSA.Input.GestureRecognizer();
        recognizer.TappedEvent += ( source, tapCount, ray ) =>
        {
            tapDetected = true;
        };
        recognizer.StartCapturingGestures();

        currState = ExpStates.InitCalib;
        lockPhantom = false;
    }

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
                break;

            case ExpStates.KeyboardFB:
                relativePos = HoloLensMarker.transform.InverseTransformVector(NeedleMarker.transform.position - HoloLensMarker.transform.position);
                relativeRot = Quaternion.Inverse(HoloLensMarker.transform.rotation) * NeedleMarker.transform.rotation;
                Needle.transform.localRotation = qC_D * relativeRot;
                Needle.transform.localPosition = rC_Co_Do + qC_D * relativePos;
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
                break;

        }

        //Vector3 relativePos = HoloLensMarker.transform.InverseTransformVector(NeedleMarker.transform.position - HoloLensMarker.transform.position);
        //Quaternion relativeRot = Quaternion.Inverse( HoloLensMarker.transform.rotation ) * NeedleMarker.transform.rotation;
        //Needle.transform.localRotation = qC_D * relativeRot;
        //Needle.transform.localPosition = rC_Co_Do + qC_D*relativePos;

        float step_off = 0.05f;
        if (Input.GetKeyDown( KeyCode.T ) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.2f);
            
        }
        else if (Input.GetKeyDown(KeyCode.W) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().upXOffset();
            //rC_Co_Do += new Vector3(step_off, 0, 0);
            //qC_D = qC_D * Quaternion.Euler(new Vector3(step_off, 0, 0));
            //Debug.Log(qC_D);
        }
        else if (Input.GetKeyDown(KeyCode.S) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().dnXOffset();
            //rC_Co_Do -= new Vector3(step_off, 0, 0);
            //qC_D = qC_D * Quaternion.Euler(new Vector3(-step_off, 0, 0));
            //Debug.Log(qC_D);
        }
        else if (Input.GetKeyDown(KeyCode.A) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().dnZOffset();
            //rC_Co_Do += new Vector3(0, step_off, 0);
            //qC_D = qC_D * Quaternion.Euler(new Vector3(0, step_off, 0));
            //Debug.Log(qC_D);
        }
        else if (Input.GetKeyDown(KeyCode.D) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().upZOffset();
            //rC_Co_Do -= new Vector3(0, step_off, 0);
            //qC_D = qC_D * Quaternion.Euler(new Vector3(0, -step_off, 0));
            //Debug.Log(qC_D);
        }
        else if (Input.GetKeyDown(KeyCode.Q) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().dnYOffset();
            //rC_Co_Do += new Vector3(0, 0, step_off);
            //qC_D = qC_D * Quaternion.Euler(new Vector3(0, 0, step_off));
            //Debug.Log(qC_D);
        }
        else if (Input.GetKeyDown(KeyCode.E) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().upYOffset();
            //rC_Co_Do -= new Vector3(0, 0, step_off);
            //qC_D = qC_D * Quaternion.Euler(new Vector3(0, 0, -step_off));
            //Debug.Log(qC_D);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha0) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            currState = ExpStates.KeyboardFB;
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateTip();
            Debug.Log("current state: keyboard feedback");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            currState = ExpStates.Straight;
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateStraight();
            Debug.Log("current state: straight");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            currState = ExpStates.Shape;
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateShape();
            Debug.Log("current state: shape");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            currState = ExpStates.Projection;
            NeedleRenderer.GetComponent<ShapeSensing.ProcNeedle>().setStateProjection();
            Debug.Log("current state: project");
        }
        else if (Input.GetKeyDown(KeyCode.L) && (debounceCalib == false))
        {
            debounceCalib = true;
            Invoke("recoverDebounce", 0.08f);
            lockPhantom ^= true;
        }
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
