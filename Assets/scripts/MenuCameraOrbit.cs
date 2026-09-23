using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class MenuCameraOrbit : MonoBehaviour
{
    [Header("Orbit Center")]
    [Tooltip("カメラが周回する中心")]
    [SerializeField]
    private Transform orbitCenter;

    [Tooltip("実際にカメラが見る位置の、中心からのずれ")]
    [SerializeField]
    private Vector3 lookAtOffset = Vector3.up;

    [Header("Orbit Settings")]
    [Tooltip("1秒間に回転する角度。負数にすると逆回転します")]
    [SerializeField]
    private float degreesPerSecond = 6f;

    [Tooltip("常に中心方向を向かせる")]
    [SerializeField]
    private bool lookAtCenter = true;

    [Tooltip("Time.timeScaleが0でも動かす")]
    [SerializeField]
    private bool useUnscaledTime = true;

    private float radius;
    private float height;
    private float currentAngle;

    private void Start()
    {
        if (orbitCenter == null)
        {
            Debug.LogError(
                $"{nameof(MenuCameraOrbit)}: " +
                "Orbit Centerが設定されていません。",
                this
            );

            enabled = false;
            return;
        }

        Vector3 offset =
            transform.position - orbitCenter.position;

        height = offset.y;
        offset.y = 0f;

        radius = offset.magnitude;

        if (radius <= 0.01f)
        {
            Debug.LogError(
                $"{nameof(MenuCameraOrbit)}: " +
                "Main CameraをOrbit Centerから離してください。",
                this
            );

            enabled = false;
            return;
        }

        // 現在のカメラ位置を開始角度として使用する
        currentAngle = Mathf.Atan2(
            offset.x,
            offset.z
        ) * Mathf.Rad2Deg;

        ApplyCameraTransform();
    }

    private void LateUpdate()
    {
        float deltaTime = useUnscaledTime
            ? Time.unscaledDeltaTime
            : Time.deltaTime;

        currentAngle += degreesPerSecond * deltaTime;

        ApplyCameraTransform();
    }

    private void ApplyCameraTransform()
    {
        Quaternion rotation =
            Quaternion.Euler(0f, currentAngle, 0f);

        Vector3 horizontalOffset =
            rotation * Vector3.forward * radius;

        transform.position =
            orbitCenter.position +
            horizontalOffset +
            Vector3.up * height;

        if (lookAtCenter)
        {
            transform.LookAt(
                orbitCenter.position + lookAtOffset,
                Vector3.up
            );
        }
    }
}