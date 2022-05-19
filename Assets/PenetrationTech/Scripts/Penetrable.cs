using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class Penetrable : CatmullDisplay {
        [SerializeField]
        private Vector3[] points;
        private List<Vector3> worldPoints;
        void Start() {
            worldPoints = new List<Vector3>();
            foreach(Vector3 point in points) {
                worldPoints.Add(transform.TransformPoint(point));
            }
            path = new CatmullPath().SetWeightsFromPoints(worldPoints);
        }
        // This is all really nasty, it'd be nice if I can just set a transform on the path directly.
        private void CheckUpdate() {
            if (worldPoints == null) {
                worldPoints = new List<Vector3>();
            }
            if(transform.hasChanged) {
                worldPoints.Clear();
                for(int i=0;i<points.Length;i++) {
                    worldPoints.Add(transform.TransformPoint(points[i]));
                }
                if (path == null) {
                    path = new CatmullPath().SetWeightsFromPoints(worldPoints);
                } else {
                    (path as CatmullPath).SetWeightsFromPoints(worldPoints);
                }
                transform.hasChanged = false;
            }
        }
        void Update() {
            CheckUpdate();
        }
        protected override void OnDrawGizmosSelected() {
            CheckUpdate();
            base.OnDrawGizmosSelected();
        }
        void OnValidate() {
            transform.hasChanged = true;
        }
    }
}
