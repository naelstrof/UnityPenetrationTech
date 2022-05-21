using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PenetrationTech {
    [System.Serializable]
    public class GirthData {
        [Header("This data is generated automatically on Start()")]
        [ReadOnly][SerializeField]
        private RenderTexture texture;
        [ReadOnly][SerializeField]
        private float maxLocalLength;
        [ReadOnly][SerializeField]
        private float maxLocalGirth;
        [ReadOnly][SerializeField]
        private AnimationCurve localGirthCurve;
        private Renderer renderer;
        public float GetWorldLength() {
            float scaleFactor = 1f;
            if (renderer is SkinnedMeshRenderer) scaleFactor = (renderer as SkinnedMeshRenderer).rootBone.lossyScale.x; 
            if (renderer is MeshRenderer) scaleFactor = (renderer as MeshRenderer).transform.lossyScale.x; 
            return maxLocalLength * scaleFactor;
        }
        public float GetWorldGirth(float worldDistanceAlongDick) {
            float scaleFactor = 1f;
            if (renderer is SkinnedMeshRenderer) scaleFactor = (renderer as SkinnedMeshRenderer).rootBone.lossyScale.x; 
            if (renderer is MeshRenderer) scaleFactor = (renderer as MeshRenderer).transform.lossyScale.x; 
            return localGirthCurve.Evaluate(worldDistanceAlongDick/scaleFactor)*scaleFactor;
        }
        private void PopulateGirthCurve(RenderTexture girthMap) {
            // First we use the GPU to scrunch the 2D girthmap into a 1D one. This averages all the pixels.
            RenderTexture temp = RenderTexture.GetTemporary(32,1);
            Graphics.Blit(girthMap, temp);
            Texture2D cpuTex = new Texture2D(32,1, TextureFormat.RGB24, false);
            RenderTexture.active = temp;
            cpuTex.ReadPixels(new Rect(0,0,girthMap.width, girthMap.height), 0, 0);
            cpuTex.Apply();
            RenderTexture.ReleaseTemporary(temp);
            // Then after we got it on the CPU, we use it to generate some curves that we can visualize in the editor (and easily sample).
            localGirthCurve = new AnimationCurve();
            localGirthCurve.postWrapMode = WrapMode.ClampForever;
            localGirthCurve.preWrapMode = WrapMode.ClampForever;
            for (int i=0;i<32;i++) {
                float t = (float)i/(float)32;
                localGirthCurve.AddKey(t*maxLocalLength,cpuTex.GetPixel(i,0).r*maxLocalGirth);
            }
        }
        
        public GirthData(Renderer renderer, Transform root, Vector3 localDickRoot, Vector3 localDickForward, Vector3 localDickUp) {
            this.renderer = renderer;
            Mesh mesh;
            texture = new RenderTexture(512,512,16);
            Vector3 localDickRight = Vector3.Cross(localDickForward, localDickUp);
            Vector3 worldSpaceDickRoot = root.TransformPoint(localDickRoot);
            Vector3 localSpaceDickRoot;
            if (renderer is SkinnedMeshRenderer) {
                mesh = (renderer as SkinnedMeshRenderer).sharedMesh;
                localSpaceDickRoot = (renderer as SkinnedMeshRenderer).rootBone.InverseTransformPoint(worldSpaceDickRoot);
            } else if (renderer is MeshRenderer) {
                mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                localSpaceDickRoot = renderer.transform.InverseTransformPoint(worldSpaceDickRoot);
            } else {
                throw new UnityException("Girth data can only be generated on SkinnedMeshRenderers and MeshRenderers.");
            }
            Material mat = new Material(Shader.Find("PenetrationTech/GirthUnwrapRaw"));
            mat.SetVector("_DickOrigin", localSpaceDickRoot);
            mat.SetVector("_DickForward", localDickForward);
            mat.SetVector("_DickRight", localDickRight);

            // Do a quick pass to figure out how girthy and lengthy we are
            maxLocalGirth = 0f;
            maxLocalLength = 0f;
            foreach(Vector3 vertexPosition in mesh.vertices) {
                //Vector3 dickSpacePosition = changeOfBasis.MultiplyPoint(vertexPosition);
                float length = Vector3.Dot(localDickForward, vertexPosition-localSpaceDickRoot);
                float girth = Vector3.Distance(vertexPosition,(localSpaceDickRoot+localDickForward*length));
                maxLocalGirth = Mathf.Max(girth, maxLocalGirth);
                maxLocalLength = Mathf.Max(length, maxLocalLength);
            }
            mat.SetFloat("_MaxLength", maxLocalLength);
            mat.SetFloat("_MaxGirth", maxLocalGirth);
            mat.SetFloat("_AngleOffset", Mathf.PI/2f);

            // Then use the GPU to rasterize
            CommandBuffer buffer = new CommandBuffer();
            buffer.SetRenderTarget(texture);
            buffer.ClearRenderTarget(true, true, Color.clear, 0f);
            buffer.DrawMesh(mesh, Matrix4x4.identity, mat, 0, 0);
            Graphics.ExecuteCommandBuffer(buffer);

            // We need to do one more blits to ensure the full image gets filled.
            CommandBuffer additiveBuffer = new CommandBuffer();
            additiveBuffer.SetRenderTarget(texture);
            additiveBuffer.DrawMesh(mesh, Matrix4x4.identity, mat, 0, 0);
            mat.SetFloat("_AngleOffset", -Mathf.PI/2f);
            Graphics.ExecuteCommandBuffer(additiveBuffer);
            PopulateGirthCurve(texture);
        }
        ~GirthData() {
            texture.Release();
        }
        public RenderTexture GetGirthMap() => texture;
    }
}