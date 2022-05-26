using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
#endif

namespace PenetrationTech {
    #if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Penetrator))]
    public class PenetratorEditor : Editor {
        static IEnumerable<PenetratorListenerAttribute> GetPenetratorListenerAttributes() {
            foreach(Type type in Assembly.GetExecutingAssembly().GetTypes()) {
                var attributes = (PenetratorListenerAttribute[])type.GetCustomAttributes(typeof(PenetratorListenerAttribute), true);
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
            List<PenetratorListenerAttribute> attributes =
                new List<PenetratorListenerAttribute>(GetPenetratorListenerAttributes());
            foreach(var attribute in attributes) {
                menu.AddItem(new GUIContent(attribute.name), false, ()=>{
                    foreach (var t in targets) {
                        Penetrator p = t as Penetrator;
                        if (p.listeners == null) {
                            p.listeners = new List<PenetratorListener>();
                        }
                        p.listeners.Add((PenetratorListener)Activator.CreateInstance(attribute.type));
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(p);
                    }
                });
            }
            menu.ShowAsContext();
        }
    }
    #endif
    public class Penetrator : CatmullDeformer {
        [SerializeField] [Range(0f, 2f)] private float virtualSquashAndStretch;
        private List<Vector3> weights;
        //[SerializeField]
        private GirthData girthData;
        [SerializeField]
        private Penetrable targetHole;
        private float length;
        private bool inserted;
        private float insertionFactor;
        [SerializeReference]
        public List<PenetratorListener> listeners;
        public float GetGirthScaleFactor() => girthData.GetGirthScaleFactor();
        public float GetWorldLength() => girthData.GetWorldLength();
        public float GetLocalLength() => girthData.GetLocalLength();
        public float GetWorldGirthRadius(float worldDistanceAlongDick) => girthData.GetWorldGirthRadius(worldDistanceAlongDick);
        public RenderTexture GetGirthMap() => girthData.GetGirthMap();
        private static readonly int startClipID = Shader.PropertyToID("_StartClip");
        private static readonly int endClipID = Shader.PropertyToID("_EndClip");
        private static readonly int squashStretchCorrectionID = Shader.PropertyToID("_SquashStretchCorrection");
        private static readonly int distanceToHoleID = Shader.PropertyToID("_DistanceToHole");
        private static readonly int dickWorldLengthID = Shader.PropertyToID("_DickWorldLength");
        public float GetPenetratorAngleOffset() {
            Vector3 initialRight = path.GetBinormalFromT(0f);
            Vector3 initialForward = path.GetVelocityFromT(0f).normalized;
            Vector3 initialUp = Vector3.Cross(initialForward, initialRight).normalized;
            Vector3 worldDickUp = rootBone.TransformDirection(localRootUp).normalized;
            Vector2 worldDickUpFlat = new Vector2(Vector3.Dot(worldDickUp,initialRight), Vector3.Dot(worldDickUp,initialUp));
            float angle = Mathf.Atan2(worldDickUpFlat.y, worldDickUpFlat.x)-Mathf.PI/2f;
            return angle;
        }
        public Vector3 GetWorldOffset(float worldDistanceAlongDick) {
            Vector3 offset = girthData.GetScaledSplineSpaceOffset(worldDistanceAlongDick);

            // Then we find our angle offset to the spline...
            // Then we rotate to the spline.
            float t = path.GetTimeFromDistance(worldDistanceAlongDick);
            float angle = GetPenetratorAngleOffset();
            return Quaternion.AngleAxis(angle*Mathf.Rad2Deg,path.GetVelocityFromT(t).normalized) * path.GetReferenceFrameFromT(t).MultiplyVector(offset);
        }

        protected override void OnEnable() {
            foreach (PenetratorListener listener in listeners) {
                listener.OnEnable(this);
            }
            base.OnEnable();
        }

        protected override void OnDisable() {
            foreach (PenetratorListener listener in listeners) {
                listener.OnDisable();
            }

            base.OnDisable();
        }


        protected override void Start() {
            base.Start();
            var position = transform.position;
            var forward = transform.forward;
            weights = new List<Vector3> {
                position,
                position+forward * 0.5f,
                position+forward*0.5f,
                position+forward
            };
            path = new CatmullSpline().SetWeights(weights);
            girthData = new GirthData(GetTargetRenderers()[0], rootBone, Vector3.zero, localRootForward, localRootUp, localRootRight);
        }

        void Update() {
            foreach (PenetratorListener listener in listeners) {
                listener.Update();
            }

        }

        void OnSetClip(float startDistance, float endDistance) {
            foreach (Material material in GetTargetMaterials()) {
                material.SetFloat(startClipID, startDistance);
                material.SetFloat(endClipID, endDistance);
            }
        }

