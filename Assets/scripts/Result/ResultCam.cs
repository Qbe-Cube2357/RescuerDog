using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ResultCam : MonoBehaviour
{
    Camera maincamera;
    [SerializeField] private Camera camera2;
    [SerializeField] private Camera camera3;
    [SerializeField] private Camera camera4;

    [SerializeField] private GameObject cam;
    [SerializeField] private float moveSpeed1 = 7f, moveSpeed2 = 8f;    // 移動の速さ
    float moveZ = 1f;

    private float timer = 0f;
    [SerializeField] private GameObject targetObject1; // 消したいオブジェクトをインスペクターで指定
    [SerializeField] private GameObject targetObject2;
    [SerializeField] private GameObject targetObject3;
    [SerializeField] private GameObject targetObject4;

    [SerializeField] private Image targetImage;
    [SerializeField] private float ImoveSpeed = 2f;

    bool f1 = true, f2 = true, f3 = true, f4 = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        maincamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 3f && f1 == true)
        {
            f1 = false;
            maincamera.enabled = true;
            camera2.enabled = false;
            camera3.enabled = false;
            camera4.enabled = false;
        }
        else if (timer >= 5f && f2 == true)
        {
            f2 = false;
            maincamera.enabled = false;
            camera2.enabled = true;
            camera3.enabled = false;
            camera4.enabled = false;
        }
        else if (timer >= 7f && f3 == true)
        {
            f3 = false;
            maincamera.enabled = false;
            camera2.enabled = false;
            camera3.enabled = true;
            camera4.enabled = false;
        }
        else if (timer >= 9f && f4 == true)
        {
            f4 = false;
            maincamera.enabled = false;
            camera2.enabled = false;
            camera3.enabled = false;
            camera4.enabled = true;
        }

        if (timer >= 12f && timer <= 17f)
        {
            // 現在の位置情報を取得
            Vector3 currentPos = cam.transform.position;

            // Z座標だけを滑らかに増やす（移動スピードに応じて加算）
            currentPos.x -= (moveSpeed1 / 5f) * Time.deltaTime;

            // XとYはそのまま維持し、Z座標だけを適用する
            cam.transform.position = currentPos;
        }
        else if (timer >= 19f && timer <= 19.5f)
        {
            // 現在の位置情報を取得
            Vector3 currentPos = cam.transform.position;

            // Z座標だけを滑らかに増やす（移動スピードに応じて加算）
            currentPos.z += moveSpeed2 * Time.deltaTime;

            // XとYはそのまま維持し、Z座標だけを適用する
            cam.transform.position = currentPos;
        }

        if (timer >= 19.5f && timer <= 20f)
        {
            RectTransform rect = targetImage.rectTransform;
            rect.anchoredPosition +=
                Vector2.up * ImoveSpeed * Time.deltaTime;
        }

        if (timer >= 10f)
        {
            Destroy(targetObject1);
            Destroy(targetObject2);
            Destroy(targetObject3);
            Destroy(targetObject4);
        }
    }
}
