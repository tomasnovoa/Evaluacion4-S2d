using UnityEngine;
using UnityEngine.SceneManagement;
using PUCV.PhysicEngine2D;  

public class CheckWin : MonoBehaviour, IHasCollider
{
    [Header("Reinicio")]
    [Tooltip("Tiempo de espera antes de recargar la escena (segundos).")]
    public float reloadDelay = 0f;


    public void OnInformCollisionEnter2D(CollisionInfo collisionInfo)
    {
        

        RestartScene();
    }

    private void RestartScene()
    {
        if (reloadDelay <= 0f)
        {
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }
        else
        {
            StartCoroutine(RestartAfterDelay());
        }
    }

    private System.Collections.IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(reloadDelay);
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}


