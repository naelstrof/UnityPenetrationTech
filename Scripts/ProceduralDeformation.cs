using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace PenetrationTech {
#if UNITY_EDITOR
    [CustomEditor(typeof(ProceduralDeformation))]
    public class ProceduralDeformationEditor : Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            if (GUILayout.Button("Bake All...")) {
                SerializedProperty renderTargetList = serializedObject.FindProperty("renderTargets");
                string path = EditorUtility.OpenFolderPanel("Output mesh location","","");
                if (path.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase) == -1) {
                    throw new UnityException("Must save assets to the Unity project");
                }
                path = path.Substring(path.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(path)) {
                    return;
                }
                for (int j = 0; j < renderTargetList.arraySize; j++) {
                    SerializedProperty targetProp = renderTargetList.GetArrayElementAtIndex(j);
                    SerializedProperty skinnedMeshRendererProp = targetProp.FindPropertyRelative("renderer");
                    SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRendererProp.objectReferenceValue as SkinnedMeshRenderer;
                    if (skinnedMeshRenderer == null) {
                        continue;
                    }
                    Mesh newMesh = Mesh.Instantiate(skinnedMeshRenderer.sharedMesh);
                    List<Vector3> vertices = new List<Vector3>();
                    List<Vector4> uvs = new List<Vector4>();
                    newMesh.GetVertices(vertices);
                    newMesh.GetUVs(2, uvs);
                    // If we have no uvs, the array is empty. so we correct that by adding a bunch of zeros.
                    for (int i=uvs.Count;i<vertices.Count;i++) {
                        uvs.Add(Vector4.zero);
                    }

                    SerializedProperty penetrableTargetsProp = serializedObject.FindProperty("penetrableTargets");
                    for (int i=0;i<uvs.Count;i++) {
                        EditorUtility.DisplayProgressBar("Baking meshes...", $"Baking for mesh {newMesh.name}", (float)i / (float)uvs.Count);
                        for(int o=0;o<penetrableTargetsProp.arraySize;o++) {
                            Penetrable p = penetrableTargetsProp.GetArrayElementAtIndex(o).objectReferenceValue as Penetrable;
                            if (p == null) {
                                throw new UnityException(
                                    "Please make sure the Penetrables array doesn't have any nulls...");
                            }
                            Vector3 worldPosition = skinnedMeshRenderer.rootBone.TransformPoint(newMesh.bindposes[0].MultiplyPoint(vertices[i]));
                            CatmullSpline penPath = p.GetPathExpensive();
                            float nearestT = penPath.GetClosestTimeFromPosition(worldPosition, 256);
                            float nearestDistance = penPath.GetDistanceFromTime(nearestT);
                            //Debug.DrawLine(worldPosition, p.GetPath().GetPositionFromT(nearestT), Color.red, 10f);
                            switch(o) {
                                case 0: uvs[i] = new Vector4(nearestDistance,uvs[i].y,uvs[i].z,uvs[i].w);break;
                                case 1: uvs[i] = new Vector4(uvs[i].x,nearestDistance,uvs[i].z,uvs[i].w);break;
                                case 2: uvs[i] = new Vector4(uvs[i].x,uvs[i].y,nearestDistance,uvs[i].w);break;
                                case 3: uvs[i] = new Vector4(uvs[i].x,uvs[i].y,uvs[i].z,nearestDistance);break;
                                default: throw new UnityException("We only support up to 4 penetrables per procedural deformation...");
                            }
                        }
                    }
                    newMesh.SetUVs(2, uvs);
                    string meshPath = $"{path}/{newMesh.name}.mesh";
                    AssetDatabase.CreateAsset(newMesh, meshPath);
                    targetProp.FindPropertyRelative("bakedMesh").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                }
            }
            serializedObject.ApplyModifiedProperties();
            EditorUtility.ClearProgressBar();
        }
    }
