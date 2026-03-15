using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Lobby Service と Relay 起動の全体制御を担当するクラス。
/// UI からの要求を受けてロビー作成・参加・開始を行い、参加中ロビーの状態を監視する。
/// </summary>
public class LobbyManager : MonoBehaviour
{
    // ロビー参加者に対して Relay の JoinCode を共有するためのキー。
    public const string KEY_RELAY_CODE = "RelayCode";
    // ゲーム開始ボタンを有効にする最少人数。
    private const int MIN_PLAYERS_TO_START = 2;
    // ホストが Lobby Service へ生存通知を送る間隔。
    private const float HEARTBEAT_INTERVAL = 15f;
    // 非ホスト側がロビー状態を取り直す間隔。
    private const float POLLING_INTERVAL = 1f;
    // ロビー作成時の基本設定。
    private const string DEFAULT_LOBBY_NAME = "TestLobby";
    private const int DEFAULT_MAX_PLAYERS = 4;

    // ロビーUIの更新・ボタン制御を行う窓口。
    [SerializeField] private LobbyEventHandler eventHandler;
    // Relay セッションの作成 / 参加を実行するコンポーネント。
    [SerializeField] private RelayTest relayTest;
    // Netcodeで遷移させるゲーム本編シーン名（Build Settingsに登録が必要）。
    [SerializeField] private string gameplaySceneName = "Battle";

    // Lobby API 呼び出しとシーン遷移の責務を委譲するコンポーネント。
    private LobbyApiClient lobbyApi;
    private NetcodeSceneTransitionCoordinator sceneTransitionCoordinator;

    // 自分が現在参加中ロビーのホストかどうか。
    private bool isHost = false;
    // Unity Services / Authentication 初期化完了フラグ。
    private bool isInitialized;
    // 多重実行防止用の各種フラグ。
    private bool isCreatingLobby;
    private bool isJoiningLobby;
    private bool isStartingGame;
    private bool isRefreshingLobbies;
    private bool isJoiningRelayNetwork;

    // Authentication プロファイルにも使う表示名。
    private string playerName;
    // 自分が現在参加しているロビー。未参加なら null。
    private Lobby joinedLobby;

    // ホストがロビー生存を維持するための Heartbeat 送信制御。
    private float heartbeatTimer;
    // 非ホスト側がロビー更新を取りにいくポーリング間隔管理。
    private float pollingTimer;
    // 終了時のロビー離脱処理が重複しないようにするフラグ。
    private bool isLeavingLobby;

    private async void Start()
    {
        await InitializeAsync();
    }

    /// <summary>
    /// 終了時にロビーから離脱するための処理。
    /// </summary>
    private void OnDestroy()
    {
        UnsubscribeUiEvents();

        if (joinedLobby != null)
        {
            // 終了時にロビーへ残留しないよう、非同期で離脱または削除を試みる。
            _ = LeaveJoinedLobbyAsync(deleteLobbyIfHost: isHost);
        }
    }

    private void OnCreateLobbyRequested(object sender, System.EventArgs e)
    {
        CreateLobby();
    }

    private void OnRefreshLobbyListRequested(object sender, System.EventArgs e)
    {
        RefreshLobbies();
    }

    private void OnGameStartRequested(object sender, System.EventArgs e)
    {
        StartGame();
    }

    private void OnJoinLobbyRequested(object sender, Lobby lobby)
    {
        JoinLobby(lobby);
    }

    /// <summary>
    /// Lobby / Relay / UI 連携に必要な初期化を順番に行う。
    /// 
    /// Start を細かい処理で埋めないよう、初期化の流れはここへまとめている。
    /// これにより、起動直後の準備手順を上から追いやすくしている。
    /// </summary>
    private async Task InitializeAsync()
    {
        playerName = CreatePlayerName();
        await InitializeServicesAsync(playerName);

        lobbyApi = new LobbyApiClient(playerName, KEY_RELAY_CODE);
        sceneTransitionCoordinator = new NetcodeSceneTransitionCoordinator();
        isInitialized = true;

        SubscribeUiEvents();
        await DeleteOwnedLobbiesAsync();

        SyncUiState();
        await RefreshLobbiesAsync();
    }

