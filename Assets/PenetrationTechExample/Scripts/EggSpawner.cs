using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PenetrationTech;

namespace PenetrationTechExample {
    public class EggSpawner : MonoBehaviour {
        public Penetrable targetPenetrable;
        [Range(0f,1f)]
        public float spawnAlongLength = 0.5f;
        [Range(-1,1f)]
        public float pushDirection = -1f;
        public GameObject penetratorPrefab;
        private List<Penetrator> penetrators = new List<Penetrator>();
        public void Start() {
            StartCoroutine(SpawnEgg());
        }
        public void Update() {
            for(int i=0;i<penetrators.Count;i++) {
                Penetrator d = penetrators[i];
                d.PushTowards(pushDirection*0.05f);
                if (!d.IsInside(0.25f)) {
                    d.Decouple(true);
                    penetrators.Remove(d);
                }
            }
        }
        public IEnumerator SpawnEgg() {
            while(true) {
                Penetrator d = GameObject.Instantiate(penetratorPrefab).GetComponentInChildren<Penetrator>();
                // Manually control penetration parameters
                d.autoPenetrate = false;
                d.canOverpenetrate = true;
                d.CoupleWith(targetPenetrable, ((spawnAlongLength*targetPenetrable.orificeLength)/d.GetLength()));
                penetrators.Add(d);
                Destroy(d.gameObject, 60f);
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f,5f));
            }
        }
    }
}
