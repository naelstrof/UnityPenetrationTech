using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PenetrationTech {
    [System.Serializable]
    [PenetrableListener(typeof(SimpleBlendshapeListener), "Simple Blendshape Listener")]
    public class SimpleBlendshapeListener : BoneTransformListener {
        [SerializeField]
        SkinnedMeshBlendshapePair[] targets;
        [SerializeField]
        float blendShapeGirth;
        public override void OnEnable() {
            base.OnEnable();
            foreach(SkinnedMeshBlendshapePair target in targets) {
                target.OnEnable();
            }
        }
        public override void OnPenetrationGirthChange(Penetrator penis, float newGirth) {
            base.OnPenetrationGirthChange(penis, newGirth);
            foreach(SkinnedMeshBlendshapePair target in targets) {
                target.skinnedMeshRenderer.SetBlendShapeWeight(target.blendshapeID, (newGirth/blendShapeGirth)*100f);
            }
        }
        public override void OnDrawGizmosSelected(Penetrable p) {
            base.OnDrawGizmosSelected(p);
            #if UNITY_EDITOR
            Vector3 position = p.GetPath().GetPositionFromT(t);
            Vector3 normal = p.GetPath().GetVelocityFromT(t).normalized;
            UnityEditor.Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(position, normal, blendShapeGirth);
            #endif
        }
    }
}
