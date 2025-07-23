using UnityEngine;
using TMPro;
using System.Collections;
using Unity.Netcode; // Required for networking

public class GameTimer : NetworkBehaviour // Inherit from NetworkBehaviour
{
    // UI Elements (no changes)
    public TMP_Text timerText;
    public TMP_Text roundText;
    public TMP_Text phaseText;
    public TMP_Text countdownText;
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;

    // Game Settings (no changes)
    public int maxRounds = 5;
    public float roundDuration = 60f;
    public float hidingDuration = 30f;

    // Networked variables to sync state across all clients
    private readonly NetworkVariable<float> netTimeLeft = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone);
    private readonly NetworkVariable<int> netCurrentRound = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone);
    private readonly NetworkVariable<GamePhase> netPhase = new NetworkVariable<GamePhase>(GamePhase.PreHiding, NetworkVariableReadPermission.Everyone);
    private readonly NetworkVariable<bool> netGameOver = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);

    private enum GamePhase { PreHiding, Hiding, Searching }
    private bool isFading = false; // Local state for UI

    public override void OnNetworkSpawn()
    {
        // Initial setup for UI elements
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        if (countdownText != null) countdownText.gameObject.SetActive(false);

        // Server starts the game loop
        if (IsServer)
        {
            StartCoroutine(PreHidingCountdown());
        }
    }

    void Update()
    {
        // All clients update their UI based on networked variables
        UpdateUI();

        // Server is the only one who runs the game logic
        if (!IsServer || isFading || netGameOver.Value) return;

        if (netTimeLeft.Value > 0)
        {
            netTimeLeft.Value -= Time.deltaTime;
        }
        else // Timer reached zero, change the phase
        {
            if (netPhase.Value == GamePhase.Hiding)
            {
                // Transition to Searching
                StartNextPhaseServerRpc(GamePhase.Searching);
            }
            else if (netPhase.Value == GamePhase.Searching)
            {
                if (netCurrentRound.Value < maxRounds)
                {
                    // Transition to next Hiding phase
                    StartNextPhaseServerRpc(GamePhase.Hiding);
                }
                else
                {
                    // Game Over
                    netGameOver.Value = true;
                }
            }
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void StartNextPhaseServerRpc(GamePhase nextPhase)
    {
        netPhase.Value = nextPhase;

        if (nextPhase == GamePhase.Hiding)
        {
            netCurrentRound.Value++;
            netTimeLeft.Value = hidingDuration;
        }
        else if (nextPhase == GamePhase.Searching)
        {
            netTimeLeft.Value = roundDuration;
        }

        // Tell all clients to fade and update their text
        StartPhaseClientRpc(nextPhase, netCurrentRound.Value);
    }

    [ClientRpc]
    private void StartPhaseClientRpc(GamePhase newPhase, int round)
    {
        StartCoroutine(FadeAndPreparePhase(newPhase, round));
    }
    
    private IEnumerator PreHidingCountdown()
    {
        // This coroutine now runs on the server and tells clients what to display
        TriggerCountdownClientRpc("Get Ready!", 1f);
        yield return new WaitForSeconds(1f);

        for (int i = 3; i > 0; i--)
        {
            TriggerCountdownClientRpc(i.ToString(), 1f);
            yield return new WaitForSeconds(1f);
        }

        TriggerCountdownClientRpc("Go!", 1f);
        yield return new WaitForSeconds(1f);

        TriggerCountdownClientRpc("", 0f); // Clear the countdown text
        
        // Start the first phase
        StartNextPhaseServerRpc(GamePhase.Hiding);
    }
    
    [ClientRpc]
    private void TriggerCountdownClientRpc(string text, float waitTime)
    {
        StartCoroutine(ShowCountdownText(text));
    }

    private IEnumerator ShowCountdownText(string text)
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = text;
            if (string.IsNullOrEmpty(text))
            {
                countdownText.gameObject.SetActive(false);
            }
        }
        yield return null;
    }

    private IEnumerator FadeAndPreparePhase(GamePhase newPhase, int round)
    {
        isFading = true;
        if (fadeCanvasGroup != null) yield return StartCoroutine(Fade(0f, 1f));

        // Update UI text for the new phase
        UpdatePhaseTextUI(newPhase, round);
        
        if (fadeCanvasGroup != null) yield return StartCoroutine(Fade(1f, 0f));
        isFading = false;
    }
    
    private void UpdatePhaseTextUI(GamePhase phase, int round)
    {
        string phaseString = "";
        switch (phase)
        {
            case GamePhase.Hiding:
                phaseString = "Hiding Phase";
                break;
            case GamePhase.Searching:
                phaseString = "Searching Phase";
                break;
        }

        if (phaseText != null)
        {
            phaseText.text = phaseString;
            phaseText.alpha = 1f;
            phaseText.gameObject.SetActive(true);
            StartCoroutine(FadeOutPhaseTextAfterDelay(5f));
        }

        if (roundText != null)
        {
            roundText.text = "Round " + round;
        }
    }

    private void UpdateUI()
    {
        // Reads from NetworkVariable
        int minutes = Mathf.FloorToInt(netTimeLeft.Value / 60f);
        int seconds = Mathf.FloorToInt(netTimeLeft.Value % 60f);
        if (timerText != null)
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if(netGameOver.Value && phaseText != null)
        {
            phaseText.text = "Game Over!";
        }
    }

    // --- UI Coroutines (no changes) ---

    IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            if (fadeCanvasGroup != null)
                fadeCanvasGroup.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = to;
    }

    IEnumerator FadeOutPhaseTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (phaseText != null)
        {
            float fadeTime = 1f;
            float elapsed = 0f;
            Color originalColor = phaseText.color;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeTime);
                phaseText.alpha = 1f - t;
                yield return null;
            }
            phaseText.alpha = 0f;
            phaseText.gameObject.SetActive(false);
            phaseText.color = originalColor;
        }
    }
}
