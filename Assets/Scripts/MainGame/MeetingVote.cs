using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class VoteButton
{
    public Button button;
    public Image highlightImage;
    public int voteId; // This represents a player
}

public class MeetingVote : NetworkBehaviour
{
    public VoteButton[] voteButtons;
    public Button confirmVoteButton;
    public TMP_Text timerText;
    public GameObject meetingPanel;
    public float meetingDuration = 30f;

    private float currentTime;
    private bool timerRunning = false;

    private Dictionary<int, NetworkVariable<int>> voteCounts = new Dictionary<int, NetworkVariable<int>>();
    private Dictionary<ulong, int> playerVotes = new Dictionary<ulong, int>();
    private HashSet<ulong> confirmedPlayers = new HashSet<ulong>();
    private Dictionary<int, ulong> voteIdToClientId = new Dictionary<int, ulong>(); // NEW: voteId → player clientId

    private int? localCurrentVoteId = null;
    private bool voteLockedIn = false;

    public GameTimer gameTimer;
    private PointManager pointManager;

    private ulong currentHiderClientId;
    private ulong currentSeekerClientId;

    private void Awake()
    {
        if (gameTimer == null)
            gameTimer = FindObjectOfType<GameTimer>();
    }

    private void Update()
    {
        if (!IsServer || !timerRunning) return;

        currentTime -= Time.deltaTime;
        UpdateTimerClientRpc(Mathf.CeilToInt(currentTime));

        if (currentTime <= 0)
        {
            timerRunning = false;
            EndMeeting(false, currentHiderClientId, currentSeekerClientId);
        }
    }

    public override void OnNetworkSpawn()
    {
        pointManager = FindObjectOfType<PointManager>();
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (timerRunning)
        {
            StartMeetingClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new List<ulong> { clientId } }
            });
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartMeetingServerRpc(ulong hiderId, ulong seekerId, ulong[] playerClientIds)
    {
        if (timerRunning)
        {
            Debug.LogWarning("Meeting already in progress. Ignoring duplicate StartMeetingServerRpc call.");
            return;
        }

        currentHiderClientId = hiderId;
        currentSeekerClientId = seekerId;

        // Map vote buttons to player client IDs
        voteIdToClientId.Clear();
        for (int i = 0; i < voteButtons.Length && i < playerClientIds.Length; i++)
        {
            voteIdToClientId[voteButtons[i].voteId] = playerClientIds[i];
        }

        InitializeVotes();
        StartMeetingTimer();
        StartMeetingClientRpc();
    }

    [ClientRpc]
    private void StartMeetingClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (meetingPanel == null)
        {
            Debug.LogError("❌ Meeting panel is not assigned on client.");
            return;
        }

        meetingPanel.SetActive(true);
        voteLockedIn = false;
        localCurrentVoteId = null;

        foreach (var vb in voteButtons)
        {
            vb.highlightImage.enabled = false;
            vb.button.interactable = true;

            vb.button.onClick.RemoveAllListeners();
            var captured = vb;
            vb.button.onClick.AddListener(() =>
            {
                if (!voteLockedIn)
                {
                    SubmitVoteToggleServerRpc(captured.voteId);
                    HandleLocalToggle(captured.voteId);
                }
            });
        }

        confirmVoteButton.interactable = true;
        confirmVoteButton.onClick.RemoveAllListeners();
        confirmVoteButton.onClick.AddListener(() =>
        {
            if (!voteLockedIn && localCurrentVoteId.HasValue)
            {
                ConfirmVoteServerRpc();
                LockInLocalVote();
            }
        });
    }

    private void InitializeVotes()
    {
        voteCounts.Clear();
        foreach (var vb in voteButtons)
        {
            voteCounts[vb.voteId] = new NetworkVariable<int>(0);
        }

        confirmedPlayers.Clear();
        playerVotes.Clear();
    }

    private void StartMeetingTimer()
    {
        currentTime = meetingDuration;
        timerRunning = true;
    }

    [ClientRpc]
    private void UpdateTimerClientRpc(int timeRemaining)
    {
        if (timerText != null)
        {
            timerText.text = "Time: " + timeRemaining + "s";
        }
    }

    [ClientRpc]
    private void EndMeetingClientRpc()
    {
        if (meetingPanel != null)
        {
            meetingPanel.SetActive(false);
        }
    }

    private void EndMeeting(bool objectFound, ulong hiderClientId, ulong seekerClientId)
    {
        timerRunning = false;

        if (meetingPanel != null)
            meetingPanel.SetActive(false);

        EndMeetingClientRpc();

        if (IsServer)
        {
            // Show all votes to clients
            ShowAllVotesClientRpc(GetVoteClientIds(), GetVoteIds());

            // Allocate points based on votes
            AllocateMeetingPoints();
        }

        if (gameTimer != null)
        {
            gameTimer.EndMeeting(objectFound, hiderClientId, seekerClientId);
        }
    }

    private void HandleLocalToggle(int voteId)
    {
        foreach (var vb in voteButtons)
        {
            vb.highlightImage.enabled = (vb.voteId == voteId && localCurrentVoteId != voteId);
        }

        if (localCurrentVoteId == voteId)
            localCurrentVoteId = null;
        else
            localCurrentVoteId = voteId;
    }

    private void LockInLocalVote()
    {
        voteLockedIn = true;

        foreach (var vb in voteButtons)
        {
            vb.button.interactable = false;
        }

        confirmVoteButton.interactable = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitVoteToggleServerRpc(int voteId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (confirmedPlayers.Contains(clientId)) return;

        if (playerVotes.ContainsKey(clientId) && playerVotes[clientId] == voteId)
        {
            voteCounts[voteId].Value--;
            playerVotes.Remove(clientId);
        }
        else
        {
            if (playerVotes.ContainsKey(clientId))
            {
                int previousVote = playerVotes[clientId];
                voteCounts[previousVote].Value--;
            }

            playerVotes[clientId] = voteId;
            voteCounts[voteId].Value++;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ConfirmVoteServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!playerVotes.ContainsKey(clientId)) return;

        confirmedPlayers.Add(clientId);

        if (confirmedPlayers.Count >= NetworkManager.Singleton.ConnectedClients.Count)
        {
            EndMeeting(true, currentHiderClientId, currentSeekerClientId);
        }
    }

    private void AllocateMeetingPoints()
    {
        if (pointManager == null) return;

        foreach (var kvp in playerVotes)
        {
            ulong voterId = kvp.Key;
            int voteId = kvp.Value;

            if (!voteIdToClientId.ContainsKey(voteId)) continue;

            ulong votedPlayerId = voteIdToClientId[voteId];

            if (votedPlayerId == currentHiderClientId)
            {
                pointManager.AddScore(voterId, 1); // Voter voted correctly
            }
        }
    }

    public int GetVoteCount(int voteId)
    {
        return voteCounts.ContainsKey(voteId) ? voteCounts[voteId].Value : 0;
    }

    private ulong[] GetVoteClientIds()
    {
        var keys = new ulong[playerVotes.Count];
        playerVotes.Keys.CopyTo(keys, 0);
        return keys;
    }

    private int[] GetVoteIds()
    {
        var values = new int[playerVotes.Count];
        playerVotes.Values.CopyTo(values, 0);
        return values;
    }

    [ClientRpc]
    private void ShowAllVotesClientRpc(ulong[] clientIds, int[] voteIds)
    {
        for (int i = 0; i < clientIds.Length; i++)
        {
            Debug.Log($"Client {clientIds[i]} voted for player {voteIds[i]}");
        }
    }
}
