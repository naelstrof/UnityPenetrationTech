using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class PenetrableListenerAttribute : System.Attribute { }

    [System.Serializable]
    public class PenetrableListener {
        [HideInInspector]
        public float t;
        public float dist;
        [HideInInspector]
        public float penetratedGirth;
        [HideInInspector]
        public float penetratedDepth;
        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
        public virtual void OnPenetrationGirthChange(float newGirth) {
            penetratedGirth = newGirth;
        }
        public virtual void OnPenetrationDepthChange(float newDepth) {
            penetratedDepth = newDepth;
        }
        public virtual void OnDrawGizmosSelected(Penetrable p) { }
        public virtual void OnValidate(Penetrable p) {
            t = p.GetPath().GetTimeFromDistance(dist);
        }
    }
}
