using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace PenetrationTech {
    [System.Serializable]
    //[PenetrableListener(typeof(ClipListener), "Clip range listener")]
    public class ClipListener : PenetrableListener {
        [SerializeField]
        private bool allowForAllTheWayThrough = true;
        [Range(0f,1f)][SerializeField]
        private float endT = 1f;

        public bool GetAllowForAllTheWayThrough() {
            return allowForAllTheWayThrough;
        }

        public override void OnDrawGizmosSelected(Penetrable p) {
            base.OnDrawGizmosSelected(p);
#if UNITY_EDITOR
            CatmullSpline path = p.GetPath();
            Vector3 position = path.GetPositionFromT(t);
            Vector3 tangent = path.GetVelocityFromT(t).normalized;
            Vector3 normal = path.GetBinormalFromT(t).normalized;
            
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(position, tangent, 0.025f);
            UnityEditor.Handles.DrawLine(position, position + tangent * 0.1f);
            UnityEditor.Handles.DrawWireArc(position + tangent * 0.05f, normal, -tangent, 90f, 0.05f);
            UnityEditor.Handles.DrawWireArc(position + tangent * 0.05f, normal, -tangent, -90f, 0.05f);

            if (allowForAllTheWayThrough) {
                Vector3 endPosition = path.GetPositionFromT(endT);
                Vector3 endTangent = path.GetVelocityFromT(endT).normalized;
                Vector3 endNormal = path.GetBinormalFromT(endT).normalized;
                UnityEditor.Handles.DrawWireDisc(endPosition, -endTangent, 0.025f);
                UnityEditor.Handles.DrawLine(endPosition, endPosition - endTangent * 0.1f);
                UnityEditor.Handles.DrawWireArc(endPosition - endTangent * 0.05f, endNormal, endTangent, 90f, 0.05f);
                UnityEditor.Handles.DrawWireArc(endPosition - endTangent * 0.05f, endNormal, endTangent, -90f, 0.05f);
            }
#endif
        }

        public override void NotifyPenetration(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenisRoot, Penetrable.SetClipDistanceAction clipAction) {
            float penetrationDepth = Mathf.Max(0f, penetrator.GetWorldLength() - worldSpaceDistanceToPenisRoot);
            var spline = penetrable.GetPath();
            float startDist = spline.GetDistanceFromTime(t);
            float endDist = spline.GetDistanceFromTime(endT);

            float penetratorLength = penetrator.GetWorldLength();
            float clipStart = Mathf.Max(0f,(penetratorLength+startDist-penetrationDepth));
            float clipEnd = Mathf.Max(0f,(penetratorLength+endDist-penetrationDepth));
            clipAction?.Invoke(clipStart, allowForAllTheWayThrough ? clipEnd : penetratorLength*1.5f);
        }

        public override void OnValidate(Penetrable p) {
            base.OnValidate(p);
            t = Mathf.Clamp01(t);
            endT = Mathf.Clamp(endT, t, 1f);
        }
    }
}
