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
    public class Penetrable : MonoBehaviour {
        public delegate void PenetrateNotifyAction(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenetrator);
        public PenetrateNotifyAction penetrationNotify;
        [SerializeField]
        private Transform[] points;
        private List<Vector3> worldPoints;
        // Keep this on the bottom, so it lines up with the custom inspector.
        [SerializeReference]
        public List<PenetrableListener> listeners;
        private CatmullSpline splinePath;

        public CatmullSpline GetPathExpensive() {
            if (splinePath == null) {
                splinePath = new CatmullSpline();
                worldPoints = new List<Vector3>();
            }
            worldPoints.Clear();
            foreach(Transform point in points) {
                worldPoints.Add(point.position);
            }
            return splinePath.SetWeightsFromPoints(worldPoints);
        }

        public Vector3 GetTangent(float t) {
            if (t < 0.5f) {
                return (points[1].position - points[0].position).normalized;
            } else {
                return points[points.Length - 1].position - points[points.Length - 2].position;
            }
        }

        public Vector3 GetHolePosition(float t) {
            if (t < 0.5f) {
                return points[0].position;
            } else {
                return points[points.Length - 1].position;
            }
        }

        public void GetWeights(ICollection<Vector3> collection) {
            worldPoints.Clear();
            foreach(Transform point in points) {
                worldPoints.Add(point.position);
            }
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
        void OnDrawGizmosSelected() {
            foreach(PenetrableListener listener in listeners) {
                if (listener == null) {
                    continue;
                }
                listener.OnDrawGizmosSelected(this);
            }

            for (int i = 0; i < points.Length - 1; i++) {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(points[i].position, 0.025f);
                Gizmos.DrawWireSphere(points[i+1].position, 0.025f);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(points[i].position, points[i + 1].position);
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
        public void SetPenetrationDepth(Penetrator penetrator, float worldSpaceDistanceToPenisRoot) {
            foreach(PenetrableListener listener in listeners) {
                listener.NotifyPenetration(penetrator, worldSpaceDistanceToPenisRoot);
            }
            penetrationNotify?.Invoke(this, penetrator, worldSpaceDistanceToPenisRoot);
        }
    }
}
