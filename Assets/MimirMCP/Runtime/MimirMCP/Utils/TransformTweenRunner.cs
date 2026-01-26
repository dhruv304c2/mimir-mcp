using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MimirMCP.Utils
{
    internal static class TransformTweenRunner
    {
        enum TweenKind
        {
            Position,
            Rotation,
            Scale,
        }

        static readonly Dictionary<(Transform, TweenKind), CancellationTokenSource> ActiveTweens =
            new();

        public static UniTask RunPositionTween(Transform target, Vector3 goal, float duration)
        {
            return RunTween(
                target,
                goal,
                duration,
                TweenKind.Position,
                t => t.position,
                (t, value) => t.position = value
            );
        }

        public static UniTask RunRotationTween(Transform target, Vector3 goalEuler, float duration)
        {
            return RunTween(
                target,
                goalEuler,
                duration,
                TweenKind.Rotation,
                t => t.eulerAngles,
                (t, value) => t.eulerAngles = value
            );
        }

        public static UniTask RunScaleTween(Transform target, Vector3 goalScale, float duration)
        {
            return RunTween(
                target,
                goalScale,
                duration,
                TweenKind.Scale,
                t => t.localScale,
                (t, value) => t.localScale = value
            );
        }

        static UniTask RunTween(
            Transform target,
            Vector3 goal,
            float duration,
            TweenKind kind,
            Func<Transform, Vector3> getter,
            Action<Transform, Vector3> setter
        )
        {
            if (target == null)
            {
                return UniTask.CompletedTask;
            }

            if (duration <= 0f)
            {
                setter(target, goal);
                CancelTween(target, kind);
                return UniTask.CompletedTask;
            }

            CancelTween(target, kind);
            var cts = new CancellationTokenSource();
            ActiveTweens[(target, kind)] = cts;
            return RunTweenAsync(target, goal, duration, kind, getter, setter, cts);
        }

        static async UniTask RunTweenAsync(
            Transform target,
            Vector3 goal,
            float duration,
            TweenKind kind,
            Func<Transform, Vector3> getter,
            Action<Transform, Vector3> setter,
            CancellationTokenSource cts
        )
        {
            var start = getter(target);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                if (target == null || cts.IsCancellationRequested)
                {
                    Cleanup(target, kind, cts);
                    return;
                }

                await UniTask.Yield(PlayerLoopTiming.Update);
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                setter(target, Vector3.Lerp(start, goal, t));
            }

            if (target != null && !cts.IsCancellationRequested)
            {
                setter(target, goal);
            }

            Cleanup(target, kind, cts);
        }

        static void CancelTween(Transform target, TweenKind kind)
        {
            if (target == null)
            {
                return;
            }

            if (ActiveTweens.TryGetValue((target, kind), out var existing))
            {
                existing.Cancel();
                ActiveTweens.Remove((target, kind));
                existing.Dispose();
            }
        }

        static void Cleanup(Transform target, TweenKind kind, CancellationTokenSource cts)
        {
            if (target == null)
            {
                cts.Dispose();
                return;
            }

            if (
                ActiveTweens.TryGetValue((target, kind), out var existing)
                && existing == cts
            )
            {
                ActiveTweens.Remove((target, kind));
            }

            cts.Dispose();
        }
    }
}
