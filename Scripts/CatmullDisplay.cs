using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class CatmullDisplay : CatmullBehaviour {
        protected virtual void OnDrawGizmosSelected() {
            Gizmos.color = Color.red;
            if (path == null || path.GetWeights().Count < 4) {
                return;
            }
            Vector3 lastPoint = path.GetPositionFromT(0f);
            for (int i=0;i<64;i++) {
                Vector3 newPoint = path.GetPositionFromT((float)i/64f);
                Gizmos.DrawLine(lastPoint, newPoint);
                lastPoint = newPoint;
            }
            for (int i=0;i<path.GetWeights().Count;i+=4) {
                Gizmos.color = new Color(0,1,0,0.25f);
                Gizmos.DrawSphere(path.GetWeights()[i], 0.02f);
                Gizmos.DrawSphere(path.GetWeights()[i+3], 0.02f);
                
                //Gizmos.color = Color.blue;
                //Gizmos.DrawLine(path.GetWeights()[i], path.GetWeights()[i]+path.GetWeights()[i+1]);
                //Gizmos.DrawWireSphere(path.GetWeights()[i]+path.GetWeights()[i+1], 0.01f);
                //Gizmos.DrawWireSphere(path.GetWeights()[i+3]+path.GetWeights()[i+2], 0.01f);
                //Gizmos.DrawLine(path.GetWeights()[i+3], path.GetWeights()[i+3]+path.GetWeights()[i+2]);
            }
            /*for (int i=0;i<path.GetBounds().Count;i++) {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(path.GetBounds()[i].center, path.GetBounds()[i].size);
            }*/
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
