using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System;
using UnityEditor;
using System.Reflection;
#endif

namespace PenetrationTech {
    #if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Penetrable))]
    public class PenetrableEditor : Editor {
        static IEnumerable<PenetrableListenerAttribute> GetPenetrableListenerAttributes() {
            foreach(Type type in Assembly.GetExecutingAssembly().GetTypes()) {
                var attributes = (PenetrableListenerAttribute[])type.GetCustomAttributes(typeof(PenetrableListenerAttribute), true);
                if (attributes.Length > 0) {
                    yield return attributes[0];
                }
            }
        }
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            
            if (!EditorGUILayout.DropdownButton(new GUIContent("Add listener"), FocusType.Passive)) {
                return;
            }

            GenericMenu menu = new GenericMenu();
            List<PenetrableListenerAttribute> attributes = new List<PenetrableListenerAttribute>(GetPenetrableListenerAttributes());
            foreach(var attribute in attributes) {
                menu.AddItem(new GUIContent(attribute.name), false, ()=>{
                    foreach (var t in targets) {
                        Penetrable p = t as Penetrable;
                        if (p.listeners == null) {
                            p.listeners = new List<PenetrableListener>();
                        }
                        p.listeners.Add((PenetrableListener)Activator.CreateInstance(attribute.type));
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(p);
                    }
                });
            }
            menu.ShowAsContext();
        }
    }
    #endif
    public class Penetrable : CatmullDisplay {
        [SerializeField]
        private Vector3[] points;
        private List<Vector3> worldPoints;
        // Keep this on the bottom, so it lines up with the custom inspector.
        [SerializeReference]
        public List<PenetrableListener> listeners;
        void OnEnable() {
            worldPoints = new List<Vector3>();
            foreach(Vector3 point in points) {
                worldPoints.Add(transform.TransformPoint(point));
            }
            path = new CatmullSpline().SetWeightsFromPoints(worldPoints);
            foreach(PenetrableListener listener in listeners) {
                listener.OnEnable();
            }
        }
        void OnDisable() {
            foreach(PenetrableListener listener in listeners) {
                listener.OnDisable();
            }
        }
        // This is all really nasty, it'd be nice if I can just set a transform on the path directly.
        private void CheckUpdate() {
            if (worldPoints == null) {
                worldPoints = new List<Vector3>();
            }
            if(transform.hasChanged) {
                worldPoints.Clear();
                for(int i=0;i<points.Length;i++) {
                    worldPoints.Add(transform.TransformPoint(points[i]));
                }
                if (path == null) {
                    path = new CatmullSpline().SetWeightsFromPoints(worldPoints);
                } else {
                    path.SetWeightsFromPoints(worldPoints);
                }
                transform.hasChanged = false;
            }
        }
        void Update() {
            CheckUpdate();
            foreach(PenetrableListener listener in listeners) {
                listener.Update();
            }
        }
        protected override void OnDrawGizmosSelected() {
            CheckUpdate();
            foreach(PenetrableListener listener in listeners) {
                if (listener == null) {
                    continue;
                }
                listener.OnDrawGizmosSelected(this);
            }
            base.OnDrawGizmosSelected();
        }
        void OnValidate() {
            transform.hasChanged = true;
            CheckUpdate();
            foreach(PenetrableListener listener in listeners) {
                if (listener == null) {
                    continue;
                }
                listener.OnValidate(this);
            }
        }
        public void SetPenetrationDepth(Penetrator penis, float worldSpaceDistanceToPenisRoot) {
            float penetratedAmount = penis.GetWorldLength()-worldSpaceDistanceToPenisRoot;
            foreach(PenetrableListener listener in listeners) {
                if (listener.GetDist() < penetratedAmount) {
                    float newGirth = penis.GetWorldGirth(worldSpaceDistanceToPenisRoot+listener.GetDist());
                    listener.OnPenetrationGirthChange(penis, newGirth);
                    float newDepth = Mathf.Max(penetratedAmount-listener.GetDist(),0f);
                    listener.OnPenetrationDepthChange(penis, newDepth);
                } else {
                    listener.OnPenetrationGirthChange(penis, 0f);
                    listener.OnPenetrationDepthChange(penis, 0f);
                }
            }
        }
    }
}
