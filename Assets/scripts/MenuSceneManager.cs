using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MenuSceneManager : MonoBehaviour
{
    public void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"シーン '{sceneName}' がBuild ProfileのScene Listにありません。"
            );
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}