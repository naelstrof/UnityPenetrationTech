using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                List<UnityEngine.Object> renderersToUndo = new List<UnityEngine.Object>();
                for (int j = 0; j < renderTargetList.arraySize; j++) {
                    SerializedProperty skinnedMeshRendererProp = renderTargetList.GetArrayElementAtIndex(j);
                    SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRendererProp.objectReferenceValue as SkinnedMeshRenderer;
                    if (skinnedMeshRenderer == null) {
                        continue;
                    }

                    if (skinnedMeshRenderer.sharedMesh.name.Contains("Clone")) {
                        throw new UnityException("Possibly trying to bake data to a baked mesh. Reset your mesh if you want to bake from original data.");
                    }

                    renderersToUndo.Add(skinnedMeshRenderer);
                }
                string path = EditorUtility.OpenFolderPanel("Output mesh location","Assets","");
                // Catch user pressing the cancel button or closing the window
                if (string.IsNullOrEmpty(path)) {
                    return;
                }
                int startIndex = path.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
                if (startIndex == -1) {
                    throw new UnityException("Must save assets to the Unity project");
                }
                path = path.Substring(startIndex);
                
                Undo.RecordObjects(renderersToUndo.ToArray(), "Swapped existing meshes with baked mesh.");
                
                for (int j = 0; j < renderTargetList.arraySize; j++) {
                    SerializedProperty skinnedMeshRendererProp = renderTargetList.GetArrayElementAtIndex(j);
                    SkinnedMeshRenderer skinnedMeshRenderer = skinnedMeshRendererProp.objectReferenceValue as SkinnedMeshRenderer;
                    if (skinnedMeshRenderer == null) {
                        continue;
                    }

                    Mesh newMesh = Mesh.Instantiate(skinnedMeshRenderer.sharedMesh);
                    // Generate second mesh to insure properties are in bakespace before bake
                    Mesh bakeMesh = new Mesh();
                    skinnedMeshRenderer.BakeMesh(bakeMesh);
                    
                    List<Vector3> vertices = new List<Vector3>();
                    List<Vector4> uvs = new List<Vector4>();
                    bakeMesh.GetVertices(vertices);
                    bakeMesh.GetUVs(2, uvs);
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
                            Vector3 worldPosition = skinnedMeshRenderer.localToWorldMatrix.MultiplyPoint(vertices[i]);
                            CatmullSpline penPath = p.GetPath();
                            float nearestT = penPath.GetClosestTimeFromPosition(worldPosition, 256);
                            // Debug.DrawLine(worldPosition, penPath.GetPositionFromT(nearestT), Color.red, 10f);
                            switch(o) {
                                case 0: uvs[i] = new Vector4(nearestT,uvs[i].y,uvs[i].z,uvs[i].w);break;
                                case 1: uvs[i] = new Vector4(uvs[i].x,nearestT,uvs[i].z,uvs[i].w);break;
                                case 2: uvs[i] = new Vector4(uvs[i].x,uvs[i].y,nearestT,uvs[i].w);break;
                                case 3: uvs[i] = new Vector4(uvs[i].x,uvs[i].y,uvs[i].z,nearestT);break;
                                default: throw new UnityException("We only support up to 4 penetrables per procedural deformation...");
                            }
                        }
                    }
                    newMesh.SetUVs(2, uvs);
                    string meshPath = $"{path}/{newMesh.name}.mesh";
                    AssetDatabase.CreateAsset(newMesh, meshPath);
                    skinnedMeshRenderer.sharedMesh = newMesh;
                }
                serializedObject.ApplyModifiedProperties();
                EditorUtility.ClearProgressBar();
            }
        }
    }
