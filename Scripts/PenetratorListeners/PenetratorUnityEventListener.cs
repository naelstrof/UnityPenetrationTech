using System.Collections;
using System.Collections.Generic;
using PenetrationTech;
using UnityEngine;
using UnityEngine.Events;

namespace PenetrationTech {
    [System.Serializable]
    //[PenetratorListener(typeof(PenetratorUnityEventListener), "Event listener")]
    public class PenetratorUnityEventListener : PenetratorListener {
        [SerializeField]
        private UnityEvent penetrationStart;
        [SerializeField]
        private UnityEvent penetrationEnd;

        private float lastDepth = 0f;

        protected override void OnPenetrationDepthChange(float depth) {
            if (depth <= 0f && lastDepth > 0f) { penetrationEnd.Invoke(); }
            if (depth > 0f && lastDepth <= 0f) { penetrationStart.Invoke(); }
            lastDepth = depth;
        }
    }
}
