using System.Collections;
using System.Collections.Generic;
using PenetrationTech;
using UnityEngine;

namespace PenetrationTech {
    [System.Serializable]
    [PenetrableListener(typeof(LengthwiseBoneDepthPenetrableListener), "Lengthwise Bone Depth Penetrable")]
    public class LengthwiseBoneDepthPenetrableListener : PenetrableListener {
        [SerializeField]
        private Transform targetTransform;
        [SerializeField]
        private float girthScaleMultiplier = 30f;
        private Penetrable penetrable;
        private Penetrator penetrator;
        private Vector3 startScale;

        public override void OnEnable(Penetrable p) {
            base.OnEnable(p);
            penetrable = p;
            startScale = targetTransform.localScale;
        }
        protected override void OnPenetrationDepthChange(float newDepth) {
            base.OnPenetrationDepthChange(newDepth);
            if (newDepth == 0f) {
                targetTransform.position = penetrable.GetSplinePath().GetPositionFromT(GetT(penetrable));
            } else {
                targetTransform.position = penetrator.GetSplinePath()
                    .GetPositionFromDistance(penetrator.GetWorldLength());
            }
        }

        protected override void OnPenetrationGirthRadiusChange(float newGirthRadius) {
            base.OnPenetrationGirthRadiusChange(newGirthRadius);
            targetTransform.localScale = startScale + Vector3.one * (newGirthRadius * girthScaleMultiplier);
        }

        public override void AssertValid() {
            base.AssertValid();
            if (targetTransform == null) {
                throw new PenetrableListenerValidationException($"targetTransform is null on {this}");
            }
        }

        public override void OnDrawGizmosSelected(Penetrable p) {
#if UNITY_EDITOR
            CatmullSpline path = p.GetSplinePath();
            Vector3 position = path.GetPositionFromT(t);
            Vector3 normal = path.GetVelocityFromT(t).normalized;
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.DrawWireDisc(position, normal, 0.1f);
            UnityEditor.Handles.DrawLine(position, targetTransform.transform.position);
#endif
        }

        public override void NotifyPenetration(Penetrable penetrable, Penetrator penetrator,
            float worldSpaceDistanceToPenisRoot,
            Penetrable.SetClipDistanceAction clipAction) {
            this.penetrator = penetrator;
            NotifyPenetrationGDO(penetrable, penetrator, worldSpaceDistanceToPenisRoot, clipAction,
                PenData.Depth | PenData.Girth);
        }
    }
}
