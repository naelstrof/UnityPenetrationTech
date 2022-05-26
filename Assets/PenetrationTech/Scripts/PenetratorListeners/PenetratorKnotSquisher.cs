using System.Collections;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;
using PenetrationTech;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace PenetrationTech {
    [System.Serializable] [PenetratorListener(typeof(PenetratorKnotSquisher), "Knot Squisher listener")]
    public class PenetratorKnotSquisher : PenetratorListener {
        [Range(0f,1f)]
        [SerializeField] private float knotForceFactor = 0.1f;
        private Penetrator penetrator;
        private float currentVelocity;
        private float lastDepth;
        public override void OnEnable(Penetrator newPenetrator) {
            penetrator = newPenetrator;
        }

        protected override void OnPenetrationDepthChange(float depth) {
            float dir = Mathf.Sign(depth - lastDepth);
            float knotForce = penetrator.GetKnotForce(penetrator.GetWorldLength()-depth);
            penetrator.squashAndStretch = Mathf.SmoothDamp(penetrator.squashAndStretch, 1f, ref currentVelocity, 30f);
            if (knotForce * dir < 0f) {
                penetrator.squashAndStretch += knotForce*Time.deltaTime*2f;
            } else {
                penetrator.squashAndStretch += knotForce * Time.deltaTime*0.5f;
            }
            penetrator.squashAndStretch = Mathf.Clamp(penetrator.squashAndStretch, Mathf.Max(1f-knotForceFactor,0f), 1f+knotForceFactor);
            lastDepth = depth;
        }

        public override void OnDrawGizmosSelected(Penetrator p) {
            base.OnDrawGizmosSelected(p);
            #if UNITY_EDITOR
            float realDist = (1f - localDist) * p.GetWorldLength() - lastDepth;
            Vector3 position = p.GetSplinePath().GetPositionFromDistance(realDist);
            Vector3 tangent = p.GetSplinePath().GetVelocityFromT(realDist).normalized;
            UnityEditor.Handles.color = Color.blue;
            UnityEditor.Handles.DrawLine(position,
                position + tangent * p.GetKnotForce(p.GetWorldLength() - lastDepth));
            UnityEditor.Handles.DrawWireCube(position + tangent * p.GetKnotForce(p.GetWorldLength() - lastDepth), Vector3.one*0.05f);
#endif
        }
    }
}