    /// <summary>
    /// クライアントごとに固有のプロフィール名を作る。
    /// Lobby の参加者表示と Authentication プロファイルの衝突回避に使う。
    /// </summary>
    private string CreatePlayerName()
    {
        return "PlayerName" + UnityEngine.Random.Range(0, 1000);
    }

    /// <summary>
    /// Unity Services と匿名認証を初期化する。
    /// Lobby / Relay 利用前提をここでまとめて満たす。
    /// </summary>
    private static async Task InitializeServicesAsync(string profileName)
    {
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(profileName);

        await UnityServices.InitializeAsync(initializationOptions);
        AuthenticationService.Instance.SignedIn += () => Debug.Log("Signed in:" + AuthenticationService.Instance.PlayerId);
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    /// <summary>
    /// UI から飛んでくるイベントを購読する。
    /// 購読開始を初期化後へ寄せることで、未準備状態のイベント受信を避ける。
    /// </summary>
    private void SubscribeUiEvents()
    {
        LobbyEventHandler.OnCreateLobbyRequest += OnCreateLobbyRequested;
        LobbyEventHandler.OnRefreshLobbyListRequest += OnRefreshLobbyListRequested;
        LobbyEventHandler.OnGameStartRequest += OnGameStartRequested;
        LobbyBanner.OnJoinLobby += OnJoinLobbyRequested;
    }

    /// <summary>
    /// static event の購読を解除する。
    /// 解除し忘れると、破棄済みオブジェクトへ通知が飛んで例外や重複実行の原因になる。
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        LobbyEventHandler.OnCreateLobbyRequest -= OnCreateLobbyRequested;
        LobbyEventHandler.OnRefreshLobbyListRequest -= OnRefreshLobbyListRequested;
        LobbyEventHandler.OnGameStartRequest -= OnGameStartRequested;
        LobbyBanner.OnJoinLobby -= OnJoinLobbyRequested;
    }

    private void SyncUiState()
    {
        LobbyControlState controlState = LobbyUiStatePolicy.Build(
            isInitialized,
            isCreatingLobby,
            isJoiningLobby,
            isStartingGame,
            isRefreshingLobbies,
            isJoiningRelayNetwork,
            isHost,
            joinedLobby,
            MIN_PLAYERS_TO_START);

        eventHandler.SetControlsInteractable(controlState.CanCreateLobby, controlState.CanRefresh, controlState.CanStartGame);
    }

    /// <summary>
    /// 現在の認証済みプレイヤー ID を返す。
    /// 同じ取り方が複数箇所に出るため、意図を込めて 1 か所にまとめる。
    /// </summary>
    private static string CurrentPlayerId => AuthenticationService.Instance.PlayerId;

    /// <summary>
    /// 開始可能人数を満たしているかを返す。
    /// StartGame の条件式を短くして、何を見ているかを明確にする。
    /// </summary>
    private static bool HasEnoughPlayers(Lobby lobby)
    {
        return lobby != null && lobby.Players != null && lobby.Players.Count >= MIN_PLAYERS_TO_START;
    }

    /// <summary>
    /// Lobby Service 上にロビーを作成し、作成者自身も参加する。成功したらロビー一覧を更新する。
    /// </summary>
    public async void CreateLobby()
    {
        // まだ初期化前 / 作成中 / 既にどこかのロビー参加中なら何もしない。
        if (!CanCreateLobby())
        {
            SyncUiState();
            return;
        }

        try
        {
            isCreatingLobby = true;
            SyncUiState();

            joinedLobby = await lobbyApi.CreateLobbyAsync(DEFAULT_LOBBY_NAME, DEFAULT_MAX_PLAYERS, CurrentPlayerId);
            isHost = true;
            await RefreshLobbiesAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Error creating lobby: " + e.Message);
        }
        finally
        {
            isCreatingLobby = false;
            SyncUiState();
        }
    }

