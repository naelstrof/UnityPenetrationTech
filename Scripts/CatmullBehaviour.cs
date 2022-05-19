using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PenetrationTech;

public class CatmullBehaviour : MonoBehaviour {
    protected CatmullSpline path;
    public CatmullSpline GetPath() => path;
}
