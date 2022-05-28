using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace PenetrationTech {

    public class CatmullDeformer : CatmullDisplay {
        [SerializeField]
        protected Vector3 localRootUp = Vector3.forward;
        [SerializeField]
        protected Vector3 localRootForward = -Vector3.up;
        [SerializeField]
        protected Vector3 localRootRight = Vector3.right;
        [SerializeField]
        protected Transform rootBone;
        [SerializeField]
        protected Transform tipTarget;
        [SerializeField]
        private List<RendererSubMeshMask> targetRenderers;
        private static readonly int catmullSplinesID = Shader.PropertyToID("_CatmullSplines");
        private static readonly int dickForwardID = Shader.PropertyToID("_DickForwardWorld");
        private static readonly int dickRightID = Shader.PropertyToID("_DickRightWorld");
        private static readonly int dickUpID = Shader.PropertyToID("_DickUpWorld");
        private static readonly int dickRootID = Shader.PropertyToID("_DickRootWorld");
        private static readonly int curveBlendID = Shader.PropertyToID("_CurveBlend");
        private ComputeBuffer catmullBuffer;
        private NativeArray<CatmullSplineData> data;
        private MaterialPropertyBlock propertyBlock;

        protected List<RendererSubMeshMask> GetTargetRenderers() {
            if (targetRenderers == null) {
                targetRenderers = new List<RendererSubMeshMask>();
            }
            return targetRenderers;
        }

        //TODO: Currently this is only used to send CatmullSplines to the GPU. It's used in other places (currently the ProceduralDeformation class), and should be considered for refactoring.
        public unsafe struct CatmullSplineData {
            private const int subSplineCount = 6;
            private const int binormalCount = 16;
            private const int distanceCount = 32;
            int pointCount;
            float arcLength;
            fixed float weights[subSplineCount*4*3];
            fixed float distanceLUT[distanceCount];
            fixed float binormalLUT[binormalCount*3];
            public CatmullSplineData(CatmullSpline spline) {
                pointCount = (spline.GetWeights().Count/4)+1;
                arcLength = spline.arcLength;
                for(int i=0;i<subSplineCount*4&&i<spline.GetWeights().Count;i++) {
                    Vector3 weight = spline.GetWeights()[i];
                    weights[i*3] = weight.x;
                    weights[i*3+1] = weight.y;
                    weights[i*3+2] = weight.z;
                }
                UnityEngine.Assertions.Assert.AreEqual(spline.GetDistanceLUT().Count, distanceCount);
                for(int i=0;i<distanceCount;i++) {
                    distanceLUT[i] = spline.GetDistanceLUT()[i];
                }
                UnityEngine.Assertions.Assert.AreEqual(spline.GetBinormalLUT().Count, binormalCount);
                for(int i=0;i<binormalCount;i++) {
                    Vector3 binormal = spline.GetBinormalLUT()[i];
                    binormalLUT[i*3] = binormal.x;
                    binormalLUT[i*3+1] = binormal.y;
                    binormalLUT[i*3+2] = binormal.z;
                }
            }
            public static int GetSize() {
                return sizeof(float)*(subSplineCount*3*4+1+binormalCount*3+distanceCount) + sizeof(int);
            }
        }
        protected override void OnEnable() {
            base.OnEnable();
            catmullBuffer = new ComputeBuffer(1, CatmullSplineData.GetSize());
            data = new NativeArray<CatmullSplineData>(1, Allocator.Persistent);
            List<Material> tempMaterials = new List<Material>();
            propertyBlock = new MaterialPropertyBlock();
        }
        protected virtual void OnDisable() {
            catmullBuffer.Release();
            data.Dispose();
            propertyBlock = null;
        }
        protected virtual void LateUpdate() {
            data[0] = new CatmullSplineData(path);
            catmullBuffer.SetData(data, 0, 0, 1);
            foreach(RendererSubMeshMask rsm in targetRenderers) {
                rsm.renderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(curveBlendID, 1f);
                propertyBlock.SetVector(dickForwardID, rootBone.TransformDirection(localRootForward));
                propertyBlock.SetVector(dickRightID, rootBone.TransformDirection(localRootRight));
                propertyBlock.SetVector(dickUpID, rootBone.TransformDirection(localRootUp));
                propertyBlock.SetVector(dickRootID, rootBone.position);
                propertyBlock.SetBuffer(catmullSplinesID, catmullBuffer);
                rsm.renderer.SetPropertyBlock(propertyBlock);
            }
        }
    }

}