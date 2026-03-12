using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;

public class NetWorkManagerUI : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button cliantButton;
    [SerializeField] Button battleStartButton;
    [SerializeField] string battleSceneName = "Battle";
    [SerializeField] int minPlayersToStart = 2;
    bool callbacksRegistered;

    void Awake()
    {
        battleStartButton.interactable = false;

        hostButton.onClick.AddListener(() =>
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                Debug.LogWarning("NetworkManager が見つかりません");
                return;
            }

            if (networkManager.IsListening)
            {
                Debug.LogWarning("すでにセッションが開始されています");
                return;
            }

            bool started = networkManager.StartHost();
            if (!started)
            {
                Debug.LogWarning("Host の起動に失敗しました");
                return;
            }

            hostButton.interactable = false;
            cliantButton.interactable = false;
            RegisterNetworkCallbacks(networkManager);
            RefreshBattleStartButtonState(networkManager);
        });

        cliantButton.onClick.AddListener(() =>
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                Debug.LogWarning("NetworkManager が見つかりません");
                return;
            }

            if (networkManager.IsListening)
            {
                Debug.LogWarning("すでにセッションが開始されています");
                return;
            }

            bool started = networkManager.StartClient();
            if (!started)
            {
                Debug.LogWarning("Client の接続開始に失敗しました");
                return;
            }

            hostButton.interactable = false;
            cliantButton.interactable = false;
            RegisterNetworkCallbacks(networkManager);
            RefreshBattleStartButtonState(networkManager);
        });

        battleStartButton.onClick.AddListener(() =>
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null)
            {
                Debug.LogWarning("NetworkManager が見つかりません");
                return;
            }

            // サーバー側でゲーム開始の処理を呼び出す
            if (!networkManager.IsServer) return;

            if (!networkManager.IsListening)
            {
                Debug.LogWarning("ネットワークが開始されていません");
                return;
            }

            int playerCount = networkManager.ConnectedClientsList.Count;
            int requiredPlayers = Mathf.Max(2, minPlayersToStart);
            if (playerCount < requiredPlayers)
            {
                Debug.LogWarning($"プレイヤーが不足しています（現在: {playerCount}人）。{requiredPlayers}人以上で開始できます");
                return;
            }

            Debug.Log("ゲーム開始!（ネットワーク同期でBattleへ遷移）");
            networkManager.SceneManager.LoadScene(battleSceneName, LoadSceneMode.Single);
        });
    }

    void OnDestroy()
    {
        UnregisterNetworkCallbacks();
    }

    void RegisterNetworkCallbacks(NetworkManager networkManager)
    {
        if (callbacksRegistered || networkManager == null) return;

        networkManager.OnClientConnectedCallback += OnClientConnectionChanged;
        networkManager.OnClientDisconnectCallback += OnClientConnectionChanged;
        callbacksRegistered = true;
    }

    void UnregisterNetworkCallbacks()
    {
        if (!callbacksRegistered) return;

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnectionChanged;
            networkManager.OnClientDisconnectCallback -= OnClientConnectionChanged;
        }

        callbacksRegistered = false;
    }

    void OnClientConnectionChanged(ulong _)
    {
        RefreshBattleStartButtonState(NetworkManager.Singleton);
    }

    void RefreshBattleStartButtonState(NetworkManager networkManager)
    {
        if (battleStartButton == null) return;
        if (networkManager == null || !networkManager.IsListening)
        {
            battleStartButton.interactable = false;
            return;
        }

        // クライアント側では常に開始ボタンを無効化
        if (!networkManager.IsServer)
        {
            battleStartButton.interactable = false;
            return;
        }

        int requiredPlayers = Mathf.Max(2, minPlayersToStart);
        battleStartButton.interactable = networkManager.ConnectedClientsList.Count >= requiredPlayers;
    }
}