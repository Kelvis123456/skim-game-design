using UnityEngine;
using UnityEngine.SceneManagement;

// Placed on the Game scene. If Game loads without Boot having run first,
// redirects to Boot which will then load Game additively.
[DefaultExecutionOrder(-999)]
public class GameSceneGuard : MonoBehaviour
{
    void Awake()
    {
        if (!ServiceLocator.TryGet<IScoringSystem>(out _))
        {
            Debug.Log("[GameSceneGuard] Boot not loaded. Redirecting...");
            SceneManager.LoadScene("Boot");
        }
    }
}
