using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Common;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

/// <summary>
/// Relay の割り当て作成・参加と、Netcode の Host / Client 起動を担当するクラス。
/// LobbyManager から JoinCode を受け取り、実際の通信セッションへ接続する。
/// </summary>
public class RelayTest : MonoBehaviour
{
    // RelayTest は Lobby で初期化したあと、Load や Battle へ移っても役割が続く。
    // そのためシーン遷移で破棄せず、1インスタンスだけ維持する。
    private static RelayTest instance;

    // シーン上の NetworkManager をキャッシュして毎回 Singleton を引かないようにする。
    private NetworkManager networkManager;
    // Battle 到達後に手動生成するため、元の PlayerPrefab を保持する。
    private GameObject originalPlayerPrefab;

    /// <summary>
    /// インスタンスを 1 つに保ちつつ、シーンをまたいで利用できるように初期化する。
    /// 
    /// Lobby から Load、Battle へ進んだあとも Player 生成や接続監視が必要なので、
    /// このコンポーネントは破棄させない。
    /// </summary>
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (!TryInitializeNetworkManager())
        {
            return;
        }

        SubscribeSceneEvents();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        // Unity 標準のシーンロード購読も解除して、破棄済みインスタンスへ通知が飛ばないようにする。
        SceneManager.sceneLoaded -= HandleUnitySceneLoaded;

        if (networkManager == null)
        {
            return;
        }

