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
        private float maxLocalGirthRadius;
        [ReadOnly][SerializeField]
        private AnimationCurve localGirthRadiusCurve;
        [ReadOnly][SerializeField]
        private AnimationCurve localXOffsetCurve;
        [ReadOnly][SerializeField]
        private AnimationCurve localYOffsetCurve;
        private Renderer renderer;
        private Vector3 localDickForward;
        private Vector3 localDickUp;
        private Vector3 localDickRight;
        private Vector3 localDickRoot;
        private Matrix4x4 objectToWorld {
            get { return renderer.localToWorldMatrix; }
        }
        private Matrix4x4 worldToObject {
            get { return renderer.worldToLocalMatrix; }
        }
        public float GetGirthScaleFactor() {
            Vector3 localGirth = localDickUp*maxLocalGirthRadius;
            float scaleFactor = objectToWorld.MultiplyVector(localGirth).magnitude;
            return scaleFactor;
        }
        public float GetWorldLength() {
            // This handles skewed forwards, and even non-proportional scales of the dick (making it stubbier or longer)
            Vector3 length = maxLocalLength * localDickForward;
            return objectToWorld.MultiplyVector(length).magnitude;
        }
        // Dick space is arbitrary, "Spline space" refers to Z forward, Y Up, and X right space. 
        // This is to make it easier to place onto a spline.
        public Vector3 GetScaledSplineSpaceOffset(float worldDistanceAlongDick) {
            float localDistanceAlongDick = worldToObject.MultiplyVector(worldDistanceAlongDick*objectToWorld.MultiplyVector((localDickForward)).normalized).magnitude;
            float localXOffsetSample = localXOffsetCurve.Evaluate(localDistanceAlongDick);
            float localYOffsetSample = localYOffsetCurve.Evaluate(localDistanceAlongDick);

            Vector3 worldOffset = objectToWorld.MultiplyVector(localDickRight*localXOffsetSample+localDickUp*localYOffsetSample);
            Matrix4x4 changeOfBasis = Matrix4x4.identity;
            changeOfBasis.SetRow(0,objectToWorld.MultiplyVector(localDickRight).normalized);
            changeOfBasis.SetRow(1,objectToWorld.MultiplyVector(localDickUp).normalized);
            changeOfBasis.SetRow(2,objectToWorld.MultiplyVector(localDickForward).normalized);
            changeOfBasis[3,3] = 1f;
            return changeOfBasis.MultiplyVector(worldOffset);
        }
        public float GetWorldGirthRadius(float worldDistanceAlongDick) {
            float localDistanceAlongDick = worldToObject.MultiplyVector(worldDistanceAlongDick*objectToWorld.MultiplyVector(localDickForward).normalized).magnitude;
            // TODO: There's no real way to actually get the girth correctly, since we cannot interpret skewed scales.
            // I currently just choose a single axis, though users shouldn't skew scale on the up/right axis anyway.
            float localGirthSample = localGirthRadiusCurve.Evaluate(localDistanceAlongDick);
            Vector3 localGirth = localDickUp*localGirthSample;
            return objectToWorld.MultiplyVector(localGirth).magnitude;
        }
        private void PopulateOffsetCurves(RenderTexture girthMap) {
            // First we use the GPU to scrunch the 2D girthmap a little, this reduces the work we have to do, and smooths the data a bit.
            RenderTexture temp = RenderTexture.GetTemporary(32,32,16,RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(girthMap, temp);
            Texture2D cpuTex = new Texture2D(32,32, TextureFormat.RGB24, false, true);
            RenderTexture.active = temp;
            cpuTex.ReadPixels(new Rect(0,0,temp.width, temp.height), 0, 0);
            cpuTex.Apply();
            RenderTexture.ReleaseTemporary(temp);

            localXOffsetCurve = new AnimationCurve();
            localXOffsetCurve.postWrapMode = WrapMode.ClampForever;
            localXOffsetCurve.preWrapMode = WrapMode.ClampForever;
            localYOffsetCurve = new AnimationCurve();
            localYOffsetCurve.postWrapMode = WrapMode.ClampForever;
            localYOffsetCurve.preWrapMode = WrapMode.ClampForever;
            for (int x = 0;x<cpuTex.width;x++) {
                Vector2 positionSum = Vector2.zero;
                for (int y = 0;y<cpuTex.height/2;y++) {
                    float color = cpuTex.GetPixel(x,y).r;
                    float rad = ((float)y/(float)cpuTex.height)*Mathf.PI*2f;
                    float distFromCore = color*maxLocalGirthRadius;
                    float xPosition = Mathf.Sin(rad-Mathf.PI/2f)*distFromCore;
                    float yPosition = Mathf.Cos(rad-Mathf.PI/2f)*distFromCore;
                    Vector2 position = new Vector2(xPosition, yPosition);

                    int oppositeY = (y+cpuTex.height/2)%cpuTex.height;
                    float oppositeColor = cpuTex.GetPixel(x,oppositeY).r;
                    float oppositeRad = ((float)oppositeY/(float)cpuTex.height)*Mathf.PI*2f;
                    float oppositeDistFromCore = oppositeColor*maxLocalGirthRadius;
                    float oppositeXPosition = Mathf.Sin(oppositeRad-Mathf.PI/2f)*oppositeDistFromCore;
                    float oppositeYPosition = Mathf.Cos(oppositeRad-Mathf.PI/2f)*oppositeDistFromCore;
                    Vector2 oppositePosition = new Vector2(oppositeXPosition, oppositeYPosition);
                    positionSum += (position+oppositePosition)*0.5f;

                    Vector3 point = localDickForward*((float)x/(float)cpuTex.width)*maxLocalLength;
                    Vector3 otherPoint = point+localDickRight*xPosition + localDickUp*yPosition;
                    //Debug.DrawLine(objectToWorld.MultiplyPoint(point),objectToWorld.MultiplyPoint(otherPoint), Color.red, 10f);

                    Vector3 oppositeOtherPoint = point+localDickRight*oppositeXPosition + localDickUp*oppositeYPosition;
                    //Debug.DrawLine(objectToWorld.MultiplyPoint(point),objectToWorld.MultiplyPoint(oppositeOtherPoint), Color.blue, 10f);
                }
                float distFromRoot = ((float)x/(float)cpuTex.width)*maxLocalLength;
                Vector2 positionAverage = positionSum/(float)(cpuTex.height/2);
                positionAverage *= 2;
                if (x == 31) {
                    positionAverage *= 0f;
                }
                localXOffsetCurve.AddKey(distFromRoot, positionAverage.x);
                localYOffsetCurve.AddKey(distFromRoot, positionAverage.y);
            }
        }
        private void PopulateGirthCurve(RenderTexture girthMap) {
            // First we use the GPU to scrunch the 2D girthmap a little, this reduces the work we have to do, and smooths the data a bit.
            RenderTexture temp = RenderTexture.GetTemporary(32,32,16,RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(girthMap, temp);
            Texture2D cpuTex = new Texture2D(32,32, TextureFormat.RGB24, false, true);
            RenderTexture.active = temp;
            cpuTex.ReadPixels(new Rect(0,0,temp.width, temp.height), 0, 0);
            cpuTex.Apply();
            RenderTexture.ReleaseTemporary(temp);
            // Then after we got it on the CPU, we use it to generate some curves that we can visualize in the editor (and easily sample).
            localGirthRadiusCurve = new AnimationCurve();
            localGirthRadiusCurve.postWrapMode = WrapMode.ClampForever;
            localGirthRadiusCurve.preWrapMode = WrapMode.ClampForever;
            for (int x=0;x<32;x++) {
                float averagePixelColor = 0f;
                for (int y=0;y<32;y++) {
                    averagePixelColor += cpuTex.GetPixel(x,y).r;
                }
                averagePixelColor/=32f;
                if (x==31) {
                    averagePixelColor*=0f;
                }
                localGirthRadiusCurve.AddKey((float)x/32f*maxLocalLength,averagePixelColor*maxLocalGirthRadius);
            }
        }
        
        public GirthData(Renderer renderer, Transform root, Vector3 rootLocalDickRoot, Vector3 rootDickForward, Vector3 rootDickUp, Vector3 rootDickRight) {
            this.renderer = renderer;
            texture = new RenderTexture(256,256, 16, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            texture.useMipMap = true;
            texture.autoGenerateMips = false;
            texture.wrapMode = TextureWrapMode.Repeat;

            localDickForward = worldToObject.MultiplyVector(root.TransformDirection(rootDickForward)).normalized;
            localDickUp = worldToObject.MultiplyVector(root.TransformDirection(rootDickUp)).normalized;
            localDickRight = worldToObject.MultiplyVector(root.TransformDirection(rootDickRight)).normalized;

            Vector3 worldSpaceDickRoot = root.TransformPoint(rootLocalDickRoot);
            localDickRoot = worldToObject.MultiplyPoint(worldSpaceDickRoot);


            Mesh mesh;
            if (renderer is SkinnedMeshRenderer) {
                mesh = (renderer as SkinnedMeshRenderer).sharedMesh;
            } else if (renderer is MeshRenderer) {
                mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            } else {
                throw new UnityException("Girth data can only be generated on SkinnedMeshRenderers and MeshRenderers.");
            }

            Material mat = new Material(Shader.Find("PenetrationTech/GirthUnwrapRaw"));
            mat.SetVector("_DickOrigin", localDickRoot);
            mat.SetVector("_DickForward", this.localDickForward);
            mat.SetVector("_DickUp", this.localDickUp);
            mat.SetVector("_DickRight", this.localDickRight);
            // Do a quick pass to figure out how girthy and lengthy we are
            maxLocalGirthRadius = 0f;
            maxLocalLength = 0f;
            foreach(Vector3 vertexPosition in mesh.vertices) {
                //Vector3 dickSpacePosition = changeOfBasis.MultiplyPoint(vertexPosition);
                float length = Vector3.Dot(localDickForward, vertexPosition-localDickRoot);
                float girth = Vector3.Distance(vertexPosition,(localDickRoot+localDickForward*length));
                maxLocalGirthRadius = Mathf.Max(girth, maxLocalGirthRadius);
                maxLocalLength = Mathf.Max(length, maxLocalLength);
            }
            mat.SetFloat("_MaxLength", maxLocalLength);
            mat.SetFloat("_MaxGirth", maxLocalGirthRadius);
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

            texture.GenerateMips();
            PopulateGirthCurve(texture);
            PopulateOffsetCurves(texture);
        }
        ~GirthData() {
            texture.Release();
        }
        public RenderTexture GetGirthMap() => texture;
    }
}