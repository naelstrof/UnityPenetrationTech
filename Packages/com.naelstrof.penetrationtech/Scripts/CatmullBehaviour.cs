using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PenetrationTech;

public abstract class CatmullBehaviour : MonoBehaviour {
    public abstract CatmullSpline GetPath();
}
