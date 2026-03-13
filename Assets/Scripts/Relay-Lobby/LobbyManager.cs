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
    private float heartbeatInterval = 15f;
    // 非ホスト側がロビー更新を取りにいくポーリング間隔管理。
    private float pollingTimer = 0f;
    // 終了時のロビー離脱処理が重複しないようにするフラグ。
    private bool isLeavingLobby;

    private async void Start()
    {
        // クライアントごとに別プロフィールを使うため、ランダム名を付与する。
        playerName = "PlayerName" + UnityEngine.Random.Range(0, 1000).ToString();
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);

        // Lobby / Relay / Authentication 利用前に Unity Services を初期化する。
        await UnityServices.InitializeAsync(initializationOptions);

        // 認証状態の変化をログに出す。実際のサインインは匿名で行う。
        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in:" + AuthenticationService.Instance.PlayerId);
        };
        
        // 開発時に扱いやすい匿名認証を利用する。
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        isInitialized = true;
        lobbyApi = new LobbyApiClient(playerName, KEY_RELAY_CODE);
        sceneTransitionCoordinator = new NetcodeSceneTransitionCoordinator();

        // 前回異常終了で残った自分ホストのロビーを掃除してから開始する。
        await DeleteOwnedLobbiesAsync();

        // UI からのイベント購読を初期化完了後に開始する。
        LobbyEventHandler.OnCreateLobbyRequest += OnCreateLobbyRequested;
        LobbyEventHandler.OnRefreshLobbyListRequest += OnRefreshLobbyListRequested;
        LobbyEventHandler.OnGameStartRequest += OnGameStartRequested;
        LobbyBanner.OnJoinLobby += OnJoinLobbyRequested;

        SyncUiState();
        await RefreshLobbiesAsync();
    }

    /// <summary>
    /// 終了時にロビーから離脱するための処理。
    /// </summary>
    private void OnDestroy()
    {
        // static event の購読解除を忘れると、破棄済みオブジェクトに通知が飛ぶ。
        LobbyEventHandler.OnCreateLobbyRequest -= OnCreateLobbyRequested;
        LobbyEventHandler.OnRefreshLobbyListRequest -= OnRefreshLobbyListRequested;
        LobbyEventHandler.OnGameStartRequest -= OnGameStartRequested;
        LobbyBanner.OnJoinLobby -= OnJoinLobbyRequested;

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
    /// Lobby Service 上にロビーを作成し、作成者自身も参加する。成功したらロビー一覧を更新する。
    /// </summary>
    public async void CreateLobby()
    {
        // まだ初期化前 / 作成中 / 既にどこかのロビー参加中なら何もしない。
        if (!isInitialized || isCreatingLobby || joinedLobby != null)
        {
            SyncUiState();
            return;
        }

        try
        {
            isCreatingLobby = true;
            SyncUiState();

            string lobbyName = "TestLobby";
            int maxPlayers = 4;
            string playerId = AuthenticationService.Instance.PlayerId;

            joinedLobby = await lobbyApi.CreateLobbyAsync(lobbyName, maxPlayers, playerId);
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

        RefreshLobbyHeatbeat();
        HandleLobbyPolling();
    }

    /// <summary>
    /// ホストは定期的に ping を送り、Lobby Service 上でロビー失効を防ぐ。
    /// </summary>
    private async void RefreshLobbyHeatbeat()
    {
        if (joinedLobby != null && isHost)
        {
            heartbeatTimer += Time.deltaTime;
            if (heartbeatTimer > heartbeatInterval)
            {
                heartbeatTimer = 0.0f;
                // ホストは定期的に ping を送り、Lobby Service 上でロビー失効を防ぐ。
                await lobbyApi.SendHeartbeatAsync(joinedLobby.Id);
            }
        }
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
            foreach (var lobby in queryResponse.Results)
            {
                Debug.Log($"Lobby:name {lobby.Name} id {lobby.Id} code {lobby.LobbyCode} maxPlayers {lobby.MaxPlayers}");
            }

            // 取得結果をそのまま一覧表示のベースにする。
            var displayList = new List<Lobby>(queryResponse.Results);

            if (joinedLobby != null)
            {
                int index = displayList.FindIndex(lobby => lobby.Id == joinedLobby.Id);
                if (index >= 0)
                {
                    // クエリ結果で最新情報に更新
                    joinedLobby = displayList[index];
                }
                else
                {
                    // Query 結果へまだ反映されていない場合でも、参加中ロビーだけは画面から消さない。
                    displayList.Insert(0, joinedLobby);
                }
            }
            else
            {
                Debug.Log("Not joined to any lobby");
            }
            
            eventHandler.Refresh(displayList);
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
        if (!isInitialized || isJoiningLobby || joinedLobby != null)
        {
            SyncUiState();
            Debug.Log("参加できませんでした");
            return;
        }

        try
        {
            isJoiningLobby = true;
            SyncUiState();

            joinedLobby = await lobbyApi.JoinLobbyByIdAsync(lobby.Id, AuthenticationService.Instance.PlayerId);
            isHost = false;
            Debug.Log("参加しました。:" + joinedLobby.Id);
            await RefreshLobbiesAsync();
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
        if (!isInitialized || !isHost || joinedLobby == null || isStartingGame)
        {
            SyncUiState();
            // 状態が整っていない原因を確かめる
            Debug.Log("ゲームを開始できませんでした。isInitialized=" + isInitialized + " isHost=" + isHost + " joinedLobby=" + (joinedLobby != null) + " isStartingGame=" + isStartingGame);
            return;
        }

        // 開始前の状態を再確認し、開始要件を満たしていない場合は開始処理を中断する。
        if (joinedLobby.Players == null || joinedLobby.Players.Count < MIN_PLAYERS_TO_START)
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
        if (!isInitialized || joinedLobby == null)
        {
            return;
        }

        // 毎フレーム問い合わせないよう、1秒ごとにロビー状態を取り直す。
        pollingTimer += Time.deltaTime;
        if (pollingTimer < 1f)
        {
            return;
        }
        pollingTimer = 0f;

        try
        {
            // Lobby Service 上の最新状態を取り直し、開始フラグの変化を検出する。
            joinedLobby = await lobbyApi.GetLobbyAsync(joinedLobby.Id);
            if (!isHost)
            {
                if (isJoiningRelayNetwork)
                {
                    return;
                }

                if (joinedLobby.Data != null
                    && joinedLobby.Data.ContainsKey(KEY_RELAY_CODE)
                    && joinedLobby.Data[KEY_RELAY_CODE].Value != "0")
                {
                    // ホストが JoinCode を書き込んだら、クライアント側は Relay 参加へ進む。
                    Debug.Log("StartGame");
                    string relayCode = joinedLobby.Data[KEY_RELAY_CODE].Value;
                    isJoiningRelayNetwork = true;
                    bool joinedRelay = await relayTest.JoinRelayAsync(relayCode);
                    isJoiningRelayNetwork = false;

                    if (joinedRelay)
                    {
                        // 通信開始後はロビー一覧表示へ戻すため、参加中ロビー参照を手放す。
                        joinedLobby = null;
                        await RefreshLobbiesAsync();
                    }
                    else
                    {
                        Debug.LogWarning("Relay 参加に失敗しました。次のポーリングで再試行します");
                    }
                };
            }
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
        string playerId = AuthenticationService.Instance.PlayerId;
        if (string.IsNullOrEmpty(playerId)) return;

        try
        {
            await lobbyApi.DeleteOwnedLobbiesAsync(playerId);
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
        string playerId = AuthenticationService.Instance.PlayerId;

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
}