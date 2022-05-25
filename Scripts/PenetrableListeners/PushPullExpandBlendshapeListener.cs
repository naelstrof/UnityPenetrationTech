using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PenetrationTech {
    [System.Serializable]
    [PenetrableListener(typeof(PushPullExpandBlendshapeListener), "Push Pull Expand Blendshape Listener")]
    public class PushPullExpandBlendshapeListener : BoneTransformListener {
        [SerializeField]
        private SkinnedMeshBlendshapePushPullExpandSet[] targets;
        [SerializeField]
        private float blendShapeGirth;
        [SerializeField]
        private float pullDist;
        [SerializeField]
        private float pushDist;
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
            foreach(SkinnedMeshBlendshapePushPullExpandSet target in targets) {
                target.skinnedMeshRenderer.SetBlendShapeWeight(target.expandBlendshapeID, (newGirthRadius/blendShapeGirth)*100f);
            }
        }
        protected override void OnPenetrationDepthChange(float newDepth) {
            float diff = newDepth - lastPenetrationDepth;
            pullPushAmount += diff * 10f;
            pullPushAmount = Mathf.Clamp(pullPushAmount, -1f, 1f);
            lastPenetrationDepth = newDepth;
        }
        public override void OnValidate(Penetrable p) {
            base.OnValidate(p);
            pullDist = Mathf.Min(pullDist, 0f);
            pushDist = Mathf.Max(pushDist, 0f);
        }
        protected override float GetDist() {
            float pushAmount = Mathf.Clamp01(pullPushAmount);
            float pullAmount = Mathf.Clamp01(-pullPushAmount);
            float d = Mathf.Lerp(dist, dist+pushDist, pushAmount);
            d = Mathf.Lerp(d, dist-pullDist, pullAmount);
            return d;
        }
        public override void Update() {
            pullPushAmount = Mathf.MoveTowards(pullPushAmount, 0f, Time.deltaTime*1f);
            foreach(SkinnedMeshBlendshapePushPullExpandSet target in targets) {
                float pushAmount = Mathf.Clamp01(pullPushAmount);
                target.skinnedMeshRenderer.SetBlendShapeWeight(target.pushBlendshapeID, pushAmount*100f);
                float pullAmount = Mathf.Clamp01(-pullPushAmount);
                target.skinnedMeshRenderer.SetBlendShapeWeight(target.pullBlendshapeID, pullAmount*100f);
            }
        }
        public override void OnDrawGizmosSelected(Penetrable p) {
            base.OnDrawGizmosSelected(p);
            #if UNITY_EDITOR
            CatmullSpline path = p.GetPathExpensive();
            Vector3 position = path.GetPositionFromT(t);
            Vector3 normal = path.GetVelocityFromT(t).normalized;
            UnityEditor.Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(position, normal, blendShapeGirth);

            Vector3 tugPosition = position + normal * pullDist;
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(tugPosition, normal, blendShapeGirth);

            Vector3 pushPosition = position + normal * pushDist;
            UnityEditor.Handles.color = Color.magenta;
            UnityEditor.Handles.DrawWireDisc(pushPosition, normal, blendShapeGirth);
            #endif
        }
        public override void NotifyPenetration(Penetrator penetrator, float worldSpaceDistanceToPenisRoot) {
            NotifyPenetrationGDO(penetrator, worldSpaceDistanceToPenisRoot, true, true, true);
        }
    }
}
