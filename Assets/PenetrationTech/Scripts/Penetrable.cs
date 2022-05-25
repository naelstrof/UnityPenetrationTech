using System.Collections.Generic;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

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
        public delegate void SetClipDistanceAction(float startDistWorld, float endDistWorld);
        public delegate void PenetrateNotifyAction(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenetrator, SetClipDistanceAction clipAction);
        public PenetrateNotifyAction penetrationNotify;
        [SerializeField]
        private Transform[] points;
        private List<Vector3> worldPoints;
        // Keep this on the bottom, so it lines up with the custom inspector.
        [SerializeReference]
        public List<PenetrableListener> listeners;

        private void UpdateWorldPoints() {
            if (worldPoints == null) {
                worldPoints = new List<Vector3>();
            }
            worldPoints.Clear();
            foreach(Transform point in points) {
                worldPoints.Add(point.position);
            }
        }

        public void GetWeights(ICollection<Vector3> collection) {
            UpdateWorldPoints();
            CatmullSpline.GetWeightsFromPoints(collection, worldPoints);
        }

        void OnEnable() {
            worldPoints = new List<Vector3>();
            foreach(PenetrableListener listener in listeners) {
                listener.OnEnable(this);
            }
        }
        void OnDisable() {
            foreach(PenetrableListener listener in listeners) {
                listener.OnDisable();
            }
        }
        void Update() {
            foreach(PenetrableListener listener in listeners) {
                listener.Update();
            }
        }
        protected override void OnDrawGizmosSelected() {
            base.OnDrawGizmosSelected();
            foreach(PenetrableListener listener in listeners) {
                if (listener == null) {
                    continue;
                }
                listener.OnDrawGizmosSelected(this);
            }
        }
        void OnValidate() {
            foreach(PenetrableListener listener in listeners) {
                if (listener == null) {
                    continue;
                }
                listener.OnValidate(this);
            }
        }
        public void SetPenetrationDepth(Penetrator penetrator, float worldSpaceDistanceToPenisRoot, SetClipDistanceAction clipAction) {
            UpdateWorldPoints();
            path.SetWeightsFromPoints(worldPoints);
            foreach(PenetrableListener listener in listeners) {
                listener.NotifyPenetration(this, penetrator, worldSpaceDistanceToPenisRoot, clipAction);
            }
            penetrationNotify?.Invoke(this, penetrator, worldSpaceDistanceToPenisRoot, clipAction);
        }
        public CatmullSpline GetSplinePath() {
            if (path == null) {
                path = new CatmullSpline();
            }
            UpdateWorldPoints();
            path.SetWeightsFromPoints(worldPoints);
            return path;
        }
    }
}
