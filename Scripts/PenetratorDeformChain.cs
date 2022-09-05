using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PenetrationTech;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace PenetrationTech {
#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PenetratorDeformChain))]
    public class PenetratorChainEditor : PenetratorEditor {
    }
#endif
    [ExecuteAlways]
    public class PenetratorDeformChain : Penetrator {
        [SerializeField] [Tooltip("A set of transforms for the curve to pass through when it's not busy penetrating, useful to tie the penetrator to some physics! Completely optional.")]
        protected Transform[] poseTarget;
        [SerializeField] [Range(0f,1f)] [Tooltip("The tension applied to match the pose target when generating the spline.")]
        protected float poseTension = 0.75f;

        private List<Vector3> reusePositions;
        protected override void OnEnable() {
            reusePositions ??= new List<Vector3>();
            base.OnEnable();
        }

        protected override void ConstructPathForIdle(ICollection<Vector3> output) {
            output.Clear();
            reusePositions.Clear();
            foreach (Transform t in poseTarget) {
                reusePositions.Add(t.position);
            }
            CatmullSpline.GetWeightsFromPoints(output, reusePositions, poseTension);
        }
        protected override void ConstructPathToPenetrable(ICollection<Vector3> output, Penetrable penetrable, out int penetrableSplineIndex) {
            output.Clear();
            if (penetrable == null) {
                penetrableSplineIndex = 1;
                return;
            }
            ConstructPathForIdle(output);
            
            CatmullSpline holeSplinePath = penetrable.GetSplinePath();
            Vector3 holePos = holeSplinePath.GetPositionFromT(0f);
            Vector3 holeForward = holeSplinePath.GetVelocityFromT(0f).normalized;
            var tipPosition = poseTarget[poseTarget.Length - 1].position;
            float fakeDistance = Vector3.Distance(tipPosition, holePos);
            Vector3 penetratorTangent = (tipPosition - poseTarget[poseTarget.Length - 2].position).normalized*(fakeDistance*2f);
            output.Add(tipPosition);
            output.Add(penetratorTangent);
            Vector3 insertionTangent = holeForward * fakeDistance;
            Vector3 insertionPoint = holePos;
            output.Add(insertionTangent);
            output.Add(insertionPoint);
            
            penetrableSplineIndex = output.Count / 4;
            penetrable.GetWeights(output);
            Vector3 outPosition = holeSplinePath.GetPositionFromT(1f);
            Vector3 outTangent = holeSplinePath.GetVelocityFromT(1f).normalized;
            output.Add(outPosition);
            output.Add(outTangent);
            output.Add(outTangent);
            output.Add(outPosition+outTangent*GetWorldLength());
        }

        protected override void CheckValid() {
            reusePositions ??= new List<Vector3>();
            base.CheckValid();
            try {
                AssertValid(poseTarget != null && poseTarget.Length >= 2, "Pose target list needs at least two bones.");
                foreach (var t in poseTarget) {
                    AssertValid(t != null, "Null bone found in the pose target list. This isn't allowed.");
                    foreach (var rendererMask in GetTargetRenderers()) {
                        if (!(rendererMask.renderer is SkinnedMeshRenderer skinnedRenderer)) continue;
                        foreach (var checkBone in skinnedRenderer.bones) {
                            AssertValid(t != checkBone, "You shouldn't deform the skinned mesh renderer bones. Use blank transforms as a pose target instead.");
                        }
                    }
                }
            } catch (PenetratorValidationException error) {
                SetError($"{error.Message}\n\n{error.StackTrace}");
            }
        }
    }
}