#endif
    public class ProceduralDeformation : MonoBehaviour {
        [SerializeField]
        private List<Penetrable> penetrableTargets;
        [System.Serializable]
        public class RenderTarget {
            [SerializeField]
            public SkinnedMeshRenderer renderer;
            [SerializeField]
            public Mesh bakedMesh;
            [HideInInspector]
            public Mesh savedMesh;
        }
        [SerializeField]
        private List<RenderTarget> renderTargets;
        private List<Material> materials;
        private ComputeBuffer penetratorBuffer;
        private ComputeBuffer splineBuffer;
        private NativeArray<PenetratorData> data;
        private NativeArray<CatmullDeformer.CatmullSplineData> splineData;
        private static readonly int penetratorDataArrayID = Shader.PropertyToID("_PenetratorData");
        private static readonly int splineDataArrayID = Shader.PropertyToID("_CatmullSplines");
        private static readonly int dickGirthMapXID = Shader.PropertyToID("_DickGirthMapX");
        private static readonly int dickGirthMapYID = Shader.PropertyToID("_DickGirthMapY");
        private static readonly int dickGirthMapZID = Shader.PropertyToID("_DickGirthMapZ");
        private static readonly int dickGirthMapWID = Shader.PropertyToID("_DickGirthMapW");

        private struct PenetratorData {
            float blend;
            float worldLength;
            float worldDistance;
            float girthScaleFactor;
            float angle;
            public PenetratorData(float blend) {
                this.blend = worldLength = worldDistance = girthScaleFactor = angle = blend;
            }
            public PenetratorData(Penetrator penetrator, float worldDistance) {
                worldLength = penetrator.GetWorldLength();
                blend = worldDistance<worldLength ? 1f : 0f;
                this.worldDistance = worldDistance;
                girthScaleFactor = penetrator.GetGirthScaleFactor();
                angle = penetrator.GetPenetratorAngleOffset();
            }
            public static int GetSize() {
                return sizeof(float)*5;
            }
        }
        //private void Bake() { }
        void OnEnable() {
            materials = new List<Material>();
            penetratorBuffer = new ComputeBuffer(4,PenetratorData.GetSize());
            data = new NativeArray<PenetratorData>(4, Allocator.Persistent);
            for (int i=0;i<4;i++) {
                data[i] = new PenetratorData(0);
            }
            splineBuffer = new ComputeBuffer(4,CatmullDeformer.CatmullSplineData.GetSize());
            splineData = new NativeArray<CatmullDeformer.CatmullSplineData>(4, Allocator.Persistent);
            foreach(RenderTarget target in renderTargets) {
                target.savedMesh = target.renderer.sharedMesh;
                target.renderer.sharedMesh = target.bakedMesh;
                List<Material> grabMaterials = new List<Material>();
                target.renderer.GetMaterials(grabMaterials);
                materials.AddRange(grabMaterials);
            }
            foreach(Penetrable penetrable in penetrableTargets) {
                penetrable.penetrationNotify += NotifyPenetration;
            }
        }
        void OnDisable() {
            foreach(Penetrable penetrable in penetrableTargets) {
                penetrable.penetrationNotify -= NotifyPenetration;
            }
            penetratorBuffer.Dispose();
            splineBuffer.Dispose();
            splineData.Dispose();
            data.Dispose();
            foreach (RenderTarget target in renderTargets) {
                target.renderer.sharedMesh = target.savedMesh;
            }
        }
        void LateUpdate() {
            // Make sure data we're not using is zero'd so the shader doesn't freak out.
            for (int i=penetrableTargets.Count;i<4;i++) {
                data[i] = new PenetratorData(0);
            }
            penetratorBuffer.SetData(data);
            splineBuffer.SetData(splineData);
            foreach(Material m in materials) {
                m.SetBuffer(splineDataArrayID, splineBuffer);
                m.SetBuffer(penetratorDataArrayID,penetratorBuffer);
            }
        }
        private void NotifyPenetration(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenisRoot) {
            int index = penetrableTargets.IndexOf(penetrable);
            data[index] = new PenetratorData(penetrator, worldSpaceDistanceToPenisRoot);
            splineData[index] = new CatmullDeformer.CatmullSplineData(penetrator.GetPath());
            foreach(Material m in materials) {
                switch(index) {
                    case 0: m.SetTexture(dickGirthMapXID, penetrator.GetGirthMap()); break;
                    case 1: m.SetTexture(dickGirthMapYID, penetrator.GetGirthMap()); break;
                    case 2: m.SetTexture(dickGirthMapZID, penetrator.GetGirthMap()); break;
                    case 3: m.SetTexture(dickGirthMapWID, penetrator.GetGirthMap()); break;
                }
            }
        }
    }
}
