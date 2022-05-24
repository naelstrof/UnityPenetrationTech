using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class Penetrator : CatmullDeformer {
        private List<Vector3> weights = new List<Vector3>();
        [SerializeField]
        private GirthData girthData;
        [SerializeField]
        private Penetrable targetHole;
        private float length;
        private bool inserted;
        private float insertionFactor;
        public float GetGirthScaleFactor() => girthData.GetGirthScaleFactor();
        public float GetWorldLength() => girthData.GetWorldLength();
        public float GetWorldGirthRadius(float worldDistanceAlongDick) => girthData.GetWorldGirthRadius(worldDistanceAlongDick);
        public RenderTexture GetGirthMap() => girthData.GetGirthMap();
        public float GetPenetratorAngleOffset() {
            Vector3 initialRight = path.GetBinormalFromT(0f);
            Vector3 initialForward = path.GetVelocityFromT(0f).normalized;
            Vector3 initialUp = Vector3.Cross(initialForward, initialRight).normalized;
            Vector3 worldDickUp = rootBone.TransformDirection(localRootUp).normalized;
            Vector2 worldDickUpFlat = new Vector2(Vector3.Dot(worldDickUp,initialRight), Vector3.Dot(worldDickUp,initialUp));
            float angle = Mathf.Atan2(worldDickUpFlat.y, worldDickUpFlat.x)-Mathf.PI/2f;
            return angle;
        }
        public Vector3 GetWorldOffset(float worldDistanceAlongDick) {
            Vector3 offset = girthData.GetScaledSplineSpaceOffset(worldDistanceAlongDick);

            // Then we find our angle offset to the spline...
            // Then we rotate to the spline.
            float t = path.GetTimeFromDistance(worldDistanceAlongDick);
            float angle = GetPenetratorAngleOffset();
            return Quaternion.AngleAxis(angle*Mathf.Rad2Deg,path.GetVelocityFromT(t).normalized) * path.GetReferenceFrameFromT(t).MultiplyVector(offset);
        }
        protected override void Start() {
            base.Start();
            weights = new List<Vector3>();
            weights.Add(transform.position);
            weights.Add(transform.position+transform.forward*0.5f);
            weights.Add(transform.position+transform.forward*0.5f);
            weights.Add(transform.position+transform.forward);
            path = new CatmullSpline().SetWeights(weights);
            girthData = new GirthData(GetTargetRenderers()[0], rootBone, Vector3.zero, localRootForward, localRootUp, localRootRight);
        }
        protected override void Update() {
            Vector3 holePos = targetHole.GetPath().GetPositionFromT(0f);
            Vector3 holeForward = (targetHole.GetPath().GetVelocityFromT(0f)).normalized;
            ConstructPath(holePos, holeForward);
            if (inserted) {
                float firstArcLength = path.GetDistanceFromTime(1f/(float)(weights.Count/4));
                //targetHole.SetPenetrationDepth(this, Vector3.Distance(rootBone.position,holePos));
                targetHole.SetPenetrationDepth(this, firstArcLength);
            }
            base.Update();
        }

        private void ConstructPath(Vector3 holePos, Vector3 holeForward) {
            float dist = Vector3.Distance(rootBone.position, holePos);
            Vector3 tipPosition = rootBone.position + rootBone.TransformDirection(localRootForward) * girthData.GetWorldLength();
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
                rootBone.TransformDirection(localRootForward) * girthData.GetWorldLength() * 0.66f,
                rootBone.TransformDirection(localRootForward) * dist * 0.66f,
                insertionFactor
            );
            weights.Add(rootBone.position);
            weights.Add(PenetratorTangent);
            Vector3 insertionTangent = Vector3.Lerp(
                -rootBone.TransformDirection(localRootForward) * girthData.GetWorldLength() * 0.66f, 
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

        protected override void OnDrawGizmosSelected() {
            base.OnDrawGizmosSelected();
            #if UNITY_EDITOR
            if (Application.isPlaying) {
                for(float t=0;t<GetWorldLength();t+=0.025f) {
                    UnityEditor.Handles.color = Color.white;
                    Vector3 position = path.GetPositionFromDistance(t) + GetWorldOffset(t);
                    float girth = GetWorldGirthRadius(t);
                    UnityEditor.Handles.DrawWireDisc(position, path.GetVelocityFromDistance(t).normalized, girth);
                }
            }
            #endif
        }
        
    }

}
