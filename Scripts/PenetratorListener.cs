using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class PenetratorListenerAttribute : System.Attribute {
        public System.Type type;
        public string name;
        public PenetratorListenerAttribute(System.Type type, string name) {
            this.type = type;
            this.name = name;
        }
    }

    [System.Serializable]
    public class PenetratorListener {
        [SerializeField][Range(0f,1f)]
        protected float localDist;
        protected virtual void OnPenetrationDepthChange(float depthDist) { }
        protected virtual void OnPenetrationKnotForceChange(float girthVelocity) { }
        public virtual void OnValidate(Penetrator p) {
            // TODO: Right now it's not clear that OnEnable would be called during OnValidate(). This is so that penetrators can run during the editor properly.
            // It's probably fine, but just writing a note to remind myself that it's kinda weird and can trip people up.
            OnEnable(p);
        }
        public virtual void OnEnable(Penetrator newPenetrator) { }
        public virtual void Update() { }
        public virtual void OnDisable() { }
        public virtual void OnDrawGizmosSelected(Penetrator p) {
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.blue;
            float dist = (1f-localDist) * p.GetWorldLength();
            Vector3 position = p.GetSplinePath().GetPositionFromDistance(dist);
            UnityEditor.Handles.Label(position, GetType().Name);
            UnityEditor.Handles.DrawWireDisc(position, p.GetSplinePath().GetVelocityFromDistance(dist).normalized, p.GetWorldGirthRadius(dist));
#endif
        }
        public virtual void NotifyPenetrationUpdate(Penetrator a, Penetrable b, float distToHole) {
            float realDist = (1f - localDist) * a.GetWorldLength();
            float penetrateDist = realDist - distToHole;
            if (penetrateDist > 0f) {
                OnPenetrationDepthChange(penetrateDist);
                OnPenetrationKnotForceChange(a.GetKnotForce(a.GetWorldLength()-penetrateDist));
            } else {
                OnPenetrationDepthChange(0f);
                OnPenetrationKnotForceChange(0f);
            }
        }
    }
}