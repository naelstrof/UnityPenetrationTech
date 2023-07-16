using System.Collections;
using System.Collections.Generic;
using PenetrationTech;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PenetrationTech {
    [System.Serializable]
    //[PenetratorListener(typeof(PenetratorUnityEventListener), "Event listener")]
    public class PenetratorUnityEventListener : PenetratorListener {
        [SerializeField, Range(0f,1f)]
        private float localDistanceFromTipOfDick = 0f;
        [SerializeField]
        private UnityEvent penetrationStart;
        [SerializeField]
        private UnityEvent penetrationEnd;

        private float lastDepth = 0f;

        protected override void OnPenetrationDepthChange(float depth) {
            if (depth <= 0f && lastDepth > 0f) { penetrationEnd.Invoke(); }
            if (depth > 0f && lastDepth <= 0f) { penetrationStart.Invoke(); }
            lastDepth = depth;
        }
        public override void OnDrawGizmosSelected(Penetrator p) {
#if UNITY_EDITOR
            Handles.color = Color.blue;
            float dist = (1f-localDistanceFromTipOfDick) * p.GetWorldLength();
            Vector3 position = p.GetPath().GetPositionFromDistance(dist);
            Handles.Label(position, GetType().Name);
            Handles.DrawWireDisc(position, p.GetPath().GetVelocityFromDistance(dist).normalized, p.GetWorldGirthRadius(dist));
#endif
        }
        public override void NotifyPenetrationUpdate(Penetrator a, Penetrable b, float distToHole) {
            float realDist = (1f - localDistanceFromTipOfDick) * a.GetWorldLength();
            float penetrateDist = realDist - distToHole;
            OnPenetrationDepthChange(penetrateDist);
            OnPenetrationKnotForceChange(penetrateDist > 0f ? a.GetKnotForce(a.GetWorldLength() - penetrateDist) : 0f);
        }
    }
}
