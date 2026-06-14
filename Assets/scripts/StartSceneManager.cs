using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneManager : MonoBehaviour
{
    void Start()
    {
    }

    void Update()
    {
    }

    public void OnStartButtonClick()
    {
        //PlayScene1に遷移
        SceneManager.LoadScene("MainScene");
    }
}
