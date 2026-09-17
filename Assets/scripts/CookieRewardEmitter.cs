using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class CookieRewardEmitter : MonoBehaviour
{
    [Header("参照")]
    [SerializeField]
    private GameObject cookiePrefab;

    [SerializeField]
    private Transform cookieSpawnPoint;

    [Header("生成間隔")]
    [SerializeField, Min(0f)]
    private float spawnInterval = 0.06f;

    private readonly Queue<PendingCookie> pendingCookies =
        new Queue<PendingCookie>();

    private Coroutine spawnRoutine;

    private struct PendingCookie
    {
        public Transform Target;
        public Action OnArrived;

        public PendingCookie(Transform target, Action onArrived)
        {
            Target = target;
            OnArrived = onArrived;
        }
    }

    /// <summary>
    /// Cookie演出を開始できた場合はtrue。
    /// </summary>
    public bool Emit(
        int cookieCount,
        Transform target,
        Action onCookieArrived
    )
    {
        if (cookieCount <= 0)
        {
            return false;
        }

        if (cookiePrefab == null ||
            cookieSpawnPoint == null ||
            target == null)
        {
            Debug.LogWarning(
                "CookieRewardEmitterの参照が設定されていません。",
                this
            );

            return false;
        }

        for (int i = 0; i < cookieCount; i++)
        {
            pendingCookies.Enqueue(
                new PendingCookie(target, onCookieArrived)
            );
        }

        if (spawnRoutine == null)
        {
            spawnRoutine = StartCoroutine(SpawnRoutine());
        }

        return true;
    }

    private IEnumerator SpawnRoutine()
    {
        while (pendingCookies.Count > 0)
        {
            PendingCookie pending = pendingCookies.Dequeue();

            if (pending.Target == null)
            {
                pending.OnArrived?.Invoke();
                continue;
            }

            GameObject cookieObject = Instantiate(
                cookiePrefab,
                cookieSpawnPoint.position,
                cookieSpawnPoint.rotation
            );

            CookieFlyEffect flyEffect =
                cookieObject.GetComponent<CookieFlyEffect>();

            if (flyEffect != null)
            {
                flyEffect.Play(
                    pending.Target,
                    pending.OnArrived
                );
            }
            else
            {
                Debug.LogWarning(
                    "CookieプレハブにCookieFlyEffectがありません。",
                    cookieObject
                );

                pending.OnArrived?.Invoke();
                Destroy(cookieObject);
            }

            if (spawnInterval > 0f)
            {
                yield return new WaitForSeconds(spawnInterval);
            }
            else
            {
                yield return null;
            }
        }

        spawnRoutine = null;
    }
}