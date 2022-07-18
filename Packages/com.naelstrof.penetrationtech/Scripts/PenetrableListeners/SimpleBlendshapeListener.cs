using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace PenetrationTech {
    [System.Serializable]
    //[PenetrableListener(typeof(SimpleBlendshapeListener), "Simple Blendshape Listener")]
    public class SimpleBlendshapeListener : BoneTransformListener {
        [SerializeField]
        SkinnedMeshBlendshapePair[] targets;

        [SerializeField][Tooltip("How open the hole is by default. Important for donuts or mouths where they can stay open without a penetration.")]
        private float baseGirthRadius;
        [FormerlySerializedAs("blendShapeGirth")][SerializeField][Tooltip("How open the hole is when the blendshape is fully triggered.")]
        float blendShapeGirthRadius;
        [SerializeField][Tooltip("Allows the blendshape to be triggered past 100%.")]
        private bool overdrive = true;
        public override void OnEnable(Penetrable p) {
            base.OnEnable(p);
            foreach(SkinnedMeshBlendshapePair target in targets) {
                target.OnEnable();
            }
        }
        protected override void OnPenetrationGirthRadiusChange(float newGirthRadius) {
            base.OnPenetrationGirthRadiusChange(newGirthRadius);
            foreach(SkinnedMeshBlendshapePair target in targets) {
                float triggerAmount = (newGirthRadius-baseGirthRadius) / (blendShapeGirthRadius*offsetCorrectionBone.lossyScale.x);
                target.skinnedMeshRenderer.SetBlendShapeWeight(target.blendshapeID, overdrive ? Mathf.Max(triggerAmount * 100f,0f) : Mathf.Clamp01(triggerAmount)*100f);
            }
        }
        public override void OnDrawGizmosSelected(Penetrable p) {
            base.OnDrawGizmosSelected(p);
            #if UNITY_EDITOR
            CatmullSpline path = p.GetSplinePath();
            Vector3 position = path.GetPositionFromT(t);
            Vector3 normal = path.GetVelocityFromT(t).normalized;
            UnityEditor.Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(position, normal, blendShapeGirthRadius);
            UnityEditor.Handles.color = Color.grey;
            UnityEditor.Handles.DrawWireDisc(position, normal, baseGirthRadius);
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
            NotifyPenetrationGDO(penetrable, penetrator, worldSpaceDistanceToPenisRoot, clipAction, PenData.Girth | PenData.Offset);
        }
    }
}
