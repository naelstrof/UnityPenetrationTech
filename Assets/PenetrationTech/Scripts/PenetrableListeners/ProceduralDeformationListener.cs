using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace PenetrationTech {
    [System.Serializable]
    [PenetrableListener(typeof(ProceduralDeformationListener), "Procedural Deformation Listener")]
    public class ProceduralDeformationListener : PenetrableListener {
        private enum TargetDataLocation {
            x, y, z, w,
        }
        [SerializeField]
        TargetDataLocation dataLocation;
        [SerializeField]
        SkinnedMeshRenderer[] targets;
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
        public override void OnEnable(Penetrable p) {
            base.OnEnable(p);
            materials = new List<Material>();
            penetratorBuffer = new ComputeBuffer(4,PenetratorData.GetSize());
            data = new NativeArray<PenetratorData>(4, Allocator.Persistent);
            splineBuffer = new ComputeBuffer(4,CatmullDeformer.CatmullSplineData.GetSize());
            splineData = new NativeArray<CatmullDeformer.CatmullSplineData>(4, Allocator.Persistent);
            penetratorDataArrayID = Shader.PropertyToID("_PenetratorData");
            splineDataArrayID = Shader.PropertyToID("_CatmullSplines");
            foreach(SkinnedMeshRenderer target in targets) {
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
                    Vector3 worldPosition = target.rootBone.TransformPoint(newMesh.bindposes[0].MultiplyPoint(vertices[i]));
                    float nearestDistance = p.GetPath().GetClosestDistanceFromPosition(worldPosition, 64);
                    nearestDistance = Mathf.Clamp(nearestDistance, 0, p.GetPath().arcLength);
                    switch(dataLocation) {
                        case TargetDataLocation.x: uvs[i] = new Vector4(nearestDistance,uvs[i].y,uvs[i].z,uvs[i].w);break;
                        case TargetDataLocation.y: uvs[i] = new Vector4(uvs[i].x,nearestDistance,uvs[i].z,uvs[i].w);break;
                        case TargetDataLocation.z: uvs[i] = new Vector4(uvs[i].x,uvs[i].y,nearestDistance,uvs[i].w);break;
                        case TargetDataLocation.w: uvs[i] = new Vector4(uvs[i].x,uvs[i].y,uvs[i].z,nearestDistance);break;
                        default: throw new UnityException("TargetDataLocation enum " + dataLocation + " doesn't exist...");
                    }
                }
                newMesh.SetUVs(2, uvs);
                target.sharedMesh = newMesh;
                List<Material> grabMaterials = new List<Material>();
                target.GetMaterials(grabMaterials);
                materials.AddRange(grabMaterials);
            }
        }
        public override void OnDisable() {
            base.OnDisable();
            penetratorBuffer.Dispose();
            splineBuffer.Dispose();
            splineData.Dispose();
            data.Dispose();
        }
        public override void NotifyPenetration(Penetrator penetrator, float worldSpaceDistanceToPenisRoot) {
            data[0] = new PenetratorData(penetrator, worldSpaceDistanceToPenisRoot);
            data[1] = new PenetratorData(0f);
            data[2] = new PenetratorData(0f);
            data[3] = new PenetratorData(0f);
            penetratorBuffer.SetData(data);
            splineData[0] = new CatmullDeformer.CatmullSplineData(penetrator.GetPath());
            splineBuffer.SetData(splineData);
            foreach(Material m in materials) {
                m.SetTexture("_DickGirthMapX", penetrator.GetGirthMap());
                m.SetBuffer(splineDataArrayID, splineBuffer);
                m.SetBuffer(penetratorDataArrayID,penetratorBuffer);
            }
            NotifyPenetrationGDO(penetrator, worldSpaceDistanceToPenisRoot, false, true, false);
        }
    }
}
