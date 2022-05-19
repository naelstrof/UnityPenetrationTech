using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class Penetrator : CatmullDeformer {
        private List<Vector3> points = new List<Vector3>();
        [SerializeField]
        private Penetrable targetHole;
        protected override void Start() {
            base.Start();
            points = new List<Vector3>();
            points.Add(transform.position);
            points.Add(transform.position+transform.forward);
            path = new CatmullPath(points);
        }
        protected override void Update() {
            points.Clear();
            points.Add(transform.position);
            points.AddRange(targetHole.GetPath().GetPoints());
            path.SetWeightsFromPoints(points);
            base.Update();
        }
    }

}
