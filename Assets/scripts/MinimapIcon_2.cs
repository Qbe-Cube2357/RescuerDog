using UnityEngine;

public class MinimapIcon_2 : MonoBehaviour
{
    // 固定したい頭上の高さ（プレイヤーからの相対的な高さ）
    public float fixedHeight = 10.0f;

    [Header("表示角度修正")]
    public float fixedRotateX = 0.0f;
    public float fixedRotateY = 0.0f;
    public float fixedRotateZ = 0.0f;

    void LateUpdate()
    {
        // 親（プレイヤー）の位置を取得
        Vector3 parentPos = transform.parent.position;
        Vector3 parentEuler = transform.parent.rotation.eulerAngles;

        // XとZはプレイヤーに合わせ、Yだけ固定（親のY + 指定の高さ）
        transform.position = new Vector3(parentPos.x, fixedHeight, parentPos.z);

        // 回転固定
        transform.rotation = Quaternion.Euler(fixedRotateX + parentEuler.x, fixedRotateY + parentEuler.y, fixedRotateZ + parentEuler.z);
    }
}