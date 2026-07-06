using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToyFollower : MonoBehaviour
{
    private Transform target;      // 追いかける対象（プレイヤー、または前のおもちゃ）
    private float followSpeed = 6f; // 追従するスピード
    private float distanceOffset = 0.6f; // 前のオブジェクトとの適切な距離

    // 過去の位置と回転を記録するリスト
    private List<Vector3> positionHistory = new List<Vector3>();
    private List<Quaternion> rotationHistory = new List<Quaternion>();


    [Header("エフェクト設定")]
    [SerializeField] public GameObject splashEffectPrefab; // 着水エフェクトのPrefab

    private bool isFollowing = false;

    [Header("納品時のエフェクト設定")]
    public GameObject deliveryEffectPrefab; // 納品時に出すパーティクルなどのプレハブ（空でも動きます）
    private bool isDelivering = false;     // 納品演出中かどうかのフラグ

    // 追従を開始するときに外部（Player）から呼び出される関数
    public void StartFollowing(Transform newTarget)
    {
        target = newTarget;
        isFollowing = true;

        // おもちゃを手に入れた時のエフェクト
        Vector3 splashPosition = new Vector3(transform.position.x, 0.0f, transform.position.z);
        GameObject splash = Instantiate(splashEffectPrefab, splashPosition, Quaternion.identity);
        Destroy(splash, 2f);

        // おもちゃの当たり判定（トリガー）をオフにする（集めた後はプレイヤーとぶつからないようにするため）
        if (TryGetComponent<Collider>(out Collider col))
        {
            col.enabled = false;
        }
    }

    void Update()
    {
        // 納品演出中は追従計算をスキップする
        if (isDelivering) return;

        if (!isFollowing || target == null) return;

        // 自分の現在の位置と回転を履歴に記録（後ろのおもちゃ用）
        positionHistory.Insert(0, transform.position);
        rotationHistory.Insert(0, transform.rotation);

        // 履歴が長くなりすぎないように制限（最大100件程度）
        if (positionHistory.Count > 100)
        {
            positionHistory.RemoveAt(positionHistory.Count - 1);
            rotationHistory.RemoveAt(rotationHistory.Count - 1);
        }

        // ターゲット（前を進むもの）との距離を計算
        float distance = Vector3.Distance(transform.position, target.position);

        // 設定した距離（distanceOffset）より離れている場合だけ、近づくように移動する
        if (distance > distanceOffset)
        {
            // ターゲットに向けてなめらかに移動・回転させる
            transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * followSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, Time.deltaTime * followSpeed);
        }
    }

    // 後ろに繋がるおもちゃが、このおもちゃの「過去の位置」を取得するための関数
    public Vector3 GetBufferedPosition(int delayFrames)
    {
        int index = Mathf.Min(delayFrames, positionHistory.Count - 1);
        return index >= 0 ? positionHistory[index] : transform.position;
    }

    public Quaternion GetBufferedRotation(int delayFrames)
    {
        int index = Mathf.Min(delayFrames, rotationHistory.Count - 1);
        return index >= 0 ? rotationHistory[index] : transform.rotation;
    }

    public void Deliver(System.Action onComplete)
    {
        if (isDelivering) return;
        StartCoroutine(DeliverCoroutine(onComplete));
    }

    private IEnumerator DeliverCoroutine(System.Action onComplete)
    {
        isDelivering = true;
        isFollowing = false;

        // エフェクト（ParticleSystemなど）があれば生成
        if (deliveryEffectPrefab != null)
        {
            Vector3 effectPosition = new Vector3(transform.position.x, 0.0f, transform.position.z);

            GameObject ExploRffect = Instantiate(deliveryEffectPrefab, effectPosition, Quaternion.identity);

            Destroy(ExploRffect, 2f);
        }

        // おもちゃが変形（だんだん小さくなる）しながら消える演出
        Vector3 initialScale = transform.localScale;
        float duration = 0.5f; // 演出にかける時間（秒）
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // サイズを徐々に0に近づける
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);

            // オプション：少し上に浮き上がりながら消えるようにするとより綺麗です
            transform.position += Vector3.up * Time.deltaTime * 1f;

            yield return null;
        }

        // 完全に消滅する前に、呼び出し元（納品エリア）に通知する
        onComplete?.Invoke();

        // オブジェクトを削除
        Destroy(gameObject);
    }

    public void Drop()
    {
        isFollowing = false;
        target = null;

        // 履歴をクリアして、次に拾われた時に変な場所からワープしないようにする
        positionHistory.Clear();
        rotationHistory.Clear();

        // 再びプレイヤーが拾えるように当たり判定を戻す
        if (TryGetComponent<Collider>(out Collider col))
        {
            col.enabled = true;
        }

        // 物理演算（Rigidbody）を使って周りに散らばらせる
        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            // 追従中は物理演算をオフにしている場合を考慮してオンに戻す
            rb.isKinematic = false;

            // ランダムな方向（上方向＋前後左右）と力を計算
            Vector3 randomDir = new Vector3(Random.Range(2f, 3f), 3f, Random.Range(2f, 3f)).normalized;
            float dropForce = Random.Range(6f, 9f); // 飛び散る強さ

            // 瞬間的な力（Impulse）を加えてポーンと飛ばす
            rb.AddForce(randomDir * dropForce, ForceMode.Impulse);
        }
    }
}