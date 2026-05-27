using UnityEngine;
using UnityEngine.SceneManagement;

public static class ScoreboardLoader
{
    const string SCOREBOARD_SCENE = "Scene_ScoreBoard";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        EnsureLoaded();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
            EnsureLoaded();
    }

    static void EnsureLoaded()
    {
        Scene sb = SceneManager.GetSceneByName(SCOREBOARD_SCENE);
        if (!sb.isLoaded)
            SceneManager.LoadScene(SCOREBOARD_SCENE, LoadSceneMode.Additive);
    }
}
