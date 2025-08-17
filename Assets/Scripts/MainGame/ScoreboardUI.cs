using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ScoreboardUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject scoreboardPanel;       // Parent panel for the scoreboard
    public Transform contentParent;          // Content holder inside ScrollRect
    public GameObject playerScorePrefab;     // Prefab with TMP_Text for name & score

    private Dictionary<ulong, GameObject> scoreEntries = new Dictionary<ulong, GameObject>();

    /// <summary>
    /// Call this to display the scoreboard with the current player scores.
    /// </summary>
    public void ShowScoreboard(List<PlayerScore> playerScores)
    {
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(true);

        // Clear previous entries
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
        scoreEntries.Clear();

        // Sort by descending score
        playerScores.Sort((a, b) => b.score.CompareTo(a.score));

        // Create UI entry for each player
        foreach (var ps in playerScores)
        {
            GameObject entry = Instantiate(playerScorePrefab, contentParent);
            TMP_Text text = entry.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = $"Player {ps.clientId}: {ps.score} pts";
            }
            scoreEntries[ps.clientId] = entry;
        }
    }

    /// <summary>
    /// Optional: Hide the scoreboard.
    /// </summary>
    public void HideScoreboard()
    {
        if (scoreboardPanel != null)
            scoreboardPanel.SetActive(false);
    }
}
