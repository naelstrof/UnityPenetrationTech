using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    [Obsolete("Currently this feature has been replaced by https://github.com/TextusGames/UnitySerializedReferenceUI with its handy SerializedReferenceButton attribute.")]
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
        public class PenetratorListenerValidationException : System.Exception {
            public PenetratorListenerValidationException(string message) : base(message) {
            }
        }

        [SerializeField][Range(0f,1f)]
        protected float localDist;
        protected virtual void OnPenetrationDepthChange(float depthDist) { }
        protected virtual void OnPenetrationKnotForceChange(float girthVelocity) { }
        public virtual void OnValidate(Penetrator p) { }
        public virtual void OnEnable(Penetrator newPenetrator) { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void OnDisable() { }
        public virtual void AssertValid() { }
        public virtual void OnPenetrationStart(Penetrable penetrable) { }
        public virtual void OnPenetrationEnd(Penetrable penetrable) { }
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