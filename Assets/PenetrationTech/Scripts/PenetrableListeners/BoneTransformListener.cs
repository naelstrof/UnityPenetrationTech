using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PenetrationTech {
    [System.Serializable]
    [PenetrableListener(typeof(BoneTransformListener), "Simple Bone Offset Correction Listener")]
    public class BoneTransformListener : PenetrableListener {
        [SerializeField]
        Transform targetBone;
        Vector3 originalLocalPosition;
        Vector3 localOffset;
        public override void OnEnable() {
            base.OnEnable();
            if (targetBone != null) {
                originalLocalPosition = targetBone.localPosition;
            }
        }
        public override void OnDisable() {
            base.OnDisable();
            if (targetBone != null) {
                targetBone.localPosition = originalLocalPosition;
            }
        }
        protected override void OnPenetrationOffsetChange(Vector3 worldOffset) {
            base.OnPenetrationOffsetChange(worldOffset);
            if (targetBone != null) {
                localOffset = targetBone.parent.InverseTransformVector(worldOffset);
                targetBone.localPosition = originalLocalPosition + localOffset; 
            }
        }
        public override void OnDrawGizmosSelected(Penetrable p) {
            base.OnDrawGizmosSelected(p);
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.DrawLine(targetBone.parent.TransformPoint(originalLocalPosition), targetBone.parent.TransformPoint(originalLocalPosition + localOffset));
            UnityEditor.Handles.DrawWireCube(targetBone.parent.TransformPoint(originalLocalPosition + localOffset), Vector3.one*0.005f);
            #endif
        }
        public override void NotifyPenetration(Penetrator penetrator, float worldSpaceDistanceToPenisRoot) {
            NotifyPenetrationGDO(penetrator, worldSpaceDistanceToPenisRoot, false, false, true);
        }

    }
}
