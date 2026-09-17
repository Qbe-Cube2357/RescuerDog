using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultSceneManager : MonoBehaviour
{
    void Start()
    {
    }

    void Update()
    {
    }

    public void OnStartButtonClick()
    {
        //ResultSceneに遷移
        SceneManager.LoadScene("ResultScene");
    }
}
