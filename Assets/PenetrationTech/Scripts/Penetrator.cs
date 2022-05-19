using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class Penetrator : CatmullDeformer {
        private List<Vector3> weights = new List<Vector3>();
        [SerializeField]
        private Penetrable targetHole;
        protected override void Start() {
            base.Start();
            weights = new List<Vector3>();
            weights.Add(transform.position);
            weights.Add(transform.position+transform.forward*0.5f);
            weights.Add(transform.position+transform.forward*0.5f);
            weights.Add(transform.position+transform.forward);
            path = new CatmullSpline().SetWeights(weights);
        }
        protected override void Update() {
            Vector3 holePos = targetHole.GetPath().GetPositionFromT(0f);
            Vector3 holeForward = (targetHole.GetPath().GetVelocityFromT(0f)).normalized;
            float dist = Vector3.Distance(transform.position, holePos);

            weights.Clear();
            weights.Add(transform.position);
            weights.Add(transform.forward*dist);
            weights.Add(holeForward*dist);
            weights.Add(holePos);
            weights.AddRange(targetHole.GetPath().GetWeights());
            path.SetWeights(weights);
            base.Update();
        }
    }

}
