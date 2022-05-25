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
        public virtual void OnEnable(Penetrable p) { }
        public virtual void OnDisable() { }
        public virtual void Update() { }
        protected virtual float GetDist() {
            return dist;
        }
        protected virtual void OnPenetrationGirthRadiusChange(float newGirthRadius) { }
        protected virtual void OnPenetrationDepthChange(float newDepth) { }
        protected virtual void OnPenetrationOffsetChange(Vector3 worldOffset) { }
        public virtual void OnDrawGizmosSelected(Penetrable p) { }
        public virtual void OnValidate(Penetrable p) {
            t = p.GetPathExpensive().GetTimeFromDistance(dist);
        }
        public virtual void NotifyPenetration(Penetrator penetrator, float worldSpaceDistanceToPenisRoot) {
            NotifyPenetrationGDO(penetrator, worldSpaceDistanceToPenisRoot, true, true, true);
        }
        
        protected void NotifyPenetrationGDO(Penetrator penetrator, float worldSpaceDistanceToPenisRoot, bool girth, bool depth, bool offset) {
            float penetratedAmount = penetrator.GetWorldLength()-worldSpaceDistanceToPenisRoot;
            float newGirthRadius = 0f;
            float newDepth = 0f;
            Vector3 newOffset = Vector3.zero;
            if (GetDist() < penetratedAmount) {
                if (girth) {
                    newGirthRadius = penetrator.GetWorldGirthRadius(worldSpaceDistanceToPenisRoot+GetDist());
                }
                if (depth) {
                    newDepth = Mathf.Max(penetratedAmount-GetDist(),0f);
                }
                if (offset) {
                    newOffset = penetrator.GetWorldOffset(worldSpaceDistanceToPenisRoot+GetDist());
                }
            }
            OnPenetrationGirthRadiusChange(newGirthRadius);
            OnPenetrationOffsetChange(newOffset);
            OnPenetrationDepthChange(newDepth);
        }
    }
}
