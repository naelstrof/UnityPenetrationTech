using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using System.Net.Mime;
using Vector3 = UnityEngine.Vector3;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace PenetrationTech {
    #if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Penetrable))]
    public class PenetrableEditor : Editor {
        /*static IEnumerable<PenetrableListenerAttribute> GetPenetrableListenerAttributes() {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (Type type in assembly.GetTypes()) {
                    var attributes =
                        (PenetrableListenerAttribute[])type.GetCustomAttributes(typeof(PenetrableListenerAttribute),
                            true);
                    if (attributes.Length > 0) {
                        yield return attributes[0];
                    }
                }
            }
        }*/
        public override void OnInspectorGUI() {
            string lastError = ((Penetrable)target).GetLastError();
            if (!string.IsNullOrEmpty(lastError)) {
                EditorGUILayout.HelpBox(lastError, MessageType.Error);
            }

            DrawDefaultInspector();
            
            /*if (!EditorGUILayout.DropdownButton(new GUIContent("Add listener"), FocusType.Passive)) {
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
            menu.ShowAsContext();*/
        }
    }
    #endif
    [ExecuteAlways]
    public class Penetrable : CatmullDisplay {
        public delegate void SetClipDistanceAction(float startDistWorld, float endDistWorld);
        public delegate void PenetrateNotifyAction(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenetrator, SetClipDistanceAction clipAction);
        public PenetrateNotifyAction penetrationNotify;
        [SerializeField]
        private Transform[] points;

        [SerializeField, Range(0f,1f)] private float splineTension = 0.5f;
        private List<Vector3> worldPoints;
        // Keep this on the bottom, so it lines up with the custom inspector.
        [SerializeReference,SerializeReferenceButton]
        public List<PenetrableListener> listeners;

        private GameObject colliderEntrance;
        private bool refreshListeners;
        private bool valid;
        private string lastError;
        private bool reinitialize = false;
        public string GetLastError() {
            return lastError;
        }
        private class PenetrableValidationException : System.SystemException {
            public PenetrableValidationException(string msg) : base(msg) { }
        }

        private void UpdateWorldPoints() {
            if (!valid && !Application.isPlaying) {
                return;
            }
            worldPoints.Clear();
            foreach(Transform point in points) {
                worldPoints.Add(point.position);
            }
        }

        public void GetWeights(ICollection<Vector3> collection) {
            UpdateWorldPoints();
            CatmullSpline.GetWeightsFromPoints(collection, worldPoints, splineTension);
        }

        private void SetUpCollider(ref GameObject obj, Transform point) {
            obj = new GameObject($"{name} collider", new System.Type[] { typeof(SphereCollider), typeof(PenetrableOwner) });
            obj.layer = PenetrationTechTools.GetPenetrableLayer();
            obj.hideFlags = HideFlags.HideAndDontSave;
            obj.transform.parent = point;
            obj.transform.localPosition = Vector3.zero;
            obj.GetComponent<SphereCollider>().isTrigger = true;
            obj.GetComponent<SphereCollider>().radius = 0.2f;
            obj.GetComponent<PenetrableOwner>().owner = this;
        }


        protected override void OnEnable() {
            base.OnEnable();
            if (!Application.isPlaying) {
                CheckValid();
                if (!valid) {
                    return;
                }
            }

            worldPoints = new List<Vector3>();
            foreach(PenetrableListener listener in listeners) {
                listener.OnEnable(this);
            }
            if (colliderEntrance == null) {
                SetUpCollider(ref colliderEntrance, points[0]);
            }
        }
        void OnDisable() {
            if (colliderEntrance != null) {
                if (Application.isPlaying) {
                    GameObject.Destroy(colliderEntrance);
                } else {
                    GameObject.DestroyImmediate(colliderEntrance);
                }
            }

            if (!valid && !Application.isPlaying) {
                return;
            }
            foreach(PenetrableListener listener in listeners) {
                listener.OnDisable();
            }

        }
        void Update() {
            #if UNITY_EDITOR
            if (reinitialize && !Application.isPlaying) {
                CheckValid();
                if (!valid) {
                    return;
                }
                foreach (PenetrableListener listener in listeners) {
                    listener.OnDisable();
                }

                foreach (PenetrableListener listener in listeners) {
                    listener.OnEnable(this);
                }

                reinitialize = false;
            }
            #endif
            if (!valid && !Application.isPlaying) {
                return;
            }


            foreach(PenetrableListener listener in listeners) {
                listener.Update();
            }
        }
        protected override void OnDrawGizmosSelected() {
            base.OnDrawGizmosSelected();
            CheckValid();
            if (!valid && !Application.isPlaying) {
                return;
            }
            UpdateWorldPoints();
            path.SetWeightsFromPoints(worldPoints, splineTension);
            foreach(PenetrableListener listener in listeners) {
                listener.OnDrawGizmosSelected(this);
            }
        }

        void AssertValid(bool condition, string message) {
            valid &= condition;
            if (!condition) {
                throw new PenetrableValidationException(message);
            }
        }

        void CheckValid() {
            lastError = "";
            valid = true;
            listeners ??= new List<PenetrableListener>();
            worldPoints ??= new List<Vector3>();
            path ??= new CatmullSpline();
            try {
                AssertValid(points != null && points.Length > 1, "Please specify at least two points to form a curve.");
                bool hasNullTransform = false;
                foreach (Transform t in points) {
                    if (t == null) {
                        hasNullTransform = true;
                        break;
                    }
                }
                
                if (colliderEntrance != null && points.Length > 0 && colliderEntrance.transform.parent != points[0]) {
                    colliderEntrance.transform.parent = points[0];
                    colliderEntrance.transform.localPosition = Vector3.zero;
                }
                UpdateWorldPoints();
                path.SetWeightsFromPoints(worldPoints, splineTension);
                
                AssertValid(!hasNullTransform, "One of the path transforms is null.");
                bool hasNullListener = false;
                foreach (PenetrableListener listener in listeners) {
                    if (listener == null || !listener.GetType().IsSubclassOf(typeof(PenetrableListener))) {
                        hasNullListener = true;
                    }
                }

                AssertValid(hasNullListener == false,
                    "There's a null or empty listener in the listener list, this is not allowed.");
                foreach (var listener in listeners) {
                    listener.AssertValid();
                }
            } catch (PenetrableValidationException error) {
                valid = false;
                lastError = $"{error.Message}\n\n{error.StackTrace}";
            } catch (PenetrableListener.PenetrableListenerValidationException error) {
                valid = false;
                lastError = $"{error.Message}\n\n{error.StackTrace}";
            }
            
        }

        void OnValidate() {
            valid = false;
            reinitialize = true;
            foreach(PenetrableListener listener in listeners) {
                if (listener == null) {
                    continue;
                }
                listener.OnValidate(this);
            }
        }
        public void SetPenetrationDepth(Penetrator penetrator, float worldSpaceDistanceToPenisRoot, SetClipDistanceAction clipAction) {
            if (!valid && !Application.isPlaying) {
                return;
            }
            UpdateWorldPoints();
            path.SetWeightsFromPoints(worldPoints, splineTension);
            foreach(PenetrableListener listener in listeners) {
                listener.NotifyPenetration(this, penetrator, worldSpaceDistanceToPenisRoot, clipAction);
            }
            penetrationNotify?.Invoke(this, penetrator, worldSpaceDistanceToPenisRoot, clipAction);
        }
        public CatmullSpline GetSplinePath() {
            if (!valid && !Application.isPlaying) {
                return path;
            }

            UpdateWorldPoints();
            path.SetWeightsFromPoints(worldPoints, splineTension);
            return path;
        }
    }
}
