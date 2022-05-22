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
        [SerializeField]
        private AnimationCurve localXOffsetCurve;
        [SerializeField]
        private AnimationCurve localYOffsetCurve;
        private Renderer renderer;
        private Vector3 localDickForward;
        private Vector3 localDickUp;
        private Vector3 localDickRight;
        private Transform meshTransform {
            get {
                // TODO: SkinnedMeshRenderer transform selection probably isn't exactly correct, if the mesh is a skinned humanoid-- we probably want the base dick bone rather than the root bone.
                // We currently don't pass that down yet though, we probably need to define our parameters better now that GirthData is being used more.
                if (renderer is SkinnedMeshRenderer) return (renderer as SkinnedMeshRenderer).rootBone; 
                if (renderer is MeshRenderer) return (renderer as MeshRenderer).transform; 
                throw new UnityException("This should never happen! GirthData needs a MeshRenderer or a SkinnedMeshRenderer.");
            }
        }
        public float GetWorldLength() {
            // This handles skewed forwards, and even non-proportional scales of the dick (making it stubbier or longer)
            Vector3 length = maxLocalLength * localDickForward;
            return meshTransform.TransformVector(length).magnitude;
        }
        // Dick space is arbitrary, "Spline space" refers to Z forward, Y Up, and X right space. 
        // This is to make it easier to place onto a spline.
        public Vector3 GetScaledSplineSpaceOffset(float worldDistanceAlongDick) {
            float localDistanceAlongDick = meshTransform.InverseTransformVector(worldDistanceAlongDick*meshTransform.TransformDirection(localDickForward)).magnitude;
            float localXOffsetSample = localXOffsetCurve.Evaluate(localDistanceAlongDick);
            float localYOffsetSample = localYOffsetCurve.Evaluate(localDistanceAlongDick);
            Vector3 localGirth = localDickUp;
            float scaleFactor = meshTransform.TransformVector(localGirth).magnitude;
            Vector3 localOffset = localDickRight*localXOffsetSample + localDickUp*localYOffsetSample;
            Matrix4x4 changeOfBasis = Matrix4x4.identity;
            changeOfBasis.SetRow(0, localDickRight);
            changeOfBasis.SetRow(1, localDickUp);
            changeOfBasis.SetRow(2, localDickForward);
            changeOfBasis[3,3] = 1f;
            return changeOfBasis.MultiplyPoint(localOffset*scaleFactor);
        }
        public float GetWorldGirth(float worldDistanceAlongDick) {
            float localDistanceAlongDick = meshTransform.InverseTransformVector(worldDistanceAlongDick*meshTransform.TransformDirection(localDickForward)).magnitude;
            // TODO: There's no real way to actually get the girth correctly, since we cannot interpret skewed scales.
            // I currently just choose a single axis, though users shouldn't skew scale on the up/right axis anyway.
            float localGirthSample = localGirthCurve.Evaluate(localDistanceAlongDick);
            Vector3 localGirth = localDickUp*localGirthSample;
            return meshTransform.TransformVector(localGirth).magnitude;
        }
        private void PopulateOffsetCurves(RenderTexture girthMap) {
            Texture2D cpuTex = new Texture2D(girthMap.width,girthMap.height, TextureFormat.RGB24, false, true);
            RenderTexture.active = girthMap;
            cpuTex.ReadPixels(new Rect(0,0,girthMap.width, girthMap.height), 0, 0);
            cpuTex.Apply();
            localXOffsetCurve = new AnimationCurve();
            localXOffsetCurve.postWrapMode = WrapMode.ClampForever;
            localXOffsetCurve.preWrapMode = WrapMode.ClampForever;
            localYOffsetCurve = new AnimationCurve();
            localYOffsetCurve.postWrapMode = WrapMode.ClampForever;
            localYOffsetCurve.preWrapMode = WrapMode.ClampForever;
            for (int x = 0;x<girthMap.width;x++) {
                Vector2 positionSum = Vector2.zero;
                for (int y = 0;y<girthMap.height;y++) {
                    float rad = ((float)y/(float)girthMap.height)*Mathf.PI*2f;
                    float distFromCore = cpuTex.GetPixel(x,y).r*maxLocalGirth;
                    float xPosition = Mathf.Cos(rad)*distFromCore;
                    float yPosition = Mathf.Sin(rad)*distFromCore;
                    positionSum += new Vector2(xPosition,yPosition);
                }
                float distFromRoot = ((float)x/(float)girthMap.width)*maxLocalLength;
                Vector2 positionAverage = positionSum/(float)girthMap.width;
                localXOffsetCurve.AddKey(distFromRoot, positionAverage.x);
                localYOffsetCurve.AddKey(distFromRoot, positionAverage.y);
            }
        }
        private void PopulateGirthCurve(RenderTexture girthMap) {
            // First we use the GPU to scrunch the 2D girthmap into a 1D one. This averages all the pixels.
            RenderTexture temp = RenderTexture.GetTemporary(32,1,16,RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(girthMap, temp);
            Texture2D cpuTex = new Texture2D(32,1, TextureFormat.RGB24, false, true);
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
            texture = new RenderTexture(256,256,16, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Vector3 localDickRight = Vector3.Cross(localDickForward, localDickUp);
            this.localDickForward = localDickForward;
            this.localDickUp = localDickUp;
            this.localDickRight = localDickRight;
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
            PopulateOffsetCurves(texture);
        }
        ~GirthData() {
            texture.Release();
        }
        public RenderTexture GetGirthMap() => texture;
    }
}