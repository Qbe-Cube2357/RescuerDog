using UnityEngine;

public class Obstacle : MonoBehaviour
{
    // [Header("移動の設定")]
    // public float moveDistance = 2f; // 動く距離
    // public float moveSpeed = 2f;    // 動く速さ
    // public bool moveOnXAxis = true; // X軸方向に動くか（チェックを外すとZ軸方向に動く）
    // public bool moveRoundAxis = true; // 回るように動くか（チェックを外すとZ軸方向に動く）


    [Header("プレイヤーが当たった時のエフェクト")]
    public GameObject conflictEffectPrefab;

    private AudioSource AudioSource;

    public AudioClip conflictSE;

    private Vector3 startPos;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position; // 最初の位置を記憶

        AudioSource = gameObject.AddComponent<AudioSource>();//変数「audioSource」にAudioSourceコンポネントを入れる
    }

    void Update()
    {
        // Mathf.Sin を使うことで、-1 から 1 の間を滑らかに往復する数値が作れます
        // float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;

        // if (moveRoundAxis)
        // {
        //     transform.position = startPos + new Vector3(offset, 0, -offset);

        // }
        // else
        // {
        //     transform.position = startPos + new Vector3(0, 0, offset);
        // }
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            AudioSource.PlayOneShot(conflictSE); //サウンド流す

            Instantiate(conflictEffectPrefab, transform.position, Quaternion.identity);
        }
    }
}