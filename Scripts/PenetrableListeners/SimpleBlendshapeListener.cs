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
        public override void OnEnable(Penetrable p) {
            base.OnEnable(p);
            foreach(SkinnedMeshBlendshapePair target in targets) {
                target.OnEnable();
            }
        }
        protected override void OnPenetrationGirthRadiusChange(float newGirthRadius) {
            base.OnPenetrationGirthRadiusChange(newGirthRadius);
            foreach(SkinnedMeshBlendshapePair target in targets) {
                target.skinnedMeshRenderer.SetBlendShapeWeight(target.blendshapeID, (newGirthRadius*2f/blendShapeGirth)*100f);
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
            #endif
        }
        public override void NotifyPenetration(Penetrator penetrator, float worldSpaceDistanceToPenisRoot) {
            NotifyPenetrationGDO(penetrator, worldSpaceDistanceToPenisRoot, true, false, true);
        }
    }
}
