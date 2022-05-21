using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class PenetrableListenerAttribute : System.Attribute { }

    [System.Serializable]
    public class PenetrableListener {
        public float t;
        public virtual void OnGirthChange(float newGirth) { }
        public virtual void OnPenetrateStart() { }
        public virtual void OnPenetrateEnd() { }
        public virtual void OnPenetrationDepthChange(float newDepth) { }
        public virtual void OnDrawGizmosSelected(Penetrable p) { }
    }
}
