using UnityEngine;
using System;
using System.Collections;

namespace ShapeSensing {
    /// <summary>
    /// Generates the mesh of the needle which is update in real time from stream of data.
    /// </summary>
    public class ProcNeedle : ProcBase
    {
        // *** Resolution of generated mesh *** //
        private const int m_RadialSegmentCount = 10;  // Number of radial segments
        private const int m_HeightSegmentCount = 80;  // Number of height segments

        // *** Needle dimensions  *** //
        public float m_Radius = 0.001f;         // Radius of the needle 0.5 mm
        public float needleLength = 0.14512f;   // Length of needle 145.12 mm
        private const float tipLength = 0.002f;        // Dimensions of needle tip

        // *** Needle coordinates *** //
        //public Vector3[] polyPoints;                  // Polynomial interpolated points
        private Vector3[] chainedPoints;               // Chain interpolated points
        private float ax = 0, bx = 0;         // Coefficients of linear fit to the curavture along needle in xz plane
        private float ay = 0, by = 0;         // Coefficients of linear fit to the curavture along needle in yz plane

        public Vector3 offset;
        private float step = 0.0002f;

        // *** Needle States ***/
        private NeedleStates needleState;
        enum NeedleStates
        {
            Tip,
            WaitTip,
            Straight,
            Shape,
            Project
        };

        // *** Peripheral objects *** //
        private TcpClientManager tcp;
        public GameObject projection;
        public GameObject tip;

        /// <summary>
        /// MonoBehavior method called on start.
        /// </summary>
        public new void Start()
        {
            Debug.Log("Initializing HoloNeedle");
            chainedPoints = GetChainedPoints(m_HeightSegmentCount, ax, ay, bx, by);
            Mesh mesh = BuildMesh();
            GetComponent<MeshFilter>().mesh = mesh;

            // Get tcp object
            tcp = GetComponent<TcpClientManager>();

            Debug.Log("Running HoloNeedle ...");
            projection.SetActive(false);
            tip.SetActive(false);
            needleState = NeedleStates.Straight;
        }

        /// <summary>
        /// MonoBehavior method that updates at rate of physics engine
        /// </summary>
        void FixedUpdate()
        {
            Mesh mesh;
            switch (needleState)
            {
                case NeedleStates.WaitTip:
                    break;
                case NeedleStates.Tip:
                    if (tcp.interlock == 1)
                    {
                        // When the tcp object has coefficients ready, get them.
                        ax = tcp.ax; bx = tcp.bx;
                        ay = tcp.ay; by = tcp.by;
                        tcp.interlock--;

                        chainedPoints = GetChainedPoints(m_HeightSegmentCount, ax, ay, bx, by);
                        tip.transform.localPosition = offset + chainedPoints[chainedPoints.Length - 1];
                    }

                    break;
                case NeedleStates.Straight:
                    ax = 0; bx = 0;
                    ay = 0; by = 0;
                    chainedPoints = GetChainedPoints(m_HeightSegmentCount, ax, ay, bx, by);
                    mesh = BuildMesh();
                    GetComponent<MeshFilter>().mesh = mesh;          // Set the mesh so that it appears in the graphics
                    break;
                case NeedleStates.Shape:
                    if (tcp.interlock == 1)
                    {
                        // When the tcp object has coefficients ready, get them.
                        ax = tcp.ax; bx = tcp.bx;
                        ay = tcp.ay; by = tcp.by;
                        tcp.interlock--;

                        chainedPoints = GetChainedPoints(m_HeightSegmentCount, ax, ay, bx, by);
                        mesh = BuildMesh();
                        GetComponent<MeshFilter>().mesh = mesh;          // Set the mesh so that it appears in the graphics
                    }
                    break;
                case NeedleStates.Project:
                    if (tcp.interlock == 1)
                    {
                        // When the tcp object has coefficients ready, get them.
                        ax = tcp.ax; bx = tcp.bx;
                        ay = tcp.ay; by = tcp.by;
                        tcp.interlock--;

                        chainedPoints = GetChainedPoints(m_HeightSegmentCount, ax, ay, bx, by);
                        mesh = BuildMeshWithProjection();
                        GetComponent<MeshFilter>().mesh = mesh;          // Set the mesh so that it appears in the graphics
                    }
                    break;
                default:
                    break;
            }

        }

        public void setStateTip()
        {
            projection.SetActive(false);
            tip.SetActive(true);
            needleState = NeedleStates.Tip;
            offset = new Vector3();
            // clear current mesh
            GetComponent<MeshFilter>().mesh.Clear();
        }

        public void setStateStraight()
        {
            projection.SetActive(false);
            tip.SetActive(false);
            needleState = NeedleStates.Straight;
        }

        public void setStateShape()
        {
            projection.SetActive(false);
            tip.SetActive(false);
            needleState = NeedleStates.Shape;
        }

        public void setStateProjection()
        {
            projection.SetActive(true);
            tip.SetActive(false);
            needleState = NeedleStates.Project;
            chainedPoints = GetChainedPoints(m_HeightSegmentCount, ax, ay, bx, by);
            Mesh mesh = BuildMeshWithProjection();
            GetComponent<MeshFilter>().mesh = mesh;          // Set the mesh so that it appears in the graphics
        }

