using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [Range(0f,1f)][SerializeField]
        protected float t;
        public virtual void OnEnable(Penetrable p) { }
        public virtual void OnDisable() { }
        public virtual void Update() { }
        protected virtual float GetT(Penetrable p) {
            return t;
        }
        protected virtual void OnPenetrationGirthRadiusChange(float newGirthRadius) { }
        protected virtual void OnPenetrationDepthChange(float newDepth) { }
        protected virtual void OnPenetrationOffsetChange(Vector3 worldOffset) { }
        public virtual void OnDrawGizmosSelected(Penetrable p) { }
        public virtual void OnValidate(Penetrable p) { }
        public virtual void NotifyPenetration(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenisRoot) {
            NotifyPenetrationGDO(penetrable, penetrator, worldSpaceDistanceToPenisRoot, true, true, true);
        }
        protected void NotifyPenetrationGDO(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenisRoot, bool girth, bool depth, bool offset) {
            CatmullSpline spline = penetrator.GetPath();
            // We need to sample the sub-spline that's 
            float dist = Mathf.Max(0f,spline.GetDistanceFromSubT(1, 1+penetrable.GetSubSplineCount(), GetT(penetrable))-worldSpaceDistanceToPenisRoot);
            float penetratedAmount = Mathf.Max(0f,penetrator.GetWorldLength()-worldSpaceDistanceToPenisRoot);
            float newGirthRadius = 0f;
            float newDepth = 0f;
            Vector3 newOffset = Vector3.zero;
            if (dist < penetratedAmount) {
                if (girth) {
                    newGirthRadius = penetrator.GetWorldGirthRadius(worldSpaceDistanceToPenisRoot+dist);
                }
                if (depth) {
                    newDepth = Mathf.Max(penetratedAmount-dist,0f);
                }
                if (offset) {
                    newOffset = penetrator.GetWorldOffset(worldSpaceDistanceToPenisRoot+dist);
                }
            }
            OnPenetrationGirthRadiusChange(newGirthRadius);
            OnPenetrationOffsetChange(newOffset);
            OnPenetrationDepthChange(newDepth);
        }
    }
}
