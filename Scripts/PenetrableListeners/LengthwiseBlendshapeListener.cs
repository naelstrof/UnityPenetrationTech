using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace PenetrationTech {
    [System.Serializable]
    //[PenetrableListener(typeof(LengthwiseBlendshapeListener), "Lengthwise Blendshape Listener (depth rather than girth)")]
    public class LengthwiseBlendshapeListener : PenetrableListener {
        [SerializeField]
        SkinnedMeshBlendshapePair[] targets;
        [SerializeField][Tooltip("The distance from the start of the listener to how far the blendshape triggers.")]
        float blendShapeTDepth;
        [SerializeField][Tooltip("Allows the blendshape to trigger past 100%.")]
        private bool overdrive;

        private Penetrable penetrable;
        public override void OnEnable(Penetrable p) {
            base.OnEnable(p);
            penetrable = p;
            foreach(SkinnedMeshBlendshapePair target in targets) {
                target.OnEnable();
            }
        }
        protected override void OnPenetrationDepthChange(float newDepth) {
            base.OnPenetrationDepthChange(newDepth);
            CatmullSpline spline = penetrable.GetPath();
            float dist = spline.GetDistanceFromTime(t+blendShapeTDepth) - spline.GetDistanceFromTime(t);
            foreach(SkinnedMeshBlendshapePair target in targets) {
                target.skinnedMeshRenderer.SetBlendShapeWeight(target.blendshapeID, overdrive ? (newDepth/dist)*100f : Mathf.Clamp01(newDepth/dist)*100f);
            }
        }

        public override void OnDrawGizmosSelected(Penetrable p) {
            base.OnDrawGizmosSelected(p);
            #if UNITY_EDITOR
            CatmullSpline path = p.GetPath();
            Vector3 position = path.GetPositionFromT(t);
            Vector3 tangent = path.GetVelocityFromT(t).normalized;
            Vector3 normal = path.GetBinormalFromT(t).normalized;
            
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawWireDisc(position, tangent, 0.025f);
            UnityEditor.Handles.DrawLine(position, position + tangent * 0.1f);
            UnityEditor.Handles.DrawWireArc(position + tangent * 0.05f, normal, -tangent, 90f, 0.05f);
            UnityEditor.Handles.DrawWireArc(position + tangent * 0.05f, normal, -tangent, -90f, 0.05f);

            Vector3 endPosition = path.GetPositionFromT(blendShapeTDepth+t);
            Vector3 endTangent = path.GetVelocityFromT(blendShapeTDepth+t).normalized;
            Vector3 endNormal = path.GetBinormalFromT(blendShapeTDepth+t).normalized;
            UnityEditor.Handles.DrawWireDisc(endPosition, -endTangent, 0.025f);
            UnityEditor.Handles.DrawLine(endPosition, endPosition - endTangent * 0.1f);
            UnityEditor.Handles.DrawWireArc(endPosition - endTangent * 0.05f, endNormal, endTangent, 90f, 0.05f);
            UnityEditor.Handles.DrawWireArc(endPosition - endTangent * 0.05f, endNormal, endTangent, -90f, 0.05f);
            #endif
        }

        public override void AssertValid() {
            base.AssertValid();
            if (targets == null || targets.Length == 0) {
                throw new PenetrableListenerValidationException(
                    $"Need at least one target renderer for listener {this}.");
            }

            foreach (var target in targets) {
                if (target.skinnedMeshRenderer == null) {
                    throw new PenetrableListenerValidationException($"SkinnedMeshRenderer on listener {this} is null.");
                }
            }
        }

        public override void NotifyPenetration(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenisRoot, Penetrable.SetClipDistanceAction clipAction) {
            NotifyPenetrationGDO(penetrable, penetrator, worldSpaceDistanceToPenisRoot, clipAction, PenData.Depth);
        }
    }
}
