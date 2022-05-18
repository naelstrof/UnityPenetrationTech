using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {

    public class Penetrable : CatmullDebugger {
        [SerializeField]
        private List<Renderer> targetRenderers;
        private List<Material> targetMaterials;
        private int weightArrayID;
        private int distanceLUTID;
        private int arcLengthID;
        private int pointCountID;
        private int binormalLUTID;
        private List<float> packedVectors;
        private List<float> packedLUT;
        private List<float> packedBinormalLUT;
        protected override void Start() {
            base.Start();
            packedVectors = new List<float>();
            packedLUT = new List<float>();
            targetMaterials = new List<Material>();
            packedBinormalLUT = new List<float>();
            List<Material> tempMaterials = new List<Material>();
            foreach(Renderer renderer in targetRenderers) {
                renderer.GetMaterials(tempMaterials);
                targetMaterials.AddRange(tempMaterials);
            }
            weightArrayID = Shader.PropertyToID("_WeightArray");
            distanceLUTID = Shader.PropertyToID("_DistanceLUT");
            arcLengthID = Shader.PropertyToID("_ArcLength");
            pointCountID = Shader.PropertyToID("_PointCount");
            binormalLUTID = Shader.PropertyToID("_BinormalLUT");
        }
        void Update() {
            List<Vector3> weights = path.GetWeights();
            packedVectors.Clear();
            foreach(Vector3 weight in weights) {
                packedVectors.Add(weight.x);
                packedVectors.Add(weight.y);
                packedVectors.Add(weight.z);
            }
            List<float> LUT = path.GetDistanceLUT();
            packedLUT.Clear();
            foreach(float dist in LUT) {
                packedLUT.Add(dist);
            }
            List<Vector3> binormals = path.GetBinormalLUT();
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
                material.SetInt(pointCountID, points.Length);
            }
        }
    }

}