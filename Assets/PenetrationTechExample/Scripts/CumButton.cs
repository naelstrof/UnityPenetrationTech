using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PenetrationTech;

public class CumButton : MonoBehaviour {
    public void Cum() {
        foreach(var dick in UnityEngine.Object.FindObjectsOfType<Penetrator>()) {
            dick.Cum();
        }
    }
}
