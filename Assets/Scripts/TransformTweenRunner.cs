using UnityEngine;

public class TransformTweenRunner : MonoBehaviour
{
    struct TweenState
    {
        public bool Active;
        public Vector3 Start;
        public Vector3 Target;
        public float Duration;
        public float Elapsed;

        public void Begin(Vector3 currentValue, Vector3 targetValue, float duration)
        {
            Start = currentValue;
            Target = targetValue;
            Duration = Mathf.Max(duration, 0f);
            Elapsed = 0f;
            Active = Duration > 0f;
        }

        public float Step(float deltaTime)
        {
            if (!Active)
            {
                return 1f;
            }

            if (Duration <= Mathf.Epsilon)
            {
                Active = false;
                return 1f;
            }

            Elapsed += deltaTime;
            if (Elapsed >= Duration)
            {
                Active = false;
                return 1f;
            }

            return Mathf.Clamp01(Elapsed / Duration);
        }
    }

    TweenState _positionTween;
    TweenState _rotationTween;
    TweenState _scaleTween;

    public static TransformTweenRunner GetOrCreate(Transform target)
    {
        var runner = target.GetComponent<TransformTweenRunner>();
        if (runner == null)
        {
            runner = target.gameObject.AddComponent<TransformTweenRunner>();
        }
        runner.enabled = true;
        return runner;
    }

    public void StartPositionTween(Vector3 target, float duration)
    {
        _positionTween.Begin(transform.position, target, duration);
        if (!_positionTween.Active)
        {
            transform.position = target;
        }
    }

    public void StartRotationTween(Vector3 targetEuler, float duration)
    {
        _rotationTween.Begin(transform.eulerAngles, targetEuler, duration);
        if (!_rotationTween.Active)
        {
            transform.eulerAngles = targetEuler;
        }
    }

    public void StartScaleTween(Vector3 targetScale, float duration)
    {
        _scaleTween.Begin(transform.localScale, targetScale, duration);
        if (!_scaleTween.Active)
        {
            transform.localScale = targetScale;
        }
    }

    void Update()
    {
        var delta = Time.deltaTime;

        if (_positionTween.Active)
        {
            var t = Mathf.Clamp01(_positionTween.Step(delta));
            transform.position = Vector3.Lerp(_positionTween.Start, _positionTween.Target, t);
            if (!_positionTween.Active)
            {
                transform.position = _positionTween.Target;
            }
        }

        if (_rotationTween.Active)
        {
            var t = Mathf.Clamp01(_rotationTween.Step(delta));
            transform.eulerAngles = Vector3.Lerp(_rotationTween.Start, _rotationTween.Target, t);
            if (!_rotationTween.Active)
            {
                transform.eulerAngles = _rotationTween.Target;
            }
        }

        if (_scaleTween.Active)
        {
            var t = Mathf.Clamp01(_scaleTween.Step(delta));
            transform.localScale = Vector3.Lerp(_scaleTween.Start, _scaleTween.Target, t);
            if (!_scaleTween.Active)
            {
                transform.localScale = _scaleTween.Target;
            }
        }
    }
}
