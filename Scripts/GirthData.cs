using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PenetrationTech {
    [System.Serializable]
    public class GirthData {
        [System.Serializable]
        public class GirthFrame {
            [SerializeField]
            public float maxLocalLength;
            [SerializeField]
            public float maxLocalGirthRadius;
            [SerializeField]
            public AnimationCurve localGirthRadiusCurve;
            [SerializeField]
            public AnimationCurve localXOffsetCurve;
            [SerializeField]
            public AnimationCurve localYOffsetCurve;
            [SerializeField]
            public RenderTexture girthMap;
            public void PopulateOffsetCurves(Vector3 rendererLocalDickForward, Vector3 rendererLocalDickRight, Vector3 rendererLocalDickUp) {
                // First we use the GPU to scrunch the 2D girthmap a little, this reduces the work we have to do, and smooths the data a bit.
                Texture2D cpuTex = new Texture2D(girthMap.width,girthMap.height, TextureFormat.R8, false, true);
                var lastActive = RenderTexture.active;
                RenderTexture.active = girthMap;
                cpuTex.ReadPixels(new Rect(0,0, girthMap.width, girthMap.height), 0, 0);
                cpuTex.Apply();
                RenderTexture.active = lastActive;

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

                        Vector3 point = rendererLocalDickForward * (((float)x/(float)cpuTex.width) * maxLocalLength);
                        Vector3 otherPoint = point+rendererLocalDickRight*xPosition + rendererLocalDickUp*yPosition;
                        //Debug.DrawLine(objectToWorld.MultiplyPoint(point),objectToWorld.MultiplyPoint(otherPoint), Color.red, 10f);

                        Vector3 oppositeOtherPoint = point+rendererLocalDickRight*oppositeXPosition + rendererLocalDickUp*oppositeYPosition;
                        //Debug.DrawLine(objectToWorld.MultiplyPoint(point),objectToWorld.MultiplyPoint(oppositeOtherPoint), Color.blue, 10f);
                    }
                    float distFromRoot = ((float)x/(float)cpuTex.width)*maxLocalLength;
                    Vector2 positionAverage = positionSum/(float)(cpuTex.height/2);
                    positionAverage *= 2;
                    localXOffsetCurve.AddKey(distFromRoot, positionAverage.x);
                    localYOffsetCurve.AddKey(distFromRoot, positionAverage.y);
                }
                localXOffsetCurve.AddKey(maxLocalLength, 0f);
                localYOffsetCurve.AddKey(maxLocalLength, 0f);
                if (Application.isPlaying) {
                    Object.Destroy(cpuTex);
                } else {
                    Object.DestroyImmediate(cpuTex);
                }
            }
            public void PopulateGirthCurve() {
                // First we use the GPU to scrunch the 2D girthmap a little, this reduces the work we have to do, and smooths the data a bit.
                Texture2D cpuTex = new Texture2D(girthMap.width,girthMap.height, TextureFormat.R8, false, true);
                var lastActive = RenderTexture.active;
                RenderTexture.active = girthMap;
                cpuTex.ReadPixels(new Rect(0,0,girthMap.width, girthMap.height), 0, 0);
                cpuTex.Apply();
                RenderTexture.active = lastActive;
                // Then after we got it on the CPU, we use it to generate some curves that we can visualize in the editor (and easily sample).
                localGirthRadiusCurve = new AnimationCurve();
                localGirthRadiusCurve.postWrapMode = WrapMode.ClampForever;
                localGirthRadiusCurve.preWrapMode = WrapMode.ClampForever;
                for (int x=0;x<cpuTex.width;x++) {
                    float averagePixelColor = 0f;
                    float maxPixelColor = 0f;
                    for (int y=0;y<cpuTex.height;y++) {
                        float pixelColor = cpuTex.GetPixel(x,y).r;
                        averagePixelColor += pixelColor;
                        maxPixelColor = Mathf.Max(pixelColor, maxPixelColor);
                    }
                    averagePixelColor/=cpuTex.height;
                    averagePixelColor = (averagePixelColor + maxPixelColor) / 2f;
                    localGirthRadiusCurve.AddKey((float)x/(float)cpuTex.width*maxLocalLength,averagePixelColor*maxLocalGirthRadius);
                }
                localGirthRadiusCurve.AddKey(maxLocalLength,0f);
                if (Application.isPlaying) {
                    Object.Destroy(cpuTex);
                } else {
                    Object.DestroyImmediate(cpuTex);
                }
            }
            public void Release() {
                girthMap.Release();
                girthMap = null;
            }
        }

        [SerializeField]
        private GirthFrame baseGirthFrame;
        [SerializeField]
        private List<GirthFrame> girthDeltaFrames;
        
        private RendererSubMeshMask rendererMask;
        private Transform dickRoot;
        private Vector3 rendererLocalDickForward;
        private Vector3 rendererLocalDickUp;
        private Vector3 rendererLocalDickRight;
        private Vector3 rendererLocalDickRoot;
        private Vector3 rootLocalDickForward;
        private Vector3 rootLocalDickUp;
        private Vector3 rootLocalDickRight;
        private Vector3 rootLocalDickRoot;

        private static float GetPiecewiseDerivative(AnimationCurve curve, float t) {
            float epsilon = 0.00001f;
            float a = curve.Evaluate(t - epsilon);
            float b = curve.Evaluate(t + epsilon);
            return (b - a) / (epsilon * 2f);
        }

        private Matrix4x4 objectToWorld => rendererMask.renderer.localToWorldMatrix;
        private Matrix4x4 worldToObject => rendererMask.renderer.worldToLocalMatrix;

        public static bool IsValid(GirthData data, Vector3 forward, Vector3 right, Vector3 up) {
            return data != null && data.rendererMask != null && data.girthDeltaFrames != null && data.girthDeltaFrames.Count != 0 && data.rootLocalDickForward == forward && data.rootLocalDickRight == right && data.rootLocalDickUp == up;
        }

        public float GetLocalLength() {
            float baseLength = baseGirthFrame.maxLocalLength;
            float length = baseLength;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    length += (girthDeltaFrames[i].maxLocalLength-baseLength) * (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
                }
            }
            return length;
        }

        public float GetKnotForce(float worldDistanceAlongDick) {
            if (worldDistanceAlongDick < 0f || worldDistanceAlongDick > GetWorldLength()) {
                return 0f;
            }

            float localDistanceAlongDick = worldToObject.MultiplyVector(worldDistanceAlongDick*objectToWorld.MultiplyVector((rendererLocalDickForward)).normalized).magnitude / GetDickRootScaleFactor(rootLocalDickForward);
            float baseKnotForce = GetPiecewiseDerivative(baseGirthFrame.localGirthRadiusCurve, localDistanceAlongDick*(baseGirthFrame.maxLocalLength / GetLocalLength()));
            float knotForce = baseKnotForce;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    knotForce += (GetPiecewiseDerivative(girthDeltaFrames[i].localGirthRadiusCurve, localDistanceAlongDick*(girthDeltaFrames[i].maxLocalLength/GetLocalLength()))-baseKnotForce) * (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
                }
            }
            return knotForce;
        }

        private float GetDickRootScaleFactor(Vector3 axis) {
            if (rendererMask.renderer is SkinnedMeshRenderer) {
                float scale = Vector3.Dot(dickRoot.localScale,new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z)));
                return scale;
            }

            return 1f;
        }

        public float GetGirthScaleFactor() {
            float baseGirthRadius = baseGirthFrame.maxLocalGirthRadius;
            float girthRadius = baseGirthRadius;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    girthRadius += (girthDeltaFrames[i].maxLocalGirthRadius-baseGirthRadius) *
                             (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
                }
            }
            Vector3 localGirth = rendererLocalDickUp*girthRadius;
            float scaleFactor = objectToWorld.MultiplyVector(localGirth).magnitude;
            return scaleFactor * GetDickRootScaleFactor(rootLocalDickUp);
        }
        public float GetWorldLength() {
            // This handles skewed forwards, and even non-proportional scales of the dick (making it stubbier or longer)
            Vector3 length = GetLocalLength() * rendererLocalDickForward;
            return objectToWorld.MultiplyVector(length).magnitude * GetDickRootScaleFactor(rootLocalDickForward);
        }
        // Dick space is arbitrary, "Spline space" refers to Z forward, Y Up, and X right space. 
        // This is to make it easier to place onto a spline.
        public Vector3 GetScaledSplineSpaceOffset(float worldDistanceAlongDick) {
            float localDistanceAlongDick = worldToObject.MultiplyVector(worldDistanceAlongDick*objectToWorld.MultiplyVector((rendererLocalDickForward)).normalized).magnitude / GetDickRootScaleFactor(rootLocalDickForward);
            float lengthScaleFactor = baseGirthFrame.maxLocalLength / GetLocalLength();
            float baseLocalXOffsetSample = baseGirthFrame.localXOffsetCurve.Evaluate(localDistanceAlongDick*lengthScaleFactor);
            float baseLocalYOffsetSample = baseGirthFrame.localYOffsetCurve.Evaluate(localDistanceAlongDick*lengthScaleFactor);
            float localXOffsetSample = baseLocalXOffsetSample;
            float localYOffsetSample = baseLocalYOffsetSample;
            
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    float scaleFactor = girthDeltaFrames[i].maxLocalLength / GetLocalLength();
                    localXOffsetSample += (girthDeltaFrames[i].localXOffsetCurve.Evaluate(localDistanceAlongDick*scaleFactor)-baseLocalXOffsetSample) *
                             (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
                    localYOffsetSample += (girthDeltaFrames[i].localYOffsetCurve.Evaluate(localDistanceAlongDick*scaleFactor)-baseLocalYOffsetSample) *
                             (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
                }
            }

            Vector3 worldOffset = objectToWorld.MultiplyVector(rendererLocalDickRight*localXOffsetSample+rendererLocalDickUp*localYOffsetSample)*GetDickRootScaleFactor(rootLocalDickRight);
            Matrix4x4 changeOfBasis = Matrix4x4.identity;
            changeOfBasis.SetRow(0,objectToWorld.MultiplyVector(rendererLocalDickRight).normalized);
            changeOfBasis.SetRow(1,objectToWorld.MultiplyVector(rendererLocalDickUp).normalized);
            changeOfBasis.SetRow(2,objectToWorld.MultiplyVector(rendererLocalDickForward).normalized);
            changeOfBasis[3,3] = 1f;
            return changeOfBasis.MultiplyVector(worldOffset);
        }

        public RenderTexture GetGirthMap() {
            RenderTexture bestMatch = baseGirthFrame.girthMap;
            float bestMatchAmount = 50f;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    float amount = skinnedMeshRenderer.GetBlendShapeWeight(i);
                    if (amount > bestMatchAmount) {
                        bestMatch = girthDeltaFrames[i].girthMap;
                        bestMatchAmount = amount;
                    }
                }
            }

            return bestMatch;
        }

        public float GetWorldGirthRadius(float worldDistanceAlongDick) {
            float localDistanceAlongDick = worldToObject.MultiplyVector(worldDistanceAlongDick*objectToWorld.MultiplyVector((rendererLocalDickForward)).normalized).magnitude / GetDickRootScaleFactor(rootLocalDickForward);
            // TODO: There's no real way to actually get the girth correctly, since we cannot interpret skewed scales. This is probably acceptable, though instead of just using localDickUp, maybe it should be a diagonal between up and right.
            // I currently just choose a single axis, though users shouldn't skew scale on the up/right axis anyway.
            float baseLocalGirthSample = baseGirthFrame.localGirthRadiusCurve.Evaluate(localDistanceAlongDick*(baseGirthFrame.maxLocalLength / GetLocalLength()));
            float localGirthSample = baseLocalGirthSample;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                for (int i = 0; i < skinnedMeshRenderer.sharedMesh.blendShapeCount; i++) {
                    localGirthSample += (girthDeltaFrames[i].localGirthRadiusCurve.Evaluate(localDistanceAlongDick*(girthDeltaFrames[i].maxLocalLength / GetLocalLength()))-baseLocalGirthSample) * (skinnedMeshRenderer.GetBlendShapeWeight(i) / 100f);
                }
            }

            Vector3 localGirth = rendererLocalDickUp*localGirthSample;
            return objectToWorld.MultiplyVector(localGirth).magnitude * GetDickRootScaleFactor(rootLocalDickUp);
        }
        private static void GetBindPoseBoneLocalPositionRotation(Matrix4x4 boneMatrix, out Vector3 position, out Quaternion rotation) {
            // Get global matrix for bone
            Matrix4x4 bindMatrixGlobal = boneMatrix.inverse;
 
            // Get local X, Y, Z, and position of matrix
            Vector3 mX = new Vector3(bindMatrixGlobal.m00, bindMatrixGlobal.m10, bindMatrixGlobal.m20);
            Vector3 mY = new Vector3(bindMatrixGlobal.m01, bindMatrixGlobal.m11, bindMatrixGlobal.m21);
            Vector3 mZ = new Vector3(bindMatrixGlobal.m02, bindMatrixGlobal.m12, bindMatrixGlobal.m22);
            Vector3 mP = new Vector3(bindMatrixGlobal.m03, bindMatrixGlobal.m13, bindMatrixGlobal.m23);
            position = mP;
            
            // Set rotation
            // Check if scaling is negative and handle accordingly
            if (Vector3.Dot(Vector3.Cross(mX, mY), mZ) >= 0) {
                rotation = Quaternion.LookRotation(mZ, mY);
            } else {
                rotation = Quaternion.LookRotation(-mZ, -mY);
            }
        }
        public void Release() {
            if (baseGirthFrame != null) {
                baseGirthFrame.Release();
                baseGirthFrame = null;
            }

            if (girthDeltaFrames != null && girthDeltaFrames.Count > 0) {
                foreach (var girthFrame in girthDeltaFrames) {
                    girthFrame.Release();
                }

                girthDeltaFrames.Clear();
            }
        }
        private GirthFrame GenerateFrame(Mesh mesh, int blendshapeIndex, Shader girthUnwrapShader) {
            GirthFrame frame = new GirthFrame();
            frame.girthMap = new RenderTexture(64, 64, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            frame.girthMap.useMipMap = true;
            frame.girthMap.autoGenerateMips = false;
            frame.girthMap.wrapModeU = TextureWrapMode.Clamp;
            frame.girthMap.wrapModeV = TextureWrapMode.Repeat;
            
            List<Vector3> vertices = new List<Vector3>();
            mesh.GetVertices(vertices);
            if (blendshapeIndex != -1) {
                Vector3[] blendDeltaVertices = new Vector3[vertices.Count];
                Vector3[] blendDeltaNormals = new Vector3[vertices.Count];
                Vector3[] blendDeltaTangents = new Vector3[vertices.Count];
                mesh.GetBlendShapeFrameVertices(blendshapeIndex,0, blendDeltaVertices, blendDeltaNormals, blendDeltaTangents);
                for (int i = 0; i < vertices.Count; i++) {
                    vertices[i] += blendDeltaVertices[i];
                }
            }

            Mesh blitMesh;

            // If we're a skinned mesh renderer, we mask by bone weights.
            if (rendererMask.renderer is SkinnedMeshRenderer meshRenderer) {
                blitMesh = new Mesh();
                blitMesh.SetVertices(vertices);
                blitMesh.subMeshCount = mesh.subMeshCount;
                for (int i = 0; i < mesh.subMeshCount; i++) {
                    if (rendererMask.ShouldDrawSubmesh(i)) {
                        blitMesh.SetTriangles(mesh.GetTriangles(i), i);
                    }
                }

                var weights = mesh.GetAllBoneWeights();
                var bonesPerVertex = mesh.GetBonesPerVertex();
                int vt = 0;
                int wt = 0;
                for (int o = 0; o < bonesPerVertex.Length; o++) {
                    for (int p = 0; p < bonesPerVertex[o]; p++) {
                        BoneWeight1 weight = weights[wt];
                        //TODO: This can be made much faster by caching the bone ids that match our search.
                        Transform boneWeightTarget = meshRenderer.bones[weights[wt].boneIndex];
                        if (boneWeightTarget.IsChildOf(dickRoot) && weights[wt].weight > 0.1f) {
                            Vector3 pos = vertices[vt];
                            float length = Vector3.Dot(rendererLocalDickForward, pos - rendererLocalDickRoot);
                            float girth = Vector3.Distance(pos,
                                (rendererLocalDickRoot + rendererLocalDickForward * length));
                            frame.maxLocalGirthRadius = Mathf.Max(girth, frame.maxLocalGirthRadius);
                            frame.maxLocalLength = Mathf.Max(length, frame.maxLocalLength);
                        }

                        wt++;
                    }

                    vt++;
                }
            } else {
                // Otherwise we can just use every vert.
                foreach (Vector3 vertexPosition in vertices) {
                    //Vector3 dickSpacePosition = changeOfBasis.MultiplyPoint(vertexPosition);
                    float length = Vector3.Dot(rendererLocalDickForward, vertexPosition - rendererLocalDickRoot);
                    float girth = Vector3.Distance(vertexPosition,
                        (rendererLocalDickRoot + rendererLocalDickForward * length));
                    frame.maxLocalGirthRadius = Mathf.Max(girth, frame.maxLocalGirthRadius);
                    frame.maxLocalLength = Mathf.Max(length, frame.maxLocalLength);
                }

                blitMesh = mesh;
            }

            Material mat = new Material(girthUnwrapShader);
            mat.SetVector("_DickOrigin", rendererLocalDickRoot);
            mat.SetVector("_DickForward", rendererLocalDickForward);
            mat.SetVector("_DickUp", rendererLocalDickUp);
            mat.SetVector("_DickRight", rendererLocalDickRight);
            mat.SetFloat("_MaxLength", frame.maxLocalLength);
            mat.SetFloat("_MaxGirth", frame.maxLocalGirthRadius);
            mat.SetFloat("_AngleOffset", Mathf.PI / 2f);

            // Then use the GPU to rasterize
            CommandBuffer buffer = new CommandBuffer();
            buffer.SetRenderTarget(frame.girthMap);
            buffer.ClearRenderTarget(false, true, Color.clear);
            for (int j = 0; j < mesh.subMeshCount; j++) {
                if (!rendererMask.ShouldDrawSubmesh(j)) {
                    continue;
                }

                //buffer.DrawRenderer(rendererMask.renderer, mat, j, 0);
                buffer.DrawMesh(blitMesh, Matrix4x4.identity, mat, j, 0);
            }


            Graphics.ExecuteCommandBuffer(buffer);

            // We need to do one more blits to ensure the full image gets filled.
            CommandBuffer additiveBuffer = new CommandBuffer();
            additiveBuffer.SetRenderTarget(frame.girthMap);
            for (int j = 0; j < mesh.subMeshCount; j++) {
                if (!rendererMask.ShouldDrawSubmesh(j)) {
                    continue;
                }

                //additiveBuffer.DrawRenderer(rendererMask.renderer, mat, j, 0);
                additiveBuffer.DrawMesh(blitMesh, Matrix4x4.identity, mat, j, 0);
            }

            mat.SetFloat("_AngleOffset", -Mathf.PI / 2f);
            Graphics.ExecuteCommandBuffer(additiveBuffer);

            frame.girthMap.GenerateMips();
            
            frame.PopulateOffsetCurves(rendererLocalDickForward, rendererLocalDickRight, rendererLocalDickUp);
            frame.PopulateGirthCurve();

            return frame;
        }

        public GirthData(RendererSubMeshMask rendererWithMask, Shader girthUnwrapShader, Transform root, Vector3 rootLocalDickRoot, Vector3 rootDickForward, Vector3 rootDickUp, Vector3 rootDickRight) {
            rendererMask = rendererWithMask;
            dickRoot = root;
            this.rootLocalDickRoot = rootLocalDickRoot;
            this.rootLocalDickUp = rootDickUp;
            this.rootLocalDickForward = rootDickForward;
            this.rootLocalDickRight = rootDickRight;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer) {
                int rootBoneID = -1;
                for (int i = 0; i < skinnedMeshRenderer.bones.Length; i++) {
                    if (skinnedMeshRenderer.bones[i] == root) {
                        rootBoneID = i;
                    }
                }

                if (rootBoneID == -1) {
                    throw new UnityException("You must choose a bone on the armature...");
                }

                Mesh skinnedMesh = skinnedMeshRenderer.sharedMesh;
                GetBindPoseBoneLocalPositionRotation(skinnedMesh.bindposes[rootBoneID], out Vector3 posePosition, out Quaternion poseRotation);
                rendererLocalDickForward = (poseRotation * rootDickForward).normalized;
                rendererLocalDickUp = (poseRotation * rootDickUp).normalized;
                rendererLocalDickRight = (poseRotation * rootDickRight).normalized;
                rendererLocalDickRoot = posePosition;
            } else {
                rendererLocalDickForward = worldToObject.MultiplyVector(root.TransformDirection(rootDickForward)).normalized;
                rendererLocalDickUp = worldToObject.MultiplyVector(root.TransformDirection(rootDickUp)).normalized;
                rendererLocalDickRight = worldToObject.MultiplyVector(root.TransformDirection(rootDickRight)).normalized;
                rendererLocalDickRoot = worldToObject.MultiplyPoint(root.TransformPoint(rootLocalDickRoot));
            }


            Mesh mesh;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer1) {
                mesh = skinnedMeshRenderer1.sharedMesh;
            } else if (rendererMask.renderer is MeshRenderer) {
                mesh = rendererMask.renderer.GetComponent<MeshFilter>().sharedMesh;
            } else {
                throw new UnityException("Girth data can only be generated on SkinnedMeshRenderers and MeshRenderers.");
            }

            baseGirthFrame = GenerateFrame(mesh, -1, girthUnwrapShader);
            // Do a quick pass to figure out how girthy and lengthy we are
            girthDeltaFrames = new List<GirthFrame>();
            for (int i = 0; i < mesh.blendShapeCount; i++) {
                girthDeltaFrames.Add(GenerateFrame(mesh, i, girthUnwrapShader));
            }
        }
    }
}