using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryArea : MonoBehaviour
{
    [Header("納品設定")]
    public int pointsPerToy = 10;     // おもちゃ1個あたりの獲得ポイント
    public float deliveryInterval = 0.15f; // おもちゃを回収する時間の間隔（連打感を出すため）

    private bool isProcessingDelivery = false; // 連投防止フラグ

    private bool ManEmo = false; // 船上の男性

    private void OnTriggerEnter(Collider other)
    {
        // エリアに入ってきたのがプレイヤー（犬）だった場合
        if (other.CompareTag("Player") || other.gameObject.name.Contains("Player"))
        {

            // プレイヤーのスクリプトから獲得しているおもちゃリストを取得
            // ※ 前回のコードに合わせて、コンポーネント名はご自身のプレイヤースクリプト名に書き換えてください
            var player = other.GetComponent<PlayerControl_2>();

            // リストを操作するために、リフレクションではなくプレイヤー側のメソッドを安全に呼び出すためのキャストが必要です。
            // ここでは前回の仕様「GetCollectedToys」を持つスクリプトがプレイヤーについている前提で処理します。
            // （もしプレイヤースクリプトの名前が「PlayerController」なら、下のようにキャストしてください）
            // PlayerController player = other.GetComponent<PlayerController>();

            if (player != null)
            {
                // メッセージを動的に送るか、特定のコンポーネントを狙い撃ちします。
                // 今回は最もシンプルな「SendMessage」または直接コンポーネントを呼ぶ方法をとります。
                StartCoroutine(DeliverToysRoutine(player));

            }
        }
    }

    private IEnumerator DeliverToysRoutine(MonoBehaviour playerComponent)
    {
        if (isProcessingDelivery) yield break;
        isProcessingDelivery = true;

        // プレイヤーのコンポーネントから直接メソッドを呼べるように、
        // 実際のプレイヤースクリプト名（例: PlayerMovement等）が分かればベストですが、
        // ここでは「ユーザーがプレイヤーに実装したリスト」を取得する処理を行います。

        // 【重要】プレイヤーのスクリプト名に合わせてここを書き換えてください！
        // ここでは仮にプレイヤースクリプトの名前を「PlayerController」とします。
        var player = playerComponent.GetComponent<PlayerControl_2>();
        if (player == null)
        {
            isProcessingDelivery = false;
            yield break;

        }

        List<ToyFollower> toys = player.GetCollectedToys();

        // 後ろについているおもちゃを、最後尾から順番に納品していく（ヘビの尻尾から消えていくイメージ）
        while (toys.Count > 0)
        {
            // Debug.Log($"反応あり");

            int lastIndex = toys.Count - 1;
            ToyFollower targetToy = toys[lastIndex];

            // リストから除外（追従を外す）
            toys.RemoveAt(lastIndex);

            // おもちゃに納品演出を実行させる
            targetToy.Deliver(() =>
            {
                // 演出が完了（消滅）した瞬間にGameManagerにポイントを加算
                GameManager.Instance.AddScore(pointsPerToy);
            });

            ManEmo = true;

            // 次のおもちゃを回収するまで少し待つ（シュシュシュシュッと順番に消える演出のため）
            yield return new WaitForSeconds(deliveryInterval);
        }

        isProcessingDelivery = false;
    }
}