#endif
    [ExecuteAlways]
    public class ProceduralDeformation : MonoBehaviour {
        [SerializeField]
        private List<Penetrable> penetrableTargets;
        [SerializeField]
        private List<Renderer> renderTargets;

        [Tooltip("When enabled, only deforms the differences between the approximated girth curve and the real shape. Enable this if you've already authored the main deformations with blendshape listeners. Does not affect mesh baking."), SerializeField] private bool detailOnly;
        private ComputeBuffer penetratorBuffer;
        private ComputeBuffer splineBuffer;
        private NativeArray<PenetratorData> data;
        private NativeArray<CatmullDeformer.CatmullSplineData> splineData;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int penetratorDataArrayID = Shader.PropertyToID("_PenetratorData");
        private static readonly int splineDataArrayID = Shader.PropertyToID("_CatmullSplines");
        private static readonly int dickGirthMapXID = Shader.PropertyToID("_DickGirthMapX");
        private static readonly int dickGirthMapYID = Shader.PropertyToID("_DickGirthMapY");
        private static readonly int dickGirthMapZID = Shader.PropertyToID("_DickGirthMapZ");
        private static readonly int dickGirthMapWID = Shader.PropertyToID("_DickGirthMapW");

        private unsafe struct PenetratorData {
            float blend;
            float worldDickLength;
            float worldDistance;
            float girthScaleFactor;
            float angle;
            fixed float initialRight[3];
            fixed float initialUp[3];
            int holeSubCurveCount;
            public PenetratorData(float blend) {
                this.blend = worldDickLength = worldDistance = girthScaleFactor = angle = blend;
                holeSubCurveCount = 0;
                initialRight[0] = 0;
                initialRight[1] = 0;
                initialRight[2] = 0;
                initialUp[0] = 0;
                initialUp[1] = 0;
                initialUp[2] = 0;
            }
            public PenetratorData(Penetrable penetrable, Penetrator penetrator, float worldDistance) {
                worldDickLength = penetrator.GetWorldLength();
                blend = worldDistance > worldDickLength ? 0f : 1f;
                this.worldDistance = worldDistance;
                girthScaleFactor = penetrator.GetGirthScaleFactor();
                angle = penetrator.GetPenetratorAngleOffset();
                holeSubCurveCount = penetrable.GetPath().GetWeights().Count;
                Vector3 iRight = penetrator.GetPath().GetBinormalFromT(0f);
                Vector3 iForward = penetrator.GetPath().GetVelocityFromT(0f).normalized;
                Vector3 iUp = Vector3.Cross(iForward, iRight).normalized;
                initialRight[0] = iRight.x;
                initialRight[1] = iRight.y;
                initialRight[2] = iRight.z;
                initialUp[0] = iUp.x;
                initialUp[1] = iUp.y;
                initialUp[2] = iUp.z;
            }
            public static int GetSize() {
                return sizeof(float)*11+sizeof(int)*1;
            }
        }

        public void AddTargetRenderer(Renderer targetRenderer) {
            if (!renderTargets.Contains(targetRenderer)) {
                renderTargets.Add(targetRenderer);
            }
        }

        public void RemoveTargetRenderer(Renderer targetRenderer) {
            renderTargets.Remove(targetRenderer);
        }

        void OnEnable() {
            penetratorBuffer = new ComputeBuffer(4,PenetratorData.GetSize());
            data = new NativeArray<PenetratorData>(4, Allocator.Persistent);
            for (int i=0;i<4;i++) {
                data[i] = new PenetratorData(0);
            }
            splineBuffer = new ComputeBuffer(4,CatmullDeformer.CatmullSplineData.GetSize());
            splineData = new NativeArray<CatmullDeformer.CatmullSplineData>(4, Allocator.Persistent);

            propertyBlock = new MaterialPropertyBlock();

            foreach (Penetrable penetrable in penetrableTargets) {
                if (penetrable == null) {
                    continue;
                }
                penetrable.penetrationNotify -= NotifyPenetration;
                penetrable.penetrationNotify += NotifyPenetration;
            }
            
            if (!Application.isPlaying) {
                return;
            }

            foreach (Renderer ren in renderTargets) {
                if (!(ren is SkinnedMeshRenderer skinnedMeshRenderer)) continue;
                if (skinnedMeshRenderer == null) {
                    continue;
                }
                foreach (Material sharedMat in skinnedMeshRenderer.materials) {
                    sharedMat.EnableKeyword("_PENETRATION_DEFORMATION_ON");
                    if (detailOnly) {
                        sharedMat.EnableKeyword("_PENETRATION_DEFORMATION_DETAIL_ON");
                    } else {
                        sharedMat.DisableKeyword("_PENETRATION_DEFORMATION_DETAIL_ON");
                    }
                }
            }
        }

        void OnDisable() {
            penetratorBuffer.Dispose();
            splineBuffer.Dispose();
            splineData.Dispose();
            data.Dispose();
            foreach (Penetrable penetrable in penetrableTargets) {
                if (penetrable == null) {
                    continue;
                }
                penetrable.penetrationNotify -= NotifyPenetration;
            }

            foreach (Renderer ren in renderTargets) {
                if (!(ren is SkinnedMeshRenderer skinnedMeshRenderer)) continue;
                if (skinnedMeshRenderer == null) {
                    continue;
                }

                if (Application.isPlaying) {
                    foreach (Material sharedMat in skinnedMeshRenderer.materials) {
                        sharedMat.DisableKeyword("_PENETRATION_DEFORMATION_ON");
                    }
                }
            }
        }

        void LateUpdate() {
            // Make sure data we're not using is zero'd so the shader doesn't freak out.
            for (int i=penetrableTargets.Count;i<4;i++) {
                data[i] = new PenetratorData(0);
            }
            penetratorBuffer.SetData(data);
            splineBuffer.SetData(splineData);
            foreach(Renderer target in renderTargets) {
                if (target == null) {
                    continue;
                }
                target.GetPropertyBlock(propertyBlock);
                propertyBlock.SetBuffer(splineDataArrayID, splineBuffer);
                propertyBlock.SetBuffer(penetratorDataArrayID,penetratorBuffer);
                target.SetPropertyBlock(propertyBlock);
            }
        }
        private void NotifyPenetration(Penetrable penetrable, Penetrator penetrator, float worldSpaceDistanceToPenisRoot, Penetrable.SetClipDistanceAction clipAction) {
            int index = penetrableTargets.IndexOf(penetrable);
            data[index] = new PenetratorData(penetrable, penetrator, worldSpaceDistanceToPenisRoot);
            splineData[index] = new CatmullDeformer.CatmullSplineData(penetrable.GetPath());
            if (penetrator.GetWorldLength() > worldSpaceDistanceToPenisRoot) {
                foreach (Renderer target in renderTargets) {
                    target.GetPropertyBlock(propertyBlock);
                    Texture targetTexture = detailOnly ? penetrator.GetDetailMap() : penetrator.GetGirthMap();
                    switch (index) {
                        case 0: propertyBlock.SetTexture(dickGirthMapXID, targetTexture); break;
                        case 1: propertyBlock.SetTexture(dickGirthMapYID, targetTexture); break;
                        case 2: propertyBlock.SetTexture(dickGirthMapZID, targetTexture); break;
                        case 3: propertyBlock.SetTexture(dickGirthMapWID, targetTexture); break;
                    }

                    target.SetPropertyBlock(propertyBlock);
                }
            } else { // We gotta clear references to the render textures, just in case the penetrator was removed, this way the garbage collector can clean them up.
                foreach (Renderer target in renderTargets) {
                    target.GetPropertyBlock(propertyBlock);
                    Texture targetTexture = detailOnly ? Texture2D.grayTexture : Texture2D.blackTexture;
                    switch (index) {
                        case 0: propertyBlock.SetTexture(dickGirthMapXID, targetTexture); break;
                        case 1: propertyBlock.SetTexture(dickGirthMapYID, targetTexture); break;
                        case 2: propertyBlock.SetTexture(dickGirthMapZID, targetTexture); break;
                        case 3: propertyBlock.SetTexture(dickGirthMapWID, targetTexture); break;
                    }
                    target.SetPropertyBlock(propertyBlock);
                }
            }
        }

        private void OnValidate() {
            penetrableTargets ??= new List<Penetrable>();
            renderTargets ??= new List<Renderer>();
            foreach (Penetrable penetrable in penetrableTargets) {
                if (penetrable == null) {
                    continue;
                }
                penetrable.penetrationNotify -= NotifyPenetration;
                penetrable.penetrationNotify += NotifyPenetration;
            }

            foreach (Renderer ren in renderTargets) {
                if (!(ren is SkinnedMeshRenderer skinnedMeshRenderer)) continue;
                if (skinnedMeshRenderer == null) {
                    continue;
                }
                foreach (Material sharedMat in skinnedMeshRenderer.sharedMaterials) {
                    sharedMat.EnableKeyword("_PENETRATION_DEFORMATION_ON");
                    if (detailOnly) {
                        sharedMat.EnableKeyword("_PENETRATION_DEFORMATION_DETAIL_ON");
                    } else {
                        sharedMat.DisableKeyword("_PENETRATION_DEFORMATION_DETAIL_ON");
                    }
                }
            }
        }
    }
}
