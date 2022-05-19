using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class CatmullDisplay : CatmullBehaviour {
        protected virtual void OnDrawGizmosSelected() {
            Gizmos.color = Color.red;
            if (path == null || path.GetPoints().Count < 2) {
                return;
            }
            Vector3 lastPoint = path.GetPositionFromT(0f);
            for (int i=0;i<64;i++) {
                Vector3 newPoint = path.GetPositionFromT((float)i/64f);
                Gizmos.DrawLine(lastPoint, newPoint);
                lastPoint = newPoint;
            }
            Gizmos.color = Color.green;
            foreach(Vector3 point in path.GetPoints()) {
                Gizmos.DrawSphere(point, 0.1f);
            }
            /*Matrix4x4 savedMatrix = Gizmos.matrix;
            int frames = 32;
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
            Gizmos.matrix = savedMatrix;*/
        }
    }
}
