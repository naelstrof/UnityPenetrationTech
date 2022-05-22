using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class PenetrableListenerAttribute : System.Attribute {
        public System.Type type;
        public string name;
        public PenetrableListenerAttribute(System.Type type, string name) {
            this.type = type;
            this.name = name;
        }
    }

    [System.Serializable]
    public class PenetrableListener {
        [HideInInspector]
        protected float t;
        [SerializeField]
        protected float dist;
        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
        public virtual void Update() { }
        public virtual float GetDist() {
            return dist;
        }
        public virtual void OnPenetrationGirthChange(float newGirth) { }
        public virtual void OnPenetrationDepthChange(float newDepth) { }
        public virtual void OnPenetrationOffsetChange(Vector3 worldOffset) { }
        public virtual void OnDrawGizmosSelected(Penetrable p) { }
        public virtual void OnValidate(Penetrable p) {
            t = p.GetPath().GetTimeFromDistance(dist);
        }
    }
}
