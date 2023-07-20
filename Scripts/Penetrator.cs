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
        public override void OnInspectorGUI() {
            string lastError = ((Penetrator)target).GetLastError();
            if (!string.IsNullOrEmpty(lastError)) {
                EditorGUILayout.HelpBox(lastError, MessageType.Error);
            } else {
                EditorGUILayout.HelpBox("Make sure the blue dotted line is pointed along the penetrator by adjusting the Local Root forward/up/right.\n" +
                                                "If the model is inside-out, one of the vectors is backwards.\n" +
                                                "If you don't see the blue dotted line, ensure Gizmos are enabled.", MessageType.Info);
                Penetrator penetrator = (Penetrator)target;
                if (penetrator.GetGirthMap() != null) {
                    EditorGUILayout.PrefixLabel("Preview Girthmap/Detailmap");
                    var rectContainer = EditorGUILayout.GetControlRect(false, 128f, GUILayout.MaxWidth(256f));
                    var firstRect = rectContainer;
                    firstRect.width = 128f;
                    EditorGUI.DrawPreviewTexture(firstRect, penetrator.GetGirthMap());
                    var secondRect = rectContainer;
                    secondRect.x += 128f;
                    secondRect.width = 128f;
                    EditorGUI.DrawPreviewTexture(secondRect, penetrator.GetDetailMap());
                }
            }

            DrawDefaultInspector();
        }
    }
    #endif
    [ExecuteAlways]
    public class Penetrator : CatmullDeformer {
        private CatmullSpline path;
        [System.Flags]
        private enum AutoPenetrateMode {
            None = 0,
            AutoSeek = 1,
            AutoDecouple = 2,
        }

        [SerializeField] [Range(0f, 2f)] [Tooltip("Squash or stretch the visuals of the penetrator, this can be triggered through listeners, script, or animation to simulate knot forces.")]
        private float virtualSquashAndStretch = 1f;

        [SerializeField] [Tooltip("If you need to mask parts of the model out, customizing this shader will allow you to mask the girthmap generation (so things like heads or feet don't show up in it).")]
        private Shader girthUnwrapShader;
        //[SerializeField]
        private GirthData girthData;
        [FormerlySerializedAs("targetHole")] [SerializeField] [Tooltip("If autoPenetrate is disabled, you can tell the penetrator specifically what to penetrate with here.")]
        private Penetrable penetratedHole;

        [SerializeField] [Range(0.1f,1f)] [Tooltip("How lenient we are with penetrators being off-target, measured in penetrator-lengths.")]
        private float penetrationMarginOfError = 0.5f;
        [SerializeField] [Tooltip("Automate discovery of penetrables, and automatically penetrate with them if some basic conditions are met (roughly the right angle, and distance). Also decouple automatically if basic conditions are met (penetrator is certain distance away).")]
        private AutoPenetrateMode autoPenetrate = AutoPenetrateMode.AutoSeek | AutoPenetrateMode.AutoDecouple;
        [SerializeField]
        private List<Penetrable> ignorePenetrables;
        [SerializeReference,SerializeReferenceButton] [Tooltip("Programmable listeners, they can respond to penetrations in a variety of ways. Great for triggering audio and such.")]
        public List<PenetratorListener> listeners;
        


        public delegate void PenetrationAction(Penetrable penetrable);
        public event PenetrationAction penetrationStart;
        public event PenetrationAction penetrationEnd;
        
        private Penetrable targetHoleA;
        private Penetrable targetHoleB;
        private float targetHoleLerp;
        
        private List<Vector3> pointsA;
        private List<Vector3> pointsB;
        private List<Vector3> pointsC;
        private List<Vector3> outputPointsA;
        private List<Vector3> outputPointsB;
        private bool inserted;
        private float insertionFactor;
        private MaterialPropertyBlock propertyBlock;
        private static Collider[] colliders = new Collider[32];
        public float GetGirthScaleFactor() => girthData.GetGirthScaleFactor();
        public float GetWorldLength() => girthData.GetWorldLength();
        public float GetWorldGirthRadius(float worldDistanceAlongDick) => girthData.GetWorldGirthRadius(worldDistanceAlongDick);

        public float GetKnotForce(float worldDistanceAlongDick) {
            float length = GetWorldLength();
            float tipKnotForce = girthData.GetKnotForce(length*0.95f);
            float trueKnotForce = girthData.GetKnotForce(worldDistanceAlongDick);
            if (worldDistanceAlongDick > length * 0.95f) {
                return Mathf.Lerp(tipKnotForce, 0f, Mathf.Clamp01((worldDistanceAlongDick - length * 0.95f)/length)*20f);
            }
            return trueKnotForce;
        }

        public float squashAndStretch {
            get => virtualSquashAndStretch;
            set => virtualSquashAndStretch = value;
        }

        public RenderTexture GetGirthMap() {
            if (girthData == null) {
                return null;
            }
            return girthData.GetGirthMap();
        }

        public Texture2D GetDetailMap() {
            if (girthData == null) {
                return null;
            }
            return girthData.GetDetailMap();
        }

        public void SetPenetrationMarginOfError(float error) {
            penetrationMarginOfError = Mathf.Max(error,0f);
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
            path = new CatmullSpline();
            propertyBlock = new MaterialPropertyBlock();
            pointsA = new List<Vector3>();
            pointsB = new List<Vector3>();
            pointsC = new List<Vector3>();
            outputPointsA = new List<Vector3>();
            outputPointsB = new List<Vector3>();
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
            //GetTipPositionAndTangent(out Vector3 tipPosition, out Vector3 tipTangent);
            ConstructPathForIdle(pointsA);
            path.SetWeightsFromPoints(pointsA);
            foreach (PenetratorListener listener in listeners) {
                listener.OnEnable(this);
            }
            OnSetClip(0f, 0f);
        }
        
        public Transform GetRootBone() {
            return rootBone;
        }

        public void SetRealtimeConfiguration(RendererSubMeshMask[] newRenderers, Transform newRootBone,
            Vector3 newLocalForward, Vector3 newLocalRight, Vector3 newLocalUp,
            PenetratorListener[] newListeners) {
            SetTargetRenderers(newRenderers);
            rootBone = newRootBone;
            localRootForward = newLocalForward;
            localRootRight = newLocalRight;
            localRootUp = newLocalUp;
            listeners = new List<PenetratorListener>(newListeners);
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
        
        public void AddIgnorePenetrable(Penetrable p) {
            ignorePenetrables.Add(p);
        }
        public void RemoveIgnorePenetrable(Penetrable p) {
            ignorePenetrables.Remove(p);
        }

        void Update() {
            #if UNITY_EDITOR
            if (reinitialize && !Application.isPlaying) {
                CheckValid();
                if (!valid) {
                    return;
                }

                if (girthData != null) {
                    girthData.Release();
                    girthData = null;
                    girthData = new GirthData(GetTargetRenderers()[0], girthUnwrapShader, rootBone, Vector3.zero, localRootForward, localRootUp, localRootRight);
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
                if ((autoPenetrate&AutoPenetrateMode.AutoDecouple) != 0) {
                    Penetrate(null);
                }
                return;
            }

            Vector3 holePos = penetrable.GetPath().GetPositionFromT(0f);
            if (inserted) {
                return;
            }
            insertionFactor = Mathf.MoveTowards(insertionFactor, 0f, Time.deltaTime * 4f);
            float temp = Vector3.Distance(tipPosition, holePos)/GetWorldLength();
            insertionFactor = Mathf.Max(
                insertionFactor,
                Mathf.Clamp01((1f+penetrationMarginOfError) - temp)
            );
            if (insertionFactor >= 0.99f) {
                Penetrate(penetrable);
            }
        }

        private void StitchPoints(List<Vector3> a, List<Vector3> b, List<Vector3> output, float t) {
            output.Clear();
            if (t == 0f || b.Count == 0) {
                output.AddRange(a);
                return;
            }

            if (Mathf.Approximately(t, 1f) || a.Count == 0) {
                output.AddRange(b);
                return;
            }
            for (int i = 0; i < Mathf.Min(a.Count, b.Count); i++) {
                output.Add(Vector3.Lerp(a[i], b[i], t));
            }
            for (int i = output.Count; i < a.Count; i++) {
                output.Add(a[i]);
            }
            for (int i = output.Count; i < b.Count; i++) {
                output.Add(b[i]);
            }
            /*for (int i = 0; i < output.Count-1; i++) {
                if (output[i] == output[i + 1]) {
                    output.RemoveAt(i--);
                }
            }*/
        }

        private void GetTipPositionAndTangent(CatmullSpline idlePath, out Vector3 tipPosition, out Vector3 tipTangent) {
            tipPosition = idlePath.GetPositionFromDistance(GetWorldLength());
            tipTangent = idlePath.GetVelocityFromDistance(GetWorldLength());
        }

        protected override void LateUpdate() {
            if (!valid && !Application.isPlaying) {
                return;
            }

            ConstructPathForIdle(pointsA);
            path.SetWeightsFromPoints(pointsA);
            GetTipPositionAndTangent(path, out Vector3 tipPosition, out Vector3 tipTangent);
            
            UpdateInsertionAmount(targetHoleA, tipPosition);

            ConstructPathToPenetrable(pointsB, targetHoleA, out int outHoleIndexA);
            ConstructPathToPenetrable(pointsC, targetHoleB, out int outHoleIndexB);
            int outHoleIndex = Mathf.Max(outHoleIndexB, outHoleIndexA);
            StitchPoints(pointsC, pointsB, outputPointsA, targetHoleLerp);
            StitchPoints(pointsA, outputPointsA, outputPointsB, insertionFactor);
            
            path.SetWeightsFromPoints(outputPointsB);

            float realDistanceToHole = path.GetDistanceFromSubT(0,  outHoleIndex, 1f);

            if (penetratedHole != null) {
                OnSetClip(1f, 1f);
                foreach (PenetratorListener listener in listeners) {
                    listener.NotifyPenetrationUpdate(this, penetratedHole, realDistanceToHole/virtualSquashAndStretch);
                }
                penetratedHole.SetPenetrationDepth(this, realDistanceToHole/virtualSquashAndStretch, OnSetClip);
            }
            if ((autoPenetrate&AutoPenetrateMode.AutoDecouple)!=0 && inserted && realDistanceToHole > GetWorldLength()*Mathf.Max(virtualSquashAndStretch,1f)) {
                Penetrate(null);
            }

            foreach (RendererSubMeshMask rendererMask in GetTargetRenderers()) {
                rendererMask.renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(squashStretchCorrectionID, virtualSquashAndStretch);
                propertyBlock.SetFloat(dickWorldLengthID, GetWorldLength());
                propertyBlock.SetFloat(distanceToHoleID, realDistanceToHole);
                rendererMask.renderer.SetPropertyBlock(propertyBlock);
            }
            
            targetHoleLerp = Mathf.MoveTowards(targetHoleLerp, 1f, Time.deltaTime*8f);
            if ((autoPenetrate&AutoPenetrateMode.AutoSeek)!=0 && !inserted && Math.Abs(targetHoleLerp - 1f) < 0.001f) {
                GetTipPositionAndTangent(path, out Vector3 newTipPosition, out Vector3 newTipTangent);
                int hits = Physics.OverlapSphereNonAlloc(newTipPosition, GetWorldLength()*penetrationMarginOfError+1f, colliders, PenetrationTechTools.GetPenetrableMask(), QueryTriggerInteraction.Collide);
                PenetrableOwner bestMatch = null;
                float bestValue = float.MaxValue;
                for (int i = 0; i < hits; i++) {
                    PenetrableOwner owner = colliders[i].GetComponent<PenetrableOwner>();
                    if (owner != null && !ignorePenetrables.Contains(owner.owner)) {
                        float distance = Vector3.Distance(colliders[i].transform.position, tipPosition);
                        // Being a full 90f degrees around will add a meter to the distance evaluation.
                        float angle = Vector3.Angle(owner.owner.GetPath().GetVelocityFromT(0f).normalized,
                            newTipTangent.normalized)/90f;
                        float value = angle+distance;
                        if (value < bestValue) {
                            bestMatch = owner;
                            bestValue = value;
                        }
                    }
                }

                if (bestMatch != null && !ignorePenetrables.Contains(bestMatch.owner)) {
                    SetTargetHole(bestMatch.owner);
                }
            }
            foreach (PenetratorListener listener in listeners) {
                listener.LateUpdate();
            }
            base.LateUpdate();
        }

        protected virtual void ConstructPathForIdle(ICollection<Vector3> outputPoints) {
            outputPoints.Clear();
            var rootBonePosition = rootBone.position;
            float worldLength = GetWorldLength();
            var tipPosition = rootBonePosition + rootBone.TransformDirection(localRootForward) * (worldLength * 1.1f);
            Vector3 tangent = tipPosition - rootBonePosition;
            outputPoints.Add(rootBonePosition);
            outputPoints.Add(rootBonePosition+tangent.normalized*(worldLength*0.5f));
            outputPoints.Add(tipPosition);
        }

        protected virtual void ConstructPathToPenetrable(ICollection<Vector3> output, Penetrable penetrable, out int penetrableSplineIndex) {
            output.Clear();
            if (penetrable == null) {
                penetrableSplineIndex = output.Count;
                return;
            }
            CatmullSpline holeSplinePath = penetrable.GetPath();
            
            Vector3 holePos = holeSplinePath.GetPositionFromT(0f);
            Vector3 holeForward = holeSplinePath.GetVelocityFromT(0f).normalized;

            var rootBonePosition = rootBone.position;
            float fakeDistance = Vector3.Distance(rootBonePosition, holePos);
            float length = GetWorldLength();
            Vector3 penetratorTangent = rootBone.TransformDirection(localRootForward) * length;
            Vector3 insertionTangent = -holeForward * length;
            Vector3 insertionPoint = holePos;
            output.Add(rootBonePosition);
            
            penetrableSplineIndex = output.Count;
            penetrable.GetPoints(output);
            Vector3 outPosition = holeSplinePath.GetPositionFromT(1f);
            Vector3 outTangent = holeSplinePath.GetVelocityFromT(1f).normalized;
            output.Add(outPosition + outTangent * length);
        }
        protected class PenetratorValidationException : SystemException {
            public PenetratorValidationException(string msg) : base(msg) { }
        }
        
        protected void AssertValid(bool condition, string errorMsg) {
            valid = valid && condition;
            if (!condition) {
                throw new PenetratorValidationException(errorMsg);
            }
        }

        protected void SetError(string error) {
            valid = false;
            lastError = error;
        }

        protected virtual void CheckValid() {
            girthUnwrapShader ??= Shader.Find("PenetrationTech/GirthUnwrapRaw");
            listeners ??= new List<PenetratorListener>();
            pointsA ??= new List<Vector3>();
            pointsB ??= new List<Vector3>();
            outputPointsA ??= new List<Vector3>();
            outputPointsB ??= new List<Vector3>();
            path ??= new CatmullSpline();
            if (path.GetWeights().Count == 0) {
                path.SetWeightsFromPoints(new List<Vector3>{ Vector3.zero, Vector3.up });
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
                
                foreach (var renderMask in GetTargetRenderers()) {
                    if (renderMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                        AssertValid(skinnedMeshRenderer.rootBone != null,
                        $"The SkinnedMeshRenderer {skinnedMeshRenderer} must have a specified root bone.");
                    }
                }

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
                SetError($"{error.Message}\n\n{error.StackTrace}");
            } catch (PenetratorListener.PenetratorListenerValidationException error) {
                SetError($"{error.Message}\n\n{error.StackTrace}");
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
                if ((autoPenetrate & AutoPenetrateMode.AutoDecouple) == 0) {
                    SetTargetHole(null);
                }
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
            Vector3.OrthoNormalize(ref localRootForward, ref localRootUp, ref localRootRight);
            reinitialize = true;
            if (penetratedHole != null && targetHoleA == null) {
                targetHoleA = penetratedHole;
            }
            listeners ??= new List<PenetratorListener>();
            foreach (PenetratorListener listener in listeners) {
                if (listener == null) {
                    continue;
                }
                listener.OnValidate(this);
            }
            valid = false;
        }
        public override CatmullSpline GetPath() {
            return path;
        }
        void OnDrawGizmosSelected() {
            if (!valid && !Application.isPlaying) {
                return;
            }
#if UNITY_EDITOR
            for(float t=0;t<GetWorldLength()-0.025f;t+=0.025f) {
                Handles.color = Color.white;
                Vector3 position = path.GetPositionFromDistance(t) + GetWorldOffset(t);
                float girth = GetWorldGirthRadius(t);
                Handles.DrawWireDisc(position, path.GetVelocityFromDistance(t).normalized, girth);
            }
            Handles.color = Color.blue;
            var rootBonePosition = rootBone.transform.position;
            Handles.DrawDottedLine(rootBonePosition,
                rootBonePosition + rootBone.transform.TransformDirection(localRootForward), 10f);
            foreach (PenetratorListener listener in listeners) {
                listener.OnDrawGizmosSelected(this);
            }
#endif
        }
    }

}
