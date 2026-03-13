using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

/// <summary>
/// Lobby Service へのアクセスを集約する API クライアント。
/// </summary>
public class LobbyApiClient
{
    private readonly string playerName;
    private readonly string relayCodeKey;

    public LobbyApiClient(string playerName, string relayCodeKey)
    {
        this.playerName = playerName;
        this.relayCodeKey = relayCodeKey;
    }

    public Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, string playerId)
    {
        var options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Player = new Player(playerId)
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
                }
            },
            Data = new Dictionary<string, DataObject>
            {
                { relayCodeKey, new DataObject(DataObject.VisibilityOptions.Member, "0") }
            }
        };

        return LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
    }

    public Task<QueryResponse> QueryLobbiesAsync()
    {
        return LobbyService.Instance.QueryLobbiesAsync();
    }

    public Task<Lobby> JoinLobbyByIdAsync(string lobbyId, string playerId)
    {
        var options = new JoinLobbyByIdOptions
        {
            Player = new Player(playerId)
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
                }
            }
        };

        return LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
    }

    public Task<Lobby> UpdateRelayCodeAsync(string lobbyId, string relayCode)
    {
        var options = new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { relayCodeKey, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
            }
        };

        return LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);
    }

    public Task<Lobby> GetLobbyAsync(string lobbyId)
    {
        return LobbyService.Instance.GetLobbyAsync(lobbyId);
    }

    public Task SendHeartbeatAsync(string lobbyId)
    {
        return LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
    }

    public async Task DeleteOwnedLobbiesAsync(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return;
        }

        QueryResponse queryResponse = await QueryLobbiesAsync();
        foreach (Lobby lobby in queryResponse.Results)
        {
            if (lobby.HostId == playerId)
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
            }
        }
    }

    public Task LeaveLobbyAsync(string lobbyId, string playerId, bool deleteLobbyIfHost)
    {
        if (deleteLobbyIfHost)
        {
            return LobbyService.Instance.DeleteLobbyAsync(lobbyId);
        }

        if (string.IsNullOrEmpty(playerId))
        {
            return Task.CompletedTask;
        }

        return LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
    }
}
