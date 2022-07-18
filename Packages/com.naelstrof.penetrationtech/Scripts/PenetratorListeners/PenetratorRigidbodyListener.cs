using System.Collections;
using System.Collections.Generic;
using PenetrationTech;
using UnityEngine;
using UnityEngine.Events;

namespace PenetrationTech {
    [System.Serializable]
    //[PenetratorListener(typeof(PenetratorRigidbodyListener), "Rigidbody Apply Force listener")]
    public class PenetratorRigidbodyListener : PenetratorListener {
        [SerializeField]
        private Rigidbody body;

        [SerializeField][Range(0f,100f)][Tooltip("The amount of force that aims the penetrator at the hole. This force is increased the more penetrated the penetrator is.")]
        private float angularForceMultiplier = 30f;
        [SerializeField][Range(0f,10f)][Tooltip("The amount of force that moves the penetrator planar to the hole tangent.")]
        private float planarAlignmentForceMultiplier = 4f;
        [SerializeField][Range(0f,1f)][Tooltip("How much movement is dampened within the hole")]
        private float friction = 0.5f;

        private Penetrable penetrableMem;
        private Penetrator penetratorMem;
        
        private Vector3 localOffset;
        private Vector3 wantedPosition;
        private Vector3 wantedDir;
        private bool applyForce = false;
        private Rigidbody penetrableBody;
        private float rotationStrength;
        private float distToHoleMem;

        public override void OnEnable(Penetrator newPenetrator) {
            base.OnEnable(newPenetrator);
            penetratorMem = newPenetrator;
            localOffset = body.transform.InverseTransformPoint(newPenetrator.GetWorldPosition());
        }
        public override void OnPenetrationStart(Penetrable penetrable) {
            base.OnPenetrationStart(penetrable);
            applyForce = true;
            penetrableMem = penetrable;
            body.useGravity = false;
            penetrableBody = penetrable.GetComponentInParent<Rigidbody>();
        }

        public override void FixedUpdate() {
            base.FixedUpdate();
            if (!applyForce) {
                return;
            }

            Vector3 angularVelocity = body.angularVelocity;
            angularVelocity = Vector3.Lerp(angularVelocity, Vector3.Cross(penetratorMem.GetWorldForward(), wantedDir)*angularForceMultiplier,
                rotationStrength);

            Vector3 velocity = body.velocity;
            
            
            float gravityStrength = 1f - rotationStrength;
            velocity += Physics.gravity * (gravityStrength * gravityStrength * Time.deltaTime);
            
            velocity = Vector3.Project(velocity, wantedDir);
            // Friction toward zero, though zero in our case is whatever velocity our hole is moving.
            velocity = Vector3.Lerp(velocity, penetrableBody==null?Vector3.zero : penetrableBody.velocity, friction*rotationStrength);

            Vector3 diff = wantedPosition -
                           (penetratorMem.GetWorldPosition() + penetratorMem.GetWorldForward() * distToHoleMem);
            Vector3 diffPlanar = Vector3.ProjectOnPlane(diff, wantedDir);
            velocity += diffPlanar*(rotationStrength*planarAlignmentForceMultiplier);
            
            // Spring force to keep it from over-penetrating
            float dist = Vector3.Distance(penetratorMem.GetWorldPosition(), wantedPosition);
            float springStrength = Mathf.Max(penetratorMem.GetWorldLength()*0.25f - dist,0f);
            Vector3 springForce = -wantedDir * springStrength;
            velocity += springForce * rotationStrength;
            


            body.velocity = velocity;
            body.angularVelocity = angularVelocity;
        }

        protected override void OnPenetrationDepthChange(float depthDist) {
            base.OnPenetrationDepthChange(depthDist);
            if (depthDist <= 0f || penetrableMem == null) {
                applyForce = false;
                return;
            }

            applyForce = true;
            var path = penetrableMem.GetSplinePath();
            //wantedDir = path.GetVelocityFromT(0f).normalized;
            float worldLength = penetratorMem.GetWorldLength();
            wantedPosition = path.GetPositionFromDistance(0f);
            rotationStrength = depthDist / worldLength;
            wantedDir = Vector3.Lerp((wantedPosition - body.transform.TransformPoint(localOffset)).normalized, path.GetVelocityFromT(0f).normalized, rotationStrength);
        }

        public override void OnPenetrationEnd(Penetrable penetrable) {
            base.OnPenetrationEnd(penetrable);
            applyForce = false;
            body.useGravity = true;
        }

        public override void AssertValid() {
            base.AssertValid();
            if (body == null) {
                throw new PenetratorListenerValidationException($"Rigidbody on listener {this} is null.");
            }

            // TODO: Rigidbodies with joints aren't affected by force changes-- probably should print a warning rather than an Assert. Though probably doesn't really matter at all really. Hopefully people just don't use joints, and if they do it might work okay anyway.
            //Joint j = body.GetComponent<Joint>();
            //if (j != null && j.connectedBody == body) {
                //throw new PenetratorListenerValidationException($"Rigidbody on listener {this} has a joint affecting it, this is unsupported. (Forces are ignored)");
            //}
        }

        public override void OnDrawGizmosSelected(Penetrator p) {
            base.OnDrawGizmosSelected(p);
            if (applyForce) {
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.green;
            Vector3 worldOffsetPoint = (penetratorMem.GetWorldPosition() + penetratorMem.GetWorldForward() * distToHoleMem);
            Vector3 diff = wantedPosition -
                           (penetratorMem.GetWorldPosition() + penetratorMem.GetWorldForward() * distToHoleMem);
            Vector3 diffPlanar = Vector3.ProjectOnPlane(diff, wantedDir);
            UnityEditor.Handles.DrawLine(worldOffsetPoint,
                worldOffsetPoint + diffPlanar);
            UnityEditor.Handles.DrawWireCube(worldOffsetPoint+diffPlanar, Vector3.one*0.005f);
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawDottedLine(worldOffsetPoint+diffPlanar, worldOffsetPoint + diffPlanar - wantedDir, 5f);
            // Spring force to keep it from over-penetrating
            UnityEditor.Handles.color = Color.blue;
            float dist = Vector3.Distance(penetratorMem.GetWorldPosition(), wantedPosition);
            float springStrength = Mathf.Max(penetratorMem.GetWorldLength()*0.10f - dist,0f);
            Vector3 springForce = -wantedDir * springStrength;
            UnityEditor.Handles.DrawLine(worldOffsetPoint,
                worldOffsetPoint + springForce);
            UnityEditor.Handles.DrawWireCube(worldOffsetPoint+springForce, Vector3.one*0.005f);
            #endif
            }
        }

        public override void NotifyPenetrationUpdate(Penetrator a, Penetrable b, float distToHole) {
            base.NotifyPenetrationUpdate(a, b, distToHole);
            distToHoleMem = distToHole;
        }
    }
}
