using UnityEngine;
using System;
using System.Collections;

namespace ShapeSensingNeedle {
    /// <summary>
    /// Generates the mesh of the needle which is update in real time from stream of data.
    /// </summary>
    public class ProcNeedle : ProcBase
    {
        // *** Resolution of generated mesh *** //
        private const int m_RadialSegmentCount = 10;  // Number of radial segments
        private const int m_HeightSegmentCount = 20;  // Number of height segments
        private const float SegmentLength = 0.025f;   // Separation between each point along needle TODO: Won't need this

        // *** Needle dimensions  *** //
        private const float m_Radius = 0.001f;       // Radius of the needle 0.5 mm
        private const float needleLength = 0.14512f;     // Length of needle 145.12 mm
        private const float tipLength = 0.001f;       // Dimensions of needle tip

        // *** Needle coordinates *** //
        //public Vector3 needleBase;                    // Defines position of needle base
        // public Vector3[] points;                    // Points along the needle
        public Vector3[] polyPoints;                  // Polynomial interpolated points
        public Vector3[] chainedPoints;               // Chain interpolated points
        private float ax = 0, bx = 0;         // Coefficients of linear fit to the curavture along needle in xz plane
        private float ay = 0, by = 0;         // Coefficients of linear fit to the curavture along needle in yz plane

        private Vector3 offset;

        // *** Peripheral objects *** //
        //public GameObject needleHandle;
        private TcpClientManager tcp;

        /// <summary>
        /// MonoBehavior method called on start.
        /// </summary>
        public new void Start()
        {
            Debug.Log("Initializing HoloNeedle");
            //needleBase = transform.position;                                  // Define the base location of needle
            //polyPoints = GetPolyFit( m_HeightSegmentCount );
            chainedPoints = GetChainedPoints(m_HeightSegmentCount, ax, ay, bx, by);
            Mesh mesh = BuildMesh();
            GetComponent<MeshFilter>().mesh = mesh;

            // Put on needle handle manually
            //needleHandle.transform.position = needleBase;
            //needleHandle.transform.Translate(new Vector3(0f, 0f, -0.017f));
            //needleHandle.transform.Translate( new Vector3( 0f, 0f, -0.015f ) );
            //needleHandle.transform.Rotate( new Vector3( 0, 90, 0 ) );
            //needleHandle.transform.localScale -= new Vector3( 0.095f, 0.095f, 0.095f );

            //offset = transform.position - camera.transform.position;  // Get distance from the needle to the camera (user)

            // Get tcp object
            tcp = GetComponent<TcpClientManager>();

            Debug.Log("Running HoloNeedle ...");

        }

        /// <summary>
        /// MonoBehavior method that updates at rate of physics engine
        /// </summary>
        void FixedUpdate()
        {
            if (tcp.interlock == 1)
            {
                ax = (float)tcp.ax; bx = (float)tcp.bx;
                ay = (float)tcp.ay; by = (float)tcp.by;
                tcp.interlock--;

                chainedPoints = GetChainedPoints(m_HeightSegmentCount, ax, ay, bx, by);
                //polyPoints = GetPolyFit( m_HeightSegmentCount ); // Interpolate with 4th order polynomial.
                Mesh mesh = BuildMesh();
                GetComponent<MeshFilter>().mesh = mesh;          // Set the mesh so that it appears in the graphics
            }
        }

        /// <summary>
        /// MonoBehavior method that updates after all physics and translations have happened
        /// </summary>
        void LateUpdate()
        {
            //transform.position = camera.transform.position + offset; // Move the needle with the camera
        }

        /// <summary>
        /// MonoBehavior method can be used to perform any actions before quitting application.
        /// </summary>
        void OnApplicationQuit()
        {
            Debug.Log("Application ending after " + Time.time + " seconds");
        }

        /// <summary>
        /// Method for building a mesh. Called in Start(). For old method using polyPoints
        /// </summary>
        //public override Mesh BuildMesh()
        //{
        //    //Create a new mesh builder:
        //    MeshBuilder meshBuilder = new MeshBuilder();

        //    for (int i = 0; i < polyPoints.Length; i++)
        //    {
        //        Vector3 centrePos = polyPoints[i];
        //        float v = (float)i / m_HeightSegmentCount;
        //        Quaternion rot = Quaternion.FromToRotation( Vector3.up, GetDirection( v ) );
        //        BuildRing( meshBuilder, m_RadialSegmentCount, centrePos, m_Radius, v, i > 0, rot );
        //    }

        //    BuildTip( meshBuilder, m_RadialSegmentCount, polyPoints[polyPoints.Length - 1], m_Radius, GetDirection( 1 ), tipLength );

        //    return meshBuilder.CreateMesh();
        //}

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

            BuildTip(meshBuilder, m_RadialSegmentCount, chainedPoints[chainedPoints.Length - 1], m_Radius, GetDirection(chainedPoints.Length - 1), tipLength);

            return meshBuilder.CreateMesh();
        }

        /// <summary>
        /// Method for making Bezier points with several interpolated points
        /// </summary>
        public static Vector3[] GetBezierPoints(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int numPoints)
        {
            Vector3[] ret = new Vector3[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                float t = (float)i / numPoints;
                t = Mathf.Clamp01(t);
                float oneMinusT = 1f - t;
                Vector3 pt =
                    oneMinusT * oneMinusT * oneMinusT * p0 +
                    3f * oneMinusT * oneMinusT * t * p1 +
                    3f * oneMinusT * t * t * p2 +
                    t * t * t * p3;
                ret[i] = pt;
            }
            return ret;
        }

