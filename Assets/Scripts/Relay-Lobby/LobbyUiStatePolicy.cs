using Unity.Services.Lobbies.Models;

/// <summary>
/// Lobby UI のボタン活性条件を計算するポリシー。
/// </summary>
public static class LobbyUiStatePolicy
{
    public static LobbyControlState Build(
        bool isInitialized,
        bool isCreatingLobby,
        bool isJoiningLobby,
        bool isStartingGame,
        bool isRefreshingLobbies,
        bool isJoiningRelayNetwork,
        bool isHost,
        Lobby joinedLobby,
        int minPlayersToStart)
    {
        bool busy = isCreatingLobby || isJoiningLobby || isStartingGame || isRefreshingLobbies || isJoiningRelayNetwork;
        bool canCreateLobby = isInitialized && !busy && joinedLobby == null;
        bool canRefresh = isInitialized && !busy;
        bool canStartGame = isInitialized
            && !busy
            && isHost
            && joinedLobby != null
            && joinedLobby.Players != null
            && joinedLobby.Players.Count >= minPlayersToStart;

        return new LobbyControlState(canCreateLobby, canRefresh, canStartGame);
    }
}

public readonly struct LobbyControlState
{
    public readonly bool CanCreateLobby;
    public readonly bool CanRefresh;
    public readonly bool CanStartGame;

    public LobbyControlState(bool canCreateLobby, bool canRefresh, bool canStartGame)
    {
        CanCreateLobby = canCreateLobby;
        CanRefresh = canRefresh;
        CanStartGame = canStartGame;
    }
}
