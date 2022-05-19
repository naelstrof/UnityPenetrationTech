using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {

    public class CatmullDeformer : CatmullDisplay {
        [SerializeField]
        private List<Renderer> targetRenderers;
        private HashSet<Material> targetMaterials;
        private int weightArrayID;
        private int distanceLUTID;
        private int arcLengthID;
        private int pointCountID;
        private int binormalLUTID;
        private List<float> packedVectors;
        private List<float> packedLUT;
        private List<float> packedBinormalLUT;
        public void AddTargetRenderer(Renderer renderer) {
            List<Material> tempMaterials = new List<Material>();
            renderer.GetMaterials(tempMaterials);
            foreach(Material m in tempMaterials) {
                targetMaterials.Add(m);
            }
        }
        public void RemoveTargetRenderer(Renderer renderer) {
            List<Material> tempMaterials = new List<Material>();
            renderer.GetMaterials(tempMaterials);
            foreach(Material m in tempMaterials) {
                targetMaterials.Remove(m);
            }
        }
        protected virtual void Start() {
            packedVectors = new List<float>();
            packedLUT = new List<float>();
            targetMaterials = new HashSet<Material>();
            packedBinormalLUT = new List<float>();
            List<Material> tempMaterials = new List<Material>();
            foreach(Renderer renderer in targetRenderers) {
                renderer.GetMaterials(tempMaterials);
                foreach(Material m in tempMaterials) {
                    targetMaterials.Add(m);
                }
            }
            weightArrayID = Shader.PropertyToID("_WeightArray");
            distanceLUTID = Shader.PropertyToID("_DistanceLUT");
            arcLengthID = Shader.PropertyToID("_ArcLength");
            pointCountID = Shader.PropertyToID("_PointCount");
            binormalLUTID = Shader.PropertyToID("_BinormalLUT");
        }
        protected virtual void Update() {
            IList<Vector3> weights = path.GetWeights();
            packedVectors.Clear();
            foreach(Vector3 weight in weights) {
                packedVectors.Add(weight.x);
                packedVectors.Add(weight.y);
                packedVectors.Add(weight.z);
            }
            IList<float> LUT = path.GetDistanceLUT();
            packedLUT.Clear();
            foreach(float dist in LUT) {
                packedLUT.Add(dist);
            }
            IList<Vector3> binormals = path.GetBinormalLUT();
            packedBinormalLUT.Clear();
            foreach(Vector3 binormal in binormals) {
                packedBinormalLUT.Add(binormal.x);
                packedBinormalLUT.Add(binormal.y);
                packedBinormalLUT.Add(binormal.z);
            }

            foreach(Material material in targetMaterials) {
                material.SetFloatArray(weightArrayID, packedVectors);
                material.SetFloatArray(distanceLUTID, packedLUT);
                material.SetFloatArray(binormalLUTID, packedBinormalLUT);
                material.SetFloat(arcLengthID, path.arcLength);
                material.SetInt(pointCountID, path.GetWeights().Count/4);
            }
        }
    }

}