using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class CatmullDisplay : CatmullBehaviour {
        //[Range(0f,1f)]
        //public float slider = 0f;
        protected virtual void OnDrawGizmos() {
            Gizmos.color = Color.red;
            var path = GetPath();
            Vector3 lastPoint = path.GetPositionFromT(0f);
            for (int i=0;i<64;i++) {
                Vector3 newPoint = path.GetPositionFromT((float)i/64f);
                Gizmos.DrawLine(lastPoint, newPoint);
                lastPoint = newPoint;
            }
            
            // Bounding boxes
            //Gizmos.color = Color.green;
            //List<Bounds> bounds = path.GetBounds();
            //foreach (var bound in bounds) {
                //Gizmos.DrawWireCube(bound.center, bound.size);
            //}
            
            // Continuity/orientation checker
            //Gizmos.color = Color.blue;
            //Vector3 cubePosition = path.GetPositionFromT(slider);
            //Matrix4x4 frameReference = path.GetReferenceFrameFromT(slider);
            //Matrix4x4 savedMatrix = Gizmos.matrix;
            //Gizmos.matrix = Matrix4x4.Translate(cubePosition)*frameReference;
            //Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 0.5f);
            //Gizmos.matrix = savedMatrix;
        }
    }
}