        /// <summary>
        /// Method for making polynomial interpolated needle shape.
        /// </summary>
	    /// <param name="numPoints">Number of points to be drawn</param>
        //private Vector3[] GetPolyFit( int numPoints )
        //{
        //    Vector3[] ret = new Vector3[numPoints];
        //    for (int i = 0; i < numPoints; i++)
        //    {
        //        float t = (float)i / numPoints;
        //        t = Mathf.Clamp01( t );            // Ensure that values stay within 0-1
        //        double zi = (needleLength * t); // In meters
        //        double zi_mm = zi * 1000;       // In mm
        //        double xz = ax / 12 * Math.Pow( zi_mm, 4.0f ) + bx / 6 * Math.Pow( zi_mm, 3.0f ) + cx / 2 * Math.Pow( zi_mm, 2.0f );// + dx * Math.Pow(zi, 1.0f);
        //        double yz = ay / 12 * Math.Pow( zi_mm, 4.0f ) + by / 6 * Math.Pow( zi_mm, 3.0f ) + cy / 2 * Math.Pow( zi_mm, 2.0f );// + dy * Math.Pow(zi, 1.0f);
        //        float xz_m = (float)xz / 1000f; // xz in meters
        //        float yz_m = (float)yz / 1000f; // yz in meters
        //        //ret[i] = needleBase + new Vector3((-1.0f)*(float) xz, ((float) yz), (float) zi); // The negative one is just a temporary fix. Might be calibration mistake.
        //        //ret[i] = needleBase + new Vector3((float)xz, (float)yz, (float)zi);
        //        float z_stretch = 1f; // stretch the needle to match actual length
        //        ret[i] = new Vector3( -yz_m, xz_m, (float)zi * z_stretch );
        //    }
        //    return ret;
        //}

        private Vector3[] GetChainedPoints(int numPoints, float ax, float ay, float bx, float by)
        {
            //Vector2[] Zx = new Vector2[numPoints];
            //Vector2[] Zy = new Vector2[numPoints];
            Vector3[] pts = new Vector3[numPoints+1];
            float lastThetaX = 0;
            float lastThetaY = 0;
            float dL = needleLength / numPoints;

            // We start at (0,0,0)
            //Zx[0] = new Vector2(0, 0);
            //Zy[0] = new Vector2(0, 0);

            for (int i = 1; i <= numPoints; i++)
            {
                float z = i * dL;
                float Kx = z * ax + bx - ax / 2 * dL;
                float thetaX = Kx * dL + lastThetaX;
                //Zx[i].x = dL * ( (float)Math.Sin(thetaX) ) + Zx[i - 1].x; // This is x
                //Zx[i].y = dL * ((float)Math.Cos(thetaX)) + Zx[i - 1].y; // This is Z don't get confused by y


                float Ky = z * ay + by - ay / 2 * dL;
                float thetaY = Ky * dL + lastThetaY;
                //Zy[i].x = dL * ((float)Math.Sin(thetaY)) + Zy[i - 1].x; // This is Y
                //Zy[i].y = dL * ((float)Math.Cos(thetaY)) + Zy[i - 1].y; // This is Z don't get confused by y

                // Note: estimated X coordinates get assigned to Y and estimated Y coordinates get assigned to X
                // because Unity works in left hand rule and calibration was done with right hand rule.
                double a = Math.Cos(thetaX) / Math.Cos(thetaY);
                pts[i].y = (float)(Math.Sin(thetaX) * Math.Sqrt(Math.Pow(dL, 2) / (Math.Pow(a * Math.Sin(thetaY), 2) + Math.Pow(Math.Sin(thetaX), 2) + Math.Pow(Math.Cos(thetaX), 2)))) + pts[i - 1].y;
                pts[i].x = (float)(a * Math.Sin(thetaY) * Math.Sqrt(Math.Pow(dL, 2) / (Math.Pow(a * Math.Sin(thetaY), 2) + Math.Pow(Math.Sin(thetaX), 2) + Math.Pow(Math.Cos(thetaX), 2)))) + pts[i - 1].x;
                pts[i].z = (float)(Math.Cos(thetaX) * Math.Sqrt(Math.Pow(dL, 2) / (Math.Pow(a * Math.Sin(thetaY), 2) + Math.Pow(Math.Sin(thetaX), 2) + Math.Pow(Math.Cos(thetaX), 2)))) + pts[i - 1].z;

                lastThetaX = thetaX;
                lastThetaY = thetaY;
            }
            // Shorten the needle here by doing a poly fit on both sets.

            return pts;
        }

        /// <summary>
        /// Method for getting the direction vector at a specific point on the bezier curve
        /// </summary>
        //private Vector3 GetDirection( float t )
        //{
        //    //Vector3 velocity = transform.TransformPoint(GetFirstDerivative(t))
        //    //                    - transform.position;
        //    Vector3 velocity = GetFirstDerivative( t );
        //    return velocity.normalized;
        //}

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

        /// <summary> TODO
        /// Method for getting the derivative of the 4th order polynomial curve at a certain point
        /// </summary>
        //private Vector3 GetFirstDerivative( float t )
        //{
        //    t = Mathf.Clamp01( t );
        //    double zi = needleLength * t;
        //    float xi = (float)(4 * ax * Math.Pow( zi, 3 ) + 3 * bx * Math.Pow( zi, 2 ) + 2 * cx * zi);
        //    float yi = (float)(4 * ay * Math.Pow( zi, 3 ) + 3 * by * Math.Pow( zi, 2 ) + 2 * cy * zi);
        //    return new Vector3( xi, yi, 1 ); // TODO BIG TODOOO: this is wrong for now
        //}
    }
}