    private void Update()
    {
        // デバッグ用途: UI を使わなくても C キーでロビー作成できるようにしている。
        if (joinedLobby == null && Input.GetKeyDown(KeyCode.C))
        {
            CreateLobby();
        }

        UpdateLobbyHeartbeat();
        HandleLobbyPolling();
    }

    /// <summary>
    /// ホストは定期的に ping を送り、Lobby Service 上でロビー失効を防ぐ。
    /// </summary>
    private async void UpdateLobbyHeartbeat()
    {
        if (joinedLobby == null || !isHost)
        {
            return;
        }

        heartbeatTimer += Time.deltaTime;
        if (heartbeatTimer < HEARTBEAT_INTERVAL)
        {
            return;
        }

        heartbeatTimer = 0f;
        await lobbyApi.SendHeartbeatAsync(joinedLobby.Id);
    }

    /// <summary>
    /// Lobby Service からロビー一覧を取得し、UI を更新する。
    /// </summary>
    private async void RefreshLobbies() => await RefreshLobbiesAsync();

    /// <summary>
    /// Lobby Service からロビー一覧を取得し、UI を更新する。
    /// </summary>
    private async Task RefreshLobbiesAsync()
    {
        if (!isInitialized || isRefreshingLobbies)
        {
            SyncUiState();
            return;
        }

        try
        {
            isRefreshingLobbies = true;
            SyncUiState();

            QueryResponse queryResponse = await lobbyApi.QueryLobbiesAsync();
            Debug.Log("Lobbies Count:" + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results)
            {
                Debug.Log($"Lobby:name {lobby.Name} id {lobby.Id} code {lobby.LobbyCode} maxPlayers {lobby.MaxPlayers}");
            }

            eventHandler.Refresh(BuildDisplayLobbyList(queryResponse.Results));
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Exception:" + e.Message);
        }
        finally
        {
            isRefreshingLobbies = false;
            SyncUiState();
        }
    }

    /// <summary>
    /// 指定した Lobby へ参加する。参加後はロビー状態の監視に入る。
    /// </summary>
    /// <param name="lobby"></param>
    private async void JoinLobby(Lobby lobby)
    {
        // 未初期化 / Join 実行中 / すでに参加中のケースでは多重参加しない。
        if (!CanJoinLobby(lobby))
        {
            SyncUiState();
            Debug.Log("参加できませんでした");
            return;
        }

        try
        {
            isJoiningLobby = true;
            SyncUiState();

            joinedLobby = await lobbyApi.JoinLobbyByIdAsync(lobby.Id, CurrentPlayerId);
            isHost = false;
            Debug.Log("参加しました。:" + joinedLobby.Id);
            RefreshLobbies();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Exception:" + e.Message);
        }
        finally
        {
            isJoiningLobby = false;
            SyncUiState();
        }
    }

    public async void StartGame()
    {
        // 開始要求はホストのみ許可し、状態が整っていない場合は即座に戻す。
        if (!CanStartGame())
        {
            SyncUiState();
            // 状態が整っていない原因を確かめる
            Debug.Log("ゲームを開始できませんでした。isInitialized=" + isInitialized + " isHost=" + isHost + " joinedLobby=" + (joinedLobby != null) + " isStartingGame=" + isStartingGame);
            return;
        }

        // 開始前の状態を再確認し、開始要件を満たしていない場合は開始処理を中断する。
        if (!HasEnoughPlayers(joinedLobby))
        {
            Debug.Log($"プレイヤーが不足しています: {MIN_PLAYERS_TO_START}人以上必要です");
            SyncUiState();
            return;
        }

        try
        {
            isStartingGame = true;
            SyncUiState();

            Debug.Log("StartGame");
            // まずホスト側で Relay を確立し、全参加者が使う JoinCode を得る。
            string relayCode = await relayTest.CreateRelay();
            if (string.IsNullOrEmpty(relayCode))
            {
                Debug.LogWarning("Relayコードの作成に失敗しました");
                return;
            }

            // JoinCode をロビーの共有データへ保存し、参加済みクライアントへ伝播させる。
            joinedLobby = await lobbyApi.UpdateRelayCodeAsync(joinedLobby.Id, relayCode);

            Debug.Log("Lobby started: " + joinedLobby.Id);

            int expectedPlayers = joinedLobby.Players != null ? joinedLobby.Players.Count : MIN_PLAYERS_TO_START;
            await sceneTransitionCoordinator.WaitForExpectedClientsAndLoadSceneAsync(gameplaySceneName, expectedPlayers);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Error starting lobby: " + e.Message);
        }
        finally
        {
            isStartingGame = false;
            SyncUiState();
        }
    }

