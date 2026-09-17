using UnityEngine;

public class BirdTerritory : MonoBehaviour
{
    [Header("連携する鳥のAI")]
    public BirdAI bird; // インスペクターで鳥本体をセットします

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーが縄張り（トリガー）に入ったら
        if (other.CompareTag("Player") || other.gameObject.name.Contains("Player"))
        {
            if (bird != null)
            {
                // 鳥にターゲット（プレイヤー）の情報を渡して攻撃開始
                bird.StartAttack(other.transform);
            }
        }
    }
}