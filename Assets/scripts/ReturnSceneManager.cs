using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnSceneManager : MonoBehaviour
{
    //public string LoadScene = "";

    void Start()
    {
    }

    void Update()
    {
    }

    public void OnStartButtonClick()
    {
        //PlayScene1に遷移
        SceneManager.LoadScene("StartScene");
    }
}