        protected override void LateUpdate() {
            CatmullSpline holeSplinePath = targetHole.GetSplinePath();
            Vector3 holePos = holeSplinePath.GetPositionFromT(0f);
            Vector3 holeForward = holeSplinePath.GetVelocityFromT(0f).normalized;
            
            //Vector3 virtualHolePosition = holePos + holeForward * (extendAmount - retreatAmount);
            ConstructPath(holePos, holeForward);
            if (inserted) {
                float firstArcLength = path.GetDistanceFromSubT(0, 1, 1f);
                OnSetClip(1f, 1f);
                targetHole.SetPenetrationDepth(this, firstArcLength/virtualSquashAndStretch, OnSetClip);
                foreach (PenetratorListener listener in listeners) {
                    listener.NotifyPenetrationUpdate(this, targetHole, firstArcLength);
                }
            } else {
                foreach (PenetratorListener listener in listeners) {
                    listener.NotifyPenetrationUpdate(this, targetHole, GetWorldLength()+1f);
                }
            }

            foreach (Material material in GetTargetMaterials()) {
                material.SetFloat(squashStretchCorrectionID, virtualSquashAndStretch);
                material.SetFloat(dickWorldLengthID, GetWorldLength());
                material.SetFloat(distanceToHoleID, path.GetDistanceFromSubT(0, 1, 1f));
            }
            base.LateUpdate();
        }

        private void ConstructPath(Vector3 holePos, Vector3 holeForward) {
            var rootBonePosition = rootBone.position;
            float dist = Vector3.Distance(rootBonePosition, holePos);
            Vector3 tipPosition = rootBonePosition + rootBone.TransformDirection(localRootForward) * girthData.GetWorldLength();
            Vector3 tipTangent = -rootBone.TransformDirection(localRootForward) * (girthData.GetWorldLength() * 0.66f);
            if (tipTarget != null) {
                tipPosition = tipTarget.position+tipTarget.forward * (girthData.GetWorldLength() * 0.1f);
                tipTangent = tipTarget.forward * girthData.GetWorldLength();
            }
            weights.Clear();
            if (inserted) {
                insertionFactor = 1f;
                if (dist > girthData.GetWorldLength()*1.25f) {
                    inserted = false;
                    targetHole.SetPenetrationDepth(this, GetWorldLength() + 1f, OnSetClip);
                }
            } else {
                insertionFactor = Mathf.MoveTowards(insertionFactor, 0f, Time.deltaTime * 4f);
                insertionFactor = Mathf.Max(
                    insertionFactor,
                    Mathf.Clamp01(2f - Vector3.Distance(tipPosition, holePos) / (girthData.GetWorldLength() * 0.4f) * 2f)
                );
                if (insertionFactor >= 0.99f) inserted = true;
            }

            Vector3 penetratorTangent = Vector3.Lerp(
                rootBone.TransformDirection(localRootForward) * (girthData.GetWorldLength() * 0.66f),
                rootBone.TransformDirection(localRootForward) * (dist * 0.66f),
                insertionFactor
            );
            weights.Add(rootBonePosition);
            weights.Add(penetratorTangent);
            Vector3 insertionTangent = Vector3.Lerp(
                tipTangent, 
                holeForward * (dist * 0.66f),
                insertionFactor
            );
            Vector3 insertionPoint = Vector3.Lerp(
                tipPosition + (tipPosition - rootBonePosition) * (girthData.GetWorldLength() * 0.1f),
                holePos,
                insertionFactor
                );
            weights.Add(insertionTangent);
            weights.Add(insertionPoint);
            if (inserted) {
                targetHole.GetWeights(weights);
                CatmullSpline holeSplinePath = targetHole.GetSplinePath();
                Vector3 outPosition = holeSplinePath.GetPositionFromT(1f);
                Vector3 outTangent = holeSplinePath.GetVelocityFromT(1f).normalized;
                weights.Add(outPosition);
                weights.Add(outTangent);
                weights.Add(outTangent);
                weights.Add(outPosition+outTangent*GetWorldLength());
            }
            path.SetWeights(weights);
        }

        private void OnValidate() {
            if (listeners == null) {
                return;
            }

            foreach (PenetratorListener listener in listeners) {
                listener.OnValidate(this);
            }
        }
        public CatmullSpline GetSplinePath() {
            return path;
        }
        protected override void OnDrawGizmosSelected() {
            base.OnDrawGizmosSelected();
#if UNITY_EDITOR
            if (GetTargetRenderers() == null || GetTargetRenderers().Count == 0 || GetTargetRenderers()[0] == null || rootBone == null) {
                return;
            }

            if (!GirthData.IsValid(girthData)) {
                girthData = new GirthData(GetTargetRenderers()[0], rootBone, Vector3.zero, localRootForward,
                    localRootUp, localRootRight);
            }

            if (!Application.isPlaying) {
                if (path == null) {
                    path = new CatmullSpline().SetWeightsFromPoints(new Vector3[] {
                        rootBone.position,
                        rootBone.position + rootBone.TransformDirection(localRootForward) * GetWorldLength()
                    });
                }
                else {
                    path.SetWeightsFromPoints(new Vector3[] {
                        rootBone.position,
                        rootBone.position + rootBone.TransformDirection(localRootForward) * GetWorldLength()
                    });
                }
            }

            for(float t=0;t<GetWorldLength();t+=0.025f) {
                UnityEditor.Handles.color = Color.white;
                Vector3 position = path.GetPositionFromDistance(t) + GetWorldOffset(t);
                float girth = GetWorldGirthRadius(t);
                UnityEditor.Handles.DrawWireDisc(position, path.GetVelocityFromDistance(t).normalized, girth);
            }

            if (listeners == null) {
                return;
            }

            foreach (PenetratorListener listener in listeners) {
                listener.OnDrawGizmosSelected(this);
            }
#endif
        }
        
    }

}
