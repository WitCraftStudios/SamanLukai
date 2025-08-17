using System.Linq;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PointManager : NetworkBehaviour
{
    // Networked list of scores (client-readable, server-writable)
    private NetworkList<PlayerScore> playerScores;

    // UI layer subscribes to this (local only)
    public static event System.Action<IReadOnlyList<PlayerScore>> OnScoreChanged;

    private void Awake()
    {
        playerScores = new NetworkList<PlayerScore>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Add all currently-connected clients (host + any already in lobby)
            foreach (var c in NetworkManager.Singleton.ConnectedClientsList)
            {
                EnsurePlayer(c.ClientId);
            }

            // Track late joiners
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        playerScores.OnListChanged += HandleScoreListChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }

        playerScores.OnListChanged -= HandleScoreListChanged;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        EnsurePlayer(clientId);
    }

    // ========= Public API =========

    /// <summary>
    /// Server-only helper to add points.
    /// Prefer calling this from server code (e.g., MeetingVote, GameTimer).
    /// </summary>
    public void AddScore(ulong clientId, int points)
    {
        if (!IsServer)
        {
            Debug.LogWarning("AddScore called on a client. Use AddScoreServerRpc from clients.");
            return;
        }

        // Ensure entry exists, then update it
        int idx = IndexOfClient(clientId);
        if (idx >= 0)
        {
            var updated = playerScores[idx];
            updated.score += points;
            playerScores[idx] = updated;   // assignment triggers sync
        }
        else
        {
            playerScores.Add(new PlayerScore(clientId, points));
        }
    }

    /// <summary>
    /// Client-callable wrapper to add points. Server executes the mutation.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddScoreServerRpc(ulong clientId, int points)
    {
        AddScore(clientId, points);
    }

    /// <summary>
    /// Round award (compat with your existing GameTimer calls).
    /// objectFound = true -> seeker +1, false -> hider +1
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AwardRoundPointsServerRpc(bool objectFound, ulong hiderClientId, ulong seekerClientId)
    {
        if (!IsServer) return;

        EnsurePlayer(hiderClientId);
        EnsurePlayer(seekerClientId);

        if (objectFound)
            AddScore(seekerClientId, 1);
        else
            AddScore(hiderClientId, 1);
    }

    /// <summary>
    /// Optional compat: award a voter who correctly voted the hider.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AwardMeetingPointsServerRpc(ulong voterClientId, ulong votedHiderClientId)
    {
        if (!IsServer) return;
        EnsurePlayer(voterClientId);
        AddScore(voterClientId, 1);
    }

    /// <summary>
    /// Lookup current score (safe on any side).
    /// </summary>
    public int GetScore(ulong clientId)
    {
        int idx = IndexOfClient(clientId);
        return (idx >= 0) ? playerScores[idx].score : 0;
    }

    // ========= Internals =========

    private void EnsurePlayer(ulong clientId)
    {
        if (IndexOfClient(clientId) < 0)
        {
            playerScores.Add(new PlayerScore(clientId, 0));
        }
    }

    private int IndexOfClient(ulong clientId)
    {
        for (int i = 0; i < playerScores.Count; i++)
        {
            if (playerScores[i].clientId == clientId)
                return i;
        }
        return -1;
    }

    private void HandleScoreListChanged(NetworkListEvent<PlayerScore> changeEvent)
    {
        // Manually copy NetworkList<PlayerScore> into a simple array
        PlayerScore[] scoresArray = new PlayerScore[playerScores.Count];
        for (int i = 0; i < playerScores.Count; i++)
        {
            scoresArray[i] = playerScores[i];
        }

        OnScoreChanged?.Invoke(scoresArray);
    }

    public List<PlayerScore> GetAllScores()
    {
        List<PlayerScore> scores = new List<PlayerScore>();
        for (int i = 0; i < playerScores.Count; i++)
        {
            scores.Add(playerScores[i]);
        }
        return scores;
    }

}
