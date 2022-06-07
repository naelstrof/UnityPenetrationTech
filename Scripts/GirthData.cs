using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PenetrationTech {
    [System.Serializable]
    public class GirthData {
        [Header("This data is generated automatically, and is only viewable for debugging!")]
        [ReadOnly][SerializeField][Tooltip("How long the penetrator is in local object space")]
        private float maxLocalLength;
        [ReadOnly][SerializeField][Tooltip("The maximum amount of girth radius around the penetrator axis, in local object space")]
        private float maxLocalGirthRadius;
        [ReadOnly][SerializeField][Tooltip("A curve to help with sampling and viewing the generated girth radius curve, in local object space")]
        private AnimationCurve localGirthRadiusCurve;
        [ReadOnly][SerializeField][Tooltip("A curve to help with sampling and viewing the generated X offset curve, in local object space")]
        private AnimationCurve localXOffsetCurve;
        [ReadOnly][SerializeField][Tooltip("A curve to help with sampling and viewing the generated Y offset curve, in local object space")]
        private AnimationCurve localYOffsetCurve;
        
        private RendererSubMeshMask rendererMask;
        private Transform dickRoot;
        private Vector3 localDickForward;
        private Vector3 localDickUp;
        private Vector3 localDickRight;
        private Vector3 localDickRoot;

        private static float GetPiecewiseDerivative(AnimationCurve curve, float t) {
            float epsilon = 0.00001f;
            float a = curve.Evaluate(t - epsilon);
            float b = curve.Evaluate(t + epsilon);
            return (b - a) / (epsilon * 2f);
        }

        private Matrix4x4 objectToWorld => rendererMask.renderer.localToWorldMatrix;
        private Matrix4x4 worldToObject => rendererMask.renderer.worldToLocalMatrix;

        public static bool IsValid(GirthData data) {
            return data != null && data.rendererMask != null && data.localGirthRadiusCurve != null &&
                   data.localGirthRadiusCurve.keys.Length != 0;
        }

        public float GetLocalLength() => maxLocalLength;

        public float GetKnotForce(float worldDistanceAlongDick) {
            if (worldDistanceAlongDick < 0f || worldDistanceAlongDick > GetWorldLength()) {
                return 0f;
            }

            float localDistanceAlongDick = worldToObject.MultiplyVector(worldDistanceAlongDick*objectToWorld.MultiplyVector((localDickForward)).normalized).magnitude / GetDickRootScaleFactor();
            return GetPiecewiseDerivative(localGirthRadiusCurve, localDistanceAlongDick);
        }

        private float GetDickRootScaleFactor() {
            if (rendererMask.renderer is SkinnedMeshRenderer) {
                return  dickRoot.localScale.x;
            }

            return 1f;
        }

        public float GetGirthScaleFactor() {
            Vector3 localGirth = localDickUp*maxLocalGirthRadius;
            float scaleFactor = objectToWorld.MultiplyVector(localGirth).magnitude;
            return scaleFactor * GetDickRootScaleFactor();
        }
        public float GetWorldLength() {
            // This handles skewed forwards, and even non-proportional scales of the dick (making it stubbier or longer)
            Vector3 length = maxLocalLength * localDickForward;
            return objectToWorld.MultiplyVector(length).magnitude * GetDickRootScaleFactor();
        }
        // Dick space is arbitrary, "Spline space" refers to Z forward, Y Up, and X right space. 
        // This is to make it easier to place onto a spline.
        public Vector3 GetScaledSplineSpaceOffset(float worldDistanceAlongDick) {
            float localDistanceAlongDick = worldToObject.MultiplyVector(worldDistanceAlongDick*objectToWorld.MultiplyVector((localDickForward)).normalized).magnitude / GetDickRootScaleFactor();
            float localXOffsetSample = localXOffsetCurve.Evaluate(localDistanceAlongDick);
            float localYOffsetSample = localYOffsetCurve.Evaluate(localDistanceAlongDick);

            Vector3 worldOffset = objectToWorld.MultiplyVector(localDickRight*localXOffsetSample+localDickUp*localYOffsetSample)*GetDickRootScaleFactor();
            Matrix4x4 changeOfBasis = Matrix4x4.identity;
            changeOfBasis.SetRow(0,objectToWorld.MultiplyVector(localDickRight).normalized);
            changeOfBasis.SetRow(1,objectToWorld.MultiplyVector(localDickUp).normalized);
            changeOfBasis.SetRow(2,objectToWorld.MultiplyVector(localDickForward).normalized);
            changeOfBasis[3,3] = 1f;
            return changeOfBasis.MultiplyVector(worldOffset);
        }
        public float GetWorldGirthRadius(float worldDistanceAlongDick) {
            float localDistanceAlongDick = worldToObject.MultiplyVector(worldDistanceAlongDick*objectToWorld.MultiplyVector((localDickForward)).normalized).magnitude / GetDickRootScaleFactor();
            // TODO: There's no real way to actually get the girth correctly, since we cannot interpret skewed scales. This is probably acceptable, though instead of just using localDickUp, maybe it should be a diagonal between up and right.
            // I currently just choose a single axis, though users shouldn't skew scale on the up/right axis anyway.
            float localGirthSample = localGirthRadiusCurve.Evaluate(localDistanceAlongDick);
            Vector3 localGirth = localDickUp*localGirthSample;
            return objectToWorld.MultiplyVector(localGirth).magnitude * GetDickRootScaleFactor();
        }
        private void PopulateOffsetCurves(RenderTexture girthMap) {
            // First we use the GPU to scrunch the 2D girthmap a little, this reduces the work we have to do, and smooths the data a bit.
            RenderTexture temp = RenderTexture.GetTemporary(32,32,0,RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            Graphics.Blit(girthMap, temp);
            Texture2D cpuTex = new Texture2D(32,32, TextureFormat.RGB24, false, true);
            RenderTexture.active = temp;
            cpuTex.ReadPixels(new Rect(0,0,temp.width, temp.height), 0, 0);
            cpuTex.Apply();
            RenderTexture.active = null;
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
                localXOffsetCurve.AddKey(distFromRoot, positionAverage.x);
                localYOffsetCurve.AddKey(distFromRoot, positionAverage.y);
            }
            localXOffsetCurve.AddKey(maxLocalLength, 0f);
            localYOffsetCurve.AddKey(maxLocalLength, 0f);
        }
        private void PopulateGirthCurve(RenderTexture girthMap) {
            // First we use the GPU to scrunch the 2D girthmap a little, this reduces the work we have to do, and smooths the data a bit.
            RenderTexture temp = RenderTexture.GetTemporary(32,32,0,RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
            Graphics.Blit(girthMap, temp);
            Texture2D cpuTex = new Texture2D(32,32, TextureFormat.RGB24, false, true);
            RenderTexture.active = temp;
            cpuTex.ReadPixels(new Rect(0,0,temp.width, temp.height), 0, 0);
            cpuTex.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(temp);
            // Then after we got it on the CPU, we use it to generate some curves that we can visualize in the editor (and easily sample).
            localGirthRadiusCurve = new AnimationCurve();
            localGirthRadiusCurve.postWrapMode = WrapMode.ClampForever;
            localGirthRadiusCurve.preWrapMode = WrapMode.ClampForever;
            for (int x=0;x<32;x++) {
                float averagePixelColor = 0f;
                float maxPixelColor = 0f;
                for (int y=0;y<32;y++) {
                    float pixelColor = cpuTex.GetPixel(x,y).r;
                    averagePixelColor += pixelColor;
                    maxPixelColor = Mathf.Max(pixelColor, maxPixelColor);
                }
                averagePixelColor/=32f;
                averagePixelColor = (averagePixelColor + maxPixelColor) / 2f;
                localGirthRadiusCurve.AddKey((float)x/32f*maxLocalLength,averagePixelColor*maxLocalGirthRadius);
            }
            localGirthRadiusCurve.AddKey(maxLocalLength,0f);
        }
        private static void GetBindPoseBonePositionRotation(Matrix4x4 boneMatrix, out Vector3 position, out Quaternion rotation) {
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
        public GirthData(RenderTexture targetTexture, RendererSubMeshMask rendererWithMask, Shader girthUnwrapShader, Transform root, Vector3 rootLocalDickRoot, Vector3 rootDickForward, Vector3 rootDickUp, Vector3 rootDickRight) {
            rendererMask = rendererWithMask;
            dickRoot = root;
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
                GetBindPoseBonePositionRotation(skinnedMesh.bindposes[rootBoneID], out Vector3 posePosition, out Quaternion poseRotation);
                localDickForward = (poseRotation * rootDickForward).normalized;
                localDickUp = (poseRotation * rootDickUp).normalized;
                localDickRight = (poseRotation * rootDickRight).normalized;
                localDickRoot = posePosition;
            } else {
                localDickForward = worldToObject.MultiplyVector(root.TransformDirection(rootDickForward)).normalized;
                localDickUp = worldToObject.MultiplyVector(root.TransformDirection(rootDickUp)).normalized;
                localDickRight = worldToObject.MultiplyVector(root.TransformDirection(rootDickRight)).normalized;
                localDickRoot = worldToObject.MultiplyPoint(root.TransformPoint(rootLocalDickRoot));
            }


            Mesh mesh;
            if (rendererMask.renderer is SkinnedMeshRenderer skinnedMeshRenderer1) {
                mesh = skinnedMeshRenderer1.sharedMesh;
            } else if (rendererMask.renderer is MeshRenderer) {
                mesh = rendererMask.renderer.GetComponent<MeshFilter>().sharedMesh;
            } else {
                throw new UnityException("Girth data can only be generated on SkinnedMeshRenderers and MeshRenderers.");
            }

            Material mat = new Material(girthUnwrapShader);
            
            mat.SetVector("_DickOrigin", localDickRoot);
            mat.SetVector("_DickForward", localDickForward);
            mat.SetVector("_DickUp", localDickUp);
            mat.SetVector("_DickRight", localDickRight);
            // Do a quick pass to figure out how girthy and lengthy we are
            maxLocalGirthRadius = 0f;
            maxLocalLength = 0f;
            List<Vector3> vertices = new List<Vector3>();
            mesh.GetVertices(vertices);
            
            // If we're a skinned mesh renderer, we mask by bone weights.
            if (rendererMask.renderer is SkinnedMeshRenderer meshRenderer) {
                var weights = mesh.GetAllBoneWeights();
                var bonesPerVertex = mesh.GetBonesPerVertex();
                int vt = 0;
                int wt = 0;
                for (int o = 0; o < bonesPerVertex.Length; o++) {
                    for (int p = 0; p < bonesPerVertex[o]; p++) {
                        BoneWeight1 weight = weights[wt];
                        //TODO: This can be made much faster by caching the bone ids that match our search.
                        Transform boneWeightTarget = meshRenderer.bones[weights[wt].boneIndex];
                        if (boneWeightTarget.IsChildOf(root) && weights[wt].weight > 0.1f) {
                            Vector3 pos = vertices[vt];
                            float length = Vector3.Dot(localDickForward, pos-localDickRoot);
                            float girth = Vector3.Distance(pos,(localDickRoot+localDickForward*length));
                            maxLocalGirthRadius = Mathf.Max(girth, maxLocalGirthRadius);
                            maxLocalLength = Mathf.Max(length, maxLocalLength);
                        }
                        wt++;
                    }
                    vt++;
                }
            } else { // Otherwise we can just use every vert.
                foreach (Vector3 vertexPosition in vertices) {
                    //Vector3 dickSpacePosition = changeOfBasis.MultiplyPoint(vertexPosition);
                    float length = Vector3.Dot(localDickForward, vertexPosition - localDickRoot);
                    float girth = Vector3.Distance(vertexPosition, (localDickRoot + localDickForward * length));
                    maxLocalGirthRadius = Mathf.Max(girth, maxLocalGirthRadius);
                    maxLocalLength = Mathf.Max(length, maxLocalLength);
                }
            }

            mat.SetFloat("_MaxLength", maxLocalLength);
            mat.SetFloat("_MaxGirth", maxLocalGirthRadius);
            mat.SetFloat("_AngleOffset", Mathf.PI/2f);

            // Then use the GPU to rasterize
            CommandBuffer buffer = new CommandBuffer();
            buffer.SetRenderTarget(targetTexture);
            buffer.ClearRenderTarget(false, true, Color.clear);
            for (int i = 0; i < mesh.subMeshCount; i++) {
                if (!rendererMask.ShouldDrawSubmesh(i)) {
                    continue;
                }

                //buffer.DrawRenderer(rendererMask.renderer, mat, i, 0);
                buffer.DrawMesh(mesh, Matrix4x4.identity, mat, i, 0);
            }
            Graphics.ExecuteCommandBuffer(buffer);

            // We need to do one more blits to ensure the full image gets filled.
            CommandBuffer additiveBuffer = new CommandBuffer();
            additiveBuffer.SetRenderTarget(targetTexture);
            for (int i = 0; i < mesh.subMeshCount; i++) {
                if (!rendererMask.ShouldDrawSubmesh(i)) {
                    continue;
                }
                //additiveBuffer.DrawRenderer(rendererMask.renderer, mat, i, 0);
                additiveBuffer.DrawMesh(mesh, Matrix4x4.identity, mat, i, 0);
            }
            mat.SetFloat("_AngleOffset", -Mathf.PI/2f);
            Graphics.ExecuteCommandBuffer(additiveBuffer);

            targetTexture.GenerateMips();
            PopulateGirthCurve(targetTexture);
            PopulateOffsetCurves(targetTexture);
        }
    }
}