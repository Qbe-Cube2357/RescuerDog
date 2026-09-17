using UnityEngine;

public class OrbitAnchor : MonoBehaviour
{
    [Header("回転の設定")]
    public float rotateSpeed = 30f; // 1秒間に回転する角度

    void Update()
    {
        // Y軸を中心にゆっくり回転させる
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
    }
}