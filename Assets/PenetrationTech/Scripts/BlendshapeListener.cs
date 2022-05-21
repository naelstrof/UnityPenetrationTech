using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PenetrationTech {

    [System.Serializable]
    [PenetrableListener]
    public class BlendshapeListener : PenetrableListener {
        [SerializeField]
        SkinnedMeshRenderer[] targetRenderers;
        [SerializeField]
        float blendShapeGirth;
        public override void OnDrawGizmosSelected(Penetrable p) {
            #if UNITY_EDITOR
            //Vector3 position = p.Get
            //UnityEditor.Handles.color = Color.blue;
            //UnityEditor.Handles.DrawWireDisc(p.GetPath().GetPositionFromT(t), )
            #endif
        }
    }
}
