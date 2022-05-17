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
        for (int i=0;i<path.arcLength/0.5f;i++) {
            Gizmos.DrawWireSphere(path.GetPositionFromDistance(Mathf.Repeat(Time.time+i*0.5f, path.arcLength)), 0.1f);
        }
    }
}
