using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace PenetrationTech {

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RendererSubMeshMask))]
    public class RendererSubMeshMaskDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            SerializedProperty rendererProperty = property.FindPropertyRelative("renderer");
            float width = position.width;
            position.width = width*0.75f;
            EditorGUI.ObjectField(position, rendererProperty, new GUIContent("Skinned Mesh and masks") );
            position.x += width*0.75f + 10f;
            position.width = width*0.25f - 10f;
            Renderer renderer = (Renderer)rendererProperty.objectReferenceValue;
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer skinnedRenderer) {
                mesh = skinnedRenderer.sharedMesh;
            } else if (renderer is MeshRenderer) {
                mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            } else if (renderer != null) {
                EditorGUI.HelpBox(position, "We only support SkinnedMeshRenderers and MeshRenderers", MessageType.Error);
                throw new UnityException("We only support SkinnedMeshRenderers and MeshRenderers");
            }

            SerializedProperty maskProp = property.FindPropertyRelative("mask");
            if (EditorGUI.DropdownButton(position, new GUIContent("SubMeshMask"), FocusType.Passive) && renderer != null && mesh != null) {
                GenericMenu menu = new GenericMenu();
                for(int i=0;i<mesh.subMeshCount;i++) {
                    string name = $"Sub-mesh {i}";
                    int alloc = i;
                    menu.AddItem(new GUIContent(name), RendererSubMeshMask.GetMask(maskProp.intValue,i), ()=> {
                        maskProp.intValue = RendererSubMeshMask.ToggleMask(maskProp.intValue, alloc);
                        maskProp.serializedObject.ApplyModifiedProperties();
                    });
                }
                menu.ShowAsContext();
            }
        }
    }
#endif

    [System.Serializable]
    public class RendererSubMeshMask : ISerializationCallbackReceiver {
        [SerializeField]
        public Renderer renderer;
        [SerializeField]
        public int mask = ~(0);

        public bool ShouldDrawSubmesh(int index) {
            return GetMask(mask, index);
        }

        public static int SetMask(int m, int index, bool set) {
            if (set) {
                return (m | (1 << index));
            } else {
                m &= ~(1 << index);
                return (m & ~(1 << index));
            }
        }

        public static int ToggleMask(int m, int index) {
            return SetMask(m, index, !GetMask(m, index));
        }

        public static bool GetMask(int m, int index) {
            return (m & (1 << index)) != 0;
        }

        public void OnBeforeSerialize() {
        }

        public void OnAfterDeserialize() {
            if (mask == 0) {
                mask = ~(0);
            }
        }
    }
}