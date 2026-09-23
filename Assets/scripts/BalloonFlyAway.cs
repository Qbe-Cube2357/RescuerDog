using UnityEngine;

[DisallowMultipleComponent]
public sealed class BalloonFlyAway : MonoBehaviour
{
    private float flySpeed;
    private float targetWorldY;
    private bool isFlying;

    public void Launch(
        float speed,
        float despawnHeight)
    {
        flySpeed = Mathf.Max(0.01f, speed);

        float height = Mathf.Max(0f, despawnHeight);

        if (height <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        targetWorldY = transform.position.y + height;
        isFlying = true;
    }

    private void Update()
    {
        if (!isFlying)
        {
            return;
        }

        transform.position +=
            Vector3.up * flySpeed * Time.deltaTime;

        if (transform.position.y >= targetWorldY)
        {
            Destroy(gameObject);
        }
    }
}