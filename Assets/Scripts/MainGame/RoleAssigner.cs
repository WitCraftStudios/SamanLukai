using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Linq;

public class RoleAssigner : NetworkBehaviour
{
    private void Start()
    {
        if (IsServer)
        {
            StartCoroutine(AssignRolesWithDelay());
        }
    }

    private IEnumerator AssignRolesWithDelay()
    {
        yield return new WaitForSeconds(2f); // Wait for all players to spawn and be ready
        AssignRoles();
    }

    public void AssignRoles()
    {
        if (!IsServer) return;

        var players = NetworkManager.Singleton.ConnectedClientsList
            .Select(client => client.PlayerObject.GetComponent<PlayerRoleManager>())
            .ToList();

        if (players.Count == 0) return;

        // Pick exactly one random player to be the hider
        int hiderIndex = Random.Range(0, players.Count);
        for (int i = 0; i < players.Count; i++)
        {
            players[i].role.Value = (i == hiderIndex) ? PlayerRole.Hider : PlayerRole.Seeker;
        }
        // This guarantees exactly one hider, even if there are only two players
    }
}
