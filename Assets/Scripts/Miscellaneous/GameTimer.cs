using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq;
using Unity.Netcode;

public class GameTimer : NetworkBehaviour
{
    [Header("UI Elements")]
    public TMP_Text timerText;
    public TMP_Text roundText;
    public TMP_Text phaseText;
    public TMP_Text countdownText;
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;

    [Header("Game Settings")]
    public int maxRounds = 5;
    public float roundDuration = 60f;
    public float hidingDuration = 30f;

    [Header("References")]
    public MeetingVote meetingVote;       // Assign in inspector
    public GameObject meetingPanel;       // Optional: just to show/hide
    public ScoreboardUI scoreboardUI;     // Assign your scoreboard UI prefab/object

    private readonly NetworkVariable<float> netTimeLeft = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone);
    private readonly NetworkVariable<int> netCurrentRound = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    private readonly NetworkVariable<GamePhase> netPhase = new NetworkVariable<GamePhase>(GamePhase.PreHiding, NetworkVariableReadPermission.Everyone);
    private readonly NetworkVariable<bool> netGameOver = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);

    private PointManager pointManager;
    private bool isFading = false;
    private bool meetingInProgress = false;
    private bool meetingEnded = false;
    private bool objectFound = false;

    private ulong currentHiderClientId;
    private ulong currentSeekerClientId;

    private enum GamePhase { PreHiding, Hiding, Searching }

    public override void OnNetworkSpawn()
    {
        pointManager = FindObjectOfType<PointManager>();

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        if (countdownText != null) countdownText.gameObject.SetActive(false);

        if (IsServer)
        {
            StartCoroutine(PreHidingCountdown());
        }
    }

    void Update()
    {
        UpdateUI();

        if (!IsServer || isFading || netGameOver.Value) return;

        if (netTimeLeft.Value > 0)
        {
            netTimeLeft.Value -= Time.deltaTime;
        }
        else
        {
            HandlePhaseEnd();
        }
    }

    private void HandlePhaseEnd()
    {
        if (netPhase.Value == GamePhase.Hiding)
        {
            StartNextPhaseServerRpc(GamePhase.Searching);
        }
        else if (netPhase.Value == GamePhase.Searching)
        {
            if (netCurrentRound.Value < maxRounds)
            {
                if (!meetingInProgress && !meetingEnded)
                {
                    meetingInProgress = true;
                    StartCoroutine(WaitForMeetingThenNextRound());
                }
            }
            else
            {
                // Game over
                netGameOver.Value = true;

                // Show scoreboard for all players
                if (IsServer && scoreboardUI != null && pointManager != null)
                {
                    scoreboardUI.ShowScoreboard(pointManager.GetAllScores());
                    ShowScoreboardClientRpc();
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyObjectFoundServerRpc(ulong seekerClientId, ulong hiderClientId)
    {
        if (objectFound) return; // already found
        objectFound = true;
        Debug.Log($"Object found by seeker {seekerClientId}!");
        EndMeeting(objectFound, hiderClientId, seekerClientId);
    }

    private IEnumerator WaitForMeetingThenNextRound()
    {
        yield return new WaitUntil(() => !isFading);

        if (meetingPanel != null)
            meetingPanel.SetActive(true);

        currentHiderClientId = PickRandomHider();
        currentSeekerClientId = PickRandomSeeker(currentHiderClientId);

        ulong[] allPlayers = NetworkManager.Singleton.ConnectedClients.Keys.ToArray();

        if (meetingVote != null)
        {
            meetingVote.StartMeetingServerRpc(currentHiderClientId, currentSeekerClientId, allPlayers);
        }

        // Wait until meeting ends
        yield return new WaitUntil(() => !meetingInProgress);

        // Start next round
        StartNextPhaseServerRpc(GamePhase.Hiding);
    }

    private ulong PickRandomHider()
    {
        var allClients = NetworkManager.Singleton.ConnectedClients.Keys.ToList();
        if (allClients.Count == 0) return 0;
        return allClients[Random.Range(0, allClients.Count)];
    }

    private ulong PickRandomSeeker(ulong hiderId)
    {
        var allClients = NetworkManager.Singleton.ConnectedClients.Keys.Where(id => id != hiderId).ToList();
        if (allClients.Count == 0) return hiderId; // fallback
        return allClients[Random.Range(0, allClients.Count)];
    }

    public void EndMeeting(bool objectFound, ulong hiderClientId, ulong seekerClientId)
    {
        if (IsServer && pointManager != null)
        {
            pointManager.AwardRoundPointsServerRpc(objectFound, hiderClientId, seekerClientId);
            pointManager.AwardRoundPointsServerRpc(objectFound, hiderClientId, seekerClientId);
        }

        if (meetingPanel != null)
            meetingPanel.SetActive(false);

        meetingInProgress = false;
        meetingEnded = true;
        Debug.Log($"✅ Meeting ended — ObjectFound: {objectFound}");
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartNextPhaseServerRpc(GamePhase nextPhase)
    {
        netPhase.Value = nextPhase;

        if (nextPhase == GamePhase.Hiding)
        {
            netCurrentRound.Value++;
            netTimeLeft.Value = hidingDuration;

            meetingInProgress = false;
            meetingEnded = false;
            objectFound = false;
        }
        else if (nextPhase == GamePhase.Searching)
        {
            netTimeLeft.Value = roundDuration;
        }

        StartPhaseClientRpc(nextPhase, netCurrentRound.Value);
    }

    [ClientRpc]
    private void StartPhaseClientRpc(GamePhase newPhase, int round)
    {
        StartCoroutine(FadeAndPreparePhase(newPhase, round));
    }

    private IEnumerator PreHidingCountdown()
    {
        TriggerCountdownClientRpc("Get Ready!", 1f);
        yield return new WaitForSeconds(1f);

        for (int i = 3; i > 0; i--)
        {
            TriggerCountdownClientRpc(i.ToString(), 1f);
            yield return new WaitForSeconds(1f);
        }

        TriggerCountdownClientRpc("Go!", 1f);
        yield return new WaitForSeconds(1f);

        TriggerCountdownClientRpc("", 0f);
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
            countdownText.gameObject.SetActive(!string.IsNullOrEmpty(text));
            countdownText.text = text;
        }
        yield return null;
    }

    private IEnumerator FadeAndPreparePhase(GamePhase newPhase, int round)
    {
        isFading = true;
        if (fadeCanvasGroup != null) yield return StartCoroutine(Fade(0f, 1f));

        UpdatePhaseTextUI(newPhase, round);

        if (fadeCanvasGroup != null) yield return StartCoroutine(Fade(1f, 0f));
        isFading = false;
    }

    private void UpdatePhaseTextUI(GamePhase phase, int round)
    {
        string phaseString = phase == GamePhase.Hiding ? "Hiding Phase" : "Searching Phase";

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
        int minutes = Mathf.FloorToInt(netTimeLeft.Value / 60f);
        int seconds = Mathf.FloorToInt(netTimeLeft.Value % 60f);
        if (timerText != null)
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (netGameOver.Value && phaseText != null)
        {
            phaseText.text = "Game Over!";
        }
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

    [ClientRpc]
    private void ShowScoreboardClientRpc()
    {
        if (scoreboardUI != null && pointManager != null)
        {
            scoreboardUI.ShowScoreboard(pointManager.GetAllScores());
        }
    }
}
