using FixThisHouse.Outlines;
using System.Collections;
using UnityEngine;

namespace FixThisHouse.Fun
{
    public class Hydrant : MonoBehaviour, IHoverable
    {
        public bool IsDraggable => false;

        [SerializeField]
        private ParticleSystem fountain;
        [SerializeField]
        private AudioSource audioSource;
        [SerializeField]
        private int actionsToActivate = 3;
        [SerializeField]
        private float duration = 5f;
        [SerializeField]
        private float durationIncrement = 2f;
        [SerializeField]
        private float collisionDelay = 0.25f;
        [SerializeField]
        private float soundFading = 0.5f;
        [SerializeField]
        private LayerMask triggerMask = (1 << Layers.Drag) | (1 << Layers.Dragging) | (1 << Layers.Drop);

        private Outline _outline;
        private int _actions;
        private float _duration;
        private float _lastCollisionTimestamp;
        private Coroutine _animated;
        private Coroutine _sounding;

        private void Awake()
        {
            InitOutline();
            InitFountain();
        }

        private void OnEnable()
        {
            Bootstrap.OnAnimationStarted += OnAnimationStarted;
        }

        private void OnDisable()
        {
            Bootstrap.OnAnimationStarted -= OnAnimationStarted;
        }

        private void OnAnimationStarted(bool isInitial)
        {
            if (!fountain.isEmitting)
                return;

            StopAllCoroutines();

            fountain.Stop();
            StartCoroutine(Sounding(false));
        }

        private void InitFountain()
        {
            _duration = duration;
            fountain.Stop();
        }

        private void Start()
        {
            fountain.Stop();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_lastCollisionTimestamp + collisionDelay < Time.time && triggerMask.Contains(collision.collider.gameObject.layer))
            {
                _lastCollisionTimestamp = Time.time;
                IncrementAction();
            }
        }

        private void IncrementAction()
        {
            if (fountain.isEmitting)
                return;

            _actions++;
            if(_actions >= actionsToActivate)
            {
                StartCoroutine(PlayFountain());;
            }
        }

        private IEnumerator PlayFountain()
        {
            _actions = 0;
            fountain.Play();
            Unhover();

            if (_sounding != null)
                StopCoroutine(_sounding);

            StartCoroutine(Sounding(true));

            yield return new WaitForSeconds(_duration);

            _duration += durationIncrement;
            fountain.Stop();
            _sounding = StartCoroutine(Sounding(false));
        }

        private IEnumerator Sounding(bool value)
        {
            Timer timer = new();
            timer.Activate(soundFading);

            if(value)
            {
                audioSource.Play();
                var start = audioSource.volume;
                while(timer.IsActive)
                {
                    yield return null;
                    audioSource.volume = Mathf.Lerp(start, 1f, timer.Progress);
                }
            } else
            {
                var start = audioSource.volume;
                while (timer.IsActive)
                {
                    yield return null;
                    audioSource.volume = Mathf.Lerp(start, 0f, timer.Progress);
                }
                audioSource.Stop();
            }
        }

        private void InitOutline()
        {
            if (!gameObject.TryGetComponent(out _outline))
                _outline = gameObject.AddComponent<Outline>();

            _outline.OutlineColor = Color.white;
            _outline.OutlineWidth = 5f;
            _outline.enabled = false;
        }

        public void Hover()
        {
            _outline.enabled = !fountain.isEmitting;
        }

        public void Unhover()
        {
            _outline.enabled = false;
        }

        private void OnMouseDown()
        {
            if (fountain.isEmitting)
                return;

            SoundManager.Instance.Play3D(transform.position, SoundManager.Instance.Hydrant, 1f - _actions * 0.1f);

            if (_animated != null)
                StopCoroutine(_animated);

            _animated = StartCoroutine(Animations.Instance.Touched(transform, 0.25f));

            IncrementAction();
        }
    }
}
