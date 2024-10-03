using System.Collections;
using UnityEngine;

namespace FixThisHouse
{
    public class Animations : MonoBehaviour, IInitializer
    {
        public static Animations Instance { get; private set; }

        [SerializeField, Header("Touched")]
        private AnimationCurve touchedXZ;
        [SerializeField]
        private AnimationCurve touchedY;

        [SerializeField, Header("Pop up")]
        private AnimationCurve popupXZ;
        [SerializeField]
        private AnimationCurve popupY;

        [SerializeField, Header("Hide")]
        private AnimationCurve hideXZ;
        [SerializeField]
        private AnimationCurve hideY;

        public void InitializeAfter()
        {
        }

        public void InitializeSelf()
        {
            Instance = this;
        }

        public IEnumerator Touched(Transform transform, float duration)
        {
            yield return AnimateY(transform, duration, touchedXZ, touchedY);
        }

        public IEnumerator Popup(Transform transform, float duration)
        {
            yield return AnimateY(transform, duration, popupXZ, popupY);
        }

        public IEnumerator Popin(Transform transform, float duration)
        {
            yield return AnimateInvertY(transform, duration, popupXZ, popupY);
        }

        public IEnumerator Hide(Transform transform, float duration)
        {
            yield return AnimateY(transform, duration, hideXZ, hideY);
        }

        private IEnumerator AnimateY(Transform transform, float duration, AnimationCurve curveXZ, AnimationCurve curveY)
        {
            Timer timer = new();
            timer.Activate(duration);
            var scale = Vector3.one;

            scale.y = curveY.Evaluate(0f);
            scale.x = scale.z = curveXZ.Evaluate(0f);
            transform.localScale = scale;

            while (!timer.IsFinished)
            {
                yield return null;

                var progress = timer.Progress;
                scale.y = curveY.Evaluate(progress);
                scale.x = scale.z = curveXZ.Evaluate(progress);
                transform.localScale = scale;
            }
        }

        private IEnumerator AnimateInvertY(Transform transform, float duration, AnimationCurve curveXZ, AnimationCurve curveY)
        {
            Timer timer = new();
            timer.Activate(duration);
            var scale = Vector3.one;

            scale.y = curveY.Evaluate(1f);
            scale.x = scale.z = curveXZ.Evaluate(1f);
            transform.localScale = scale;

            while (!timer.IsFinished)
            {
                yield return null;

                var progress = 1f - timer.Progress;
                scale.y = curveY.Evaluate(progress);
                scale.x = scale.z = curveXZ.Evaluate(progress);
                transform.localScale = scale;
            }
        }
    }
}
