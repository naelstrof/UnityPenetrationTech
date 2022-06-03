using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace PenetrationTech {

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SkinnedMeshBlendshapePair))]
    public class SkinnedMeshBlendshapePairDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            SerializedProperty rendererProperty = property.FindPropertyRelative("skinnedMeshRenderer");
            float width = position.width;
            position.width = width*0.75f;
            EditorGUI.ObjectField(position, rendererProperty, new GUIContent("Skinned Mesh And Blendshape") );
            position.x += width*0.75f + 10f;
            position.width = width*0.25f - 10f;
            SkinnedMeshRenderer renderer = (SkinnedMeshRenderer)rendererProperty.objectReferenceValue;
            SerializedProperty blendNameProp = property.FindPropertyRelative("blendshapeName");
            string blendName = blendNameProp.stringValue;
            if (string.IsNullOrEmpty(blendNameProp.stringValue)) {
                if (renderer != null && renderer.sharedMesh != null) {
                    blendNameProp.stringValue = renderer.sharedMesh.GetBlendShapeName(0);
                    blendNameProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    blendName = blendNameProp.stringValue;
                } else {
                    blendName = "None";
                }
            } else {
                if (renderer != null && renderer.sharedMesh != null) {
                    if (renderer.sharedMesh.GetBlendShapeIndex(blendName) == -1) {
                        blendName = "<Missing>!";
                    }
                }
            }
            if (EditorGUI.DropdownButton(position, new GUIContent(blendName), FocusType.Passive) && renderer != null && renderer.sharedMesh != null) {
                GenericMenu menu = new GenericMenu();
                for(int i=0;i<renderer.sharedMesh.blendShapeCount;i++) {
                    string name = renderer.sharedMesh.GetBlendShapeName(i);
                    menu.AddItem(new GUIContent(name), name==blendName, ()=>{
                        property.FindPropertyRelative("blendshapeName").stringValue = name;
                        property.FindPropertyRelative("blendshapeID").intValue = renderer.sharedMesh.GetBlendShapeIndex(name);
                        property.serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAsContext();
            }
        }
    }
#endif

    [System.Serializable]
    public class SkinnedMeshBlendshapePair {
        [SerializeField]
        public SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField]
        private string blendshapeName;
        [SerializeField]
        public int blendshapeID;
        public void OnEnable() {
            blendshapeID = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(blendshapeName);
            if (blendshapeID == -1) {
                Debug.LogError("Failed to find blendshape " + blendshapeName + " on mesh " + skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.gameObject);
            }
        }
    }
}