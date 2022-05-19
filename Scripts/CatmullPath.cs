using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class CatmullPath : CatmullSpline {
        private List<Vector3> points;
        public System.Collections.ObjectModel.ReadOnlyCollection<Vector3> GetPoints() => points.AsReadOnly();
        public CatmullPath() : base() {
            points = new List<Vector3>();
        }
        public CatmullPath SetWeightsFromPoints(ICollection<Vector3> newPoints) {
            points.Clear();
            points.AddRange(newPoints);
            UnityEngine.Assertions.Assert.IsTrue(points.Count>1);
            weights.Clear();
            for (int i=0;i<points.Count-1;i++) {
                Vector3 p0 = points[i];
                Vector3 p1 = points[i+1];

                Vector3 m0;
                if (i==0) {
                    m0 = (p1 - p0)*0.5f;
                } else {
                    m0 = (p1 - points[i-1])*0.5f;
                }
                Vector3 m1;
                if (i < points.Count - 2) {
                    m1 = (points[(i + 2) % points.Count] - p0)*0.5f;
                } else {
                    m1 = (p1 - p0)*0.5f;
                }
                weights.Add(p0);
                weights.Add(m0);
                weights.Add(m1);
                weights.Add(p1);
            }
            GenerateDistanceLUT(32);
            GenerateBinormalLUT(16);
            return this;
        }
    }
}
