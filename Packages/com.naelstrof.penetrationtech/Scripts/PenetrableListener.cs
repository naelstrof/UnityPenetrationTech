using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PenetrationTech {
    
    [Obsolete("Currently this feature has been replaced by https://github.com/TextusGames/UnitySerializedReferenceUI with its handy SerializedReferenceButton attribute.")]
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
        public class PenetrableListenerValidationException : System.Exception {
            public PenetrableListenerValidationException(string msg) : base(msg) {
            }
        }

        [System.Flags]
        protected enum PenData {
            None = 0,
            Girth = (1 << 0),
            Depth = (1 << 1),
            Offset = (1 << 2),
            KnotForce = (1<<3),
            All = ~(0),
        }

        protected static bool HasFlag(PenData flags, PenData flag) {
            return ((int)flags & (int)flag) != 0;
        }

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
        protected virtual void OnPenetrationKnotForceChange(float knotForce) { }
        public virtual void OnDrawGizmosSelected(Penetrable p) { }
        public virtual void OnValidate(Penetrable p) { }
        public virtual void AssertValid() {
        }
        public virtual void NotifyPenetration(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenisRoot, Penetrable.SetClipDistanceAction clipAction) {
            NotifyPenetrationGDO(penetrable, penetrator, worldSpaceDistanceToPenisRoot, clipAction, PenData.All);
        }
        protected void NotifyPenetrationGDO(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenisRoot, Penetrable.SetClipDistanceAction clipAction, PenData penetrationData) {
            CatmullSpline holeSpline = penetrable.GetPath();
            float dist = holeSpline.GetDistanceFromTime(GetT(penetrable));
            float penetratedAmount = Mathf.Max(0f,penetrator.GetWorldLength()-worldSpaceDistanceToPenisRoot);
            float newGirthRadius = 0f;
            float newDepth = 0f;
            float newKnotForce = 0f;
            Vector3 newOffset = Vector3.zero;
            if (dist < penetratedAmount) {
                if (HasFlag(penetrationData, PenData.Girth)) {
                    newGirthRadius = penetrator.GetWorldGirthRadius(worldSpaceDistanceToPenisRoot+dist);
                }
                if (HasFlag(penetrationData, PenData.Depth)) {
                    newDepth = Mathf.Max(penetratedAmount-dist,0f);
                }
                if (HasFlag(penetrationData, PenData.Offset)) {
                    newOffset = penetrator.GetWorldOffset(worldSpaceDistanceToPenisRoot+dist);
                }
                if (HasFlag(penetrationData, PenData.KnotForce)) {
                    newKnotForce = penetrator.GetKnotForce(worldSpaceDistanceToPenisRoot+dist);
                }
            }
            OnPenetrationGirthRadiusChange(newGirthRadius);
            OnPenetrationOffsetChange(newOffset);
            OnPenetrationDepthChange(newDepth);
            OnPenetrationKnotForceChange(newKnotForce);
        }
    }
}
