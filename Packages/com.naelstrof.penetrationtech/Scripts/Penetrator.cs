using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
#endif

namespace PenetrationTech {
    #if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Penetrator))]
    public class PenetratorEditor : Editor {
        /*static IEnumerable<PenetratorListenerAttribute> GetPenetratorListenerAttributes() {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (Type type in assembly.GetTypes()) {
                    var attributes =
                        (PenetratorListenerAttribute[])type.GetCustomAttributes(typeof(PenetratorListenerAttribute),
                            true);
                    if (attributes.Length > 0) {
                        yield return attributes[0];
                    }
                }
            }
        }*/
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
            /*
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
            menu.ShowAsContext();*/
        }
    }
    #endif
    [ExecuteAlways]
    public class Penetrator : CatmullDeformer {
        [System.Flags]
        private enum AutoPenetrateMode {
            None = 0,
            AutoSeek = 1,
            AutoDecouple = 2,
        }

        [SerializeField] [Range(0f, 2f)] [Tooltip("Squash or stretch the visuals of the penetrator, this can be triggered through listeners, script, or animation to simulate knot forces.")]
        private float virtualSquashAndStretch = 1f;
        [SerializeField] [Tooltip("A transform for the curve to pass through when it's not busy penetrating, useful to tie the penetrator to some physics! Completely optional.")]
        protected Transform tipTarget;

        [SerializeField] [Tooltip("If you need to mask parts of the model out, customizing this shader will allow you to mask the girthmap generation (so things like heads or feet don't show up in it).")]
        private Shader girthUnwrapShader;
        private GirthData girthData;
        [FormerlySerializedAs("targetHole")] [SerializeField] [Tooltip("If autoPenetrate is disabled, you can tell the penetrator specifically what to penetrate with here.")]
        private Penetrable penetratedHole;

        [SerializeField] [Range(0.001f,5f)] [Tooltip("How lenient we are with penetrators being off-target, measured in penetrator-lengths.")]
        private float penetrationMarginOfError = 0.5f;
        [SerializeField] [Tooltip("Automate discovery of penetrables, and automatically penetrate with them if some basic conditions are met (roughly the right angle, and distance). Also decouple automatically if basic conditions are met (penetrator is certain distance away).")]
        private AutoPenetrateMode autoPenetrate = AutoPenetrateMode.AutoSeek | AutoPenetrateMode.AutoDecouple;
        [SerializeReference,SerializeReferenceButton] [Tooltip("Programmable listeners, they can respond to penetrations in a variety of ways. Great for triggering audio and such.")]
        public List<PenetratorListener> listeners;
        


        public delegate void PenetrationAction(Penetrable penetrable);
        public PenetrationAction penetrationStart;
        public PenetrationAction penetrationEnd;
        
        private Penetrable targetHoleA;
        private Penetrable targetHoleB;
        private float targetHoleLerp;
        
        private List<Vector3> weightsA;
        private List<Vector3> weightsB;
        private List<Vector3> weightsC;
        private List<Vector3> outputWeightsA;
        private List<Vector3> outputWeightsB;
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
            return girthData.GetGirthMap();
        }

        private static readonly int startClipID = Shader.PropertyToID("_StartClip");
        private static readonly int endClipID = Shader.PropertyToID("_EndClip");
        private static readonly int squashStretchCorrectionID = Shader.PropertyToID("_SquashStretchCorrection");
        private static readonly int distanceToHoleID = Shader.PropertyToID("_DistanceToHole");
        private static readonly int dickWorldLengthID = Shader.PropertyToID("_DickWorldLength");
        private bool valid = false;
        private string lastError;
        private bool reinitialize = false;
        public string GetLastError() {
            return lastError;
        }

        public bool TryGetPenetrable(out Penetrable penetrable) {
            if (!inserted) {
                penetrable = null;
                return false;
            }
            penetrable = penetratedHole;
            return true;
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

        public Vector3 GetWorldForward() {
            return rootBone.TransformDirection(localRootForward);
        }
        public Vector3 GetWorldPosition() {
            return rootBone.position;
        }
        public Vector3 GetWorldRight() {
            return rootBone.TransformDirection(localRootRight);
        }
        public Vector3 GetWorldUp() {
            return rootBone.TransformDirection(localRootUp);
        }

        protected override void OnEnable() {
            base.OnEnable();
            propertyBlock = new MaterialPropertyBlock();
            var position = transform.position;
            var forward = transform.forward;
            weightsA = new List<Vector3>();
            weightsB = new List<Vector3>();
            weightsC = new List<Vector3>();
            outputWeightsA = new List<Vector3>();
            outputWeightsB = new List<Vector3>();
            path = new CatmullSpline();
            Initialize();
        }

        private void Initialize() {
            if (!Application.isPlaying) {
                CheckValid();
                if (!valid) {
                    return;
                }
            }

            if (girthData != null) {
                girthData.Release();
                girthData = null;
            }

            girthData = new GirthData(GetTargetRenderers()[0], girthUnwrapShader, rootBone, Vector3.zero, localRootForward,
                    localRootUp, localRootRight);
            GetTipPositionAndTangent(out Vector3 tipPosition, out Vector3 tipTangent);
            ConstructPathForIdle(weightsA, tipPosition, tipTangent);
            path.SetWeights(weightsA);
            foreach (PenetratorListener listener in listeners) {
                listener.OnEnable(this);
            }
            OnSetClip(0f, 0f);
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
            targetHoleLerp = 1f;
            if (!valid && !Application.isPlaying) {
                return;
            }
            foreach (PenetratorListener listener in listeners) {
                listener.OnDisable();
            }
        }

        private void FixedUpdate() {
            if (!valid && !Application.isPlaying) {
                return;
            }

            foreach (PenetratorListener listener in listeners) {
                listener.FixedUpdate();
            }
        }

        void Update() {
            #if UNITY_EDITOR
            if (reinitialize && !Application.isPlaying) {
                CheckValid();
                if (!valid) {
                    return;
                }
                foreach (var listener in listeners) {
                    listener.OnDisable();
                }
                foreach (var listener in listeners) {
                    listener.OnEnable(this);
                }
                reinitialize = false;
            }
            #endif
            
            if (!valid && !Application.isPlaying) {
                return;
            }

            targetHoleLerp = Mathf.MoveTowards(targetHoleLerp, 1f, Time.deltaTime*4f);
            if ((autoPenetrate&AutoPenetrateMode.AutoSeek)!=0 && !inserted && targetHoleLerp == 1f) {
                Vector3 tipPosition = rootBone.position + rootBone.TransformDirection(localRootForward) * (GetWorldLength() * virtualSquashAndStretch);
                int hits = Physics.OverlapSphereNonAlloc(tipPosition, 1f, colliders, PenetrationTechTools.GetPenetrableMask(), QueryTriggerInteraction.Collide);
                PenetrableOwner bestMatch = null;
                float bestValue = float.MaxValue;
                for (int i = 0; i < hits; i++) {
                    PenetrableOwner owner = colliders[i].GetComponent<PenetrableOwner>();
                    if (owner != null) {
                        float distance = Vector3.Distance(colliders[i].transform.position, tipPosition);
                        // Being a full 180 degrees around will add a meter to the distance evaluation.
                        float angle = Vector3.Angle(owner.owner.GetSplinePath().GetVelocityFromT(0f).normalized,
                            rootBone.TransformDirection(localRootForward))/180f;
                        float value = angle+distance;
                        if (value < bestValue) {
                            bestMatch = owner;
                            bestValue = value;
                        }
                    }
                }
                if (bestMatch != null) {
                    SetTargetHole(bestMatch.owner);
                }
            }

            foreach (PenetratorListener listener in listeners) {
                listener.Update();
            }
        }

        private void OnDestroy() {
            Penetrate(null);
            if (girthData != null) {
                girthData.Release();
                girthData = null;
            }
        }

        public void SetTargetHole(Penetrable target) {
            if (target == targetHoleA) {
                return;
            }
            targetHoleB = targetHoleA;
            targetHoleA = target;
            targetHoleLerp = 1f - targetHoleLerp;
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
                Penetrate(null);
                return;
            }

            Vector3 holePos = penetrable.GetSplinePath().GetPositionFromT(0f);
            if (inserted) {
                return;
            }
            insertionFactor = Mathf.MoveTowards(insertionFactor, 0f, Time.deltaTime * 4f);
            insertionFactor = Mathf.Max(
                insertionFactor,
                Mathf.Clamp01(2f - Vector3.Distance(tipPosition, holePos) / (girthData.GetWorldLength() * penetrationMarginOfError) * 2f) * targetHoleLerp
            );
            if (insertionFactor >= 0.99f) {
                Penetrate(penetrable);
            }
        }

        private bool StitchWeights(List<Vector3> a, List<Vector3> b, List<Vector3> output, float t) {
            output.Clear();
            if (t == 0f || b.Count == 0) {
                output.AddRange(a);
                return false;
            }

            if (Math.Abs(t - 1f) < 0.001f || a.Count == 0) {
                output.AddRange(b);
                return false;
            }

            for (int i = 0; i < Mathf.Min(a.Count, b.Count); i++) {
                output.Add(Vector3.Lerp(a[i], b[i], t));
            }

            int stitchLocation = output.Count;
            
            for (int i = output.Count; i < a.Count; i++) {
                output.Add(a[i]);
            }
            for (int i = output.Count; i < b.Count; i++) {
                output.Add(b[i]);
            }

            // Generate a quick spline segment to connect the two seamlessly.
            if (stitchLocation != output.Count) {
                Vector3 p0 = output[stitchLocation - 1];
                Vector3 p1 = output[stitchLocation];
                Vector3 m0 = output[stitchLocation - 2];
                Vector3 m1 = output[stitchLocation + 1];
                output.Insert(stitchLocation, p1);
                output.Insert(stitchLocation, m1);
                output.Insert(stitchLocation, m0);
                output.Insert(stitchLocation, p0);
                return true;
            }

            return false;
        }

        private void GetTipPositionAndTangent(out Vector3 tipPosition, out Vector3 tipTangent) {
            float worldLength = girthData.GetWorldLength();
            tipPosition = rootBone.position + rootBone.TransformDirection(localRootForward) * (worldLength * 1.1f);
            tipTangent = rootBone.TransformDirection(localRootForward) * (worldLength * 0.66f);
            if (tipTarget != null) {
                var forward = tipTarget.forward;
                Vector3 newPosition = tipTarget.position + forward * (worldLength * 0.1f);
                Vector3 diff = newPosition - tipPosition;
                float dist = diff.magnitude;
                tipPosition = tipPosition + diff.normalized * Mathf.Min(dist, worldLength * 0.1f);
                tipTangent = forward * (worldLength * 2f);
            }
        }

        protected override void LateUpdate() {
            if (!valid && !Application.isPlaying) {
                return;
            }

            GetTipPositionAndTangent(out Vector3 tipPosition, out Vector3 tipTangent);
            
            UpdateInsertionAmount(targetHoleA, tipPosition);
            
            ConstructPathToPenetrable(weightsB, targetHoleA);
            ConstructPathToPenetrable(weightsC, targetHoleB);
            StitchWeights(weightsC, weightsB, outputWeightsA, targetHoleLerp);
            
            ConstructPathForIdle(weightsA, tipPosition, tipTangent);
            bool addedSegment = StitchWeights(weightsA, outputWeightsA, outputWeightsB, insertionFactor);
            
            path.SetWeights(outputWeightsB);
            
            float realDistanceToHole = path.GetDistanceFromSubT(0, addedSegment ? 2 : 1, 1f) + GetWorldLength()*(1f-targetHoleLerp);
            if (penetratedHole != null) {
                OnSetClip(1f, 1f);
                penetratedHole.SetPenetrationDepth(this, realDistanceToHole/virtualSquashAndStretch, OnSetClip);
                foreach (PenetratorListener listener in listeners) {
                    listener.NotifyPenetrationUpdate(this, penetratedHole, realDistanceToHole/virtualSquashAndStretch);
                }
            }
            if (inserted && realDistanceToHole > GetWorldLength()*Mathf.Max(virtualSquashAndStretch,1f)) {
                Penetrate(null);
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
            if (penetrable == null) {
                return;
            }
            CatmullSpline holeSplinePath = penetrable.GetSplinePath();
            
            Vector3 holePos = holeSplinePath.GetPositionFromT(0f);
            Vector3 holeForward = holeSplinePath.GetVelocityFromT(0f).normalized;

            var rootBonePosition = rootBone.position;
            float fakeDistance = Vector3.Distance(rootBonePosition, holePos);
            Vector3 penetratorTangent = rootBone.TransformDirection(localRootForward) * (fakeDistance * 0.66f);
            output.Add(rootBonePosition);
            output.Add(penetratorTangent);
            Vector3 insertionTangent = holeForward * (fakeDistance * 0.66f);
            Vector3 insertionPoint = holePos;
            output.Add(insertionTangent);
            output.Add(insertionPoint);
            
            penetrable.GetWeights(output);
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
            girthUnwrapShader ??= Shader.Find("PenetrationTech/GirthUnwrapRaw");
            listeners ??= new List<PenetratorListener>();
            weightsA ??= new List<Vector3>();
            weightsB ??= new List<Vector3>();
            outputWeightsA ??= new List<Vector3>();
            outputWeightsB ??= new List<Vector3>();
            path ??= new CatmullSpline();
            if (path.GetWeights().Count == 0) {
                path.SetWeights( new Vector3[] { Vector3.zero, Vector3.up, Vector3.up, Vector3.zero });
            }


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
            if (girthData == null || !GirthData.IsValid(girthData, localRootForward, localRootRight, localRootUp)) {
                if (girthData != null) {
                    girthData.Release();
                    girthData = null;
                }

                girthData = new GirthData(GetTargetRenderers()[0], girthUnwrapShader, rootBone, Vector3.zero, localRootForward,
                        localRootUp, localRootRight);
            }
        }
        public void Penetrate(Penetrable penetrable) {
            if (penetratedHole != null && penetratedHole != penetrable) {
                penetrationEnd?.Invoke(penetratedHole);
                foreach (var listener in listeners) {
                    listener.OnPenetrationEnd(penetratedHole);
                }
                penetratedHole.SetPenetrationDepth(this, GetWorldLength()+0.1f, OnSetClip);
                inserted = false;
            }
            
            if (penetrable == null) {
                inserted = false;
                OnSetClip(0f, 0f);
                penetratedHole = null;
                return;
            }

            SetTargetHole(penetrable);
            penetratedHole = penetrable;
            insertionFactor = 1f;
            if (!inserted) {
                penetrationStart?.Invoke(penetratedHole);
                foreach (var listener in listeners) {
                    listener.OnPenetrationStart(penetratedHole);
                }
            }
            inserted = true;
        }

        private void OnValidate() {
            reinitialize = true;
            foreach (PenetratorListener listener in listeners) {
                listener.OnValidate(this);
            }
            valid = false;
        }
        public CatmullSpline GetSplinePath() {
            return path;
        }
        protected override void OnDrawGizmosSelected() {
            base.OnDrawGizmosSelected();
            if (!valid && !Application.isPlaying) {
                return;
            }
#if UNITY_EDITOR
            for(float t=0;t<GetWorldLength()-0.025f;t+=0.025f) {
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
