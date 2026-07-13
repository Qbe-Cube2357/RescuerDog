using UnityEngine;
using TMPro;

public class ResultManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI resultScoreText;

    private void Start()
    {

        resultScoreText.text = $"Final Score: {GameManager.currentScore}";


        // GameManagerが存在していれば、保存されているスコアを取得して表示
        // if (GameManager.Instance != null)
        // {
        //     resultScoreText.text = $"Final Score: {GameManager.Instance.Score}";

        //     // もしタイトルに戻るなどしてゲームをループさせる場合は、
        //     // ここで GameManager を破棄するか、スコアをリセットする処理を入れると良いです。
        //     // Destroy(GameManager.Instance.gameObject); 
        // }
        // else
        // {
        //     resultScoreText.text = "Score: --";
        // }
    }
}