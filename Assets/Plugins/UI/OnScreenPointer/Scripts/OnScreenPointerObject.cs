using UnityEngine;
using UnityEngine.UI;

namespace OnScreenPointerPlugin
{
    public class OnScreenPointerObject : MonoBehaviour
    {
        public Vector2 offset_local;
        public bool moveInCircle = false;
        [Range(0f, 1f)]
        public float circleSizeNormalized = 0.5f;

        public Sprite inScreenSprite;
        public Sprite outScreenSprite;
        public Image uiImagePrefab;

        private Image uiImage;
        private bool isPointerInScreen = false;

        [Header("対象物との距離の設定")]
        [SerializeField]
        private float pointerDisplayDistance = 20f;
        [SerializeField]
        private float pointerMaxSizeDistance = 2f;
        [SerializeField]
        private float pointerNormalScale = 1f;
        [SerializeField]
        private float pointerMaxScale = 2.5f;

        private OnScreenPointerController onScreenPointerController
        {
            get
            {
                return OnScreenPointerController.Instance;
            }
        }

        private int screenSizeX
        {
            get
            {
                return onScreenPointerController.playerCamera.pixelWidth;
            }
        }
        private int screenSizeY
        {
            get
            {
                return onScreenPointerController.playerCamera.pixelHeight;
            }
        }

        private Vector2 ScreenMidPoint
        {
            get
            {
                return new Vector2((int)screenSizeX / 2, (int)screenSizeY / 2);
            }
        }

        private Camera camera_local { get { return OnScreenPointerController.Instance.playerCamera; } }
        // Start is called before the first frame update
        void Start()
        {
            if (uiImagePrefab == null)
            {
                Debug.LogError($"{gameObject.name}: uiImagePrefab が設定されていません");
                return;
            }

            var controller = OnScreenPointerController.Instance;

            if (controller == null)
            {
                Debug.LogError($"{gameObject.name}: OnScreenPointerController.Instance が null です");
                return;
            }

            if (controller.uiContainerOfPointers == null)
            {
                Debug.LogError($"{gameObject.name}: uiContainerOfPointers が null です");
                return;
            }

            uiImage = Instantiate(
                uiImagePrefab,
                controller.uiContainerOfPointers
            );

            uiImage.raycastTarget = false;
        }

        private void OnEnable()
        {
            if (uiImage != null)
            {
                uiImage.gameObject.SetActive(true);
            }
        }

        private void OnDisable()
        {
            if (uiImage != null)
            {
                uiImage.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (uiImage != null)
            {
                Destroy(uiImage.gameObject);
            }
        }
        // Update is called once per frame
        void Update()
        {
            if (uiImage == null)
            {
                return;
            }

            float distance = Vector3.Distance(
                transform.position,
                camera_local.transform.position
            );

            // 一定距離より遠ければ非表示
            if (distance > pointerDisplayDistance)
            {
                uiImage.gameObject.SetActive(false);
                return;
            }

            // 表示
            uiImage.gameObject.SetActive(true);

            // 距離に応じてサイズ変更
            float t = Mathf.InverseLerp(
                pointerDisplayDistance,
                pointerMaxSizeDistance,
                distance
            );

            float scale = Mathf.Lerp(
                pointerNormalScale,
                pointerMaxScale,
                t
            );

            // 距離によってスケールが変わる
            uiImage.rectTransform.localScale = Vector3.one * scale;

            // ----

            var screenPos = MyScreenPosition(transform);

            isPointerInScreen = IsPointerInScreen(screenPos);

            if (isPointerInScreen)
            {
                uiImage.sprite = inScreenSprite;
                uiImage.transform.rotation = Quaternion.Euler(0, 0, 0);
            }
            else
            {
                uiImage.sprite = outScreenSprite;
                Vector2 screenPosCentered = (Vector2)screenPos - ScreenMidPoint;

                if (screenPos.z < 0)//object is behind player/camera
                {
                    screenPosCentered = screenPosCentered * -1;
                }

                float angle = Mathf.Atan2(screenPosCentered.y, screenPosCentered.x);
                screenPosCentered = PositionPointerObjectOffScreen(angle);
                screenPos = screenPosCentered + ScreenMidPoint;

                uiImage.transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
            }

            screenPos = ClampToOffsetBounds(screenPos);
            uiImage.transform.position = screenPos;
        }

        private Vector2 PositionPointerObjectOffScreen(float angle)
        {
            if (moveInCircle)
            {
                float smallerDimOfScreen = 0;
                smallerDimOfScreen = Mathf.Min(screenSizeX, screenSizeY);
                smallerDimOfScreen = smallerDimOfScreen * 0.5f * circleSizeNormalized;
                float x = Mathf.Cos(angle) * smallerDimOfScreen;
                float y = Mathf.Sin(angle) * smallerDimOfScreen;
                return new Vector2(x, y);
            }
            else
            {
                float largerDimOfScreen = 0;
                largerDimOfScreen = Mathf.Max(screenSizeX, screenSizeY);
                largerDimOfScreen = largerDimOfScreen * 2;
                float x = Mathf.Cos(angle) * largerDimOfScreen;
                float y = Mathf.Sin(angle) * largerDimOfScreen;
                return new Vector2(x, y);
            }
        }

        private Vector2 ClampToOffsetBounds(Vector2 screenPos)
        {
            int x = (int)Mathf.Clamp(screenPos.x, offset_local.x * screenSizeX, screenSizeX - offset_local.x * screenSizeX);
            int y = (int)Mathf.Clamp(screenPos.y, offset_local.y * screenSizeY, screenSizeY - offset_local.y * screenSizeY);

            return new Vector2(x, y);
        }

        private Vector3 MyScreenPosition(Transform transform)
        {
            var screenpos = onScreenPointerController.playerCamera.WorldToScreenPoint(transform.position);
            return screenpos;
        }

        private bool IsPointerInScreen(Vector3 screenPosition)
        {
            bool isTargetVisible = screenPosition.z > 0 && screenPosition.x > 0 && screenPosition.x < camera_local.pixelWidth && screenPosition.y > 0 && screenPosition.y < camera_local.pixelHeight;
            return isTargetVisible;
        }

    }
}