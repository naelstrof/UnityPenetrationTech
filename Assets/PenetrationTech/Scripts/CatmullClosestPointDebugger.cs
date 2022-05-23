using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatmullClosestPointDebugger : MonoBehaviour {
    [SerializeField]
    CatmullBehaviour targetPath;
    void Update() {
        float t = targetPath.GetPath().GetClosestTimeFromPosition(transform.position);
        float dist = targetPath.GetPath().GetDistanceFromTime(t);
        Debug.DrawLine(transform.position, targetPath.GetPath().GetPositionFromT(t), Color.black);
        Debug.DrawLine(transform.position, targetPath.GetPath().GetPositionFromDistance(dist), Color.green);
    }
}
