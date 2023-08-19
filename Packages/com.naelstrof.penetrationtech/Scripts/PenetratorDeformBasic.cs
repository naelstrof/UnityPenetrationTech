using System.Collections;
using System.Collections.Generic;
using PenetrationTech;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;


namespace PenetrationTech {
#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PenetratorDeformBasic))]
    public class PenetratorSimpleEditor : PenetratorEditor {
    }
#endif
    [ExecuteAlways]
    public class PenetratorDeformBasic : Penetrator {
        [SerializeField] [Tooltip("The middle-most pose target of the jiggle chain.")]
        protected Transform middlePoseTarget;
        [SerializeField] [Tooltip("The tip-most pose target of the jiggle chain.")]
        protected Transform tipTarget;
        protected override void ConstructPathForIdle(ICollection<Vector3> outputPoints) {
            outputPoints.Clear();
            var rootBonePosition = rootBone.position;
            float worldLength = GetWorldLength();
            var tipProjected = rootBonePosition + rootBone.TransformDirection(localRootForward) * (worldLength * 1.1f);
            Vector3 realTipPoint = tipTarget.position;
            //Vector3 tipTangent = (realTipPoint - middlePoseTarget.position).normalized*worldLength;
            outputPoints.Add(rootBonePosition);
            outputPoints.Add(middlePoseTarget.position);
            outputPoints.Add(realTipPoint);
            outputPoints.Add(realTipPoint+(realTipPoint-middlePoseTarget.position).normalized*worldLength);
        }

        protected override void CheckValid() {
            base.CheckValid();
            try {
                AssertValid(tipTarget != null, "Tip target cannot be null.");
                AssertValid(middlePoseTarget != null, "Middle target cannot be null.");
                foreach (var rendererMask in GetTargetRenderers()) {
                    if (!(rendererMask.renderer is SkinnedMeshRenderer skinnedRenderer)) continue;
                    foreach (var checkBone in skinnedRenderer.bones) {
                        AssertValid(tipTarget != checkBone, "You shouldn't deform the skinned mesh renderer bones. Use blank transforms as a tip target instead.");
                        AssertValid(middlePoseTarget != checkBone, "You shouldn't deform the skinned mesh renderer bones. Use blank transforms as a middle tip target instead.");
                    }
                }
            } catch (PenetratorValidationException error) {
                SetError($"{error.Message}\n\n{error.StackTrace}");
            }
        }
    }
}
