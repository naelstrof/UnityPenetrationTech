using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
namespace PenetrationTech {
    public class ProceduralPenetrationModificationProcessor : UnityEditor.AssetModificationProcessor {
        public static string[] OnWillSaveAssets(string[] paths) {
            foreach (ProceduralDeformation proceduralDeformation in
                     Object.FindObjectsOfType<ProceduralDeformation>(true)) {
                proceduralDeformation.SwapTo(false);
                proceduralDeformation.runInEditor = false;
            }

            return paths;
        }
    }

}
#endif