using UnityEngine;

public class CutsceneEvents : MonoBehaviour
{
    public PlayerController scriptDoJogador;
    public CharacterController characterControllerDoJogador;

    public void DesativarControleJogador()
    {
        if (scriptDoJogador != null)
        {
            scriptDoJogador.podeMover = false;
        }

        if (characterControllerDoJogador != null)
        {
            characterControllerDoJogador.enabled = false;
        }
    }

    public void AtivarControleJogador()
    {
        if (characterControllerDoJogador != null)
        {
            characterControllerDoJogador.enabled = true;
        }

        if (scriptDoJogador != null)
        {
            scriptDoJogador.podeMover = true;
            if (SettingsManager.Instance != null && SettingsManager.Instance.playerActions != null)
            {
                SettingsManager.Instance.playerActions.FindActionMap("Player").Enable();
            }
        }
    }
}