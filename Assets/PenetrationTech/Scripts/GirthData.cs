using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GirthData {
    // Not using animation curves, just in case we need to conveniently send it to the GPU.
    private List<float> girthLUT;
    private List<Vector2> offsetLUT;
    public GirthData(Renderer renderer, Mesh mesh, Transform root, Vector3 localDickRoot, Vector3 localDickForward, Vector3 localDickUp) {
        Vector3 localDickRight = Vector3.Cross(localDickForward, localDickUp);
        Matrix4x4 changeOfBasis = new Matrix4x4();
        changeOfBasis.SetRow(0, localDickRight);
        changeOfBasis.SetRow(1, localDickUp);
        changeOfBasis.SetRow(2, localDickForward);
        changeOfBasis[3,3] = 1f;
        Vector3 worldSpaceDickRoot = root.TransformPoint(localDickRoot);
        Vector3 localSpaceDickRoot;
        if (renderer is SkinnedMeshRenderer) {
            localSpaceDickRoot = (renderer as SkinnedMeshRenderer).rootBone.InverseTransformPoint(worldSpaceDickRoot);
        } else {
            localSpaceDickRoot = renderer.transform.InverseTransformPoint(worldSpaceDickRoot);
        }

        Vector3 dickSpaceDickRoot = changeOfBasis.MultiplyPoint(localSpaceDickRoot);
        foreach(Vector3 vertexPosition in mesh.vertices) {
            
        }

    }
}
