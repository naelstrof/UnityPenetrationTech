using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace PenetrationTech {
    [System.Serializable]
    [PenetrableListener(typeof(BoneTransformListener), "Simple Bone Offset Correction Listener")]
    public class BoneTransformListener : PenetrableListener {
        [FormerlySerializedAs("targetBone")] [SerializeField]
        protected Transform offsetCorrectionBone;
        Vector3 originalLocalPosition = Vector3.positiveInfinity;
        Vector3 localOffset;
        public override void OnEnable(Penetrable p) {
            base.OnEnable(p);
            if (float.IsPositiveInfinity(originalLocalPosition.x)) {
                originalLocalPosition = offsetCorrectionBone.localPosition;
            }
        }
        public override void OnDisable() {
            base.OnDisable();
            if (!float.IsPositiveInfinity(originalLocalPosition.x)) {
                offsetCorrectionBone.localPosition = originalLocalPosition;
            }
        }
        protected override void OnPenetrationOffsetChange(Vector3 worldOffset) {
            base.OnPenetrationOffsetChange(worldOffset);
            if (float.IsPositiveInfinity(originalLocalPosition.x)) {
                return;
            }
            localOffset = offsetCorrectionBone.parent.InverseTransformVector(worldOffset);
            offsetCorrectionBone.localPosition = originalLocalPosition + localOffset; 
        }

        public override void AssertValid() {
            base.AssertValid();
            if (offsetCorrectionBone == null) {
                throw new PenetrableListenerValidationException($"Offset correction bone on listener {this} is null.");
            }
        }

        public override void OnDrawGizmosSelected(Penetrable p) {
            base.OnDrawGizmosSelected(p);
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            var parent = offsetCorrectionBone.parent;
            UnityEditor.Handles.DrawLine(parent.TransformPoint(originalLocalPosition), parent.TransformPoint(originalLocalPosition + localOffset));
            UnityEditor.Handles.DrawWireCube(parent.TransformPoint(originalLocalPosition + localOffset), Vector3.one*0.005f);
            #endif
        }
        public override void NotifyPenetration(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenisRoot, Penetrable.SetClipDistanceAction clipAction) {
            NotifyPenetrationGDO(penetrable, penetrator, worldSpaceDistanceToPenisRoot, clipAction, PenData.Offset);
        }
    }
}
