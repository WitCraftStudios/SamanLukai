using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ScoreboardUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject scoreboardPanel;       // The whole scoreboard panel
    public Transform contentParent;          // The "Content" inside ScrollView
    public GameObject playerScorePrefab;     // Prefab with PlayerNameText & PlayerScoreText

    private Dictionary<ulong, GameObject> scoreEntries = new Dictionary<ulong, GameObject>();

    /// <summary>
    /// Show the scoreboard with given player scores.
    /// </summary>
    public void ShowScoreboard(List<PlayerScore> playerScores)
    {
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(true);

        // Clear old entries
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        scoreEntries.Clear();

        // Sort players by score descending
        playerScores.Sort((a, b) => b.score.CompareTo(a.score));

        // Create new rows
        foreach (var ps in playerScores)
        {
            GameObject entry = Instantiate(playerScorePrefab, contentParent);

            // Find name & score texts
            TMP_Text[] texts = entry.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                // Assumes: first = name, second = score
                texts[0].text = $"Player {ps.clientId}";
                texts[1].text = $"{ps.score} pts";
            }

            scoreEntries[ps.clientId] = entry;
        }
    }

    /// <summary>
    /// Hide scoreboard.
    /// </summary>
    public void HideScoreboard()
    {
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(false);
    }
}
