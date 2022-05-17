using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PenetrationTech;

public class CatmullDebugger : MonoBehaviour {
    [SerializeField]
    protected Vector3[] points;
    protected CatmullPath path;
    protected virtual void Start() {
        path = new CatmullPath(points);
    }
    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        if (points == null || points.Length < 2) {
            return;
        }
        if (path == null) {
            path = new CatmullPath(points);
        }
        path.SetWeightsFromPoints(points);
        Vector3 lastPoint = path.GetPositionFromT(0f);
        for (int i=0;i<64;i++) {
            Vector3 newPoint = path.GetPositionFromT((float)i/64f);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }
        Gizmos.color = Color.green;
        for (int i=0;i<points.Length;i++) {
            Gizmos.DrawSphere(points[i], 0.1f);
        }
        //for (int i=0;i<path.arcLength/0.5f;i++) {
            //Gizmos.DrawWireSphere(path.GetPositionFromDistance(Mathf.Repeat(Time.time+i*0.5f, path.arcLength)), 0.1f);
        //}
        Matrix4x4 savedMatrix = Gizmos.matrix;
        int frames = 128;
        for (int i=0;i<frames;i++) {
            // https://en.wikipedia.org/wiki/Frenet%E2%80%93Serret_formulas
            // https://janakiev.com/blog/framing-parametric-curves/
            float t = (float)i/(float)frames;
            Vector3 point = path.GetPositionFromT(t);
            Vector3 tangent = path.GetTangentFromT(t).normalized;
            Vector3 binormal = Vector3.Cross(path.GetTangentFromT(t),path.GetAccelerationFromT(t)).normalized;
            Vector3 normal = Vector3.Cross(binormal, tangent);

            // Change of basis https://math.stackexchange.com/questions/3540973/change-of-coordinates-and-change-of-basis-matrices
            Matrix4x4 BezierBasis = new Matrix4x4();
            BezierBasis.SetRow(0,binormal); // Our X axis
            BezierBasis.SetRow(1,normal); // Y Axis
            BezierBasis.SetRow(2,tangent); // Z Axis
            BezierBasis[3,3] = 1f;
            // Change of basis formula is B = P⁻¹ * A * P, where P is the basis transform.
            Matrix4x4 ToBezierSpace = Matrix4x4.Translate(point)*BezierBasis.inverse;

            Gizmos.matrix = ToBezierSpace;
            //Gizmos.DrawMesh(debugMesh);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Vector3.zero, Vector3.forward*0.2f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(Vector3.zero, Vector3.right*0.2f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(Vector3.zero, Vector3.up*0.2f);
            //Gizmos.DrawLine(point, point+tangent*0.1f);*/
        }
        Gizmos.matrix = savedMatrix;
    }
}
