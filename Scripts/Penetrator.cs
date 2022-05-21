using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class Penetrator : CatmullDeformer {
        private List<Vector3> weights = new List<Vector3>();
        [SerializeField]
        private Transform rootBone;
        [SerializeField]
        private GirthData girthData;
        [SerializeField]
        private Penetrable targetHole;
        private float length;
        private bool inserted;
        private float insertionFactor;
        protected override void Start() {
            base.Start();
            weights = new List<Vector3>();
            weights.Add(transform.position);
            weights.Add(transform.position+transform.forward*0.5f);
            weights.Add(transform.position+transform.forward*0.5f);
            weights.Add(transform.position+transform.forward);
            path = new CatmullSpline().SetWeights(weights);
            girthData = new GirthData(GetTargetRenderers()[0], rootBone, Vector3.zero, -Vector3.up, Vector3.forward);
        }
        protected override void Update() {
            ConstructPath();
            base.Update();
        }

        private void ConstructPath() {
            Vector3 holePos = targetHole.GetPath().GetPositionFromT(0f);
            Vector3 holeForward = (targetHole.GetPath().GetVelocityFromT(0f)).normalized;
            float dist = Vector3.Distance(transform.position, holePos);
            weights.Clear();
            Vector3 PenetratorTangent = Vector3.Lerp(
                transform.forward * girthData.GetWorldLength() * 0.5f,
                transform.forward * dist * 0.5f,
                insertionFactor
            );
            weights.Add(transform.position);
            weights.Add(PenetratorTangent);
            Vector3 insertionTangent = Vector3.Lerp(
                -transform.forward * dist * 0.5f, holeForward * dist * 0.5f,
                insertionFactor);
            //Debug.Log(Vector3.forward * girthData.GetWorldLength());
            Vector3 insertionPoint = Vector3.Lerp(
                transform.TransformPoint(Vector3.forward * girthData.GetWorldLength()),
                holePos,
                insertionFactor
                );
            weights.Add(insertionTangent);
            weights.Add(insertionPoint);
            if (inserted) {
                weights.AddRange(targetHole.GetPath().GetWeights());
            }
            path.SetWeights(weights);
        }
        
    }

}
