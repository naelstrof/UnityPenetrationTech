using System.Collections;
using System.Collections.Generic;
using PenetrationTech;
using UnityEngine;
using UnityEngine.Events;

namespace PenetrationTech {
    [System.Serializable]
    //[PenetratorListener(typeof(PenetratorNoCollideListener), "Rigidbody NoCollide listener")]
    public class PenetratorNoCollideListener : PenetratorListener {
        [SerializeField]
        private Rigidbody body;
        private Rigidbody penetrableBody;
        private Collider[] colliders;
        public override void OnEnable(Penetrator newPenetrator) {
            base.OnEnable(newPenetrator);
            colliders = body.GetComponentsInChildren<Collider>();
        }

        public override void OnPenetrationStart(Penetrable penetrable) {
            base.OnPenetrationStart(penetrable);
            penetrableBody = penetrable.GetComponentInParent<Rigidbody>();
            if (penetrableBody == null) {
                return;
            }
            foreach (Collider a in colliders) {
                foreach (Collider b in penetrableBody.GetComponentsInChildren<Collider>()) {
                    Physics.IgnoreCollision(a, b, true);
                }
            }
        }

        public override void OnPenetrationEnd(Penetrable penetrable) {
            base.OnPenetrationEnd(penetrable);
            if (penetrable != null && penetrableBody != null) {
                foreach (Collider a in colliders) {
                    foreach (Collider b in penetrableBody.GetComponentsInChildren<Collider>()) {
                        Physics.IgnoreCollision(a, b, false);
                    }
                }
            }
        }

        public override void AssertValid() {
            base.AssertValid();
            if (body == null) {
                throw new PenetratorListenerValidationException($"Rigidbody on listener {this} is null.");
            }
        }
    }
}
