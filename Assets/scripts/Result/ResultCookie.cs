using UnityEngine;

public class ResultCookie : MonoBehaviour
{
    // 湧かせたいオブジェクト（プレハブ）
    [SerializeField] private GameObject cookie/*, dog*/;

    // 湧かせる間隔（秒）
    [SerializeField] private float spawnInterval = 0f;
    [SerializeField] private float spawnIntervalM = 50f;

    //クッキーが出る時間
    [SerializeField] private float cookietime = 5f;

    // タイマー管理用
    private float timer1, timer2, c = 0f;

    void Start() {
        if (GameManager.currentScore != 0)
        {
            spawnInterval = 1f / (GameManager.currentScore * spawnIntervalM);
        }
        else
        {
            spawnInterval = 1f / (1 * spawnIntervalM);
        }
    }

    //GameManager.currentScore //ゲーム本編のスコア

    void Update()
    {
        // 時間をカウント
        timer1 += Time.deltaTime;
        timer2 += Time.deltaTime;

        if (timer1 <= cookietime)
        {
            // 設定した間隔を超えたらオブジェクトを生成
            if (timer2 >= spawnInterval)
            {
                SpawnObject(cookie);
                timer2 = 0f; // タイマーをリセット
            }
        }
        //else if(c == 0f)
        //{
        //    SpawnObject(dog);
        //    c++;
        //}

        
    }

    void SpawnObject(GameObject x)
    {
        // スクリプトが配置されている位置（このオブジェクトの位置）に生成
        Instantiate(x, transform.position, transform.rotation);
    }
}
