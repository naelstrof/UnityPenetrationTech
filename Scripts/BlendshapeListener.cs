using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    [System.Serializable]
    public class BlendshapeListener : PenetrableListener {
        [SerializeField]
        SkinnedMeshRenderer[] targetRenderers;
    }
}
