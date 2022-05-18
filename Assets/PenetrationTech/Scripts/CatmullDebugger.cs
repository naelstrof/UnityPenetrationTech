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
        Matrix4x4 savedMatrix = Gizmos.matrix;
        int frames = 128;
        for (int i=0;i<frames;i++) {
            float t = (float)i/(float)frames;
            Gizmos.matrix = path.GetReferenceFrameFromT(t);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(Vector3.zero, Vector3.forward*0.2f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(Vector3.zero, Vector3.right*0.2f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(Vector3.zero, Vector3.up*0.2f);
        }
        Gizmos.matrix = savedMatrix;
    }
}
