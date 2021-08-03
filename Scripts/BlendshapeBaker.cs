// Copyright 2019 Vilar24

// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal 
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is furnished 
// to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all 
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS 
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR 
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER 
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN 
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace PenetrationTech {
    public class BlendShapeBaker : AssetPostprocessor {
        enum BakeAttribute { X, Y, Z };
        enum BakeType { DeltaPosition, DeltaTangent, DeltaNormal };

        private void OnPostprocessModel(GameObject g) {
            Apply(g.transform);
        }

        private void Apply(Transform t) {
            BakeBlendShape(t.gameObject);
            foreach (Transform child in t) {
                Apply(child);
            }
        }

        private void BakeBlendShape(GameObject gameObject) {
            if (gameObject.GetComponent<SkinnedMeshRenderer>() == null) return;
            Mesh mesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
            int DickCumID = mesh.GetBlendShapeIndex("DickCum");
            int DickSquishID = mesh.GetBlendShapeIndex("DickSquish");
            int DickPullID = mesh.GetBlendShapeIndex("DickPull");
            if (DickCumID == -1 && DickSquishID != -1 && DickPullID != -1) {
                Bake(mesh, DickSquishID, BakeType.DeltaPosition, 2);
                Bake(mesh, DickPullID, BakeType.DeltaPosition, 3);
            } else if ( DickCumID != -1 && DickSquishID != -1 && DickPullID != -1) {
                PackLightmapUVs(mesh, DickCumID, BakeAttribute.X);
                Bake(mesh, DickSquishID, BakeType.DeltaPosition, 2, DickCumID, BakeAttribute.Y);
                Bake(mesh, DickPullID, BakeType.DeltaPosition, 3, DickCumID, BakeAttribute.Z);
            }
        }

        Vector3 ToTangentSpace(Vector3 input, Vector3 normal, Vector3 tangent) {
            Vector3 X = normal;
            Vector3 Y = new Vector3(tangent.x, tangent.y, tangent.z);
            Vector3 Z = Vector3.Cross(X, Y);
            Vector3.OrthoNormalize(ref X, ref Y, ref Z);
            Matrix4x4 toNewSpace = new Matrix4x4();
            toNewSpace.SetRow(0, X);
            toNewSpace.SetRow(1, Y);
            toNewSpace.SetRow(2, Z);
            toNewSpace[3, 3] = 1.0F;
            return toNewSpace.MultiplyPoint(input);
        }

        void PackLightmapUVs(Mesh m, int packID, BakeAttribute bakeAttribute) {
            List<Vector3> normals = new List<Vector3>();
            m.GetNormals(normals);
            List<Vector4> tangents = new List<Vector4>();
            m.GetTangents(tangents);
            Vector3[] pdeltaPositions = new Vector3[m.vertexCount];
            Vector3[] pdeltaNormals = new Vector3[m.vertexCount];
            Vector3[] pdeltaTangents = new Vector3[m.vertexCount];
            m.GetBlendShapeFrameVertices(packID, 0, pdeltaPositions, pdeltaNormals, pdeltaTangents);
            List<Vector4> uv = new List<Vector4>();
            m.GetUVs(1,uv);

            for (int i = 0; i < uv.Count; i++) { // We bake positions using the normals and tangents as an orthogonal basis.
                Vector3 packrat = ToTangentSpace(pdeltaPositions[i], normals[i], tangents[i]);
                switch(bakeAttribute) {
                    case BakeAttribute.X: uv[i] = new Vector4(uv[i].x, uv[i].y, uv[i].z, packrat.x); break;
                    case BakeAttribute.Y: uv[i] = new Vector4(uv[i].x, uv[i].y, uv[i].z, packrat.y); break;
                    case BakeAttribute.Z: uv[i] = new Vector4(uv[i].x, uv[i].y, uv[i].z, packrat.z); break;
                }
            }
            m.SetUVs(1, uv);
        }

        void Bake(Mesh m, int blendShapeID, BakeType type, int dest, int packID, BakeAttribute bakeAttribute) {
            List<Vector3> normals = new List<Vector3>();
            m.GetNormals(normals);
            List<Vector4> tangents = new List<Vector4>();
            m.GetTangents(tangents);
            Vector3[] deltaPositions = new Vector3[m.vertexCount];
            Vector3[] deltaNormals = new Vector3[m.vertexCount];
            Vector3[] deltaTangents = new Vector3[m.vertexCount];
            m.GetBlendShapeFrameVertices(blendShapeID, 0, deltaPositions, deltaNormals, deltaTangents);

            Vector3[] pdeltaPositions = new Vector3[m.vertexCount];
            Vector3[] pdeltaNormals = new Vector3[m.vertexCount];
            Vector3[] pdeltaTangents = new Vector3[m.vertexCount];
            m.GetBlendShapeFrameVertices(packID, 0, pdeltaPositions, pdeltaNormals, pdeltaTangents);
            List<Vector4> uv = new List<Vector4>();
            switch (type) {
                case BakeType.DeltaPosition:
                    for (int i = 0; i < m.vertexCount; i++) { // We bake positions using the normals and tangents as an orthogonal basis.
                        Vector3 XYZ = ToTangentSpace(deltaPositions[i], normals[i], tangents[i]);
                        Vector3 packrat = ToTangentSpace(pdeltaPositions[i], normals[i], tangents[i]);
                        switch(bakeAttribute) {
                            case BakeAttribute.X: uv.Add(new Vector4(XYZ.x, XYZ.y, XYZ.z, packrat.x)); break;
                            case BakeAttribute.Y: uv.Add(new Vector4(XYZ.x, XYZ.y, XYZ.z, packrat.y)); break;
                            case BakeAttribute.Z: uv.Add(new Vector4(XYZ.x, XYZ.y, XYZ.z, packrat.z)); break;
                        }
                    }
                    break;
            }
            m.SetUVs(dest, uv);
        }

        void Bake(Mesh m, int blendShapeID, BakeType type, int dest ) {
            List<Vector3> normals = new List<Vector3>();
            m.GetNormals(normals);
            List<Vector4> tangents = new List<Vector4>();
            m.GetTangents(tangents);
            Vector3[] deltaPositions = new Vector3[m.vertexCount];
            Vector3[] deltaNormals = new Vector3[m.vertexCount];
            Vector3[] deltaTangents = new Vector3[m.vertexCount];
            m.GetBlendShapeFrameVertices(blendShapeID, 0, deltaPositions, deltaNormals, deltaTangents);
            List<Vector3> uv = new List<Vector3>();
            switch (type) {
                case BakeType.DeltaPosition:
                    for (int i = 0; i < m.vertexCount; i++) { // We bake positions using the normals and tangents as an orthogonal basis.
                        Vector3 XYZ = ToTangentSpace(deltaPositions[i], normals[i], tangents[i]);
                        uv.Add(XYZ);
                    }
                    break;
            }

            m.SetUVs(dest, uv);
        }
    }
}
#endif