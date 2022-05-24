using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace PenetrationTech {
    public class ProceduralDeformation : MonoBehaviour {
        [SerializeField]
        private List<Penetrable> penetrableTargets;
        [SerializeField]
        SkinnedMeshRenderer[] rendererTargets;
        private List<Material> materials;
        private ComputeBuffer penetratorBuffer;
        private ComputeBuffer splineBuffer;
        private NativeArray<PenetratorData> data;
        private NativeArray<CatmullDeformer.CatmullSplineData> splineData;
        private int penetratorDataArrayID;
        private int splineDataArrayID;
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
            penetratorDataArrayID = Shader.PropertyToID("_PenetratorData");
            splineDataArrayID = Shader.PropertyToID("_CatmullSplines");
            foreach(SkinnedMeshRenderer target in rendererTargets) {
                Mesh newMesh = Mesh.Instantiate(target.sharedMesh);
                List<Vector3> vertices = new List<Vector3>();
                List<Vector4> uvs = new List<Vector4>();
                newMesh.GetVertices(vertices);
                newMesh.GetUVs(2, uvs);
                // If we have no uvs, the array is empty. so we correct that by adding a bunch of zeros.
                for (int i=uvs.Count;i<vertices.Count;i++) {
                    uvs.Add(Vector4.zero);
                }
                for (int i=0;i<uvs.Count;i++) {
                    for(int o=0;o<penetrableTargets.Count;o++) {
                        Vector3 worldPosition = target.rootBone.TransformPoint(newMesh.bindposes[0].MultiplyPoint(vertices[i]));
                        float nearestT = penetrableTargets[o].GetPath().GetClosestTimeFromPosition(worldPosition, 256);
                        float nearestDistance = penetrableTargets[o].GetPath().GetDistanceFromTime(nearestT);
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
                target.sharedMesh = newMesh;
                List<Material> grabMaterials = new List<Material>();
                target.GetMaterials(grabMaterials);
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
                    case 0: m.SetTexture("_DickGirthMapX", penetrator.GetGirthMap()); break;
                    case 1: m.SetTexture("_DickGirthMapY", penetrator.GetGirthMap()); break;
                    case 2: m.SetTexture("_DickGirthMapZ", penetrator.GetGirthMap()); break;
                    case 3: m.SetTexture("_DickGirthMapW", penetrator.GetGirthMap()); break;
                }
            }
        }
    }
}
