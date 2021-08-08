using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PenetrationTech {

    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(Penetrator))]
    public class PenetratorEditor : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            Penetrator d = ((Penetrator)target);
            if (GUILayout.Button("Auto-Populate Shapes")) {
                Undo.RecordObject(target, "Auto find dick shapes");
                d.AutoFindShapes();
                d.SetDeforms();
                EditorUtility.SetDirty(target);
            }
            if (GUILayout.Button("Bake All")) {
                Undo.RecordObject(target, "Penetrator bake");
                d.BakeAll();
                d.SetDeforms();
                EditorUtility.SetDirty(target);
            }
            if (d.dickTip == null || d.dickRoot == null) {
                EditorGUILayout.HelpBox("Specify the dick root bone and dick tip bone (dickTip, dickRoot). These MUST be mesh bones.", MessageType.Error);
                return;
            }
            if (d.bakeTargets == null || d.bakeTargets.Count == 0) {
                EditorGUILayout.HelpBox("Please specify which meshes are used for dick baking. (bakeTargets)", MessageType.Error);
                return;
            }
            if (d.deformationTargets == null || d.deformationTargets.Count == 0) {
                EditorGUILayout.HelpBox("Please specify which meshes are used for displaying deformations. (deformationTargets)", MessageType.Error);
                return;
            }
            if (d.shapes == null || d.shapes.Count == 0) {
                EditorGUILayout.HelpBox("Hit \"Auto-Populate Shapes\", or manually specify shapes that can get triggered with their proper type. (shapes)", MessageType.Error);
                return;
            }
            if (d.shapes[0].girth == null || d.shapes[0].girth.length == 0) {
                EditorGUILayout.HelpBox("Hit \"Bake\"!", MessageType.Error);
                return;
            }
            foreach(var def in d.deformationTargets) {
                foreach(var defmat in def.sharedMaterials) {
                    if (!defmat.HasProperty("_PenetratorLength")) {
                        EditorGUILayout.HelpBox("Deformation target materials don't seem to have a PenetratorDeformation compatible shader. Penetrators might not deform properly!", MessageType.Error);
                        return;
                    }
                }
            }
        }
        public void DrawWireDisk(float atLength, AnimationCurve xOffset, AnimationCurve yOffset, AnimationCurve girth, string label = "") {
            Penetrator d = (Penetrator)target;
            Vector3 offset = xOffset.Evaluate(atLength) * d.dickRoot.TransformDirection(d.dickRight) * d.dickRoot.TransformVector(d.dickRight).magnitude;
            offset += yOffset.Evaluate(atLength) * d.dickRoot.TransformDirection(d.dickUp) * d.dickRoot.TransformVector(d.dickUp).magnitude;
            offset += atLength * d.dickRoot.TransformDirection(d.dickForward) * d.dickRoot.TransformVector(d.dickForward).magnitude;
            float g = girth.Evaluate(atLength) * d.dickRoot.TransformVector(d.dickUp).magnitude;
            Handles.DrawWireDisc(d.dickRoot.position + offset, d.dickRoot.TransformDirection(d.dickForward), g*0.5f);
            if (!string.IsNullOrEmpty(label)) {
                Handles.Label(d.dickRoot.position + offset, label);
            }
        }
        public void OnSceneGUI() {
            if (Application.isPlaying) {
                return;
            }
            Transform t = (Transform)serializedObject.FindProperty("dickRoot").objectReferenceValue;
            //SerializedProperty dickOriginOffsetProp = serializedObject.FindProperty("dickOriginOffset");
            Vector3 dickForward = serializedObject.FindProperty("dickForward").vector3Value;
            Vector3 dickRight = serializedObject.FindProperty("dickRight").vector3Value;
            Vector3 dickUp = serializedObject.FindProperty("dickUp").vector3Value;
            var shapes = serializedObject.FindProperty("shapes");
            if (shapes != null && shapes.arraySize > 0) {
                AnimationCurve girth = shapes.GetArrayElementAtIndex(0).FindPropertyRelative("girth").animationCurveValue;
                AnimationCurve xOffset = shapes.GetArrayElementAtIndex(0).FindPropertyRelative("xOffset").animationCurveValue;
                AnimationCurve yOffset = shapes.GetArrayElementAtIndex(0).FindPropertyRelative("yOffset").animationCurveValue;
                SerializedProperty dickOriginOffsetProp = shapes.GetArrayElementAtIndex(0).FindPropertyRelative("localPenetratorRoot");
                Handles.color = Color.white;
                if (girth.length <= 0) {
                    return;
                }
                float shapeEnd = girth[girth.length - 1].time;
                //Vector3 tipOffset = t.TransformPoint(dickRight * xOffset.Evaluate(shapeEnd) + dickUp * yOffset.Evaluate(shapeEnd) + dickForward*shapeEnd);
                for(int i=0;i<girth.length-1;i++) {
                    var keyOne = girth[i];
                    var keyTwo = girth[i+1];
                    Handles.color = Color.white;
                    DrawWireDisk(keyOne.time, xOffset, yOffset, girth);
                    Handles.color = Color.gray;
                    DrawWireDisk(Mathf.Lerp(keyOne.time, keyTwo.time,0.5f), xOffset, yOffset, girth);
                    Handles.color = Color.white;
                    DrawWireDisk(keyTwo.time, xOffset, yOffset, girth);
                }
                var depthEvents = serializedObject.FindProperty("depthEvents");
                for(int i=0;i<depthEvents.arraySize;i++) {
                    Handles.color = Color.blue;
                    float length = girth.keys[girth.length-1].time;
                    DrawWireDisk((1f-depthEvents.GetArrayElementAtIndex(i).FindPropertyRelative("triggerAlongDepth01").floatValue)*length, xOffset, yOffset, girth, "DepthEvent"+i);
                }
                if (t != null) {
                    Vector3 globalPosition = Handles.PositionHandle(t.transform.TransformPoint(dickOriginOffsetProp.vector3Value), t.transform.rotation);
                    if (Vector3.Distance(t.transform.InverseTransformPoint(globalPosition), dickOriginOffsetProp.vector3Value) > 0.001f) {
                        //Undo.RecordObject(target, "Penetrator origin move");
                        dickOriginOffsetProp.vector3Value = t.transform.InverseTransformPoint(globalPosition);
                        serializedObject.ApplyModifiedProperties();
                        //EditorUtility.SetDirty(target);
                    }
                    SerializedProperty enumProp = shapes.GetArrayElementAtIndex(0).FindPropertyRelative("shapeType");
                    Handles.Label(t.transform.TransformPoint(dickOriginOffsetProp.vector3Value), enumProp.enumDisplayNames[enumProp.enumValueIndex] + " DICK ROOT");
                    Penetrator d = (Penetrator)target;
                    float g = d.GetWorldGirth(d.penetrationDepth01);
                    Handles.color = Color.blue;
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, g*0.5f);
                }
            }
        }
    }
    #endif

    [RequireComponent(typeof(Rigidbody))]
    public class Penetrator : MonoBehaviour {
        [Header("General Penetrator Settings")]
        [Tooltip("The \"root\" object that is the highest-most transform that would still be a part of this penetratable. This would be the character, or onahole root transform. It's used in circular-dependency checks, as well as collision phasing.")]
        public Transform root;
        [Header("")]
        [Tooltip("The layer which dick colliders are created on. Leaving it to Default is fine unless you want some performance gain.")]
        [LayerAttribute]
        public int dickLayer;
        [Tooltip("The layer which hole colliders exist on, for searching.")]
        [LayerAttribute]
        public int holeLayer;
        [Tooltip("If you want to animate the penetrationDepth01 yourself, this should be set to false. Otherwise this dick will auto-seek nearby orifices and control how deeply it is penetrated automatically.")]
        public bool autoPenetrate = true;
        [Tooltip("Pop out and reset the hole target if we end up \"outside\" of a penetratable. You might want to have this set to false in animation with manually authored targets.")]
        public bool autoDecouple = true;

        [Tooltip("This is an override to prevent a dick from penetrating too deeply, this is really important to be set to false on dicks attached to characters.")]
        public bool canOverpenetrate = false;
        [HideInInspector]
        public Rigidbody body;
        private List<Collider> selfColliders = new List<Collider>();

        [Tooltip("How many times should the dick pulse for a single cum trigger.")]
        public int cumPulseCount = 12;
        [Tooltip("A random clip from this set is played during cum pulses.")]
        public List<AudioClip> pumpingSounds = new List<AudioClip>();
        [Tooltip("A random clip is selected on each penetration, and its volume is based on how much the dick is sliding around. Slimy, fleshy, looping clips go here.")]
        public List<AudioClip> slimySlidingSounds = new List<AudioClip>();

        [Tooltip("This controls how wide (along the length) the bulge is for being filled with cum. It's percentage based, so 0.1 would be a bulge 10% the length of the dick.")]
        [Range(0.001f, 1f)]
        public float bulgePercentage = 0.1f;

        [Header("Realtime Drivers")]
        [Header("")]
        [Tooltip("Meshes that recieve PenetrationTech deformation information like squish and cum info.")]
        public List<SkinnedMeshRenderer> deformationTargets = new List<SkinnedMeshRenderer>();
        [Tooltip("The hole that the dick should be penetrating right now. If autopenetrate is on, this is handled automatically.")]
        public Penetrable holeTarget;
        [Tooltip("How much cum is in the dick at the moment. This is automatically controlled by the Cum() function, though can be animated manually otherwise.")]
        [Range(0f, 1f)]
        public float cumActive;
        [Tooltip("This is the cum moving along the dick, pulses from -1 to 2. This is automatically controlled by Cum(), though can be animated manually otherwise.")]
        [Range(-1f, 2f)]
        public float cumProgress;
        [Tooltip("Controls how much the dick squishes or tugs.")]
        [Range(-1f,1f)]
        public float squishPullAmount = 0f;

        [Tooltip("How much friction is involved in penetration, this affects dick squish and the theortical speed at which one can fuck.")]
        [Range(0.001f,0.1f)]
        public float slideFriction = 0.02f;

        [Range(-1f,5f)]
        [Tooltip("If autoPenetrate is turned off, this variable must be manually driven to make the dick go in and out. Otherwise it's automatically controlled.")]
        public float penetrationDepth01 = -1f;
        private float lastPenetrationDepth01 = -1f;
        public List<DepthEvent> depthEvents = new List<DepthEvent>();
        [Tooltip("The collider that gets scaled relatively to the root, in the dickForward direction, to approximate the \"real\" size of the dick. You can leave this blank to auto-generate a collider on Awake.")]
        public Collider dickCollider = null;
        [Tooltip("Optional audio source override for slimy sounds. One will automatically be created on Awake otherwise.")]
        public AudioSource slimySource;
        [Tooltip("Optional audio source override for playing plap sounds. One will automatically be created on Awake otherwise.")]
        public AudioSource plapSource;

        [System.Serializable]
        public class PenetratorMoveEvent : UnityEvent<float>{}
        [Tooltip("Triggers once when the dick starts entering a hole.")]
        public UnityEvent OnPenetrate;
        [Tooltip("Triggers once when the dick fully leaves a hole.")]
        public UnityEvent OnEndPenetrate;
        [Tooltip("Triggers at the very end of each cum pump.")]
        public UnityEvent OnCumEmit;
        [Tooltip("Everytime the dick moves while penetrating, it triggers an event based on how much. Can be considered how much \"stimulus\" a dick is recieving. Negative when pulling out.")]
        public PenetratorMoveEvent OnMove;
        [Header("Bake Settings")]
        [Header("")]
        [Range(3, 128)]
        [Tooltip("The number of cross sections used to generate the curves. Higher numbers lead to more accuracy, though set it too high and it'll miss verts.")]
        public int crossSections = 16;
        [Tooltip("The meshes used for baking, should be all the meshes that represent the dick shape.")]
        public List<SkinnedMeshRenderer> bakeTargets = new List<SkinnedMeshRenderer>();
        [Tooltip("The root of the dick, should be a bone thats part of the skinned mesh renderers.")]
        public Transform dickRoot;
        [Tooltip("The tip of the dick, should be a child of the dick root, and also part of the skinned mesh renderer.")]
        public Transform dickTip;
        [Tooltip("Blendshapes of the dick, with baked offset and girth information. Use the buttons below to generate most of the information.")]
        public List<PenetratorShape> shapes = new List<PenetratorShape>();

        [HideInInspector]
        public Vector3 dickForward;
        [HideInInspector]
        public Vector3 dickUp;
        [HideInInspector]
        public Vector3 dickRight;
        private HashSet<Collider> ignoringCollisions = new HashSet<Collider>();
        [HideInInspector]
        public bool backwards;
        [HideInInspector]
        public float girthAtEntrance;
        [HideInInspector]
        public float girthAtExit;
        private SphereCollider dickTipDetector;
        //private bool bodyWasKinematic;
        //private bool bodyWasAffectedByGravity;

        [System.Serializable]
        public class DepthEvent {
            public enum TriggerDirection {
                Both,
                PushIn,
                PullOut
            }
            public void Trigger(Vector3 position, AudioSource source) {
                if (lastTrigger != 0f && lastTrigger+triggerCooldown > Time.timeSinceLevelLoad) {
                    return;
                }
                lastTrigger = Time.timeSinceLevelLoad;
                if (triggerClips.Count > 0) {
                    source.pitch = Random.Range(0.7f,1.3f);
                    source.PlayOneShot(triggerClips[Random.Range(0,triggerClips.Count)], soundTriggerVolume);
                }
                triggerEvent.Invoke();
            }
            public float soundTriggerVolume = 1f;
            public float triggerCooldown = 1f;
            private float lastTrigger;
            public List<AudioClip> triggerClips = new List<AudioClip>();
            public TriggerDirection triggerDirection;
            [Range(0f,1f)]
            public float triggerAlongDepth01;
            public UnityEvent triggerEvent;
        }

        public void SetHolePositions(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
            this.p0 = p0;
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
        }
        public bool invisibleWhenInside {
            set {
                foreach (var renderer in deformationTargets) {
                    if (renderer == null) { continue; }
                    Material[] materials = renderer.sharedMaterials;
                    if (Application.isPlaying) {
                        materials = renderer.materials;
                    }
                    foreach (var material in materials) {
                        if (value) {
                            material.EnableKeyword("_INVISIBLE_WHEN_INSIDE_ON");
                        } else {
                            material.DisableKeyword("_INVISIBLE_WHEN_INSIDE_ON");
                        }
                    }
                }
            }
        }
        public bool allTheWayThrough {
            set {
                foreach (var renderer in deformationTargets) {
                    if (renderer == null) { continue; }
                    Material[] materials = renderer.sharedMaterials;
                    if (Application.isPlaying) {
                        materials = renderer.materials;
                    }
                    foreach (var material in materials) {
                        if (value) {
                            material.DisableKeyword("_CLIP_DICK_ON");
                        } else {
                            material.EnableKeyword("_CLIP_DICK_ON");
                        }
                    }
                }
            }
        }
        private Vector3 p0 = Vector3.zero;
        private Vector3 p1 = Vector3.down*0.33f;
        private Vector3 p2 = Vector3.down*0.66f;
        private Vector3 p3 = Vector3.down;

        private PenetratorShape internalPenetratorshape = null;
        public PenetratorShape defaultShape {
            get {
                if (internalPenetratorshape != null && !Application.isEditor) {
                    return internalPenetratorshape;
                }
                foreach(PenetratorShape shape in shapes) {
                    if (shape.shapeType == PenetratorShape.ShapeType.Default) {
                        internalPenetratorshape = shape;
                        return shape;
                    }
                }
                return internalPenetratorshape;
            }
        }
        private Dictionary<Mesh, Matrix4x4> bindPoseCache = new Dictionary<Mesh, Matrix4x4>();
        private Matrix4x4 GetTransformPose(SkinnedMeshRenderer renderer) {
            if (bindPoseCache.ContainsKey(renderer.sharedMesh)) {
                return bindPoseCache[renderer.sharedMesh];
            }
            bindPoseCache.Add(renderer.sharedMesh, renderer.sharedMesh.bindposes[GetPenetratorRootBoneID(renderer)]);
            return bindPoseCache[renderer.sharedMesh];
        }

        private int GetPenetratorRootBoneID(SkinnedMeshRenderer renderer) {
            for(int i=0;i<renderer.bones.Length;i++) {
                if (renderer.bones[i] == dickRoot) { return i; }
            }
            return -1;
        }

        [System.Serializable]
        public class PenetratorShape {
            public enum ShapeType {
                Default,
                Pull,
                Squish,
                Cum,
                Misc,
            }
            public float GetWeight(Penetrator d) {
                float weight = 0f;
                if (shapeType == PenetratorShape.ShapeType.Misc && d.deformationTargets.Count > 0 && d.deformationTargets[0] != null) {
                    weight = d.deformationTargets[0].GetBlendShapeWeight(blendshapeIDs[d.deformationTargets[0].sharedMesh]) / 100f;
                }
                if (shapeType == PenetratorShape.ShapeType.Pull) {
                    weight = Mathf.Clamp01(d.squishPullAmount);
                }
                if (shapeType == PenetratorShape.ShapeType.Squish) {
                    weight = Mathf.Clamp01(-d.squishPullAmount);
                }
                //if (shapeType == PenetratorShape.ShapeType.Cum) {
                    //weight = d.cumActive;
                //}
                return weight;
            }
            public float GetWeight(Penetrator d, float length) {
                float weight = 0f;
                if (shapeType == PenetratorShape.ShapeType.Misc) {
                    weight = d.deformationTargets[0].GetBlendShapeWeight(blendshapeIDs[d.deformationTargets[0].sharedMesh]) / 100f;
                }
                if (shapeType == PenetratorShape.ShapeType.Pull) {
                    weight = Mathf.Clamp01(d.squishPullAmount);
                }
                if (shapeType == PenetratorShape.ShapeType.Squish) {
                    weight = Mathf.Clamp01(-d.squishPullAmount);
                }
                if (shapeType == PenetratorShape.ShapeType.Cum) {
                    float fullLength = d.GetLocalLength();
                    weight = d.cumActive * Easing.Circular.Out(1f-Mathf.Clamp01(Mathf.Abs(length - ((d.cumProgress+girth.keys[0].time) * fullLength)) / (fullLength * d.bulgePercentage)));
                }
                return weight;
            }
            public ShapeType shapeType;
            public string blendshapeName = "";
            [HideInInspector]
            public Dictionary<Mesh, int> blendshapeIDs = new Dictionary<Mesh, int>();
            public AnimationCurve xOffset = new AnimationCurve();
            public AnimationCurve yOffset = new AnimationCurve();
            public AnimationCurve girth = new AnimationCurve();
            public Vector3 localPenetratorRoot;
        }

        void Awake() {
            StartCoroutine(AwakeRoutine());
        }

        IEnumerator AwakeRoutine() {
            if (body == null) {
                body = GetComponent<Rigidbody>();
            }
            if (body == null) {
                body = gameObject.AddComponent<Rigidbody>();
            }
            GenerateBlendshapeDict();
            //bodyWasAffectedByGravity = body.useGravity;
            //bodyWasKinematic = body.isKinematic;
            float averageGirth = 0f;
            if (defaultShape != null && defaultShape.girth != null) {
                for(int i=0;i<defaultShape.girth.length;i++) {
                    averageGirth += defaultShape.girth[i].value;
                }
                averageGirth /= defaultShape.girth.length;
            }
            if (dickCollider == null) {
                CapsuleCollider collider = dickRoot.gameObject.AddComponent<CapsuleCollider>();
                collider.height = Vector3.Distance(GetLocalRootPosition(), dickRoot.InverseTransformPoint(dickTip.position));
                collider.center = GetLocalRootPosition() + dickForward*GetLocalLength()*0.5f;
                collider.radius = averageGirth;
                selfColliders.Add(collider);
                dickCollider = collider;
            } else {
                selfColliders.Add(dickCollider);
            }

            dickTipDetector = gameObject.AddComponent<SphereCollider>();
            dickTipDetector.isTrigger = true;
            dickTipDetector.radius = averageGirth*2f;
            dickTipDetector.gameObject.layer = dickLayer;
            selfColliders.Add(dickTipDetector);
            foreach(Collider c in GetComponentsInChildren<Collider>()) {
                if (c.gameObject.layer == dickLayer) {
                    selfColliders.Add(c);
                }
            }
            while (slimySource == null && Application.isPlaying) {
                slimySource = gameObject.AddComponent<AudioSource>();
                yield return null;
            }
            while (plapSource == null && Application.isPlaying) {
                plapSource = gameObject.AddComponent<AudioSource>();
                yield return null;
            }
            if (slimySource != null) {
                slimySource.rolloffMode = AudioRolloffMode.Logarithmic;
                slimySource.loop = true;
                slimySource.spatialBlend = 1f;
            }
            if (plapSource != null) {
                plapSource.rolloffMode = AudioRolloffMode.Logarithmic;
                plapSource.loop = false;
                plapSource.spatialBlend = 1f;
            }
        }
        void OnEnable() {
            foreach (var renderer in deformationTargets) {
                if (renderer == null) {
                    continue;
                }
                Material[] materials = renderer.sharedMaterials;
                if (Application.isPlaying) {
                    materials = renderer.materials;
                }
                foreach (var material in materials) {
                    material.SetFloat("_PenetrationDepth", -1f);
                    material.SetVector("_OrificeWorldPosition", p0);
                    material.SetVector("_OrificeOutWorldPosition1", p1);
                    material.SetVector("_OrificeOutWorldPosition2", p2);
                    material.SetVector("_OrificeOutWorldPosition3", p3);
                }
            }
            invisibleWhenInside = false;
            allTheWayThrough = true;
        }
        void GenerateBlendshapeDict() {
            internalPenetratorshape = null;
            if (deformationTargets.Count == 0) {
                return;
            }
            foreach(PenetratorShape shape in shapes) {
                foreach (SkinnedMeshRenderer renderer in deformationTargets) {
                    if (renderer == null) {
                        continue;
                    }
                    if (shape.blendshapeIDs.ContainsKey(renderer.sharedMesh)) {
                        shape.blendshapeIDs[renderer.sharedMesh] = renderer.sharedMesh.GetBlendShapeIndex(shape.blendshapeName);
                    } else {
                        shape.blendshapeIDs.Add(renderer.sharedMesh, renderer.sharedMesh.GetBlendShapeIndex(shape.blendshapeName));
                    }
                }
            }
        }
        public void OnDestroy() {
            if (slimySource != null) {
                Destroy(slimySource);
                slimySource = null;
            }
        }

        public virtual Vector3 GetLocalRootPosition() {
            if (defaultShape == null || defaultShape.girth.length == 0) {
                return Vector3.zero;
            }
            Vector3 baseRootPosition = defaultShape.localPenetratorRoot;
            Vector3 rootPosition = baseRootPosition;
            foreach(PenetratorShape shape in shapes) {
                float weight = shape.GetWeight(this);
                rootPosition += (shape.localPenetratorRoot - baseRootPosition) * weight;
            }
            return rootPosition;
        }

        public virtual float GetLength() {
            return GetLocalLength() * dickRoot.TransformVector(dickForward).magnitude;
        }
        public virtual float GetLocalLength() {
            if (defaultShape == null || defaultShape.girth.length == 0) {
                return 1f;
            }
            float baseLength = defaultShape.girth[defaultShape.girth.length-1].time - defaultShape.girth[0].time;
            float length = baseLength;
            foreach(PenetratorShape shape in shapes) {
                if (shape.girth == null || shape.girth.length <= 0) {
                    continue;
                }
                float weight = shape.GetWeight(this);
                float shapeLength = shape.girth[shape.girth.length - 1].time - shape.girth[0].time;
                length += (shapeLength - baseLength) * weight;
            }
            return length;
        }
        public virtual float GetTangent(float penetrationDepth01) {
            if (defaultShape == null || defaultShape.girth.length == 0) {
                return 0f;
            }
            float fullLength = GetLocalLength();
            float localLength = (1f-penetrationDepth01) * fullLength;
            float baseTangent = defaultShape.girth.Differentiate(localLength+defaultShape.girth[0].time);
            float tangent = baseTangent;
            foreach(PenetratorShape shape in shapes) {
                float weight = shape.GetWeight(this, localLength+shape.girth[0].time);
                tangent += (shape.girth.Differentiate(localLength+shape.girth[0].time) - baseTangent) * weight;
            }
            return tangent;
        }

        public virtual float GetWorldGirth(float penetrationDepth01) {
            float fullLength = GetLocalLength();
            float localLength = (1f-penetrationDepth01) * fullLength;
            float baseGirth = defaultShape.girth.Evaluate(localLength+defaultShape.girth[0].time);
            float girth = baseGirth;
            foreach(PenetratorShape shape in shapes) {
                if (shape.girth == null || shape.girth.length <= 0) {
                    continue;
                }
                float weight = shape.GetWeight(this, localLength+shape.girth[0].time);
                girth += (shape.girth.Evaluate(localLength+shape.girth[0].time) - baseGirth) * weight;
            }
            return girth*dickRoot.TransformVector(dickUp).magnitude;
        }
        public virtual Vector3 GetWorldRootPosition() {
            return dickRoot.TransformPoint(GetLocalRootPosition());
        }
        public virtual Vector3 GetLocalPlanarOffset(float penetrationDepth01) {
            if (defaultShape == null || defaultShape.xOffset.length <=0) {
                return Vector3.zero;
            }
            float fullLength = GetLocalLength();
            float localLength = (1f-penetrationDepth01) * fullLength;
            Vector3 baseOffset = dickRight * defaultShape.xOffset.Evaluate(localLength+defaultShape.xOffset[0].time) +
                                dickUp * defaultShape.yOffset.Evaluate(localLength+defaultShape.yOffset[0].time);
            Vector3 offset = baseOffset;
            foreach(PenetratorShape shape in shapes) {
                if (shape.xOffset == null || shape.xOffset.length <= 0) {
                    continue;
                }
                float weight = shape.GetWeight(this, localLength+shape.xOffset[0].time);
                Vector3 evalOffset = dickRight * shape.xOffset.Evaluate(localLength+shape.xOffset[0].time) +
                                    dickUp * shape.yOffset.Evaluate(localLength+shape.yOffset[0].time);
                offset += (evalOffset - baseOffset) * weight;
            }
            return offset;
        }
        public virtual Vector3 GetWorldPlanarOffset(float penetrationDepth01) {
            return dickRoot.TransformVector(GetLocalPlanarOffset(penetrationDepth01));
        }

    #if UNITY_EDITOR
        void BakeShape(PenetratorShape shape) {
            shape.girth = new AnimationCurve();
            shape.xOffset = new AnimationCurve();
            shape.yOffset = new AnimationCurve();
            List<Vector3> dickVerts = new List<Vector3>();
            foreach(SkinnedMeshRenderer skinnedMesh in bakeTargets) {
                Mesh mesh = skinnedMesh.sharedMesh;
                List<Vector3> verts = new List<Vector3>();
                mesh.GetVertices(verts);

                // Apply blendshape
                if (!string.IsNullOrEmpty(shape.blendshapeName)) {
                    Vector3[] blendVerts = new Vector3[mesh.vertexCount];
                    Vector3[] blendNormals = new Vector3[mesh.vertexCount];
                    Vector3[] blendTangents = new Vector3[mesh.vertexCount];
                    mesh.GetBlendShapeFrameVertices(shape.blendshapeIDs[mesh], 0, blendVerts, blendNormals, blendTangents);
                    for (int i = 0; i < verts.Count; i++) {
                        verts[i] += blendVerts[i];
                    }
                }

                // This weird junk is to iterate through every weight of every vertex.
                // Weights aren't limited to 4 bones, they go up to bonesPerVertex[i] where i is the vertex index in question.
                // Since even each vertex could have any number of bones, I just keep a separate vertex incrementer (vt) and 
                // a weight incrementer (wt).
                var weights = mesh.GetAllBoneWeights();
                var bonesPerVertex = mesh.GetBonesPerVertex();
                int vt = 0;
                int wt = 0;
                for (int o = 0; o < bonesPerVertex.Length; o++) {
                    for (int p = 0; p < bonesPerVertex[o]; p++) {
                        BoneWeight1 weight = weights[wt];
                        Transform boneWeightTarget = skinnedMesh.bones[weights[wt].boneIndex];
                        if (boneWeightTarget.IsChildOf(dickRoot) && weights[wt].weight > 0f) {
                            dickVerts.Add(mesh.bindposes[GetPenetratorRootBoneID(skinnedMesh)].MultiplyPoint(verts[vt]));
                        }
                        wt++;
                    }
                    vt++;
                }
            }
            if (dickVerts.Count <= 0) {
                throw new UnityException("There was no dick verts found weighted to the target transform or its children! Make sure they have a weight assigned in the mesh.");
            }
            // Sort them front to back, based on the dickForward axis.
            dickVerts.Sort((a, b) => Vector3.Dot(a, dickForward).CompareTo(Vector3.Dot(b, dickForward)));
            float start = Vector3.Dot(dickVerts[0], dickForward);
            float end = Vector3.Dot(dickVerts[dickVerts.Count-1], dickForward);
            float length = end - start;
            float crossSectionLength = length / (float)(crossSections-1);
            float targetPlane = start;
            // Split them into cross sections, and analyze each one for the girth and x/y offset.
            for (int crossSectionNumber = 0;crossSectionNumber<crossSections;crossSectionNumber++) {
                // Get all the verts closest to the plane
                dickVerts.Sort((a, b) => Mathf.Abs(Vector3.Dot(a, dickForward)-targetPlane).CompareTo(Mathf.Abs(Vector3.Dot(b, dickForward)-targetPlane)));
                List<Vector3> crossSection = new List<Vector3>();
                for(int i = 0; i < (dickVerts.Count / crossSections); i++) {
                    crossSection.Add(dickVerts[i]);
                }
                crossSection.Sort((a, b) => Vector3.Dot(a, dickRight).CompareTo(Vector3.Dot(b, dickRight)));
                float crossWidth = Vector3.Dot(crossSection[crossSection.Count - 1], dickRight) - Vector3.Dot(crossSection[0], dickRight);
                float crossRightCenter = Vector3.Dot(crossSection[0], dickRight) + crossWidth / 2f;
                crossSection.Sort((a, b) => Vector3.Dot(a, dickUp).CompareTo(Vector3.Dot(b, dickUp)));
                float crossHeight = Vector3.Dot(crossSection[crossSection.Count - 1], dickUp) - Vector3.Dot(crossSection[0], dickUp);
                float crossHeightCenter = Vector3.Dot(crossSection[0], dickUp) + crossHeight / 2f;
                if (crossSectionNumber == 0 || crossSectionNumber == crossSections - 1) {
                    // We always gotta end and start at 0.
                    shape.girth.AddKey(new Keyframe(targetPlane, 0f));
                } else {
                    shape.girth.AddKey(new Keyframe(targetPlane, ((crossWidth + crossHeight) * 0.5f)));
                }
                shape.xOffset.AddKey(new Keyframe(targetPlane, crossRightCenter));
                shape.yOffset.AddKey(new Keyframe(targetPlane, crossHeightCenter));
                targetPlane += crossSectionLength;
            }
            // Instead of offseting the localPenetratorRoot on the X/Y plane, we keep it perfectly aligned with the root bone.
            // This is actually a requirement since the shader doesn't account for X/Y offets at all.
            shape.localPenetratorRoot = dickForward * start;

            // Just make sure the graph is smooth. ClampForever makes things sampling too far away get a girth/offset of 0.
            shape.girth.AutoSmooth();
            shape.girth.preWrapMode = WrapMode.ClampForever;
            shape.girth.postWrapMode = WrapMode.ClampForever;
            shape.xOffset.AutoSmooth();
            shape.xOffset.preWrapMode = WrapMode.ClampForever;
            shape.xOffset.postWrapMode = WrapMode.ClampForever;
            shape.yOffset.AutoSmooth();
            shape.yOffset.preWrapMode = WrapMode.ClampForever;
            shape.yOffset.postWrapMode = WrapMode.ClampForever;
        }
        public void BakeAll() {
            GenerateBlendshapeDict();
            dickForward = dickRoot.InverseTransformDirection((dickTip.position - dickRoot.position).normalized);
            if (Vector3.Dot(dickForward, Vector3.up) > 0.9f) {
                // if the dick root is y forward, then we should use z forward instead.
                dickUp = Vector3.forward;
            } else {
                // Otherwise up should work fine.
                dickUp = Vector3.up;
            }
            dickRight = Vector3.Cross(dickForward, dickUp);
            Vector3.OrthoNormalize(ref dickForward, ref dickUp, ref dickRight);
            foreach(PenetratorShape shape in shapes) {
                BakeShape(shape);
            }
        }
        private bool HasShape(string shapeName) {
            foreach(var shape in shapes) {
                if (shape.blendshapeName == shapeName) {
                    return true;
                }
            }
            return false;
        }
        public void AutoFindShapes() {
            if (bakeTargets.Count == 0 || bakeTargets[0] == null) {
                Debug.LogError("Please set a bake target before trying to auto-populate shapes!");
                return;
            }
            SkinnedMeshRenderer ren = bakeTargets[0];
            if (!HasShape("")) {
                shapes.Add(new PenetratorShape() {
                    shapeType = PenetratorShape.ShapeType.Default,
                    blendshapeName = "",
                });
            }
            if (!HasShape("DickPull") && bakeTargets[0].sharedMesh.GetBlendShapeIndex("DickPull") != -1) {
                shapes.Add(new PenetratorShape() {
                    shapeType = PenetratorShape.ShapeType.Pull,
                    blendshapeName = "DickPull",
                });
            }
            if (!HasShape("DickSquish") && bakeTargets[0].sharedMesh.GetBlendShapeIndex("DickSquish") != -1) {
                shapes.Add(new PenetratorShape() {
                    shapeType = PenetratorShape.ShapeType.Squish,
                    blendshapeName = "DickSquish",
                });
            }
            if (!HasShape("DickCum") && bakeTargets[0].sharedMesh.GetBlendShapeIndex("DickCum") != -1) {
                shapes.Add(new PenetratorShape() {
                    shapeType = PenetratorShape.ShapeType.Cum,
                    blendshapeName = "DickCum",
                });
            }
        }
    #endif
        public void CoupleWith(Penetrable hole, float penetrationDepth01) {
            if (holeTarget != null) {
                Decouple(true);
            }
            //body.isKinematic = true;
            IgnoreCollision(hole.root);
            //dick.body.maxAngularVelocity = 64f;
            StopCoroutine("DecoupleRoutine");
            this.penetrationDepth01 = penetrationDepth01;
            foreach(Penetrator p in hole.GetPenetrators()) {
                foreach(Collider a in p.selfColliders) {
                    foreach(Collider b in p.selfColliders) {
                        Physics.IgnoreCollision(a, b, true);
                    }
                }
            }
            holeTarget = hole;
            invisibleWhenInside = !holeTarget.canSeePenetratorInside;
            allTheWayThrough = holeTarget.canAllTheWayThrough;
            if (slimySource != null && slimySlidingSounds.Count > 0) {
                slimySource.clip = slimySlidingSounds[Random.Range(0,slimySlidingSounds.Count)];
                slimySource.volume = 0f;
                slimySource.timeSamples = Random.Range(0,slimySource.clip.samples);
                slimySource.Play();
            }
            holeTarget.AddPenetrator(this);
            OnPenetrate.Invoke();
        }
        public void Decouple(bool instantaneous) {
            if (holeTarget == null) {
                return;
            }
            //slimySource?.Stop();
            float rootTargetPoint = (penetrationDepth01-1f)*GetLength()/holeTarget.orificeLength;
            if (rootTargetPoint > 1f || instantaneous) {
                penetrationDepth01 = -1f;
                squishPullAmount = 0f;
                body.detectCollisions = true;
                SetDeforms();
                OnEndPenetrate.Invoke();
                UnignoreAll();
                holeTarget?.RemovePenetrator(this);
                foreach(Penetrator p in holeTarget.GetPenetrators()) {
                    foreach(Collider a in p.selfColliders) {
                        foreach(Collider b in p.selfColliders) {
                            Physics.IgnoreCollision(a, b, false);
                        }
                    }
                }
                holeTarget = null;
                //body.isKinematic = bodyWasKinematic;
                //body.drag = 0f;
                //body.useGravity = bodyWasAffectedByGravity;
                //body.angularDrag = 0.05f;
                return;
            }
            if (penetrationDepth01<0f) {
                StartCoroutine("DecoupleRoutine");
            }
        }
        public void SetDeforms() {
            if (dickRoot == null) {
                return;
            }
            float dickLength = GetLength();
            float orificeLength = 1f;
            if (holeTarget != null) {
                orificeLength = holeTarget.orificeLength;
            }
            if (dickRoot != null && deformationTargets.Count > 0) {
                foreach (var renderer in deformationTargets) {
                    if (renderer == null) {
                        continue;
                    }
                    Material[] materials = renderer.sharedMaterials;
                    if (Application.isPlaying) {
                        materials = renderer.materials;
                    }
                    foreach (var material in materials) {
                        if (shapes.Count < 4) {
                            material.EnableKeyword("_NOBLENDSHAPES_ON");
                        } else {
                            material.DisableKeyword("_NOBLENDSHAPES_ON");
                        }
                        material.SetVector("_PenetratorOrigin", Vector3.Scale(renderer.rootBone.worldToLocalMatrix.MultiplyPoint(dickRoot.TransformPoint(GetLocalRootPosition())), renderer.rootBone.lossyScale));
                        material.SetVector("_PenetratorForward", Vector3.Normalize(renderer.rootBone.worldToLocalMatrix.MultiplyVector(dickRoot.TransformDirection(dickForward))));
                        material.SetVector("_PenetratorRight", Vector3.Normalize(renderer.rootBone.worldToLocalMatrix.MultiplyVector(dickRoot.TransformDirection(dickRight))));
                        material.SetVector("_PenetratorUp", Vector3.Normalize(renderer.rootBone.worldToLocalMatrix.MultiplyVector(dickRoot.TransformDirection(dickUp))));
                        material.SetFloat("_PenetratorLength", dickLength);
                        if (penetrationDepth01 < 0f) {
                            material.SetFloat("_PenetrationDepth", -(1f-Easing.Exponential.In(Mathf.Clamp01(1f+penetrationDepth01))));
                        } else {
                            material.SetFloat("_PenetrationDepth", penetrationDepth01);
                        }
                        if (holeTarget != null) {
                            Vector3 entranceForward, entranceRight, entranceUp;
                            entranceForward = entranceRight = entranceUp = Vector3.zero;
                            holeTarget.GetOrtho(0f, backwards, dickRoot.TransformDirection(dickForward), dickRoot.TransformDirection(dickUp), ref entranceForward, ref entranceRight, ref entranceUp);
                            Vector3 entranceOffset = GetWorldPlanarOffset(penetrationDepth01);
                            Quaternion rotateAdjust = Quaternion.FromToRotation(dickRoot.TransformDirection(dickForward), entranceForward);
                            rotateAdjust = Quaternion.FromToRotation(rotateAdjust * dickRoot.TransformDirection(dickUp), entranceUp)*rotateAdjust;
                            Vector3 entranceOffsetAdjust = rotateAdjust * entranceOffset;
                            material.SetVector("_OrificeWorldPosition", p0 - entranceOffsetAdjust);
                            material.SetVector("_OrificeOutWorldPosition1", p1 - entranceOffsetAdjust);

                            Vector3 exitForward, exitRight, exitUp;
                            exitForward = exitRight = exitUp = Vector3.zero;
                            holeTarget.GetOrtho(1f, backwards, dickRoot.TransformDirection(dickForward), dickRoot.TransformDirection(dickUp), ref exitForward, ref exitRight, ref exitUp);
                            Vector3 exitOffset = GetWorldPlanarOffset((-orificeLength/dickLength)+penetrationDepth01);
                            Quaternion exitRotateAdjust = Quaternion.FromToRotation(dickRoot.TransformDirection(dickForward), exitForward);
                            exitRotateAdjust = Quaternion.FromToRotation(exitRotateAdjust * dickRoot.TransformDirection(dickUp), exitUp)*exitRotateAdjust;
                            Vector3 exitOffsetAdjust = exitRotateAdjust * exitOffset;
                            material.SetVector("_OrificeOutWorldPosition3", p3 - exitOffsetAdjust);
                            material.SetVector("_OrificeOutWorldPosition2", p2 - exitOffsetAdjust);
                            /*Debug.DrawLine(p3, p3+exitForward, Color.blue);
                            Debug.DrawLine(p3, p3+exitRight, Color.red);
                            Debug.DrawLine(p3, p3+exitUp, Color.green);
                            Debug.DrawLine(p3, p3 - exitOffsetAdjust);

                            Debug.DrawLine(dickRoot.position, dickRoot.position+dickRoot.TransformDirection(dickForward), Color.blue);
                            Debug.DrawLine(dickRoot.position, dickRoot.position+dickRoot.TransformDirection(dickRight), Color.red);
                            Debug.DrawLine(dickRoot.position, dickRoot.position+dickRoot.TransformDirection(dickUp), Color.green);
                            Debug.DrawLine(dickRoot.position, dickRoot.position - exitOffset);*/
                            material.SetVector("_OrificeWorldNormal", -holeTarget.GetTangent(0f,backwards));
                        }

                        material.SetFloat("_OrificeLength", orificeLength);
                        material.SetFloat("_PenetratorBlendshapeMultiplier", dickRoot.TransformVector(dickUp).magnitude * GetTransformPose(renderer).lossyScale.x);
                        material.SetFloat("_PenetratorCumActive", cumActive);
                        material.SetFloat("_PenetratorCumProgress", cumProgress);
                        material.SetFloat("_PenetratorBulgePercentage", bulgePercentage);
                        // If we're cumming, squishing and pulling will offset our girth calculations incorrectly.
                        material.SetFloat("_PenetratorSquishPullAmount", Mathf.Lerp(squishPullAmount,0f,cumActive));
                    }
                }
            }
        }
        public void OnTriggerEnter(Collider collider) {
            if (!autoPenetrate) {
                return;
            }
            CheckCollision(collider);
        }
        public void OnTriggerStay(Collider collider) {
            if (!autoPenetrate) {
                return;
            }
            CheckCollision(collider);
        }
        public void IgnoreCollision(Transform obj) {
            foreach (Collider d in selfColliders) {
                //if (d.gameObject.layer == LayerMask.NameToLayer("PlayerHitbox")) {
                //d.gameObject.layer = LayerMask.NameToLayer("Player");
                //}
                foreach (Collider e in obj.gameObject.GetComponentsInChildren<Collider>()) {
                    Physics.IgnoreCollision(e, d, true);
                    ignoringCollisions.Add(e);
                }
            }
        }
        public void UnignoreAll() {
            foreach (Collider d in selfColliders) {
                if (d == null) {
                    continue;
                }
                //if (d.gameObject.layer == LayerMask.NameToLayer("Player")) {
                //d.gameObject.layer = LayerMask.NameToLayer("PlayerHitbox");
                //}
                foreach (Collider c in ignoringCollisions) {
                    if (c == null) {
                        continue;
                    }
                    Physics.IgnoreCollision(c, d, false);
                }
            }
            ignoringCollisions.Clear();
        }
        public void CheckCollision(Collider collider) {
            if (!isActiveAndEnabled || !autoPenetrate) {
                return;
            }
            //if (!grabbed) {
            //return;
            //}
            // Don't penetrate kobolds that are penetrating us!
            //if (k.activePenetrators.Count > 0 && k.activePenetrators[0].dick.holeTarget != null && kobold == k.activePenetrators[0].dick.holeTarget.transform.root.GetComponent<Kobold>()) {
                //return;
            //}
            // Don't penetrate a kobold that's already penetrating us
            //if (k.activePenetrators.Count > 0 && k.activePenetrators[0].dick.holeTarget != null && k.activePenetrators[0].dick.holeTarget.transform.root.GetComponent<Kobold>() == kobold) {
                //return;
            //}
            if (holeTarget != null) {
                return;
            }
            Penetrable closestPenetrable = collider.GetComponentInParent<Penetrable>();
            // No circular dependencies please.
            if (closestPenetrable != null) {
                if (closestPenetrable.root == this.root) {
                    return;
                }
                float dist = Vector3.Distance(GetWorldRootPosition(), closestPenetrable.path[0].position);
                float dist2 = Vector3.Distance(GetWorldRootPosition(), closestPenetrable.path[3].position);
                backwards = (dist2 < dist && closestPenetrable.canAllTheWayThrough);
                if (backwards) {
                    dist = dist2;
                }
                float angleDiff = Vector3.Dot(closestPenetrable.GetTangent(0f, backwards), dickRoot.TransformDirection(dickForward));
                if (!closestPenetrable.ContainsPenetrator(this) && angleDiff > -0.25f) {
                    if (dist > GetLength()) {
                        return;
                    }
                    CoupleWith(closestPenetrable, 0f);
                }
            }
        }
        public void OnValidate() {
            GenerateBlendshapeDict();
            SetDeforms();
        }
        void OnDisable() {
            if (holeTarget != null && holeTarget.ContainsPenetrator(this)) {
                holeTarget.RemovePenetrator(this);
            }
        }

        void Update() {
            if (Application.isEditor && !Application.isPlaying) {
                SetDeforms();
                return;
            }
            if (holeTarget != null && !holeTarget.ContainsPenetrator(this)) {
                CoupleWith(holeTarget, penetrationDepth01);
            }
            if (dickTipDetector != null) {
                dickTipDetector.center = transform.InverseTransformPoint(dickTip.position);
            }
            if (slimySource != null) {
                slimySource.volume = Mathf.MoveTowards(slimySource.volume, 0f, Time.deltaTime);
            }
            if (holeTarget != null) {
                //penetrationDepth01 += GetTangent(penetrationDepth01)*Time.deltaTime;
                //penetrationDepth01 += GetTangent(penetrationDepth01-(holeTarget.orificeLength/GetLength()))*Time.deltaTime;
            } else {
                penetrationDepth01 = -1f;
            }
            if (autoPenetrate && holeTarget != null) {
                PushTowards(GetWorldRootPosition());
            }
            SetDeforms();
        }
        void LateUpdate() {
            InitiateEndMove();
        }
        //void FixedUpdate() {
            //if (selfColliders.Count > 0 && selfColliders[0] is CapsuleCollider) {
                //selfColliders[0].transform.localScale = (Vector3.one - dickForward*Mathf.Clamp01(penetrationDepth01));
            //}
        //}
        public void PushTowards(float direction) {
            float orificeDepth01 = ((penetrationDepth01-1f))*GetLength()/holeTarget.orificeLength;
            Vector3 holePos = holeTarget.GetPoint(orificeDepth01, backwards);
            Vector3 holeTangent = holeTarget.GetTangent(Mathf.Clamp01(orificeDepth01), backwards).normalized;
            PushTowards(holePos+holeTangent*direction);
        }
        public void PushTowards(Vector3 worldPosition) {
            if (holeTarget == null){
                return;
            }
            float length = GetLength();
            // Calculate where the "first" shape is located along the orifice path.
            float firstShapeOffset = ((backwards?1f-holeTarget.shapes[holeTarget.shapes.Count-1].alongPathAmount01:holeTarget.shapes[0].alongPathAmount01)*holeTarget.orificeLength)/length;
            float lastShapeOffset = ((backwards?holeTarget.shapes[0].alongPathAmount01:1f-holeTarget.shapes[holeTarget.shapes.Count-1].alongPathAmount01)*holeTarget.orificeLength)/length;
            // If we cannot overpenetrate, we use a method that simply uses the distance to the hole to determine how deep we are.
            if (!canOverpenetrate) {
                float dist = Vector3.Distance(worldPosition, holeTarget.GetPoint(0,backwards));
                float diff = ((1f-(dist/GetLength()))-penetrationDepth01);
                // Start squishing or pulling based purely on the distance to the crotch
                squishPullAmount = Mathf.MoveTowards(squishPullAmount, 0f, Time.deltaTime);
                squishPullAmount -= diff*Time.deltaTime*(1f/slideFriction);
                squishPullAmount = Mathf.Clamp(squishPullAmount, -1f, 1f);
                // If we're fully squished or pulled, we finally start sliding.
                if (Mathf.Abs(squishPullAmount) == 1f) {
                    float move = diff*Time.deltaTime*8f;
                    // Calculate the tangents, which is used for knot forces at both the entrance and exit shape.
                    float girthTangents = GetTangent(penetrationDepth01 - firstShapeOffset);
                    girthTangents += GetTangent(penetrationDepth01-(holeTarget.orificeLength/length) + lastShapeOffset);
                    move *= Mathf.Clamp(1f+girthTangents*Mathf.Sign(move), 0.2f, 2f);
                    penetrationDepth01 = Mathf.Clamp(penetrationDepth01+move, -1f, 1f);
                }
            // Otherwise, we use a moving plane that follows the normal of the orifice path, and use the plane distance to the desired point to determine which way we should go.
            } else {
                float orificeDepth01 = ((penetrationDepth01-1f))*GetLength()/holeTarget.orificeLength;
                Vector3 holePos = holeTarget.GetPoint(orificeDepth01, backwards);
                Vector3 holeTangent = holeTarget.GetTangent(Mathf.Clamp01(orificeDepth01), backwards).normalized;
                Vector3 holeToMouse = worldPosition - holePos;
                float squishMove = Vector3.Dot(holeToMouse, holeTangent);
                squishPullAmount -= squishMove*Time.deltaTime*(1f/slideFriction);
                squishPullAmount = Mathf.Clamp(squishPullAmount, -1f, 1f);
                if (Mathf.Abs(squishPullAmount) == 1f) {
                    float move = Vector3.Dot(holeToMouse, holeTangent)*Time.deltaTime*15f;
                    float girthTangents = GetTangent(penetrationDepth01 - firstShapeOffset);
                    girthTangents += GetTangent(penetrationDepth01-(holeTarget.orificeLength/length) + lastShapeOffset);
                    move *= Mathf.Clamp(1f+girthTangents*Mathf.Sign(move), 0.2f, 2f);
                    penetrationDepth01 = Mathf.Max(penetrationDepth01+move, -1f);
                }
            }
            // Prevent the dick from penetrating futher than intended.
            if (!holeTarget.canAllTheWayThrough) {
                penetrationDepth01 = Mathf.Min(penetrationDepth01,holeTarget.allowedPenetrationDepth01*holeTarget.orificeLength/GetLength());
            }
            if (!IsInside() && autoDecouple) {
                Decouple(false);
            }
        }
        public bool IsInside(float leeway = 0f) {
            if (holeTarget == null) {
                return false;
            }
            float rootTargetPoint = (penetrationDepth01-1f)*GetLength()/holeTarget.orificeLength;
            if (rootTargetPoint > 1f-leeway || penetrationDepth01 < 0f+leeway) {
                return false;
            }
            return true;
        }
        public void Cum() {
            StopCoroutine("CumRoutine");
            StartCoroutine("CumRoutine");
        }
        public IEnumerator CumRoutine() {
            float startTime = Time.timeSinceLevelLoad;
            while(Time.timeSinceLevelLoad<startTime+1f) {
                cumActive = (Time.timeSinceLevelLoad-startTime);
                yield return null;
            }
            for(int i=0;i<cumPulseCount;i++) {
                cumProgress = -bulgePercentage;
                if (pumpingSounds.Count > 0 && plapSource != null) {
                    plapSource.pitch = Random.Range(0.7f,1.3f);
                    plapSource.PlayOneShot(pumpingSounds[Random.Range(0,pumpingSounds.Count)], 1f);
                }
                while (cumProgress < 1f+bulgePercentage) {
                    cumProgress = Mathf.MoveTowards(cumProgress, 1f+bulgePercentage+0.1f, Time.deltaTime);
                    yield return null;
                }
                OnCumEmit.Invoke();
                //stream.Fire(dickCumContents, cumVolumePerPump/10f);
            }
            cumActive = 0f;
        }
        public IEnumerator DecoupleRoutine() {
            while (penetrationDepth01 != -1f) {
                penetrationDepth01 = Mathf.MoveTowards(penetrationDepth01, -1f, Time.deltaTime*4f);
                yield return null;
            }
            Decouple(true);
        }
        // The function called at the very end of all movement operations. (could've been pushed in, then pushed back for clipping, so we only want one move event in the end.)
        private void InitiateEndMove() {
            float diff = (penetrationDepth01 - lastPenetrationDepth01);
            float min = Mathf.Min(penetrationDepth01, lastPenetrationDepth01);
            float max = Mathf.Max(penetrationDepth01, lastPenetrationDepth01);
            foreach(DepthEvent de in depthEvents) {
                float triggerPoint = de.triggerAlongDepth01;
                if ((triggerPoint > min && triggerPoint < max)) {
                    if (de.triggerDirection == DepthEvent.TriggerDirection.Both ||
                    (de.triggerDirection == DepthEvent.TriggerDirection.PullOut && diff < 0) ||
                    (de.triggerDirection == DepthEvent.TriggerDirection.PushIn && diff > 0)) {
                        de.Trigger(dickRoot.position + dickRoot.TransformDirection(dickForward)*triggerPoint, plapSource);
                    }
                }
            }
            if(slimySource != null) {
                slimySource.volume += Mathf.Abs(diff*GetLength()*5f);
                slimySource.volume = Mathf.Clamp01(slimySource.volume);
            }
            OnMove.Invoke(diff);
            lastPenetrationDepth01 = penetrationDepth01;
        }

    }
}