    /// <summary>
    /// 非ホスト側は参加中ロビーの状態を定期的に取り直し、ホストが開始フラグを立てたら Relay 参加へ進む。
    /// </summary>
    private async void HandleLobbyPolling()
    {
        if (!ShouldPollLobby())
        {
            return;
        }

        // 毎フレーム問い合わせないよう、1秒ごとにロビー状態を取り直す。
        pollingTimer += Time.deltaTime;
        if (pollingTimer < POLLING_INTERVAL)
        {
            return;
        }
        pollingTimer = 0f;

        try
        {
            // Lobby Service 上の最新状態を取り直し、開始フラグの変化を検出する。
            joinedLobby = await lobbyApi.GetLobbyAsync(joinedLobby.Id);

            if (isHost)
            {
                RefreshJoinedLobbyDisplay();
                SyncUiState();
                return;
            }

            await TryJoinRelayFromLobbyAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Polling failed: " + e.Message);
            return;
        }

        SyncUiState();
    }

    /// <summary>
    /// 同一プロフィールで残ったホストロビーがあると、一覧や接続検証のノイズになるため削除する。
    /// </summary>
    private async Task DeleteOwnedLobbiesAsync()
    {
        if (string.IsNullOrEmpty(CurrentPlayerId))
        {
            return;
        }

        try
        {
            await lobbyApi.DeleteOwnedLobbiesAsync(CurrentPlayerId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Failed to clean stale lobbies: " + e.Message);
        }
    }

    /// <summary>
    /// 参加中ロビーからの離脱処理。
    /// ホストはロビー削除、非ホストは自分だけロビーから抜ける。
    /// </summary>
    private async Task LeaveJoinedLobbyAsync(bool deleteLobbyIfHost)
    {
        // 離脱処理の多重実行を防ぎつつ、状態が無効なら即座に終了する。
        if (!isInitialized || joinedLobby == null || isLeavingLobby)
        {
            return;
        }

        isLeavingLobby = true;

        string lobbyId = joinedLobby.Id;
        string playerId = CurrentPlayerId;

        try
        {
            bool shouldDeleteLobby = deleteLobbyIfHost && isHost;
            await lobbyApi.LeaveLobbyAsync(lobbyId, playerId, shouldDeleteLobby);

            if (shouldDeleteLobby)
            {
                Debug.Log("Lobby deleted on exit: " + lobbyId);
            }
            else if (!string.IsNullOrEmpty(playerId))
            {
                Debug.Log("Lobby left on exit: " + lobbyId);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log("Failed to leave lobby on exit: " + e.Message);
        }
        finally
        {
            joinedLobby = null;
            isHost = false;
            heartbeatTimer = 0f;
            pollingTimer = 0f;
            isLeavingLobby = false;
            SyncUiState();
        }
    }

    /// <summary>
    /// アプリケーション終了時にロビーから離脱するための処理。
    /// </summary>
    void OnApplicationQuit()
    {
        // 終了時にロビーへ残留しないよう、非同期で離脱または削除を試みる。
        _ = LeaveJoinedLobbyAsync(deleteLobbyIfHost: isHost);
    }

    /// <summary>
    /// ロビー作成要求を受け付けてよい状態かを返す。
    /// 条件式をメソッド名へ押し込むことで、呼び出し側の可読性を上げる。
    /// </summary>
    private bool CanCreateLobby()
    {
        return isInitialized && !isCreatingLobby && joinedLobby == null;
    }

    /// <summary>
    /// 指定ロビーへの参加要求を受け付けてよいかを返す。
    /// 参加先 null や多重参加をここでまとめて弾く。
    /// </summary>
    private bool CanJoinLobby(Lobby lobby)
    {
        return isInitialized && !isJoiningLobby && joinedLobby == null && lobby != null;
    }

    /// <summary>
    /// ゲーム開始要求を受け付けてよい状態かを返す。
    /// StartGame 側の条件分岐を短くし、失敗時ログの補助に使う。
    /// </summary>
    private bool CanStartGame()
    {
        return isInitialized && isHost && joinedLobby != null && !isStartingGame;
    }

    /// <summary>
    /// ロビーのポーリングを行うべき状態かを返す。
    /// 非ホストだけでなく、ホストも最新状態追従のため取得自体は行う。
    /// </summary>
    private bool ShouldPollLobby()
    {
        return isInitialized && joinedLobby != null;
    }

    /// <summary>
    /// ロビー一覧表示用の配列を組み立てる。
    /// 参加中ロビーがクエリ結果にまだ載っていない場合でも、画面から消えないように補う。
    /// </summary>
    private List<Lobby> BuildDisplayLobbyList(IReadOnlyList<Lobby> queryResults)
    {
        List<Lobby> displayList = new List<Lobby>(queryResults);
        if (joinedLobby == null)
        {
            Debug.Log("Not joined to any lobby");
            return displayList;
        }

        int index = displayList.FindIndex(lobby => lobby.Id == joinedLobby.Id);
        if (index >= 0)
        {
            joinedLobby = displayList[index];
            return displayList;
        }

        displayList.Insert(0, joinedLobby);
        return displayList;
    }

    /// <summary>
    /// ホストが参加者変化を検知したときに、自分のロビー表示だけを即時更新する。
    /// 
    /// 毎回ロビー一覧を再クエリしなくても、GetLobbyAsync で取得した最新 joinedLobby を
    /// そのまま一覧 UI へ反映できるため、処理を軽く保ちながら人数表示を追従できる。
    /// </summary>
    private void RefreshJoinedLobbyDisplay()
    {
        if (joinedLobby == null)
        {
            return;
        }

        eventHandler.Refresh(BuildDisplayLobbyList(new[] { joinedLobby }));
    }

    /// <summary>
    /// 非ホスト側でロビー共有データから RelayCode を検出したら接続を開始する。
    /// 成功後はロビー参照を手放して一覧表示へ戻り、失敗時は次回ポーリングで再試行する。
    /// </summary>
    private async Task TryJoinRelayFromLobbyAsync()
    {
        if (isJoiningRelayNetwork || !TryGetRelayCode(joinedLobby, out string relayCode))
        {
            return;
        }

        Debug.Log("StartGame");
        isJoiningRelayNetwork = true;
        bool joinedRelay = await relayTest.JoinRelayAsync(relayCode);
        isJoiningRelayNetwork = false;

        if (!joinedRelay)
        {
            Debug.LogWarning("Relay 参加に失敗しました。次のポーリングで再試行します");
            return;
        }

        joinedLobby = null;
        await RefreshLobbiesAsync();
    }

    /// <summary>
    /// ロビー共有データから RelayCode を安全に取り出す。
    /// null チェックと初期値 "0" 判定をまとめ、呼び出し側を短くする。
    /// </summary>
    private static bool TryGetRelayCode(Lobby lobby, out string relayCode)
    {
        relayCode = null;
        if (lobby == null || lobby.Data == null || !lobby.Data.ContainsKey(KEY_RELAY_CODE))
        {
            return false;
        }

        relayCode = lobby.Data[KEY_RELAY_CODE].Value;
        return !string.IsNullOrEmpty(relayCode) && relayCode != "0";
    }
}