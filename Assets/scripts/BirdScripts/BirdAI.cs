using System.Collections;
using UnityEngine;

public class BirdAI : MonoBehaviour
{
    private enum BirdState { Patrol, LockOn, Dive, Return }
    private BirdState currentState = BirdState.Patrol;

    [Header("AI設定")]
    public Transform patrolAnchor; // 巡回する中心点（縄張りエリア）
    public float patrolSpeed = 60f; // 1秒間の回転角度
    public float diveSpeed = 15f;   // 突撃スピード
    public float returnSpeed = 5f;  // 帰還スピード
    public float lockOnTime = 2f;   // 狙いを定める時間

    private Transform playerTarget;
    private Vector3 targetDivePosition;
    private Vector3 defaultLocalPosition;

    void Start()
    {
        // 開始時のアンカーからの相対位置を記憶しておく
        defaultLocalPosition = transform.localPosition;
    }

    // private void OnTriggerEnter(Collider other)
    // {
    //     // プレイヤーが縄張り（トリガー）に入ったら
    //     if (other.CompareTag("Player") || other.gameObject.name.Contains("Player"))
    //     {

    //         // 鳥にターゲット（プレイヤー）の情報を渡して攻撃開始
    //         StartAttack(other.transform);
    //     }
    // }

    void Update()
    {
        switch (currentState)
        {
            case BirdState.Patrol:
                // アンカーを回して、その子である鳥を円移動させる
                if (patrolAnchor != null)
                {
                    patrolAnchor.Rotate(Vector3.up * patrolSpeed * Time.deltaTime);
                }
                break;

            case BirdState.Return:
                // アンカーが回っていても、元の相対位置へゆっくり戻る
                Vector3 targetPos = patrolAnchor.TransformPoint(defaultLocalPosition);
                transform.position = Vector3.MoveTowards(transform.position, targetPos, returnSpeed * Time.deltaTime);

                // 進行方向を向く
                Vector3 dir = (targetPos - transform.position).normalized;
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
                }

                // 元の位置にほぼ到着したら巡回再開
                if (Vector3.Distance(transform.position, targetPos) < 0.1f)
                {
                    transform.parent = patrolAnchor; // 再び子オブジェクトにする
                    transform.localPosition = defaultLocalPosition;
                    transform.localRotation = Quaternion.Euler(180, 0, 0); // 向きをリセット
                    currentState = BirdState.Patrol;
                }
                break;

            case BirdState.Dive:
                // ロックオンした座標へ一直線に突撃
                transform.position = Vector3.MoveTowards(transform.position, targetDivePosition, diveSpeed * Time.deltaTime);

                // 突撃地点に到達したら帰還モードへ
                if (Vector3.Distance(transform.position, targetDivePosition) < 0.1f)
                {
                    currentState = BirdState.Return;
                }
                break;
        }
    }

    // 縄張りスクリプトから呼ばれる関数
    public void StartAttack(Transform target)
    {
        // 巡回中の時だけ反応する（突撃中や帰還中は無視）
        if (currentState == BirdState.Patrol)
        {
            playerTarget = target;
            StartCoroutine(LockOnCoroutine());
        }
    }

    private IEnumerator LockOnCoroutine()
    {
        currentState = BirdState.LockOn;

        // アンカーの子から外れて、その場に留まる（一緒に回転しなくなる）
        transform.parent = null;

        float elapsed = 0f;
        while (elapsed < lockOnTime)
        {
            if (playerTarget != null)
            {
                // プレイヤーの方をじわっと向く
                Vector3 dir = (playerTarget.position - transform.position).normalized;
                if (dir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 2秒経過後、その時点でのプレイヤーの現在地を突撃目標に設定
        if (playerTarget != null)
        {
            targetDivePosition = playerTarget.position;
            currentState = BirdState.Dive;
        }
        else
        {
            currentState = BirdState.Return;
        }
    }
}