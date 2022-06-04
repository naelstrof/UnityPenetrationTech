using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
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
            string lastError = ((Penetrator)target).GetLastError();
            if (!string.IsNullOrEmpty(lastError)) {
                EditorGUILayout.HelpBox(lastError, MessageType.Error);
            } else {
                EditorGUILayout.HelpBox("Make sure the blue dotted line is pointed along the penetrator by adjusting the Local Root forward/up/right.\n" +
                                                "If the model is inside-out, one of the vectors is backwards.\n" +
                                                "If you don't see the blue dotted line, ensure Gizmos are enabled.", MessageType.Info);
            }

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
        [SerializeField] [Range(0f, 2f)] [Tooltip("Squash or stretch the visuals of the penetrator, this can be triggered through listeners, script, or animation to simulate knot forces.")]
        private float virtualSquashAndStretch = 1f;
        [SerializeField] [Tooltip("A transform for the curve to pass through when it's not busy penetrating, useful to tie the penetrator to some physics! Completely optional.")]
        protected Transform tipTarget;

        [SerializeField] [Tooltip("If you need to mask parts of the model out, customizing this shader will allow you to mask the girthmap generation (so things like heads or feet don't show up in it).")]
        private Shader girthUnwrapShader;
        [SerializeField]
        private GirthData girthData;
        [SerializeField][ReadOnly]
        private RenderTexture girthMap;
        [SerializeField] [Tooltip("If autoPenetrate is disabled, you can tell the penetrator specifically what to penetrate with here.")]
        private Penetrable targetHole;
        [SerializeField] [Tooltip("Automate discovery of penetrables, and automatically penetrate with them if some basic conditions are met (roughly the right angle, and distance)")]
        private bool autoPenetrate = false;
        [SerializeReference] [Tooltip("Programmable listeners, they can respond to penetrations in a variety of ways. Great for triggering audio and such.")]
        public List<PenetratorListener> listeners;

        
        private List<Vector3> weightsA;
        private List<Vector3> weightsB;
        private List<Vector3> outputWeights;
        private bool inserted;
        private float insertionFactor;
        private MaterialPropertyBlock propertyBlock;
        private static Collider[] colliders = new Collider[32];
        public float GetGirthScaleFactor() => girthData.GetGirthScaleFactor();
        public float GetWorldLength() => girthData.GetWorldLength();
        public float GetWorldGirthRadius(float worldDistanceAlongDick) => girthData.GetWorldGirthRadius(worldDistanceAlongDick);
        public float GetKnotForce(float worldDistanceAlongDick) => girthData.GetKnotForce(worldDistanceAlongDick);
        public float squashAndStretch {
            get => virtualSquashAndStretch;
            set => virtualSquashAndStretch = value;
        }

        public RenderTexture GetGirthMap() {
            if (girthMap == null) {
                girthMap = new RenderTexture(256, 256, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
                girthMap.useMipMap = true;
                girthMap.autoGenerateMips = false;
                girthMap.wrapMode = TextureWrapMode.Repeat;
            }
            return girthMap;
        }

        private static readonly int startClipID = Shader.PropertyToID("_StartClip");
        private static readonly int endClipID = Shader.PropertyToID("_EndClip");
        private static readonly int squashStretchCorrectionID = Shader.PropertyToID("_SquashStretchCorrection");
        private static readonly int distanceToHoleID = Shader.PropertyToID("_DistanceToHole");
        private static readonly int dickWorldLengthID = Shader.PropertyToID("_DickWorldLength");
        private bool valid = false;
        private string lastError;
        public string GetLastError() {
            return lastError;
        }
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

        private void Initialize() {
            if (Application.isPlaying) {
                valid = true;
                lastError = "";
                CheckValid();
            }

            if (!valid) {
                return;
            }
            
            foreach (PenetratorListener listener in listeners) {
                listener.OnEnable(this);
            }
            girthData = new GirthData(GetGirthMap(), GetTargetRenderers()[0], girthUnwrapShader, rootBone, Vector3.zero, localRootForward,
                    localRootUp, localRootRight);
            OnSetClip(0f, 0f);
        }

        protected override void OnEnable() {
            base.OnEnable();
            propertyBlock = new MaterialPropertyBlock();
            var position = transform.position;
            var forward = transform.forward;
            weightsA = new List<Vector3> {
                position,
                position+forward * 0.5f,
                position+forward*0.5f,
                position+forward
            };
            weightsB = new List<Vector3>();
            outputWeights = new List<Vector3>();
            path = new CatmullSpline().SetWeights(weightsA);
        }
        
        public override void SetTargetRenderers(ICollection<RendererSubMeshMask> renderers) {
            base.SetTargetRenderers(renderers);
            Initialize();
            if (!string.IsNullOrEmpty(GetLastError())) {
                throw new UnityException(lastError);
            }
        }

        protected override void OnDisable() {
            base.OnDisable();
            if (!valid) {
                return;
            }
            foreach (PenetratorListener listener in listeners) {
                listener.OnDisable();
            }
        }


        void Start() {
            Initialize();
        }

        void Update() {
            if (!valid) {
                return;
            }

            if (autoPenetrate && !inserted) {
                Vector3 tipPosition;
                if (path != null && path.GetWeights().Count > 0) {
                    tipPosition = path.GetPositionFromDistance(GetWorldLength() * virtualSquashAndStretch);
                } else {
                    tipPosition = rootBone.position + rootBone.TransformDirection(localRootForward) * (GetWorldLength() * virtualSquashAndStretch);
                }
                int hits = Physics.OverlapSphereNonAlloc(tipPosition, 1f, colliders);
                PenetrableOwner bestMatch = null;
                float bestDistance = float.MaxValue;
                // TODO: Match by best result, probably weighted by distance and angle...
                for (int i = 0; i < hits; i++) {
                    PenetrableOwner owner = colliders[i].GetComponent<PenetrableOwner>();
                    if (owner != null) {
                        float distance = Vector3.Distance(colliders[i].transform.position, tipPosition);
                        if (distance < bestDistance) {
                            bestMatch = owner;
                            bestDistance = distance;
                        }
                    }
                }
                if (bestMatch != null) {
                    targetHole = bestMatch.owner;
                }
            }

            foreach (PenetratorListener listener in listeners) {
                listener.Update();
            }
        }

        void OnSetClip(float startDistance, float endDistance) {
            foreach (RendererSubMeshMask rendererMask in GetTargetRenderers()) {
                rendererMask.renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(startClipID, startDistance);
                propertyBlock.SetFloat(endClipID, endDistance);
                rendererMask.renderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void UpdateInsertionAmount(Penetrable penetrable, Vector3 tipPosition) {
            if (penetrable == null) {
                inserted = false;
                insertionFactor = 0f;
                return;
            }

            Vector3 holePos = penetrable.GetSplinePath().GetPositionFromT(0f);
            float fakeDistance = Vector3.Distance(holePos, rootBone.position);
            if (inserted) {
                if (fakeDistance > girthData.GetWorldLength()*1.25f) {
                    insertionFactor = Mathf.MoveTowards(insertionFactor, 0f, Time.deltaTime * 4f);
                } else {
                    insertionFactor = 1f;
                }

                if (insertionFactor <= 0.01f) inserted = false;
            } else {
                insertionFactor = Mathf.MoveTowards(insertionFactor, 0f, Time.deltaTime * 4f);
                insertionFactor = Mathf.Max(
                    insertionFactor,
                    Mathf.Clamp01(2f - Vector3.Distance(tipPosition, holePos) / (girthData.GetWorldLength() * 0.4f) * 2f)
                );
                if (insertionFactor >= 0.99f) inserted = true;
            }
        }

        // TODO: This doesn't properly stitch curves of different weight counts well, though this currently doesn't matter for our use-case.
        private void StitchWeights(IList<Vector3> a, IList<Vector3> b, IList<Vector3> output, float t) {
            output.Clear();
            for (int i = 0; i < Mathf.Min(a.Count, b.Count); i++) {
                output.Add(Vector3.Lerp(a[i], b[i], t));
            }
            for (int i = output.Count; i < a.Count; i++) {
                output.Add(a[i]);
            }

            if (t != 0f) {
                for (int i = output.Count; i < b.Count; i++) {
                    output.Add(b[i]);
                }
            }
        }

        protected override void LateUpdate() {
            if (!valid) {
                return;
            }
            
            Vector3 tipPosition = rootBone.position + rootBone.TransformDirection(localRootForward) * girthData.GetWorldLength();
            Vector3 tipTangent = -rootBone.TransformDirection(localRootForward) * (girthData.GetWorldLength() * 0.66f);
            if (tipTarget != null) {
                var forward = tipTarget.forward;
                tipPosition = tipTarget.position+forward * (girthData.GetWorldLength() * 0.1f);
                tipTangent = forward * (girthData.GetWorldLength() * 2f);
            }
            
            UpdateInsertionAmount(targetHole, tipPosition);
            
            if (!inserted && insertionFactor < 0.01f) {
                OnSetClip(0f, 0f);
                weightsA.Clear();
                ConstructPathForIdle(weightsA, tipPosition, tipTangent);
                path.SetWeights(weightsA);
                base.LateUpdate();
                return;
            }
            
            CatmullSpline holeSplinePath = targetHole.GetSplinePath();
            Vector3 holePos = holeSplinePath.GetPositionFromT(0f);
            Vector3 holeForward = holeSplinePath.GetVelocityFromT(0f).normalized;
            
            ConstructPathForIdle(weightsA, tipPosition, tipTangent);
            ConstructPathToPenetrable(weightsB, targetHole);
            StitchWeights(weightsA, weightsB, outputWeights, insertionFactor);
            path.SetWeights(outputWeights);
            
            float realDistanceToHole = path.GetDistanceFromSubT(0, 1, 1f);
            
            if (inserted) {
                OnSetClip(1f, 1f);
                targetHole.SetPenetrationDepth(this, realDistanceToHole/virtualSquashAndStretch, OnSetClip);
                foreach (PenetratorListener listener in listeners) {
                    listener.NotifyPenetrationUpdate(this, targetHole, realDistanceToHole/virtualSquashAndStretch);
                }
            } else {
                foreach (PenetratorListener listener in listeners) {
                    listener.NotifyPenetrationUpdate(this, targetHole, GetWorldLength()+1f);
                }
            }

            foreach (RendererSubMeshMask rendererMask in GetTargetRenderers()) {
                rendererMask.renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(squashStretchCorrectionID, virtualSquashAndStretch);
                propertyBlock.SetFloat(dickWorldLengthID, GetWorldLength());
                propertyBlock.SetFloat(distanceToHoleID, realDistanceToHole);
                rendererMask.renderer.SetPropertyBlock(propertyBlock);
            }
            base.LateUpdate();
        }

        private void ConstructPathForIdle(ICollection<Vector3> output, Vector3 tipPosition, Vector3 tipTangent) {
            output.Clear();
            var rootBonePosition = rootBone.position;

            Vector3 penetratorTangent =
                rootBone.TransformDirection(localRootForward) * (girthData.GetWorldLength() * 0.66f);
            output.Add(rootBonePosition);
            output.Add(penetratorTangent);
            Vector3 insertionTangent = tipTangent;
            Vector3 insertionPoint =
                tipPosition + (tipPosition - rootBonePosition) * (girthData.GetWorldLength() * 0.1f);
            output.Add(insertionTangent);
            output.Add(insertionPoint);
        }

        private void ConstructPathToPenetrable(ICollection<Vector3> output, Penetrable penetrable) {
            output.Clear();
            CatmullSpline holeSplinePath = targetHole.GetSplinePath();
            Vector3 holePos = holeSplinePath.GetPositionFromT(0f);
            Vector3 holeForward = holeSplinePath.GetVelocityFromT(0f).normalized;
            float worldLength = girthData.GetWorldLength();

            var rootBonePosition = rootBone.position;
            float fakeDistance = Vector3.Distance(rootBonePosition, holePos);
            Vector3 penetratorTangent = rootBone.TransformDirection(localRootForward) * (fakeDistance * 0.66f);
            output.Add(rootBonePosition);
            output.Add(penetratorTangent);
            Vector3 insertionTangent = holeForward * (fakeDistance * 0.66f);
            Vector3 insertionPoint = holePos;
            output.Add(insertionTangent);
            output.Add(insertionPoint);
            
            targetHole.GetWeights(output);
            Vector3 outPosition = holeSplinePath.GetPositionFromT(1f);
            Vector3 outTangent = holeSplinePath.GetVelocityFromT(1f).normalized;
            output.Add(outPosition);
            output.Add(outTangent);
            output.Add(outTangent);
            output.Add(outPosition+outTangent*GetWorldLength());
        }
        private class PenetratorValidationException : System.SystemException {
            public PenetratorValidationException(string msg) : base(msg) { }
        }
        
        private void AssertValid(bool condition, string errorMsg) {
            valid = valid && condition;
            if (!condition) {
                throw new PenetratorValidationException(errorMsg);
            }
        }
        private void CheckValid() {
            valid = true;
            lastError = "";
            try {
                AssertValid(rootBone != null, "Root bone must be specified.");

                AssertValid(
                    GetTargetRenderers() != null && GetTargetRenderers().Count > 0 &&
                    GetTargetRenderers()[0].renderer != null,
                    "Must specify a target renderer.");
                AssertValid(GetTargetRenderers()[0].mask != 0,
                    "Must have at least one sub-mesh enabled on the renderer, (the one that contains the penetrator hopefully).");
                bool isChild = false;
                foreach (var renderMask in GetTargetRenderers()) {
                    if (renderMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                        if (rootBone.IsChildOf(skinnedMeshRenderer.rootBone)) {
                            isChild = true;
                            break;
                        }
                    } else if (renderMask.renderer is MeshRenderer meshRenderer) {
                        if (rootBone.IsChildOf(meshRenderer.transform) || rootBone == meshRenderer.transform) {
                            isChild = true;
                            break;
                        }
                    }
                }
                foreach(var renderMask in GetTargetRenderers()) {
                    if (renderMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                        var sharedMesh = skinnedMeshRenderer.sharedMesh;
                        AssertValid(sharedMesh != null && sharedMesh.isReadable,
                            $"The mesh {sharedMesh}, is either null or not readable. Please enable Read/Write Enabled in the import settings.");
                    } else if (renderMask.renderer is MeshRenderer meshRenderer) {
                        var sharedMesh = meshRenderer.GetComponent<MeshFilter>().sharedMesh;
                        AssertValid(sharedMesh != null && sharedMesh.isReadable,
                            $"The mesh {sharedMesh} is either null or not readable. Please enable Read/Write Enabled in the import settings.");
                    } else {
                        throw new PenetratorValidationException(
                            $"Only SkinnedMeshRenderer and MeshRenderers are supported for Penetrators. {renderMask.renderer.GetType().ToString()} is not supported.");
                    }
                }

                AssertValid(isChild,
                    "Root bone must be a child transform of the Renderer. If its a skinned mesh renderer, you'd want to target the Transform at the base of the penetrator.");
                
                bool hasNullListener = false;
                foreach (PenetratorListener listener in listeners) {
                    if (listener == null || !listener.GetType().IsSubclassOf(typeof(PenetratorListener))) {
                        hasNullListener = true;
                    }
                }
                AssertValid(hasNullListener == false,
                    "There's a null or empty listener in the listener list, this is not allowed.");
                
                foreach (var listener in listeners) {
                    listener.AssertValid();
                }
            } catch (PenetratorValidationException error) {
                lastError = $"{error.Message}\n\n{error.StackTrace}";
                valid = false;
            } catch (PenetratorListener.PenetratorListenerValidationException error) {
                lastError = $"{error.Message}\n\n{error.StackTrace}";
                valid = false;
            }
        }
        public void Penetrate(Penetrable penetrable) {
            targetHole = penetrable;
            insertionFactor = 1f;
            inserted = true;
        }

        private void OnValidate() {
            if (girthUnwrapShader == null) {
                girthUnwrapShader = Shader.Find("PenetrationTech/GirthUnwrapRaw");
            }

            CheckValid();
            if (listeners == null) { listeners = new List<PenetratorListener>(); }

            weightsA ??= new List<Vector3>();
            weightsB ??= new List<Vector3>();
            outputWeights ??= new List<Vector3>();

            if (path == null) {
                path = new CatmullSpline().SetWeights(new Vector3[]
                    { Vector3.zero, Vector3.one, Vector3.one, Vector3.one });
            }

            if (path.GetWeights().Count == 0) {
                path.SetWeights(new Vector3[] { Vector3.zero, Vector3.one, Vector3.one, Vector3.one });
            }
            
            if (!valid) {
                return;
            }

            girthData = new GirthData(GetGirthMap(), GetTargetRenderers()[0], girthUnwrapShader, rootBone, Vector3.zero, localRootForward, localRootUp,
                localRootRight);

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
            if (!valid) {
                return;
            }
#if UNITY_EDITOR
            for(float t=0;t<GetWorldLength();t+=0.025f) {
                UnityEditor.Handles.color = Color.white;
                Vector3 position = path.GetPositionFromDistance(t) + GetWorldOffset(t);
                float girth = GetWorldGirthRadius(t);
                UnityEditor.Handles.DrawWireDisc(position, path.GetVelocityFromDistance(t).normalized, girth);
            }
            UnityEditor.Handles.color = Color.blue;
            var rootBonePosition = rootBone.transform.position;
            UnityEditor.Handles.DrawDottedLine(rootBonePosition,
                rootBonePosition + rootBone.transform.TransformDirection(localRootForward), 10f);
            foreach (PenetratorListener listener in listeners) {
                listener.OnDrawGizmosSelected(this);
            }
#endif
        }
    }

}
