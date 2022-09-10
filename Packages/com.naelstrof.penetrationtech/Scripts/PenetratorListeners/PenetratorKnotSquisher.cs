using System.Collections;
using System.Collections.Generic;
using PenetrationTech;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace PenetrationTech {
    [System.Serializable]
    //[PenetratorListener(typeof(PenetratorKnotSquisher), "Knot Squisher listener")]
    public class PenetratorKnotSquisher : PenetratorListener {
        [Range(0f,1f)]
        [SerializeField] private float knotForceFactor = 0.15f;
        [SerializeField] private float friction = 2.5f;
        [SerializeField] private float force = 12f;
        [SerializeField] private float elasticity = 30f;
        private Penetrator penetrator;
        private float currentKnotForce;
        private float currentVelocity;
        private float lastDistToHole;
        private Mode mode = Mode.Inserting;

        private enum Mode {
            Inserting,
            Inserted
        }

        private float GetNeededSquashStretchToReach(float distToHole) {
            return distToHole / penetrator.GetWorldLength();
        }

        public override void OnEnable(Penetrator newPenetrator) {
            penetrator = newPenetrator;
            mode = Mode.Inserting;
        }

        protected override void OnPenetrationDepthChange(float depth) {
            if (depth <= 0f) {
                mode = Mode.Inserting;
            }
        }

        public override void LateUpdate() {
            base.LateUpdate();
            switch (mode) {
                case Mode.Inserting:
                    float desiredStretch = Mathf.Clamp(GetNeededSquashStretchToReach(lastDistToHole), 1f-knotForceFactor, 1f);
                    penetrator.squashAndStretch = desiredStretch;
                    if (Mathf.Abs((1f-desiredStretch) - knotForceFactor) < 0.01f) {
                        mode = Mode.Inserted;
                    }
                    break;
                case Mode.Inserted:
                    currentVelocity = Mathf.MoveTowards(currentVelocity, 0f, Time.deltaTime*friction);
                    currentVelocity += currentKnotForce * Time.deltaTime*force;
                    currentVelocity += (1f - penetrator.squashAndStretch) * Time.deltaTime * elasticity;
                    penetrator.squashAndStretch = Mathf.Clamp(penetrator.squashAndStretch + currentVelocity * Time.deltaTime,
                        1f - knotForceFactor, 1f + knotForceFactor);
                    break;
            }
        }

        public override void NotifyPenetrationUpdate(Penetrator a, Penetrable b, float distToHole) {
            lastDistToHole = distToHole*a.squashAndStretch;
            base.NotifyPenetrationUpdate(a, b, distToHole);
        }

        protected override void OnPenetrationKnotForceChange(float knotForce) {
            base.OnPenetrationKnotForceChange(knotForce);
            currentKnotForce = knotForce;
        }

        public override void OnDrawGizmosSelected(Penetrator p) {
            base.OnDrawGizmosSelected(p);
            #if UNITY_EDITOR
            //float realDist = lastDistToHole * p.GetWorldLength();
            //Vector3 position = p.GetSplinePath().GetPositionFromDistance(realDist);
            //Vector3 tangent = p.GetSplinePath().GetVelocityFromT(realDist).normalized;
            //UnityEditor.Handles.color = Color.blue;
            //UnityEditor.Handles.DrawLine(position,
                //position + tangent * p.GetKnotForce(p.GetWorldLength() - lastDepth));
            //UnityEditor.Handles.DrawWireCube(position + tangent * p.GetKnotForce(p.GetWorldLength() - lastDepth), Vector3.one*0.05f);
            #endif
        }
    }
}
