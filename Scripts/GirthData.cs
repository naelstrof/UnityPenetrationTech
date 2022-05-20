using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class GirthData {
    [SerializeField]
    private RenderTexture texture;
    [SerializeField]
    private float maxLength;
    [SerializeField]
    private float maxGirth;
    public GirthData(Renderer renderer, Transform root, Vector3 localDickRoot, Vector3 localDickForward, Vector3 localDickUp) {
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
        maxGirth = 0f;
        maxLength = 0f;
        foreach(Vector3 vertexPosition in mesh.vertices) {
            //Vector3 dickSpacePosition = changeOfBasis.MultiplyPoint(vertexPosition);
            float length = Vector3.Dot(localDickForward, vertexPosition-localSpaceDickRoot);
            float girth = Vector3.Distance(vertexPosition,(localSpaceDickRoot+localDickForward*length));
            maxGirth = Mathf.Max(girth, maxGirth);
            maxLength = Mathf.Max(length, maxLength);
        }
        mat.SetFloat("_MaxLength", maxLength);
        mat.SetFloat("_MaxGirth", maxGirth);
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

    }
}
