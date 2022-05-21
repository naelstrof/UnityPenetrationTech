using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PenetrationTech {
    [System.Serializable][PenetrableListener]
    public class BlendshapeListener : PenetrableListener {
        [SerializeField]
        SkinnedMeshBlendshapePair[] targets;
        [SerializeField]
        float blendShapeGirth;
        public override void OnEnable() {
            foreach(SkinnedMeshBlendshapePair target in targets) {
                target.OnEnable();
            }
        }
        public override void OnPenetrationGirthChange(float newGirth) {
            base.OnPenetrationGirthChange(newGirth);
            foreach(SkinnedMeshBlendshapePair target in targets) {
                target.skinnedMeshRenderer.SetBlendShapeWeight(target.blendshapeID, (newGirth/blendShapeGirth)*100f);
            }
        }
        public override void OnDrawGizmosSelected(Penetrable p) {
            #if UNITY_EDITOR
            Vector3 position = p.GetPath().GetPositionFromT(t);
            Vector3 normal = p.GetPath().GetVelocityFromT(t).normalized;
            UnityEditor.Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(position, normal, blendShapeGirth);
            #endif
        }
    }
}
