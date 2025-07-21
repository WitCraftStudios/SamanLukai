using UnityEngine;
using TMPro;
using System.Collections;

public class GameTimer : MonoBehaviour
{
    public TMP_Text timerText;
    public TMP_Text roundText;
    public TMP_Text phaseText;
    public TMP_Text countdownText; // New: assign in Inspector for 3-2-1-Go

    public int maxRounds = 5;
    public float roundDuration = 60f; // 1 minute per round
    public float hidingDuration = 30f; // 30 seconds for hiding

    public CanvasGroup fadeCanvasGroup; // Assign in Inspector (should cover the screen)
    public float fadeDuration = 1f;

    private float timeLeft;
    private int currentRound = 1;
    private enum GamePhase { PreHiding, Hiding, Searching }
    private GamePhase phase = GamePhase.PreHiding;
    private bool isFading = false;
    private bool gameOver = false;

    void Start()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        StartCoroutine(PreHidingCountdown());
    }

    void Update()
    {
        if (isFading || phase == GamePhase.PreHiding || gameOver) return;

        if (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            UpdateUI();
        }
        else
        {
            if (phase == GamePhase.Hiding)
            {
                StartCoroutine(FadeAndThen(StartSearchingPhase));
            }
            else if (phase == GamePhase.Searching)
            {
                if (currentRound < maxRounds)
                {
                    StartCoroutine(FadeAndThen(StartNextHidingPhase));
                }
                else
                {
                    // Game over logic here
                    phaseText.text = "Game Over!";
                    timerText.text = "00:00";
                    gameOver = true; // Prevent further updates
                    StartCoroutine(FadeAndThen(null));
                }
            }
        }
    }

    IEnumerator PreHidingCountdown()
    {
        phase = GamePhase.PreHiding;
        if (phaseText != null)
        {
            phaseText.gameObject.SetActive(true);
            phaseText.text = "Get Ready!";
            phaseText.alpha = 1f;
        }
        if (timerText != null) timerText.text = "";
        if (roundText != null) roundText.text = "";
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "";
        }
        yield return new WaitForSeconds(1f);
        for (int i = 3; i > 0; i--)
        {
            if (countdownText != null) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        if (countdownText != null) countdownText.text = "Go!";
        yield return new WaitForSeconds(1f);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        StartHidingPhase();
    }

    void StartHidingPhase()
    {
        phase = GamePhase.Hiding;
        timeLeft = hidingDuration;
        if (phaseText != null)
        {
            phaseText.text = "Hiding Phase";
            phaseText.alpha = 1f;
            phaseText.gameObject.SetActive(true);
            StartCoroutine(FadeOutPhaseTextAfterDelay(5f));
        }
        roundText.text = "Round " +  currentRound;
        UpdateUI();
        // TODO: Select random hider, etc.
    }

    void StartSearchingPhase()
    {
        phase = GamePhase.Searching;
        timeLeft = roundDuration;
        if (phaseText != null)
        {
            phaseText.text = "Searching Phase";
            phaseText.alpha = 1f;
            phaseText.gameObject.SetActive(true);
            StartCoroutine(FadeOutPhaseTextAfterDelay(5f));
        }
        // roundText.text = "Round " + currentRound;
        UpdateUI();
        // TODO: Move players to spawn, fade in, etc.
    }

    void StartNextHidingPhase()
    {
        phase = GamePhase.Hiding;
        currentRound++; // Increment round at the start of hiding phase
        if (currentRound > maxRounds)
        {
            // Game over logic here
            phaseText.text = "Game Over!";
            timerText.text = "00:00";
            gameOver = true; // Prevent further updates
            StartCoroutine(FadeAndThen(null));
            return;
        }
        timeLeft = hidingDuration;
        if (phaseText != null)
        {
            phaseText.text = "Hiding Phase";
            phaseText.alpha = 1f;
            phaseText.gameObject.SetActive(true);
            StartCoroutine(FadeOutPhaseTextAfterDelay(5f));
        }
        roundText.text = "Round " + currentRound;
        UpdateUI();
        // TODO: Select next random hider, etc.
    }

    void UpdateUI()
    {
        int minutes = Mathf.FloorToInt(timeLeft / 60f);
        int seconds = Mathf.FloorToInt(timeLeft % 60f);
        if (timerText != null)
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    IEnumerator FadeAndThen(System.Action nextPhaseAction)
    {
        isFading = true;
        // Fade out
        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(0f, 1f));
            fadeCanvasGroup.blocksRaycasts = true;
        }
        // Wait a moment while faded out
        yield return new WaitForSeconds(1f);
        // Next phase
        if (nextPhaseAction != null)
            nextPhaseAction();
        // Fade in
        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(1f, 0f));
            fadeCanvasGroup.blocksRaycasts = false;
        }
        isFading = false;
    }

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
