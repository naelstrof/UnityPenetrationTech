using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class Penetrator : CatmullDeformer {
        private static Vector3 localDickUp = Vector3.forward;
        private static Vector3 localDickForward = -Vector3.up;
        private static Vector3 localDickRight = Vector3.right;

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
        public float GetWorldLength() => girthData.GetWorldLength();
        public float GetWorldGirth(float worldDistanceAlongDick) => girthData.GetWorldGirth(worldDistanceAlongDick);
        private float GetPenetratorAngleOffset() {
            Vector3 initialRight = path.GetBinormalFromT(0f);
            Vector3 initialForward = path.GetVelocityFromT(0f).normalized;
            Vector3 initialUp = Vector3.Cross(initialForward, initialRight).normalized;
            Vector3 worldDickUp = rootBone.TransformDirection(localDickUp);
            Vector2 worldDickUpFlat = new Vector2(Vector3.Dot(worldDickUp,initialRight), Vector3.Dot(worldDickUp,initialUp));
            float angle = Mathf.Atan2(worldDickUpFlat.y, worldDickUpFlat.x)-Mathf.PI/2f;
            return angle;
        }
        public Vector3 GetWorldOffset(float worldDistanceAlongDick) {
            // This value is in Z forward, Y up, and X right space-- This is a useful vector because spline space is Z-tangent, Y-normal, X-binormal.
            Vector3 offset = girthData.GetScaledSplineSpaceOffset(worldDistanceAlongDick);

            // Then we find our angle offset to the spline...
            float angle = GetPenetratorAngleOffset();
            offset = Quaternion.AngleAxis(angle,Vector3.forward) * offset;

            // Then we rotate to the spline.
            return path.GetReferenceFrameFromT(path.GetTimeFromDistance(worldDistanceAlongDick)).MultiplyVector(offset);
        }
        protected override void Start() {
            base.Start();
            weights = new List<Vector3>();
            weights.Add(transform.position);
            weights.Add(transform.position+transform.forward*0.5f);
            weights.Add(transform.position+transform.forward*0.5f);
            weights.Add(transform.position+transform.forward);
            path = new CatmullSpline().SetWeights(weights);
            girthData = new GirthData(GetTargetRenderers()[0], rootBone, Vector3.zero, localDickForward, localDickUp);
        }
        protected override void Update() {
            Vector3 holePos = targetHole.GetPath().GetPositionFromT(0f);
            Vector3 holeForward = (targetHole.GetPath().GetVelocityFromT(0f)).normalized;
            ConstructPath(holePos, holeForward);
            if (inserted) {
                targetHole.SetPenetrationDepth(this, Vector3.Distance(rootBone.position,holePos));
            }
            base.Update();
        }

        private void ConstructPath(Vector3 holePos, Vector3 holeForward) {
            float dist = Vector3.Distance(rootBone.position, holePos);
            Vector3 tipPosition = rootBone.position + transform.forward * girthData.GetWorldLength();
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
            weights.Add(rootBone.position);
            weights.Add(PenetratorTangent);
            Vector3 insertionTangent = Vector3.Lerp(
                -transform.forward * girthData.GetWorldLength() * 0.66f, 
                holeForward * dist * 0.66f,
                insertionFactor
            );
            Vector3 insertionPoint = Vector3.Lerp(
                tipPosition + (tipPosition - rootBone.position) * girthData.GetWorldLength() * 0.1f,
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
