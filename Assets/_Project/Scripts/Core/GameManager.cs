using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    [Header("Componentes de UI")]
    public ScreenFader screenFader;
    [Header("Configurações de Fim de Jogo")]
    public float restartFadeTime = 4.0f;
    public float quitFadeTime = 1.5f;
    private bool isGameOver = false;
    public static event Action OnGameOver;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        if (screenFader != null)
        {
            screenFader.FadeFromBlack(0.0f);
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("GAME MANAGER: TriggerGameOver chamado, Iniciando sequência");
        if (screenFader != null)
        {
            screenFader.FadeToBlackInstant();
        }
        OnGameOver?.Invoke();
        StartCoroutine(FadeAndReloadScene(restartFadeTime));
    }

    public void TriggerGameWin()
    {
        if (isGameOver) return;
        isGameOver = true;
        Debug.Log("GAME MANAGER: TriggerGameWin chamado");
        OnGameOver?.Invoke();
        StartCoroutine(FadeAndQuit(quitFadeTime));
    }

    public void QuitGame()
    {
        Debug.Log("GAME MANAGER: QuitGame chamado");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator FadeAndReloadScene(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log($"GAME MANAGER: Esperando {delay}s antes de recarregar");
        string sceneToLoad = SceneManager.GetActiveScene().name;
        Debug.Log($"GAME MANAGER: Recarregando a cena: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }

    private IEnumerator FadeAndQuit(float delay)
    {
        if (screenFader != null)
        {
            screenFader.FadeToBlack(delay);
        }
        yield return new WaitForSeconds(delay);
        Debug.Log($"GAME MANAGER: Esmaeceu, fechando o jogo.");
        QuitGame();
    }
}