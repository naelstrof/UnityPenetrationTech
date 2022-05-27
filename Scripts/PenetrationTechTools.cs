using System.Collections;
using System.Collections.Generic;
using Codice.Client.BaseCommands;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PenetrationTech {
    public static class PenetrationTechTools {
        private static LayerMask maskLayer = 0;
        private static int layer = -1;
        private static bool setMask;

        private static void Regenerate() {
#if UNITY_EDITOR
            layer = LayerMask.NameToLayer("Penetrables");
            if (layer == -1) {
                SerializedObject tagManager =
                    new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                SerializedProperty layers = tagManager.FindProperty("layers");
                for (int i = 0; i < 32; i++) {
                    SerializedProperty layer = layers.GetArrayElementAtIndex(i);
                    if (string.IsNullOrEmpty(layer.stringValue)) {
                        layer.stringValue = "Penetrables";
                        break;
                    }
                }
                tagManager.ApplyModifiedPropertiesWithoutUndo();
            }
            layer = LayerMask.NameToLayer("Penetrables");
            if (layer == -1) {
                throw new UnityException(
                    "Cannot create penetrable layer! Automatic collisions won't work properly for penetrables and penetrators.");
            }

            maskLayer = LayerMask.GetMask("Penetrables");
#endif
        }
        public static LayerMask GetPenetrableMask() {
            if (maskLayer == 0 || layer == -1) {
                Regenerate();
            }
            return maskLayer;
        }
        public static int GetPenetrableLayer() {
            if (maskLayer == 0 || layer == -1) {
                Regenerate();
            }
            return layer;
        }
    }
}
