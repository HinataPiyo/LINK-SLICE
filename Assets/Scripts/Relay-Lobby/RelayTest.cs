using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using System.Threading.Tasks;

/// <summary>
/// Relay の割り当て作成・参加と、Netcode の Host / Client 起動を担当するクラス。
/// LobbyManager から JoinCode を受け取り、実際の通信セッションへ接続する。
/// </summary>
public class RelayTest : MonoBehaviour
{
    // シーン上の NetworkManager をキャッシュして毎回 Singleton を引かないようにする。
    private NetworkManager networkManager;

    // Netcode が既に起動中のときに Host/Client の二重開始を防ぐ。
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

    private void Start()
    {
        // 起動時に NetworkManager を確認し、サーバー開始イベントを購読する。
        networkManager = NetworkManager.Singleton;
        if(networkManager == null)
        {
            Debug.LogError("NetworkManager が見つかりません");
            return;
        }
        networkManager.OnServerStarted += HandleServerStarted;
    }

    private void OnDestroy()
    {
        if (networkManager == null)
        {
            return;
        }

        networkManager.OnServerStarted -= HandleServerStarted;
        networkManager.OnClientConnectedCallback -= HandleClientConnected;
    }

    private void HandleServerStarted()
    {
        if (networkManager == null || !networkManager.IsServer)
        {
            return;
        }

        // Host 起動後は各クライアント接続時に PlayerObject を確認できるようにする。
        networkManager.OnClientConnectedCallback -= HandleClientConnected;
        networkManager.OnClientConnectedCallback += HandleClientConnected;
    }

    private void HandleClientConnected(ulong clientId)
    {
        EnsurePlayerObject(clientId);
    }

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

        // 自動生成されなかったケースに備え、PlayerPrefab を明示的にスポーンする。
        GameObject playerPrefab = networkManager.NetworkConfig.PlayerPrefab;
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
            // JoinCode から接続先 Relay 情報を引き当て、Client 用の Transport 設定を行う。
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            networkManager.GetComponent<UnityTransport>().SetClientRelayData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
                );

            bool started = networkManager.StartClient();
            if (!started)
            {
                Debug.LogWarning("StartClient に失敗しました");
                return false;
            }

            Debug.Log("StartClient 成功");
            return true;
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
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);

            networkManager.GetComponent<UnityTransport>().SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
                );

            networkManager.StartHost();
            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }
}