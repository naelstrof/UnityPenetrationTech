using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace PenetrationTech {
    [System.Serializable]
    //[PenetrableListener(typeof(PushPullExpandBlendshapeListener), "Push Pull Expand Blendshape Listener")]
    public class PushPullExpandBlendshapeListener : BoneTransformListener {
        [SerializeField]
        private SkinnedMeshBlendshapePushPullExpandSet[] targets;

        [SerializeField][Tooltip("How open the hole is with the blendshape set to 0.")]
        private float baseGirthRadius = 0f;
        [FormerlySerializedAs("blendShapeGirth")] [SerializeField][Tooltip("How open the hole is with the blendshape fully triggered")]
        private float blendShapeGirthRadius = 0.02f;
        [SerializeField][Tooltip("The distance that the hole travels along the curve when the Pull blendshape is fully triggered.")]
        private float pullT;
        [SerializeField][Tooltip("The distance that the hole travels along the curve when the Push blendshape is fully triggered.")]
        private float pushT;
        
        [SerializeField][Tooltip("Allows the expand blendshape to be triggered past 100%.")]
        private bool overdrive;
        
        private float pullPushAmount;
        private float lastPenetrationDepth;
        public override void OnEnable(Penetrable p) {
            base.OnEnable(p);
            
            foreach(SkinnedMeshBlendshapePushPullExpandSet target in targets) {
                target.OnEnable();
            }

            pullPushAmount = 0f;
            lastPenetrationDepth = 0f;
        }
        protected override void OnPenetrationGirthRadiusChange(float newGirthRadius) {
            base.OnPenetrationGirthRadiusChange(newGirthRadius);
            foreach(SkinnedMeshBlendshapePushPullExpandSet target in targets) {
                float triggerAmount = (newGirthRadius-baseGirthRadius) / (blendShapeGirthRadius*offsetCorrectionBone.lossyScale.x);
                target.skinnedMeshRenderer.SetBlendShapeWeight(target.expandBlendshapeID, overdrive ? Mathf.Max(triggerAmount * 100f,0f) : Mathf.Clamp01(triggerAmount)*100f);
            }
        }
        protected override void OnPenetrationDepthChange(float newDepth) {
            base.OnPenetrationDepthChange(newDepth);
            float diff = newDepth - lastPenetrationDepth;
            pullPushAmount += diff * 10f;
            pullPushAmount = Mathf.Clamp(pullPushAmount, -1f, 1f);
            lastPenetrationDepth = newDepth;
        }
        public override void OnValidate(Penetrable p) {
            base.OnValidate(p);
            targets ??= Array.Empty<SkinnedMeshBlendshapePushPullExpandSet>();
            pullT = Mathf.Min(pullT, 0f);
            pushT = Mathf.Max(pushT, 0f);
        }
        protected override float GetT(Penetrable p) {
            float pushAmount = Mathf.Clamp01(pullPushAmount);
            float pullAmount = Mathf.Clamp01(-pullPushAmount);
            float d = Mathf.Lerp(t, t+pushT, pushAmount);
            d = Mathf.Lerp(d, t-pullT, pullAmount);
            return d;
        }
        public override void Update() {
            base.Update();
            pullPushAmount = Mathf.MoveTowards(pullPushAmount, 0f, Time.deltaTime*1f);
            foreach(SkinnedMeshBlendshapePushPullExpandSet target in targets) {
                float pushAmount = Mathf.Clamp01(pullPushAmount);
                target.skinnedMeshRenderer.SetBlendShapeWeight(target.pushBlendshapeID, pushAmount*100f);
                float pullAmount = Mathf.Clamp01(-pullPushAmount);
                target.skinnedMeshRenderer.SetBlendShapeWeight(target.pullBlendshapeID, pullAmount*100f);
            }
        }

        public override void AssertValid() {
            base.AssertValid();
            if (targets == null || targets.Length == 0) {
                throw new PenetrableListenerValidationException($"Need at least one target set on listener {this}");
            }

            foreach (var target in targets) {
                if (target.skinnedMeshRenderer == null) {
                    throw new PenetrableListenerValidationException($"Can't have a null renderer on listener {this}");
                }
            }
        }

        public override void OnDrawGizmosSelected(Penetrable p) {
            base.OnDrawGizmosSelected(p);
            #if UNITY_EDITOR
            CatmullSpline path = p.GetPath();
            Vector3 position = path.GetPositionFromT(t);
            Vector3 normal = path.GetVelocityFromT(t).normalized;
            UnityEditor.Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(position, normal, blendShapeGirthRadius);

            Vector3 tugPosition = position + normal * pullT * path.arcLength;
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(tugPosition, normal, blendShapeGirthRadius);

            Vector3 pushPosition = position + normal * pushT * path.arcLength;
            UnityEditor.Handles.color = Color.magenta;
            UnityEditor.Handles.DrawWireDisc(pushPosition, normal, blendShapeGirthRadius);
            
            UnityEditor.Handles.color = Color.grey;
            UnityEditor.Handles.DrawWireDisc(position, normal, baseGirthRadius);
            #endif
        }
        public override void NotifyPenetration(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenisRoot, Penetrable.SetClipDistanceAction clipAction) {
            NotifyPenetrationGDO(penetrable, penetrator, worldSpaceDistanceToPenisRoot, clipAction, PenData.Depth | PenData.Girth | PenData.Offset );
        }
    }
}
