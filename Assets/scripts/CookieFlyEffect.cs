using System;
using System.Collections;
using UnityEngine;

public sealed class CookieFlyEffect : MonoBehaviour
{
    [Header("飛び散り")]
    [SerializeField, Min(0.01f)]
    private float scatterDuration = 0.22f;

    [SerializeField]
    private Vector2 scatterDistanceRange = new Vector2(0.3f, 0.7f);

    [SerializeField]
    private Vector2 scatterHeightRange = new Vector2(0.4f, 0.9f);

    [Header("吸い込み")]
    [SerializeField, Min(0.01f)]
    private float homingDuration = 0.45f;

    [SerializeField]
    private AnimationCurve homingCurve =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("回転")]
    [SerializeField]
    private Vector3 rotationSpeed =
        new Vector3(360f, 540f, 240f);

    private Coroutine playRoutine;

    public void Play(Transform target, Action onArrived)
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayRoutine(target, onArrived));
    }

    private IEnumerator PlayRoutine(Transform target, Action onArrived)
    {
        Vector3 initialScale = transform.localScale;
        Vector3 scatterStart = transform.position;

        Vector2 direction2D = UnityEngine.Random.insideUnitCircle;

        if (direction2D.sqrMagnitude < 0.01f)
        {
            direction2D = Vector2.right;
        }

        direction2D.Normalize();

        float scatterDistance = UnityEngine.Random.Range(
            scatterDistanceRange.x,
            scatterDistanceRange.y
        );

        float scatterHeight = UnityEngine.Random.Range(
            scatterHeightRange.x,
            scatterHeightRange.y
        );

        Vector3 horizontalDirection =
            new Vector3(direction2D.x, 0f, direction2D.y);

        Vector3 scatterEnd =
            scatterStart +
            horizontalDirection * scatterDistance +
            Vector3.up * scatterHeight * 0.3f;

        float elapsed = 0f;

        // 飼い主の手元から少し飛び散る
        while (elapsed < scatterDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scatterDuration);

            Vector3 position = Vector3.Lerp(
                scatterStart,
                scatterEnd,
                t
            );

            // 放物線状に少し上へ浮かせる
            position += Vector3.up * (4f * t * (1f - t) * scatterHeight);

            transform.position = position;
            transform.localScale = Vector3.Lerp(
                initialScale * 0.6f,
                initialScale,
                t
            );

            RotateCookie();
            yield return null;
        }

        Vector3 homingStart = transform.position;
        elapsed = 0f;

        // 移動中の犬を追いかける
        while (elapsed < homingDuration && target != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / homingDuration);
            float easedT = Mathf.Clamp01(homingCurve.Evaluate(t));

            transform.position = Vector3.Lerp(
                homingStart,
                target.position,
                easedT
            );

            // 到着直前に小さくする
            float shrinkT = Mathf.InverseLerp(0.65f, 1f, t);
            transform.localScale = Vector3.Lerp(
                initialScale,
                Vector3.zero,
                shrinkT
            );

            RotateCookie();
            yield return null;
        }

        // targetが途中で消えても表示数の同期は進める
        onArrived?.Invoke();

        Destroy(gameObject);
    }

    private void RotateCookie()
    {
        transform.Rotate(
            rotationSpeed * Time.deltaTime,
            Space.Self
        );
    }
}