        UnsubscribeNetworkEvents();
    }

    /// <summary>
    /// Netcode と PlayerPrefab の参照を初期化する。
    /// ここで失敗した場合は以降の Relay 制御が成立しないため、早めに停止する。
    /// </summary>
    private bool TryInitializeNetworkManager()
    {
        networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager が見つかりません");
            return false;
        }

        originalPlayerPrefab = networkManager.NetworkConfig != null ? networkManager.NetworkConfig.PlayerPrefab : null;
        networkManager.OnServerStarted += HandleServerStarted;
        return true;
    }

    /// <summary>
    /// シーン遷移検知に使うイベントを購読する。
    /// Netcode と Unity 標準の両方を使い、Battle 到達の取りこぼしを防ぐ。
    /// </summary>
    private void SubscribeSceneEvents()
    {
        SceneManager.sceneLoaded -= HandleUnitySceneLoaded;
        SceneManager.sceneLoaded += HandleUnitySceneLoaded;
    }

    /// <summary>
    /// RelayTest が購読したイベントをすべて解除する。
    /// イベント解除を 1 か所へまとめ、終了処理の見通しを良くする。
    /// </summary>
    private void UnsubscribeNetworkEvents()
    {
        networkManager.OnServerStarted -= HandleServerStarted;
        networkManager.OnClientConnectedCallback -= HandleClientConnected;
        networkManager.ConnectionApprovalCallback -= ApproveConnectionWithoutPlayer;

        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
        }
    }

    /// <summary>
    /// Netcode が既に起動中のときに Host / Client の二重開始を防ぐ。
    /// 起動失敗の理由もここでそろえて出す。
    /// </summary>
    private bool CanStartNetwork(string targetMode)
    {
        if (networkManager == null)
        {
            Debug.LogWarning("NetworkManager が見つかりません");
            return false;
        }

        if (!networkManager.IsListening)
        {
            return true;
        }

        Debug.LogWarning($"NetworkManager は既に起動中です。{targetMode} の開始をスキップしました");
        return false;
    }

    private void HandleServerStarted()
    {
        if (networkManager == null || !networkManager.IsServer)
        {
            return;
        }

        RegisterServerCallbacks();
    }

    /// <summary>
    /// Host 起動後に必要になるコールバックを登録する。
    /// Battle 到達後の一括生成と、Battle 中の後続接続補完の両方をここで有効化する。
    /// </summary>
    private void RegisterServerCallbacks()
    {
        networkManager.OnClientConnectedCallback -= HandleClientConnected;
        networkManager.OnClientConnectedCallback += HandleClientConnected;

        if (networkManager.SceneManager == null)
        {
            return;
        }

        networkManager.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
        networkManager.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
    }

    /// <summary>
    /// Battle シーン滞在中に新しいクライアント接続が完了したときに呼ばれる。
    /// 
    /// Lobby や Load ではプレイヤーを出したくないため、
    /// Battle がアクティブなときだけ補完生成する。
    /// </summary>
    private void HandleClientConnected(ulong clientId)
    {
        // Lobby や Load ではまだプレイヤーを出さず、Battle に入ってから生成する。
        if (!IsGameplaySceneActive())
        {
            return;
        }

        EnsurePlayerObject(clientId);
    }

    /// <summary>
    /// Netcode のシーン同期が完了したタイミングを受け取る。
    /// 
    /// Host は Battle への全体遷移が終わったあと、接続済み全員に対して
    /// PlayerObject が存在するかを確認し、足りない分だけ生成する。
    /// </summary>
    private void HandleLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        if (!ShouldSpawnPlayersForBattle(sceneName))
        {
            return;
        }

        EnsurePlayersForConnectedClients();
    }

    /// <summary>
    /// Unity 標準のシーンロード通知を受け取るフォールバック経路。
    /// 
    /// Netcode 側のイベント取りこぼしがあっても Battle 到達を検出できるようにして、
    /// Player 未生成の再発を防ぐ。
    /// </summary>
    private void HandleUnitySceneLoaded(Scene loadedScene, LoadSceneMode loadSceneMode)
    {
        if (!ShouldSpawnPlayersForBattle(loadedScene.name))
        {
            return;
        }

        EnsurePlayersForConnectedClients();
    }

    /// <summary>
    /// 指定クライアントに PlayerObject が存在しなければ生成する。
    /// 
    /// ここは「本当に生成が必要か」を最終判定する場所として使い、
    /// 複数のイベントから呼ばれても二重生成しないようにしている。
    /// </summary>
    private void EnsurePlayerObject(ulong clientId)
    {
        if (networkManager == null || !networkManager.IsServer)
        {
            return;
        }

        if (!networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client))
        {
            return;
        }

        // Netcode の自動生成で既に PlayerObject がある場合は何もしない。
        if (client.PlayerObject != null)
        {
            return;
        }

        // 接続時の自動生成は止めているため、
        // Battle 到達後の手動生成では元の PlayerPrefab を使う。
        GameObject playerPrefab = GetManualPlayerPrefab();
        if (playerPrefab == null)
        {
            Debug.LogError("NetworkConfig.PlayerPrefab が未設定です");
            return;
        }

        GameObject playerObject = Instantiate(playerPrefab);
        NetworkObject networkObject = playerObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError("PlayerPrefab に NetworkObject がありません");
            Destroy(playerObject);
            return;
        }

        // シーン遷移後もプレイヤーを維持するため destroyWithScene は false にする。
        networkObject.SpawnAsPlayerObject(clientId, false);
    }

    /// <summary>
    /// 現在接続済みの全クライアントに対して PlayerObject を確認する。
    /// 
    /// Host 自身も ConnectedClientsIds に含まれるため、ここを通せば
    /// Host と Client を分けずに同じ流れで生成できる。
    /// </summary>
    private void EnsurePlayersForConnectedClients()
    {
        if (networkManager == null || !networkManager.IsServer)
        {
            return;
        }

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            EnsurePlayerObject(clientId);
        }
    }

    /// <summary>
    /// Battle 到達に伴う Player 生成を行うべきかを判定する。
    /// Host かつ Battle シーンであることをまとめて確認する。
    /// </summary>
    private bool ShouldSpawnPlayersForBattle(string sceneName)
    {
        return networkManager != null && networkManager.IsServer && IsBattleScene(sceneName);
    }

    /// <summary>
    /// 手動生成に使う PlayerPrefab を返す。
    /// 元の参照が消えていた場合は NetworkConfig 側も見にいき、取得経路を 1 つにまとめる。
    /// </summary>
    private GameObject GetManualPlayerPrefab()
    {
        if (originalPlayerPrefab != null)
        {
            return originalPlayerPrefab;
        }

        if (networkManager != null && networkManager.NetworkConfig != null)
        {
            return networkManager.NetworkConfig.PlayerPrefab;
        }

        return null;
    }

    /// <summary>
    /// 接続承認時に PlayerObject の自動生成だけを止める。
    /// 
    /// 以前のように PlayerPrefab 自体を null にすると、NGO や Multiplayer Tools が
    /// 想定する内部状態を壊してしまうため、接続承認で生成有無だけ制御する。
    /// これなら接続は成功させつつ、Player の出現タイミングだけ Battle まで遅らせられる。
    /// </summary>
    private void ApproveConnectionWithoutPlayer(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        response.Approved = true;
        response.CreatePlayerObject = false;
        response.Pending = false;
    }

    /// <summary>
    /// いまアクティブなシーンがゲーム本編シーンかどうかを返す。
    /// 生成タイミングを Battle 限定にしたいので、接続時判定の入口に置いている。
    /// </summary>
    private bool IsGameplaySceneActive()
    {
        return IsBattleScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// 指定シーン名が Battle シーンかどうかを判定する。
    /// シーン名の比較ロジックをまとめ、条件の散在を防ぐ。
    /// </summary>
    private bool IsBattleScene(string sceneName)
    {
        return sceneName == GlobalCommon.DefaultGameplaySceneName;
    }

    /// <summary>
    /// Netcode の接続承認を使い、接続時の Player 自動生成を止める。
    /// 
    /// PlayerPrefab を null にするのではなく、承認レスポンスの
    /// CreatePlayerObject を false にすることで、安全に Lobby 生成だけを抑制する。
    /// </summary>
    private void EnableDeferredPlayerCreation()
    {
        if (networkManager == null || networkManager.NetworkConfig == null)
        {
            return;
        }

        if (originalPlayerPrefab == null)
        {
            originalPlayerPrefab = networkManager.NetworkConfig.PlayerPrefab;
        }

        // 接続自体は許可しつつ、PlayerObject の自動生成だけを止める。
        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.ConnectionApprovalCallback -= ApproveConnectionWithoutPlayer;
        networkManager.ConnectionApprovalCallback += ApproveConnectionWithoutPlayer;
    }

    /// <summary>
    /// Relay 利用前提条件が揃っているかを外部から確認するためのプロパティ。
    /// </summary>
    public bool IsReady
    {
        get
        {
            return UnityServices.State == ServicesInitializationState.Initialized
                && AuthenticationService.Instance.IsSignedIn;
        }
    }
    
    public async Task<bool> JoinRelayAsync(string joinCode)
    {
        if (!IsReady)
        {
            Debug.LogWarning("Unity Services が初期化されていません。LobbyManager の初期化完了後に実行してください");
            return false;
        }

        if (!CanStartNetwork("Client"))
        {
            return false;
        }

        try
        {
            Debug.Log("JoinRelay code = " + joinCode);

            // Host 側で ConnectionApproval を有効にしているため、
            // Client 側も同じ設定にそろえないと ConnectionRequest の形式が一致せず、
            // NetworkConfig mismatch として接続自体が失敗する。
            // 
            // Client では承認コールバックは使われないが、
            // ConnectionApproval フラグ自体は Host と同じ値にしておく必要がある。
            EnableDeferredPlayerCreation();
            await ConfigureClientRelayAsync(joinCode);

            if (!networkManager.StartClient())
            {
                Debug.LogWarning("StartClient に失敗しました");
                return false;
            }

            return await WaitForStartedClientAsync();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return false;
        }
    }

    public async void JoinRelay(string joinCode)
    {
        await JoinRelayAsync(joinCode);
    }

    public async Task<string> CreateRelay()
    {
        if (!IsReady)
        {
            Debug.LogWarning("Unity Services が初期化されていません。LobbyManager の初期化完了後に実行してください");
            return null;
        }

        if (!CanStartNetwork("Host"))
        {
            // Host として起動できない場合は null を返す。呼び出し元で JoinCode の null チェックを行う想定。
            Debug.Log("Host として起動できないため、CreateRelay をスキップします");
            return null;
        }

        try
        {
            // LobbyManager から呼ばれる本経路。ホスト用の Relay セッションを確立して JoinCode を返す。
            // Host 側は接続承認で Player 自動生成を止め、Battle 到達後にだけ手動生成する。
            EnableDeferredPlayerCreation();
            string joinCode = await ConfigureHostRelayAsync();

            networkManager.StartHost();
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

    private async Task<bool> WaitForClientConnectionAsync()
    {
        if (networkManager == null)
        {
            return false;
        }

        const int timeoutMs = 15000;
        const int waitStepMs = 100;
        int elapsedMs = 0;

        while (elapsedMs < timeoutMs)
        {
            if (networkManager.IsConnectedClient)
            {
                return true;
            }

            await Task.Delay(waitStepMs);
            elapsedMs += waitStepMs;
        }

        return false;
    }

    /// <summary>
    /// JoinCode から Client 用の Relay 設定を組み立てる。
    /// 接続準備だけを分離することで、JoinRelayAsync の見通しを良くする。
    /// </summary>
    private async Task ConfigureClientRelayAsync(string joinCode)
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        networkManager.GetComponent<UnityTransport>().SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData);
    }

    /// <summary>
    /// Host 用の Relay セッションを確立し、JoinCode を返す。
    /// Host 起動前の準備を 1 メソッドへ閉じ込め、CreateRelay を短くする。
    /// </summary>
    private async Task<string> ConfigureHostRelayAsync()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        Debug.Log(joinCode);

        networkManager.GetComponent<UnityTransport>().SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData);

        return joinCode;
    }

    /// <summary>
    /// StartClient 成功後、実際に接続完了状態になるまで待機する。
    /// 失敗時は Shutdown まで行い、呼び出し側の後始末を減らす。
    /// </summary>
    private async Task<bool> WaitForStartedClientAsync()
    {
        bool connected = await WaitForClientConnectionAsync();
        if (connected)
        {
            Debug.Log("StartClient 成功");
            return true;
        }

        Debug.LogWarning("Client 接続完了前にタイムアウトしました。NetworkManager を停止します");
        networkManager.Shutdown();
        return false;
    }
}