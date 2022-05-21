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
            Vector3 tipPosition = transform.position + transform.forward * girthData.GetWorldLength();
            weights.Clear();
            if (inserted) {
                insertionFactor = 1f;
                if (dist > girthData.GetWorldLength()) inserted = false;
            } else {
                insertionFactor = Mathf.MoveTowards(insertionFactor, 0f, Time.deltaTime * 4f);
                insertionFactor = Mathf.Max(
                    insertionFactor,
                    Mathf.Clamp01(2f - Vector3.Distance(tipPosition, holePos) / (girthData.GetWorldLength() * 0.4f) * 2f)
                );
                if (insertionFactor >= 0.99f) inserted = true;
            }

            Vector3 PenetratorTangent = Vector3.Lerp(
                transform.forward * girthData.GetWorldLength() * 0.66f,
                transform.forward * dist * 0.66f,
                insertionFactor
            );
            weights.Add(transform.position);
            weights.Add(PenetratorTangent);
            Vector3 insertionTangent = Vector3.Lerp(
                -transform.forward * girthData.GetWorldLength() * 0.66f, 
                holeForward * dist * 0.66f,
                insertionFactor
            );
            Vector3 insertionPoint = Vector3.Lerp(
                tipPosition + (tipPosition - transform.position) * girthData.GetWorldLength() * 0.1f,
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
