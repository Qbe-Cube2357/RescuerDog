using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class MenuSceneManager : MonoBehaviour
{
    [SerializeField, Min(0f)]
    private float sceneLoadDelay = 0.15f;

    private bool isLoading;

    public void LoadScene(string sceneName)
    {
        if (isLoading)
        {
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"シーン '{sceneName}' がBuild ProfileのScene Listにありません。"
            );
            return;
        }

        StartCoroutine(LoadSceneAfterDelay(sceneName));
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName)
    {
        isLoading = true;

        yield return new WaitForSecondsRealtime(sceneLoadDelay);

        SceneManager.LoadScene(sceneName);
    }
}