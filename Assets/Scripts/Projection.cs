using UnityEngine;
using System;
using System.Collections;

namespace ShapeSensing {
    /// <summary>
    /// Generates the mesh of the needle which is update in real time from stream of data.
    /// </summary>
    public class Projection : ProcBase
    {
        // *** Resolution of generated mesh *** //
        private const int m_RadialSegmentCount = 10;  // Number of radial segments
        private const int m_HeightSegmentCount = 20;  // Number of height segments

        // *** Needle dimensions  *** //
        public float m_Radius = 0.001f;         // Radius of the needle 0.5 mm
        public float needleLength = 0.14512f;   // Length of needle 145.12 mm

        // *** Needle coordinates *** //
        private Vector3[] chainedPoints;               // Chain interpolated points
        private float ax = 0, bx = 0;         // Coefficients of linear fit to the curavture along needle in xz plane
        private float ay = 0, by = 0;         // Coefficients of linear fit to the curavture along needle in yz plane

        /// <summary>
        /// MonoBehavior method called on start.
        /// </summary>
        public new void Start()
        {
            Debug.Log("Initializing HoloNeedle");
            chainedPoints = GetChainedPoints(m_HeightSegmentCount, ax, ay, bx, by);
            Mesh mesh = BuildMesh();
            GetComponent<MeshFilter>().mesh = mesh;
            GetComponent<MeshCollider>().inflateMesh = false;
        }

        /// <summary>
        /// MonoBehavior method that updates at rate of physics engine
        /// </summary>
        void LateUpdate()
        {
            Vector3 direction = this.transform.forward;
            Ray ray = new Ray(this.transform.position - direction * 0.25f, direction);
            RaycastHit hit;
            if (Physics.Raycast(this.transform.position - direction * 0.25f, direction, out hit))
            {
                if (hit.transform.gameObject.name == "Target")
                {
                    hit.transform.gameObject.GetComponent<Renderer>().material.color = Color.red;
                }

            }
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
                float v = (float)i / m_HeightSegmentCount;
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, GetDirection(i));
                BuildRing(meshBuilder, m_RadialSegmentCount, centrePos, m_Radius, v, i > 0, rot);
            }

            return meshBuilder.CreateMesh();
        }


        private Vector3[] GetChainedPoints(int numPoints, float ax, float ay, float bx, float by)
        {
            Vector3[] pts = new Vector3[numPoints+1];
            float lastThetaX = 0;
            float lastThetaY = 0;
            float dL = needleLength / numPoints;

            for (int i = 1; i <= numPoints; i++)
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