        public void upXOffset()
        {
            offset += new Vector3(step, 0, 0);
        }
        public void dnXOffset()
        {
            offset -= new Vector3(step, 0, 0);
        }
        public void upYOffset()
        {
            offset += new Vector3(0, step, 0);
        }
        public void dnYOffset()
        {
            offset -= new Vector3(0, step, 0);
        }
        public void upZOffset()
        {
            offset += new Vector3(0, 0, step);
        }
        public void dnZOffset()
        {
            offset -= new Vector3(0, 0, step);
        }
        public void resetOffset()
        {
            offset = new Vector3(0, 0, 0);
        }

        /// <summary>
        /// MonoBehavior method can be used to perform any actions before quitting application.
        /// </summary>
        void OnApplicationQuit()
        {
            Debug.Log("Application ending after " + Time.time + " seconds");
        }


        /// <summary>
        /// Method for building a mesh. Called in Start()
        /// </summary>
        public override Mesh BuildMesh()
        {
            //Create a new mesh builder:
            MeshBuilder meshBuilder = new MeshBuilder();

            for (int i = 0; i < chainedPoints.Length; i++)
            {
                Vector3 centrePos = chainedPoints[i];
                float v = ((float)i) /m_HeightSegmentCount;
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, GetDirection(i));
                BuildRing(meshBuilder, m_RadialSegmentCount, centrePos, m_Radius, v, i > 0, rot);
            }

            BuildTip(meshBuilder, m_RadialSegmentCount, chainedPoints[chainedPoints.Length - 1], m_Radius, GetDirection(chainedPoints.Length - 1), tipLength);

            return meshBuilder.CreateMesh();
        }

        /// <summary>
        /// Method for building a mesh.Called in Start()
        /// </summary>
        public Mesh BuildMeshWithProjection()
        {
            //Create a new mesh builder:
            MeshBuilder meshBuilder = new MeshBuilder();

            for (int i = 0; i < chainedPoints.Length; i++)
            {
                Vector3 centrePos = chainedPoints[i];
                float v = (float)i / m_HeightSegmentCount;
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, GetDirection(i));
                BuildRing(meshBuilder, m_RadialSegmentCount, centrePos, m_Radius, v, i > 0, rot);
            }

            BuildTip(meshBuilder, m_RadialSegmentCount, chainedPoints[chainedPoints.Length - 1], m_Radius, GetDirection(chainedPoints.Length - 1), tipLength);

            //Set the position and rotation of the projection object
            projection.transform.localPosition = chainedPoints[chainedPoints.Length - 1];
            projection.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, GetDirection(chainedPoints.Length - 1));
            return meshBuilder.CreateMesh();
        }


        private Vector3[] GetChainedPoints(int numPoints, float ax, float ay, float bx, float by)
        {
            Vector3[] pts = new Vector3[numPoints];
            float lastThetaX = 0;
            float lastThetaY = 0;
            float dL = needleLength / numPoints;

            for (int i = 1; i < numPoints; i++)
            {
                float z = i * dL;
                float Kx = z * ax + bx - ax / 2 * dL;
                float thetaX = Kx * dL + lastThetaX;


                float Ky = z * ay + by - ay / 2 * dL;
                float thetaY = Ky * dL + lastThetaY;

                // Note: estimated X coordinates get assigned to Y and estimated Y coordinates get assigned to X
                // because Unity works in left hand rule and calibration was done with right hand rule.
                double a = Math.Cos(thetaX) / Math.Cos(thetaY);
                pts[i].y = (float)(Math.Sin(thetaX) * Math.Sqrt(Math.Pow(dL, 2) / (Math.Pow(a * Math.Sin(thetaY), 2) + Math.Pow(Math.Sin(thetaX), 2) + Math.Pow(Math.Cos(thetaX), 2)))) + pts[i - 1].y;
                pts[i].x = (float)(a * Math.Sin(thetaY) * Math.Sqrt(Math.Pow(dL, 2) / (Math.Pow(a * Math.Sin(thetaY), 2) + Math.Pow(Math.Sin(thetaX), 2) + Math.Pow(Math.Cos(thetaX), 2)))) + pts[i - 1].x;
                pts[i].z = (float)(Math.Cos(thetaX) * Math.Sqrt(Math.Pow(dL, 2) / (Math.Pow(a * Math.Sin(thetaY), 2) + Math.Pow(Math.Sin(thetaX), 2) + Math.Pow(Math.Cos(thetaX), 2)))) + pts[i - 1].z;

                lastThetaX = thetaX;
                lastThetaY = thetaY;
            }

            return pts;
        }

        /// <summary>
        /// Method for getting the direction vector at a specific point on the bezier curve
        /// </summary>
        private Vector3 GetDirection(int i)
        {
            if (i == 0)
            {
                return new Vector3(0, 0, 1); // Or whatever is zero slope
            }
            else if (i == chainedPoints.Length - 1)
            {
                return (chainedPoints[i] - chainedPoints[i - 1]).normalized;
            }
            Vector3 dir1 = chainedPoints[i + 1] - chainedPoints[i];
            Vector3 dir2 = chainedPoints[i] - chainedPoints[i - 1];

            return (dir1.normalized + dir2.normalized).normalized; // average both directions
        }

    }
}
