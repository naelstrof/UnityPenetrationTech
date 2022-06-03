using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace PenetrationTech {

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SkinnedMeshBlendshapePushPullExpandSet))]
    public class SkinnedMeshBlendshapePushPullExpandSetDrawer : PropertyDrawer {
        private void DrawDropdownForProperties(Rect position, SkinnedMeshRenderer renderer, SerializedProperty blendNameProp, SerializedProperty blendIDProp) {
            string blendName = blendNameProp.stringValue;
            if (string.IsNullOrEmpty(blendName)) {
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
                        blendNameProp.stringValue = name;
                        blendIDProp.intValue = renderer.sharedMesh.GetBlendShapeIndex(name);
                        blendNameProp.serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAsContext();
            }
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            SerializedProperty rendererProperty = property.FindPropertyRelative("skinnedMeshRenderer");
            float width = position.width;
            position.width = width*0.6f;
            EditorGUI.ObjectField(position, rendererProperty, new GUIContent("Skinned Mesh And Expand, Push, Pull") );
            SkinnedMeshRenderer renderer = (SkinnedMeshRenderer)rendererProperty.objectReferenceValue;
            position.x += width*0.6f + 10f;
            position.width = (width*0.4f)/3f - 30f;
            DrawDropdownForProperties(position, renderer, property.FindPropertyRelative("expandBlendshapeName"), property.FindPropertyRelative("expandBlendshapeID"));
            position.x += (width*0.4f)/3f + 10f;
            DrawDropdownForProperties(position, renderer, property.FindPropertyRelative("pushBlendshapeName"), property.FindPropertyRelative("pushBlendshapeID"));
            position.x += (width*0.4f)/3f + 10f;
            DrawDropdownForProperties(position, renderer, property.FindPropertyRelative("pullBlendshapeName"), property.FindPropertyRelative("pullBlendshapeID"));
        }
    }
#endif

    [System.Serializable]
    public class SkinnedMeshBlendshapePushPullExpandSet {
        [SerializeField]
        public SkinnedMeshRenderer skinnedMeshRenderer;
        [SerializeField]
        private string expandBlendshapeName;
        [SerializeField]
        private string pullBlendshapeName;
        [SerializeField]
        private string pushBlendshapeName;

        [SerializeField]
        public int expandBlendshapeID;
        [SerializeField]
        public int pullBlendshapeID;
        [SerializeField]
        public int pushBlendshapeID;
        public void OnEnable() {
            pushBlendshapeID = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(pushBlendshapeName);
            pullBlendshapeID = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(pullBlendshapeName);
            expandBlendshapeID = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(expandBlendshapeName);
            if (pushBlendshapeID == -1) {
                Debug.LogError("Failed to find blendshape " + pushBlendshapeName + " on mesh " + skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.gameObject);
            }
            if (pullBlendshapeID == -1) {
                Debug.LogError("Failed to find blendshape " + pullBlendshapeName + " on mesh " + skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.gameObject);
            }
            if (expandBlendshapeID == -1) {
                Debug.LogError("Failed to find blendshape " + expandBlendshapeName + " on mesh " + skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer.gameObject);
            }
        }
    }
}