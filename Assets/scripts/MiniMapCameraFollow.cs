using UnityEngine;

[DisallowMultipleComponent]
public sealed class MiniMapCameraFollow : MonoBehaviour
{
    [Header("追従対象")]
    [SerializeField] private Transform target;

    [Header("カメラ設定")]
    [SerializeField, Min(0f)] private float height = 64f;

    [Tooltip("オンの場合、プレイヤーの進行方向が常にミニマップ上方向になります")]
    [SerializeField] private bool rotateWithTarget = true;

    [Tooltip("地図の向きを補正する角度です")]
    [SerializeField] private float yawOffset = 0f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position;

        Vector3 cameraPosition = new Vector3(
            targetPosition.x,
            targetPosition.y + height,
            targetPosition.z
        );

        float targetYaw = rotateWithTarget
            ? target.eulerAngles.y
            : 0f;

        Quaternion cameraRotation = Quaternion.Euler(
            90f,
            targetYaw + yawOffset,
            0f
        );

        transform.SetPositionAndRotation(
            cameraPosition,
            cameraRotation
        );
    }
}