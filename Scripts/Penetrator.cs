using System.Collections.Generic;
using System.Linq;
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
                        p.listeners[p.listeners.Count - 1].OnEnable(p);
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(p);
                    }
                });
            }
            menu.ShowAsContext();
        }
    }
    #endif
    [ExecuteAlways]
    public class Penetrator : CatmullDeformer {
        [SerializeField] [Range(0f, 2f)] private float virtualSquashAndStretch;
        private List<Vector3> weights;
        [SerializeField]
        private GirthData girthData;
        [SerializeField]
        private Penetrable targetHole;
        [SerializeField]
        private bool autoPenetrate = false;
        [SerializeReference]
        public List<PenetratorListener> listeners;
        private float length;
        private bool inserted;
        private float insertionFactor;
        private MaterialPropertyBlock propertyBlock;
        private static Collider[] colliders = new Collider[32];
        public float GetGirthScaleFactor() => girthData.GetGirthScaleFactor();
        public float GetWorldLength() => girthData.GetWorldLength();
        public float GetLocalLength() => girthData.GetLocalLength();
        public float GetWorldGirthRadius(float worldDistanceAlongDick) => girthData.GetWorldGirthRadius(worldDistanceAlongDick);
        public float GetKnotForce(float worldDistanceAlongDick) => girthData.GetKnotForce(worldDistanceAlongDick);
        public float squashAndStretch {
            get => virtualSquashAndStretch;
            set => virtualSquashAndStretch = value;
        }

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

            propertyBlock = new MaterialPropertyBlock();
            base.OnEnable();
        }

        protected override void OnDisable() {
            foreach (PenetratorListener listener in listeners) {
                listener.OnDisable();
            }

            propertyBlock = null;
            base.OnDisable();
        }


        void Start() {
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
            OnSetClip(0f, 0f);
        }

        void Update() {
            if (autoPenetrate && !inserted) {
                Vector3 tipPosition;
                if (path != null && path.GetWeights().Count > 0) {
                    tipPosition = path.GetPositionFromDistance(GetWorldLength() * virtualSquashAndStretch);
                } else {
                    tipPosition = rootBone.position + rootBone.TransformDirection(localRootForward) * (GetWorldLength() * virtualSquashAndStretch);
                }
                int hits = Physics.OverlapSphereNonAlloc(tipPosition, 1f, colliders);
                Penetrable bestMatch = null;
                float bestDistance = float.MaxValue;
                // TODO: Match by best result, probably weighted by distance and angle...
                for (int i = 0; i < hits; i++) {
                    PenetrableOwner owner = colliders[i].GetComponent<PenetrableOwner>();
                    if (owner != null) {
                        float distance = Vector3.Distance(colliders[i].transform.position, tipPosition);
                        if (distance < bestDistance) {
                            bestMatch = owner.owner;
                            bestDistance = distance;
                        }
                    }
                }
                targetHole = bestMatch;
            }

            foreach (PenetratorListener listener in listeners) {
                listener.Update();
            }
        }

        void OnSetClip(float startDistance, float endDistance) {
            foreach (Renderer renderer in GetTargetRenderers()) {
                renderer.GetPropertyBlock(propertyBlock);
                if (renderer is SkinnedMeshRenderer) {
                    propertyBlock.SetFloat(startClipID, startDistance);
                    propertyBlock.SetFloat(endClipID, endDistance);
                } else {
                    // TODO: So I try to keep everything in world-space, so even in the shader it ends up being the right scale. For some reason mesh renderers ignore some scale features-- which compound world space transformation problems.
                    // This seemingly only affects the clip??? This cannot be-- need to test other situations.
                    float lossyScale = Vector3.Dot(rootBone.lossyScale,localRootForward);
                    propertyBlock.SetFloat(startClipID, (startDistance/lossyScale)/lossyScale);
                    propertyBlock.SetFloat(endClipID, (endDistance/lossyScale)/lossyScale);
                }

                renderer.SetPropertyBlock(propertyBlock);
            }
        }

        protected override void LateUpdate() {
#if UNITY_EDITOR
            if (targetHole == null) {
                if (rootBone == null) {
                    return;
                }
                OnSetClip(0f, 0f);
                path.SetWeightsFromPoints(new Vector3[] {
                    rootBone.position,
                    rootBone.position + rootBone.TransformDirection(localRootForward) * GetWorldLength()
                });
                ConstructPath(Vector3.zero, Vector3.up);
                base.LateUpdate();
                return;
            }
#endif

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
                    listener.NotifyPenetrationUpdate(this, targetHole, firstArcLength/virtualSquashAndStretch);
                }
            } else {
                foreach (PenetratorListener listener in listeners) {
                    listener.NotifyPenetrationUpdate(this, targetHole, GetWorldLength()+1f);
                }
            }

            foreach (Renderer renderer in GetTargetRenderers()) {
                renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(squashStretchCorrectionID, virtualSquashAndStretch);
                propertyBlock.SetFloat(dickWorldLengthID, GetWorldLength());
                propertyBlock.SetFloat(distanceToHoleID, path.GetDistanceFromSubT(0, 1, 1f));
                renderer.SetPropertyBlock(propertyBlock);
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
                // TODO: The "uninsert" routine probably shouldn't be in ConstructPath-- it also should probably be a function so users can purposefully eject a penetrator easily.
                if (dist > girthData.GetWorldLength()*1.25f) {
                    inserted = false;
                    targetHole.SetPenetrationDepth(this, GetWorldLength() + 1f, OnSetClip);
                    OnSetClip(0f, 0f);
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
            
            // If a user added a new listener, since we're actively running in the scene we need to make sure that they're enabled.
            foreach (PenetratorListener listener in listeners) {
                listener.OnDisable();
            }
            foreach (PenetratorListener listener in listeners) {
                listener.OnEnable(this);
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

                if (targetHole == null) {
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
