using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    [SerializeField]
    private int sceneIndex = 0;
    void Start() {
        StartCoroutine(WaitAndThenLoad());
    }

    IEnumerator WaitAndThenLoad() {
        yield return new WaitForSeconds(10f);
        SceneManager.LoadScene(sceneIndex);
    }
}
