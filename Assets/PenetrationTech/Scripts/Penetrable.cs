using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PenetrationTech
{

    #if UNITY_EDITOR
    using UnityEditor;

    [CustomEditor(typeof(Penetrable))]
    public class PenetrableEditor : Editor {
        public int lastTouched = 0;
        public enum PenetrableTool {
            None = 0,
            Path,
            Expand,
            Pull,
            Push
        }
        public PenetrableTool selectedTool = PenetrableTool.None;
        //private string[] toolTexts = new string[]{"None", "Path tool", "Expand tool", "Pull tool", "Push tool"};
        private string[] toolTexts = new string[]{"None", "Path tool", "Expand tool"}; //, "Pull tool", "Push tool"};
        public override void OnInspectorGUI() {
            selectedTool = (PenetrableTool)GUILayout.Toolbar((int)selectedTool, toolTexts);
            GUILayout.Space(16);
            DrawDefaultInspector();
            bool showWarning = false;
            if (serializedObject.FindProperty("path").arraySize < 4) {
                showWarning = true;
            }
            for (int i=0;i<4&&!showWarning;i++) {
                if (serializedObject.FindProperty("path").GetArrayElementAtIndex(i).FindPropertyRelative("attachedTransform").objectReferenceValue == null) {
                    showWarning = true;
                }
            }
            if (showWarning) {
                EditorGUILayout.HelpBox("First set up 4 path nodes, specify a Transform thats nearest along the path to take. (hips, spine, neck, etc).", MessageType.Error);
            }
            switch(selectedTool) {
                case PenetrableTool.None:
                    if (serializedObject.FindProperty("holeMeshes").arraySize <= 0) {
                        EditorGUILayout.HelpBox("Now specify which meshes to target blendshape deformations.", MessageType.Info);
                    } else {
                        EditorGUILayout.HelpBox("Finally, use the Path tool, and the Expand tool at the top to adjust how this object is penetrated.", MessageType.Info);
                        EditorGUILayout.HelpBox("MAKE SURE GIZMOS ARE ENABLED, otherwise the tools and widgets won't appear.", MessageType.Warning);
                    }
                break;
                case PenetrableTool.Path:
                    EditorGUILayout.HelpBox("Path tool is used to display and adjust the bezier curve of the path that penetrators will take. It only displays when 4 path nodes are set up.", MessageType.Info);
                    EditorGUILayout.HelpBox("Adjust the BEZIER DEPTHS paths along the depth of the orifice, BEZIER DEPTHS 3 will be used as an alternate entrace if allowedPenetrationDepth is 1.", MessageType.Info);
                    break;
                case PenetrableTool.Expand: EditorGUILayout.HelpBox("For each Shape, place the corresponding widget on along the curve next to the shape. Then change its diameter so that we know how big the shape deforms.", MessageType.Info); break;
            }
        }
        public void DrawBezier() {
            Penetrable p = (Penetrable)target;
            if(p.path != null && p.path.Count>=4) {
                Vector3 p0 = p.path[0].position;
                Vector3 p1 = p.path[1].position;
                Vector3 p2 = p.path[2].position;
                Vector3 p3 = p.path[3].position;
                for (float t = 0; t <= 1f-(1f/16f); t += 1f / 16f) {
                    Vector3 startPoint = Bezier.BezierPoint(p0, p1, p2, p3, t);
                    Vector3 endPoint = Bezier.BezierPoint(p0, p1, p2, p3, t+(1f/16f));
                    //Vector3 normal = Bezier.BezierSlope(p0, p1, p2, p3, t);
                    Handles.color = Color.yellow;
                    Handles.DrawLine(startPoint, endPoint);
                    //Handles.color = Color.blue;
                    //Handles.DrawLine(startPoint, startPoint+normal*0.1f);
                }
                if (selectedTool == PenetrableTool.Expand) {
                    foreach (Penetrable.PenetrableShape shape in p.shapes) {
                        Vector3 point = Bezier.BezierPoint(p0, p1, p2, p3, shape.alongPathAmount01);
                        Vector3 normal = Bezier.BezierSlope(p0, p1, p2, p3, shape.alongPathAmount01);
                        Handles.color = Color.blue;
                        Handles.DrawWireDisc(point, normal, shape.holeDiameter * 0.5f * p.transform.lossyScale.x);
                    }
                }
            }
        }
        public void OnSceneGUI() {
            Penetrable p = (Penetrable)target;
            if (p.path.Count < 4) {
                return;
            }
            for (int i=0;i<4;i++) {
                if (p.path[i].attachedTransform == null) {
                    return;
                }
            }
            SerializedProperty shapes = serializedObject.FindProperty("shapes");
            SerializedProperty meshes = serializedObject.FindProperty("holeMeshes");
            for (int i = 0; i < shapes.arraySize; i++) {
                for (int o = 0; o < meshes.arraySize; o++) {
                    SkinnedMeshRenderer renderer = (SkinnedMeshRenderer)(meshes.GetArrayElementAtIndex(o).objectReferenceValue);
                    string expandShape = (shapes.GetArrayElementAtIndex(i).FindPropertyRelative("expandBlendshapeName").stringValue);
                    int expandID = renderer.sharedMesh.GetBlendShapeIndex(expandShape);
                    string pushShape = (shapes.GetArrayElementAtIndex(i).FindPropertyRelative("pushBlendshapeName").stringValue);
                    int pushID = renderer.sharedMesh.GetBlendShapeIndex(pushShape);
                    string pullShape = (shapes.GetArrayElementAtIndex(i).FindPropertyRelative("pullBlendshapeName").stringValue);
                    int pullID = renderer.sharedMesh.GetBlendShapeIndex(pullShape);
                    if (i == lastTouched) {
                        if (expandID != -1) { renderer.SetBlendShapeWeight(expandID, selectedTool == PenetrableTool.Expand ? 100f : 0f); }
                        if (pullID != -1) { renderer.SetBlendShapeWeight(pullID, selectedTool == PenetrableTool.Pull ? 100f : 0f); }
                        if (pushID != -1) { renderer.SetBlendShapeWeight(pushID, selectedTool == PenetrableTool.Push ? 100f : 0f); }
                    } else {
                        if (expandID != -1) { renderer.SetBlendShapeWeight(expandID, 0f); }
                        if (pullID != -1) { renderer.SetBlendShapeWeight(pullID, 0f); }
                        if (pushID != -1) { renderer.SetBlendShapeWeight(pushID, 0f); }
                    }
                }
            }
            SerializedProperty paths = serializedObject.FindProperty("path");
            if (selectedTool == PenetrableTool.Path) {
                for (int i = 0; i < paths.arraySize && i < 4; i++) {
                    Transform attachedTransform = (Transform)(paths.GetArrayElementAtIndex(i).FindPropertyRelative("attachedTransform").objectReferenceValue);
                    if (attachedTransform == null) {
                        continue;
                    }
                    SerializedProperty localOffset = paths.GetArrayElementAtIndex(i).FindPropertyRelative("localOffset");
                    Vector3 globalPosition = Handles.PositionHandle(attachedTransform.TransformPoint(localOffset.vector3Value), attachedTransform.rotation);
                    if (Vector3.Distance(attachedTransform.InverseTransformPoint(globalPosition), localOffset.vector3Value) > 0.001f) {
                        //Undo.RecordObject(target, "Penetrator origin move");
                        localOffset.vector3Value = attachedTransform.InverseTransformPoint(globalPosition);
                        serializedObject.ApplyModifiedProperties();
                        //EditorUtility.SetDirty(target);
                    }
                    if (i == 0) {
                        Handles.Label(attachedTransform.TransformPoint(localOffset.vector3Value), "ORIFACE ENTRANCE");
                    } else if (i < 3 || serializedObject.FindProperty("allowedPenetrationDepth01").floatValue != 1f) {
                        Handles.Label(attachedTransform.TransformPoint(localOffset.vector3Value), "BEZIER DEPTHS " + i);
                    } else {
                        Handles.Label(attachedTransform.TransformPoint(localOffset.vector3Value), "ORIFACE EXIT");
                    }
                }
            }
            Vector3 p0 = p.path[0].position;
            Vector3 p1 = p.path[1].position;
            Vector3 p2 = p.path[2].position;
            Vector3 p3 = p.path[3].position;
            if (selectedTool == PenetrableTool.Expand) {
                for (int i = 0; i < shapes.arraySize; i++) {
                    string expandBlendshapeName = shapes.GetArrayElementAtIndex(i).FindPropertyRelative("expandBlendshapeName").stringValue;
                    if (string.IsNullOrEmpty(expandBlendshapeName)) {
                        continue;
                    }
                    SerializedProperty alongPath = shapes.GetArrayElementAtIndex(i).FindPropertyRelative("alongPathAmount01");
                    Vector3 pushTargetPoint = Bezier.BezierPoint(p0, p1, p2, p3, Mathf.Clamp01(alongPath.floatValue));
                    Vector3 pushTargetTangent = Bezier.BezierSlope(p0, p1, p2, p3, Mathf.Clamp01(alongPath.floatValue));

                    Vector3 pushGlobalPosition = Handles.PositionHandle(pushTargetPoint, Quaternion.LookRotation(pushTargetTangent));
                    if (Vector3.Distance(pushTargetPoint, pushGlobalPosition) > 0.001f) {
                        lastTouched = i;
                        float dir = Vector3.Dot(pushGlobalPosition-pushTargetPoint, pushTargetTangent);
                        alongPath.floatValue = Mathf.Clamp01(alongPath.floatValue + dir);
                        serializedObject.ApplyModifiedProperties();
                    }
                    Handles.Label(pushGlobalPosition, expandBlendshapeName);
                }
            }
            if (selectedTool == PenetrableTool.Pull || selectedTool == PenetrableTool.Push) {
                for (int i = 0; i < shapes.arraySize; i++) {
                    string pushBlendshapeName = shapes.GetArrayElementAtIndex(i).FindPropertyRelative(selectedTool == PenetrableTool.Pull ? "pullBlendshapeName" : "pushBlendshapeName" ).stringValue;
                    if (string.IsNullOrEmpty(pushBlendshapeName)) {
                        continue;
                    }
                    SerializedProperty pushLocalOffset = shapes.GetArrayElementAtIndex(i).FindPropertyRelative(selectedTool == PenetrableTool.Pull ? "pullPositionOffset":"pushPositionOffset");
                    SerializedProperty alongPath = shapes.GetArrayElementAtIndex(i).FindPropertyRelative("alongPathAmount01");
                    Vector3 pushTargetPoint = Bezier.BezierPoint(p0, p1, p2, p3, Mathf.Clamp01( alongPath.floatValue + pushLocalOffset.floatValue));
                    Vector3 pushTargetTangent = Bezier.BezierSlope(p0, p1, p2, p3, Mathf.Clamp01(alongPath.floatValue + pushLocalOffset.floatValue));

                    Vector3 pushGlobalPosition = Handles.PositionHandle(pushTargetPoint, Quaternion.LookRotation(pushTargetTangent));
                    if (Vector3.Distance(pushTargetPoint, pushGlobalPosition) > 0.001f) {
                        lastTouched = i;
                        float dir = Vector3.Dot(pushGlobalPosition-pushTargetPoint, pushTargetTangent);
                        if (selectedTool == PenetrableTool.Pull) {
                            pushLocalOffset.floatValue = Mathf.Clamp(pushLocalOffset.floatValue - alongPath.floatValue + dir, -1f, 0f);
                        } else {
                            pushLocalOffset.floatValue = Mathf.Clamp(pushLocalOffset.floatValue - alongPath.floatValue + dir, 0f, 1f);
                        }
                        serializedObject.ApplyModifiedProperties();
                        serializedObject.ApplyModifiedProperties();
                    }
                    Handles.Label(pushGlobalPosition, selectedTool == PenetrableTool.Pull ? pushBlendshapeName + " PULL POINT" : pushBlendshapeName + " PUSH POINT");
                }
            }
            DrawBezier();
        }
        public void OnDisable() {
            Penetrable p = (Penetrable)target;
            foreach (SkinnedMeshRenderer r in p.holeMeshes) {
                if (r == null) {
                    continue;
                }
                foreach (var shape in p.shapes) {
                    int expandIndex = r.sharedMesh.GetBlendShapeIndex(shape.expandBlendshapeName);
                    int pullIndex = r.sharedMesh.GetBlendShapeIndex(shape.pullBlendshapeName);
                    int pushIndex = r.sharedMesh.GetBlendShapeIndex(shape.pushBlendshapeName);
                    if (expandIndex != -1) { r.SetBlendShapeWeight(expandIndex, 0f); }
                    if (pullIndex != -1) { r.SetBlendShapeWeight(pullIndex, 0f); }
                    if (pushIndex != -1) { r.SetBlendShapeWeight(pushIndex, 0f); }
                }
            }
        }
    }
    #endif
    public class Penetrable : MonoBehaviour {
        [Tooltip("The \"root\" object that is the highest-most transform that would still be a part of this penetratable. This would be the character, or onahole root transform. It's used in circular-dependency checks, as well as collision phasing.")]
        public Transform root;

        [LayerAttribute]
        [Tooltip("The layer that trigger colliders are automatically created on in order to detect dick intersections. It's fine to leave it as Default unless you want some performance gain.")]
        public int holeLayer;

        [Range(0f,1f)]
        [Tooltip("This variable is used to prevent a pentrator from going too deep into the path. Set it to 1 to allow dicks to go \"all the way through\".")]
        public float allowedPenetrationDepth01 = 1f;
        [Tooltip("This is read by dicks to toggle a shader flag that prevents the dick from turning invisible inside. Set to true if you've got a transparent onahole or similar.")]
        public bool canSeePenetratorInside = false;
        [Tooltip("The path nodes used to construct the Bezier path, there should ONLY BE FOUR. No more and no less.")]
        public List<PenetrablePath> path = new List<PenetrablePath>();
        [Tooltip("Hole meshes to trigger blendshapes on, they should all share the same blendshape names.")]
        public List<SkinnedMeshRenderer> holeMeshes;
        [Tooltip("A list of shapes, where they're located along the path, and how wide they expand. Used to properly drive the shapes.")]
        public List<PenetrableShape> shapes = new List<PenetrableShape>();

        // Gets a rigidbody along the path. Useful when attempting to calculate forces on a ragdoll.
        public Rigidbody GetBody(float alongPath01, bool reverse) {
            return path[Mathf.FloorToInt(Mathf.Clamp01(alongPath01)*3.99f)].connectedBody;
        }
        public ConfigurableJoint GetJoint(float alongPath01, bool reverse) {
            PenetrablePath p = path[Mathf.FloorToInt(Mathf.Clamp01(alongPath01)*3.99f)];
            if (p.joint != null) {
                return p.joint;
            }
            if (p.connectedBody == null) {
                return null;
            }
            Quaternion savedRotation = p.connectedBody.transform.rotation;
            p.connectedBody.transform.rotation = Quaternion.identity;
            p.joint = p.connectedBody.gameObject.AddComponent<ConfigurableJoint>();
            p.connectedBody.transform.rotation = savedRotation;
            var slerpd = p.joint.slerpDrive;
            slerpd.positionSpring = 1000f;
            p.joint.slerpDrive = slerpd;
            p.joint.rotationDriveMode = RotationDriveMode.Slerp;
            p.joint.configuredInWorldSpace = true;
            return p.joint;
        }
        [System.Serializable]
        public class PenetrablePath {
            public Transform attachedTransform;
            public Vector3 localOffset;
            public Rigidbody connectedBody;
            public Vector3 position {
                get { return attachedTransform.TransformPoint(localOffset); }
            }
            [HideInInspector]
            public Vector3 right;
            [HideInInspector]
            public Vector3 up;
            [HideInInspector]
            public Vector3 forward;
            [HideInInspector]
            public ConfigurableJoint joint;
            [HideInInspector]
            public Quaternion startRotation;
        }
        [HideInInspector]
        public float orificeLength {
            get { return Bezier.BezierApproxLength(path[0].position, path[1].position, path[2].position, path[3].position); }
        }
        [System.Serializable]
        public class PenetrableShape {
            [Tooltip("The blendshape name to trigger when a penetrator reaches the specified alongPathAmount.")]
            public string expandBlendshapeName = "";
            [Tooltip("How widely the blendshape triggers.")]
            public float holeDiameter;
            [Tooltip("Where along the path is this blendshape located.")]
            public float alongPathAmount01;
            [Tooltip("Can we overdrive the shape? Should set to true unless you're doing some facial viseme blendshapes.")]
            public bool canOverdriveShapes = true;

            [Tooltip("This event fires exactly once when the shape expands from one or more penetrators.")]
            public UnityEvent OnExpand;
            [Tooltip("This event fires exactly once when there's no more penetrators triggering the shape.")]
            public UnityEvent OnEndExpand;

            // Unused Right now
            [HideInInspector]
            public string pushBlendshapeName = "";
            [HideInInspector]
            public string pullBlendshapeName = "";
            [HideInInspector]
            public float pushPositionOffset;
            [HideInInspector]
            public float pullPositionOffset;
            [HideInInspector]
            public Dictionary<Mesh, int> expandBlendshape = new Dictionary<Mesh, int>();
            [HideInInspector]
            public Dictionary<Mesh, int> pushBlendshape = new Dictionary<Mesh, int>();
            [HideInInspector]
            public Dictionary<Mesh, int> pullBlendshape = new Dictionary<Mesh, int>();
            [HideInInspector]
            public bool triggeredEvent = false;
            [HideInInspector]
            public List<float> girths = new List<float>();
        }

        public bool canAllTheWayThrough {
            get { return allowedPenetrationDepth01 == 1f; }
        }
        private List<Penetrator> penetrators = new List<Penetrator>();
        private List<Penetrator> sortedPenetrators = new List<Penetrator>();
        private SphereCollider colliderEntrance;
        private SphereCollider colliderExit;
        public void Awake() {
            float averageGirth = 0f;
            if (shapes.Count > 0) {
                foreach(var shape in shapes) {
                    averageGirth += shape.holeDiameter;
                }
                averageGirth /= shapes.Count;
            }

            colliderEntrance = gameObject.AddComponent<SphereCollider>();
            gameObject.layer = holeLayer;
            colliderEntrance.radius = averageGirth*2f;
            colliderEntrance.isTrigger = true;
            if (allowedPenetrationDepth01 == 1f) {
                colliderExit = gameObject.AddComponent<SphereCollider>();
                gameObject.layer = holeLayer;
                colliderExit.radius = averageGirth*2f;
                colliderExit.isTrigger = true;
            }
        }
        public void AddPenetrator(Penetrator d) {
            if (penetrators.Contains(d)){
                return;
            }
            penetrators.Add(d);
            foreach(var shape in shapes) {
                shape.girths.Add(0f);
            }
            sortedPenetrators.Add(d);
        }
        public void RemovePenetrator(Penetrator d) {
            if (!penetrators.Contains(d)) {
                return;
            }
            d.body.useGravity = true;
            penetrators.Remove(d);
            foreach(var shape in shapes) {
                shape.girths.RemoveAt(0);
            }
            sortedPenetrators.Remove(d);
        }
        public List<Penetrator> GetPenetrators() {
            return penetrators;
        }
        public bool ContainsPenetrator(Penetrator d) {
            return penetrators.Contains(d);
        }

        void Start() {
            if (holeMeshes.Count <= 0) {
                return;
            }
            // Set up the paths to have orthonormalized forwards, rights, and ups. The forward axis follows the curve, the other two are arbitrary.
            // They're used in circle packing offsets for multiple penetrations.
            for(int i=0;i<path.Count-1;i++) {
                PenetrablePath current = path[i];
                PenetrablePath next = path[i+1];
                current.forward = current.attachedTransform.InverseTransformDirection(next.position-current.position);
                Vector3 mostPerpendicular = current.attachedTransform.right;
                float minDot = Vector3.Dot(current.forward, current.attachedTransform.right);
                if (Vector3.Dot(current.forward, current.attachedTransform.up) < minDot) {
                    minDot = Vector3.Dot(current.forward, current.attachedTransform.up);
                    mostPerpendicular = current.attachedTransform.up;
                }
                if (Vector3.Dot(current.forward, current.attachedTransform.forward) < minDot) {
                    minDot = Vector3.Dot(current.forward, current.attachedTransform.forward);
                    mostPerpendicular = current.attachedTransform.forward;
                }
                current.right = mostPerpendicular;
                Vector3.OrthoNormalize(ref current.forward, ref current.right, ref current.up);
            }

            PenetrablePath currentn = path[path.Count-1];
            currentn.forward = (currentn.position - path[path.Count-2].position).normalized;
            Vector3 mostPerpendicularn = currentn.attachedTransform.right;
            float minDotn = Vector3.Dot(currentn.forward, currentn.attachedTransform.right);
            if (Vector3.Dot(currentn.forward, currentn.attachedTransform.up) < minDotn) {
                minDotn = Vector3.Dot(currentn.forward, currentn.attachedTransform.up);
                mostPerpendicularn = currentn.attachedTransform.up;
            }
            if (Vector3.Dot(currentn.forward, currentn.attachedTransform.forward) < minDotn) {
                minDotn = Vector3.Dot(currentn.forward, currentn.attachedTransform.forward);
                mostPerpendicularn = currentn.attachedTransform.forward;
            }
            currentn.right = mostPerpendicularn;
            Vector3.OrthoNormalize(ref currentn.forward, ref currentn.right, ref currentn.up);

            // Cache the blendshape IDs, so we don't have to do a lookup constantly.
            foreach(PenetrableShape shape in shapes) {
                foreach (SkinnedMeshRenderer renderer in holeMeshes) {
                    shape.expandBlendshape[renderer.sharedMesh] = renderer.sharedMesh.GetBlendShapeIndex(shape.expandBlendshapeName);
                    shape.pushBlendshape[renderer.sharedMesh] = renderer.sharedMesh.GetBlendShapeIndex(shape.pushBlendshapeName);
                    shape.pullBlendshape[renderer.sharedMesh] = renderer.sharedMesh.GetBlendShapeIndex(shape.pullBlendshapeName);
                }
            }
        }
        public void OnValidate() {
            if (path != null && path.Count >= 4) {
                for(int i=0;i<4;i++) {
                    if (path[i].attachedTransform == null) {
                        return;
                    }
                }
            }
            foreach(PenetrableShape shape in shapes) {
                shape.alongPathAmount01 = Mathf.Clamp01(shape.alongPathAmount01);
                shape.holeDiameter = Mathf.Max(shape.holeDiameter, 0f);
            }
        }
        public Vector3 GetTangent(float pointAlongPath01, bool reverse) {
            // Simply gets the tangent along the path. Values outside 01 are clamped since the tangent should extend straight outward infinitely at the ends of the path.
            pointAlongPath01 = Mathf.Clamp01(pointAlongPath01);
            if (reverse) {
                return Bezier.BezierSlope(path[3].position, path[2].position, path[1].position, path[0].position, pointAlongPath01);
            } else {
                return Bezier.BezierSlope(path[0].position, path[1].position, path[2].position, path[3].position, pointAlongPath01);
            }
        }
        // Gets the 3d position of the path in world space. From 01 orifice space. Values outside 01 are either clamped or extend infinitely straight out at the ends of the path.
        public Vector3 GetPoint(float pointAlongPath01, bool reverse) {
            Vector3 position = Vector3.zero;
            if ((pointAlongPath01 >= 0f && pointAlongPath01 < 1f)) {
                position = reverse?Bezier.BezierPoint(path[3].position, path[2].position, path[1].position, path[0].position, pointAlongPath01) : Bezier.BezierPoint(path[0].position, path[1].position, path[2].position, path[3].position, pointAlongPath01);
            } else if ( pointAlongPath01 < 0f ) {
                position = (reverse?path[3].position:path[0].position) - GetTangent(0f, reverse).normalized*(-pointAlongPath01*orificeLength);
            } else if (pointAlongPath01 > 1f) {
                if (allowedPenetrationDepth01 == 1f) {
                    position = (reverse?path[0].position:path[3].position) + GetTangent(1f, reverse).normalized*((pointAlongPath01-1f)*orificeLength);
                } else {
                    position = (reverse?path[0].position:path[3].position);
                }
            }
            return position;
        }
        public void GetOrtho(float pointAlongPath01, bool reverse, Vector3 dickForward, Vector3 dickUp, ref Vector3 forward, ref Vector3 up, ref Vector3 right) {
            Vector3 tangent = GetTangent(pointAlongPath01, reverse);
            float bezierUpness = Vector3.Dot( tangent, dickUp);
            Vector3 bezierUp = Vector3.Lerp( dickUp , -dickForward , Mathf.Clamp01( bezierUpness ));
            float bezierDownness = Vector3.Dot( tangent , -dickUp );
            forward = tangent;
            right = Vector3.Normalize( Vector3.Lerp( bezierUp , dickForward, Mathf.Clamp01( bezierDownness )) );
            up = Vector3.Cross(forward, right);
            Vector3.OrthoNormalize(ref forward, ref right, ref up);
        }
        void Update() {
            if (path.Count < 4) {
                return;
            }
            // Automatically remove penetrators who have changed targets.
            for(int i=0;i<penetrators.Count;i++) {
                if (penetrators[i].holeTarget != this) {
                    RemovePenetrator(penetrators[i]);
                }
            }
            Vector3 p0 = path[0].position;
            Vector3 p1 = path[1].position;
            Vector3 p2 = path[2].position;
            Vector3 p3 = path[3].position;
            colliderEntrance.center = colliderEntrance.transform.InverseTransformPoint(p0);
            if (colliderExit != null) {
                colliderExit.center = colliderExit.transform.InverseTransformPoint(p3);
            }
            // Reset all girths to zero, and make sure they're the right size;
            foreach (var shape in shapes) {
                for(int i=0;i<shape.girths.Count;i++) {
                    shape.girths[i] = 0f;
                }
            }
            // Get girths at entrance and exit (for circlePacking)
            foreach(var penetrator in penetrators) {
                float rootPenetrationDepth = (penetrator.penetrationDepth01-1f) * penetrator.GetLength();
                float targetEntrancePoint = penetrator.backwards ? 1f * orificeLength : 0f * orificeLength;
                float sampleEntrancePoint = (targetEntrancePoint - rootPenetrationDepth) / penetrator.GetLength();
                float targetExitPoint = penetrator.backwards ? 0f * orificeLength : 1f * orificeLength;
                float sampleExitPoint = (targetExitPoint - rootPenetrationDepth) / penetrator.GetLength();
                penetrator.girthAtEntrance = penetrator.GetWorldGirth(1f-sampleEntrancePoint);
                penetrator.girthAtExit = penetrator.GetWorldGirth(1f-sampleExitPoint);
            }

            // We assume the whole girth of the object will be just the biggest two penetrators combined.
            // This is because circle packing reduces the radius needed, and we don't care if we're slightly too small.
            float girthEntranceTotal = 0f;
            sortedPenetrators.Sort((a,b)=>(b.girthAtEntrance.CompareTo(a.girthAtEntrance)));
            for (int i=0;i<2&&i<sortedPenetrators.Count;i++) {
                girthEntranceTotal += sortedPenetrators[i].girthAtEntrance;
            }

            float girthExitTotal = 0f;
            sortedPenetrators.Sort((a,b)=>(b.girthAtExit.CompareTo(a.girthAtExit)));
            for (int i=0;i<2&&i<sortedPenetrators.Count;i++) {
                girthExitTotal += sortedPenetrators[i].girthAtExit;
            }

            // Set their paths so they try not to collide.
            // Our circle packing algorithm is really simple, we use an angle to place the circle in a unique quadrant,
            // and just use the radius to make sure it stays "inside" the total girth.
            // We don't care too much for clipping, as long as it seems like we've "filled" the hole.
            float angleMultiplier = (2f*Mathf.PI)/((float)penetrators.Count);
            int counter = 0;
            foreach(var penetrator in penetrators) {
                float x = Mathf.Sin(angleMultiplier*(float)counter);
                float y = Mathf.Cos(angleMultiplier*(float)counter);
                if (penetrator.backwards) {
                    float entranceMovementAdjustment = (girthEntranceTotal - penetrator.girthAtEntrance)*0.5f;
                    float exitMovementAdjustment = (girthExitTotal - penetrator.girthAtExit)*0.5f;
                    Vector3 rightOffset = path[3].attachedTransform.TransformDirection(path[3].right)*x*entranceMovementAdjustment;
                    Vector3 upOffset = path[3].attachedTransform.TransformDirection(path[3].up)*y*entranceMovementAdjustment;
                    Vector3 outRightOffset = path[0].attachedTransform.TransformDirection(path[0].right)*x*exitMovementAdjustment;
                    Vector3 outUpOffset = path[0].attachedTransform.TransformDirection(path[0].up)*y*exitMovementAdjustment;
                    penetrator.SetHolePositions(p3-rightOffset-upOffset, p2, p1, p0+outRightOffset+outUpOffset);
                } else {
                    float entranceMovementAdjustment = (girthEntranceTotal - penetrator.girthAtEntrance)*0.5f;
                    float exitMovementAdjustment = (girthExitTotal - penetrator.girthAtExit)*0.5f;
                    Vector3 rightOffset = path[0].attachedTransform.TransformDirection(path[0].right)*x*entranceMovementAdjustment;
                    Vector3 upOffset = path[0].attachedTransform.TransformDirection(path[0].up)*y*entranceMovementAdjustment;
                    Vector3 outRightOffset = path[3].attachedTransform.TransformDirection(path[3].right)*x*exitMovementAdjustment;
                    Vector3 outUpOffset = path[3].attachedTransform.TransformDirection(path[3].up)*y*exitMovementAdjustment;
                    penetrator.SetHolePositions(p0+rightOffset+upOffset, p1, p2, p3-outRightOffset-outUpOffset);
                }
                counter++;
            }
            // Add up all the girths inside
            int penetratorNum = 0;
            foreach(var penetrator in penetrators) {
                if (penetrator == null) {
                    continue;
                }
                float rootPenetrationDepth = (penetrator.penetrationDepth01-1f) * penetrator.GetLength();
                foreach (var shape in shapes) {
                    if (string.IsNullOrEmpty(shape.expandBlendshapeName)) {
                        continue;
                    }
                    //float tipPenetrationDepth = penetrator.targetPenetrator.penetrationDepth01 * penetrator.targetPenetrator.GetLength();
                    float shapeTargetPoint = penetrator.backwards ? (1f-shape.alongPathAmount01) * orificeLength : shape.alongPathAmount01 * orificeLength;
                    float shapeSamplePoint = (shapeTargetPoint - rootPenetrationDepth) / penetrator.GetLength();
                    float shapeGirth = penetrator.GetWorldGirth(1f-shapeSamplePoint);
                    shape.girths[penetratorNum] = shapeGirth;
                }
                penetratorNum++;
            }
            // Finally set the blendshape, and execute triggers.
            foreach (var shape in shapes) {
                float totalGirth = 0f;
                shape.girths.Sort((a,b)=>(b.CompareTo(a)));
                for(int i=0;i<shape.girths.Count&&i<2;i++) {
                    totalGirth += shape.girths[i];
                }

                if (totalGirth > 0f && !shape.triggeredEvent) {
                    shape.triggeredEvent = true;
                    shape.OnExpand.Invoke();
                } else if (totalGirth <= 0f && shape.triggeredEvent) {
                    shape.triggeredEvent = false;
                    shape.OnEndExpand.Invoke();
                }
                if (!shape.canOverdriveShapes) {
                    totalGirth = Mathf.Min(totalGirth,shape.holeDiameter*transform.lossyScale.x);
                }
                float triggerAmount = (totalGirth / (shape.holeDiameter*transform.lossyScale.x));
                foreach(var mesh in holeMeshes) {
                    mesh.SetBlendShapeWeight(shape.expandBlendshape[mesh.sharedMesh], triggerAmount*100f);
                }
            }
        }
        public void FixedUpdate() {
            float springStrength = 500f;
            float positionForgivenessMeters = 0.1f;
            float deflectionForgivenessDegrees = 10f;
            //float deflectionSpringStrength = 1000000f;
            float overallDamping = 0.2f;
            foreach(var penetrator in penetrators) {
                if (penetrator == null || penetrator.body == null) {
                    continue;
                }
                float dickLength = penetrator.GetLength();
                float penetrationDepth = penetrator.penetrationDepth01;

                float tipTargetPoint = penetrationDepth*dickLength/orificeLength;
                float rootTargetPoint = (penetrationDepth-1f)*dickLength/orificeLength;

                float weight = 1f-Mathf.Clamp01(-penetrator.penetrationDepth01);
                if (!penetrator.body.isKinematic) {
                    penetrator.body.velocity = Vector3.zero;
                    penetrator.body.angularVelocity = Vector3.zero;
                    penetrator.body.useGravity = !penetrator.IsInside();
                    // If we're not quite "in", push ourselves to just have the tip at the entrance.
                    // But only if we're auto-penetrating, otherwise kobolds get vaccumed in
                    Vector3 tipTargetPosition = GetPoint(tipTargetPoint, penetrator.backwards);
                    if (tipTargetPoint<0f && penetrator.autoPenetrate) {
                        tipTargetPosition = GetPoint(0f,penetrator.backwards);
                    }
                    Vector3 rootTargetPosition = GetPoint(rootTargetPoint, penetrator.backwards);
                    Vector3 diff = rootTargetPosition - penetrator.GetWorldRootPosition();
                    penetrator.body.position += diff*Time.deltaTime;
                    penetrator.body.rotation = Quaternion.FromToRotation(penetrator.dickRoot.TransformDirection(penetrator.dickForward), (tipTargetPosition-rootTargetPosition).normalized)*penetrator.body.rotation;
                } else {
                    // Kill cyclical adjustments
                    /*if (penetrator.kobold != null) {
                        foreach(var dickset in kobold.activePenetrators) {
                            foreach (var holeset in penetrator.kobold.penetratables) {
                                if (dickset.dick.holeTarget == holeset.penetratable && kobold.transform.lossyScale.y > penetrator.kobold.transform.lossyScale.y) {
                                    return;
                                }
                            }
                        }
                    }*/
                    Rigidbody targetBody = GetBody(Mathf.Lerp(tipTargetPoint, rootTargetPoint, 0.5f), penetrator.backwards);
                    if (targetBody == null) {
                        return;
                    }
                    //Vector3 tipTargetPosition = GetPoint(tipTargetPoint, penetrator.backwards);
                    //Vector3 diff = penetrator.dickTip.position - tipTargetPosition;
                    //targetBody.AddForceAtPosition(diff*springStrength*weight, tipTargetPosition, ForceMode.Acceleration);
                    //targetBody.position = targetBody.position+diff;

                    //rtargetBody.AddForceAtPosition(rdiff*springStrength*weight, rootTargetPosition, ForceMode.Acceleration);

                    /*Vector3 tipTangent = GetTangent(tipTargetPoint, penetrator.backwards);
                    Vector3 rootTangent = GetTangent(rootTargetPoint, penetrator.backwards);
                    Vector3 tangent = Vector3.Lerp(tipTangent, rootTangent, 0.5f); */
                    //rtargetBody.velocity += (penetrator.body.velocity-targetBody.velocity)*overallDamping;

                    Rigidbody rtargetBody = GetBody(0, penetrator.backwards);
                    rtargetBody.velocity += (penetrator.body.velocity-rtargetBody.velocity)*overallDamping;

                    Vector3 dickForward = penetrator.dickRoot.TransformDirection(penetrator.dickForward);

                    Vector3 tangent = GetTangent(0,penetrator.backwards);
                    Vector3 cross = Vector3.Cross(-tangent, dickForward);
                    float angleDiff = Mathf.Max(Vector3.Angle(-tangent, penetrator.dickRoot.TransformDirection(penetrator.dickForward)) - deflectionForgivenessDegrees, 0f);
                    ConfigurableJoint joint = GetJoint(Mathf.Lerp(tipTargetPoint, rootTargetPoint, 0.5f), penetrator.backwards);
                    if (joint != null) {
                        //joint.targetAngularVelocity = -cross * angleDiff * deflectionSpringStrength * weight;
                        //joint.targetRotation = Quaternion.FromToRotation(-tangent, dickForward) * targetBody.rotation;
                        joint.targetRotation = Quaternion.FromToRotation(tangent,dickForward) * targetBody.rotation;
                        //joint.SetTargetRotation(Quaternion.FromToRotation(-tangent, dickForward) * targetBody.rotation, GetJointStartRotation(Mathf.Lerp(tipTargetPoint, rootTargetPoint, 0.5f), penetrator.backwards));
                    }
                    //targetBody.angularVelocity = -cross * angleDiff * deflectionSpringStrength * weight;
                    //targetBody.angularVelocity -= targetBody.angularVelocity*overallDamping;
                    //targetBody.AddTorque(-cross * angleDiff * deflectionSpringStrength * weight, ForceMode.Acceleration);

                    Vector3 rootTargetPosition = GetPoint(0, penetrator.backwards);
                    Vector3 wantedPosition = penetrator.GetWorldRootPosition()+dickForward*(1f-penetrator.penetrationDepth01)*dickLength;
                    Vector3 rdiff = (wantedPosition - rootTargetPosition);
                    Vector3 rdir = rdiff.normalized;
                    float mag = Mathf.Max(rdiff.magnitude-positionForgivenessMeters,0f);
                    rtargetBody.AddForceAtPosition(rdir*mag*springStrength*weight, rootTargetPosition, ForceMode.Acceleration);
                }

                //penetrator.body.AddForceAtPosition((rdir*rdist*weight*springStrength), penetrator.dickRoot.position, ForceMode.Acceleration);
                //GetBody(rootTargetPoint, penetrator.backwards).AddForceAtPosition((-rdir*rdist*weight*springStrength), rootTargetPosition, ForceMode.Acceleration);

                // This was meant to rotate a penis attached to a ragdolled kobold into the right direction-- though it's unecessary provided that
                // we move the tip into position.
                /*if (!penetrator.isDildo && (penetrator.kobold != false && penetrator.body != penetrator.kobold.body)) {
                    Vector3 tipTangent = GetTangent(tipTargetPoint, penetrator.backwards);
                    Vector3 rootTangent = GetTangent(rootTargetPoint, penetrator.backwards);
                    Vector3 tangent = Vector3.Lerp(tipTangent, rootTangent, 0.5f);
                    Vector3 cross = Vector3.Cross(-tangent, penetrator.dickRoot.TransformDirection(penetrator.dickForward));
                    float angleDiff = Mathf.Max(Vector3.Angle(-tangent, penetrator.dickRoot.TransformDirection(penetrator.dickForward)) - deflectionForgivenessDegrees, 0f);
                    penetrator.body.angularVelocity *= 0.9f;
                    penetrator.body.AddTorque(cross * angleDiff * deflectionSpringStrength * weight, ForceMode.VelocityChange); }*/
            }
        }
    }
}