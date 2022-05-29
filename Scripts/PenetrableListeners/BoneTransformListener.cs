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
        Vector3 originalLocalPosition = Vector3.positiveInfinity;
        Vector3 localOffset;
        public override void OnEnable(Penetrable p) {
            base.OnEnable(p);
            if (float.IsPositiveInfinity(originalLocalPosition.x)) {
                originalLocalPosition = targetBone.localPosition;
            }
        }
        public override void OnDisable() {
            base.OnDisable();
            if (!float.IsPositiveInfinity(originalLocalPosition.x)) {
                targetBone.localPosition = originalLocalPosition;
            }
        }
        protected override void OnPenetrationOffsetChange(Vector3 worldOffset) {
            base.OnPenetrationOffsetChange(worldOffset);
            if (float.IsPositiveInfinity(originalLocalPosition.x)) {
                return;
            }
            localOffset = targetBone.parent.InverseTransformVector(worldOffset);
            targetBone.localPosition = originalLocalPosition + localOffset; 
        }

        public override void AssertValid() {
            base.AssertValid();
            if (targetBone == null) {
                throw new PenetrableListenerValidationException($"Target bone on listener {this} is null.");
            }
        }

        public override void OnDrawGizmosSelected(Penetrable p) {
            base.OnDrawGizmosSelected(p);
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            var parent = targetBone.parent;
            UnityEditor.Handles.DrawLine(parent.TransformPoint(originalLocalPosition), parent.TransformPoint(originalLocalPosition + localOffset));
            UnityEditor.Handles.DrawWireCube(parent.TransformPoint(originalLocalPosition + localOffset), Vector3.one*0.005f);
            #endif
        }
        public override void NotifyPenetration(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenisRoot, Penetrable.SetClipDistanceAction clipAction) {
            NotifyPenetrationGDO(penetrable, penetrator, worldSpaceDistanceToPenisRoot, clipAction, PenData.Offset);
        }
    }
}
