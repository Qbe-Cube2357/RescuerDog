using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnSceneManager : MonoBehaviour
{
    //public string LoadScene = "";
    float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= 30f)
        {
            //PlayScene1に遷移
            SceneManager.LoadScene("StartScene");
        }
    }
}
