using System.Collections;
using System.Collections.Generic;
using PenetrationTech;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

namespace PenetrationTech {
    [System.Serializable] [PenetratorListener(typeof(PenetratorPassiveAudioListener), "Passive audio listener")]
    public class PenetratorPassiveAudioListener : PenetratorListener {
        private float lastDepth;
        [SerializeField]
        private AudioClip clip;
        [SerializeField]
        private AudioMixerGroup audioGroup;

        [Range(0f,1f)]
        [SerializeField] private float volume = 1f;
        private AudioSource source;

        public override void OnEnable(Penetrator p) {
            base.OnEnable(p);
            source = p.gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = volume;
            source.maxDistance = 7f;
            source.minDistance = 0f;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.outputAudioMixerGroup = audioGroup;
            source.enabled = false;
        }

        public override void OnDisable() {
            Object.Destroy(source);
        }

        public override void Update() {
            base.Update();
            source.volume = Mathf.MoveTowards(source.volume, 0f, Time.deltaTime*4f*volume);
            source.pitch = Mathf.Lerp(0.5f, 1f, source.volume / volume);
            if (source.volume == 0f && lastDepth == 0f) {
                source.enabled = false;
            }
        }

        protected override void OnPenetrationDepthChange(float depth) {
            if (!source.enabled) {
                source.timeSamples = UnityEngine.Random.Range(0, clip.samples);
                source.enabled = true;
            }
            float diff = Mathf.Abs(depth - lastDepth);
            source.volume = Mathf.Min(source.volume+diff*50f*volume, volume);
            lastDepth = depth;
        }
    }
}
