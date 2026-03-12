using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ロビー/セッション開始フェーズで使うネットワーク制御サービス。
///
/// 主な役割:
/// 1. Host/Client の起動要求を安全に実行する
/// 2. 参加人数などの状態を集約し、UI が参照しやすい形で公開する
/// 3. 開始条件を満たしたときだけ Battle シーン遷移を実行する
///
/// MonoBehaviour ではなく純粋なサービスクラスとして設計されているため、
/// 生成/破棄タイミングは呼び出し側（Presenter や Controller）で管理する。
/// </summary>
public sealed class NetworkSessionService : IDisposable
{
    // 最低開始人数。コンストラクタで 2 以上に丸める。
    readonly int minPlayersToStart;

    // シーン遷移先。未指定時は "Battle" を既定値として使う。
    readonly string battleSceneName;

    // OnClientConnected/OnClientDisconnect の二重登録防止フラグ。
    bool callbacksRegistered;

    // NetworkManager から取り込んだ最新状態を UI 向けに公開する。
    public bool IsListening { get; private set; }
    public bool IsServer { get; private set; }
    public int ConnectedPlayerCount { get; private set; }
    public bool CanStartBattle { get; private set; }

    // 状態が更新されたら発火。UI 側はこれをトリガーに再描画する。
    public event Action SessionStateChanged;

    public NetworkSessionService(int minPlayersToStart, string battleSceneName)
    {
        // ゲーム開始に最低 2 人必要という前提をサービス内で保証する。
        this.minPlayersToStart = Mathf.Max(2, minPlayersToStart);

        // シーン名が空/空白なら既定シーンにフォールバックする。
        this.battleSceneName = string.IsNullOrWhiteSpace(battleSceneName) ? "Battle" : battleSceneName;
    }

    /// <summary>
    /// Host（サーバー兼クライアント）としてネットワークを開始する。
    /// </summary>
    /// <returns>
    /// true: 開始要求に成功
    /// false: NetworkManager 不在、既に開始済み、または StartHost 失敗
    /// </returns>
    public bool TryStartHost()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogWarning("NetworkManager が見つかりません");
            return false;
        }

        if (networkManager.IsListening)
        {
            Debug.LogWarning("すでにセッションが開始されています");
            RefreshState();
            return false;
        }

        bool started = networkManager.StartHost();
        if (!started)
        {
            Debug.LogWarning("Host の起動に失敗しました");
            RefreshState();
            return false;
        }

        RegisterNetworkCallbacks(networkManager);
        RefreshState();
        return true;
    }

    /// <summary>
    /// Client として接続を開始する。
    /// </summary>
    /// <returns>
    /// true: 接続開始要求に成功
    /// false: NetworkManager 不在、既に開始済み、または StartClient 失敗
    /// </returns>
    public bool TryStartClient()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogWarning("NetworkManager が見つかりません");
            return false;
        }

        if (networkManager.IsListening)
        {
            Debug.LogWarning("すでにセッションが開始されています");
            RefreshState();
            return false;
        }

        bool started = networkManager.StartClient();
        if (!started)
        {
            Debug.LogWarning("Client の接続開始に失敗しました");
            RefreshState();
            return false;
        }

        RegisterNetworkCallbacks(networkManager);
        RefreshState();
        return true;
    }

    /// <summary>
    /// Battle シーンへの遷移を要求する。
    ///
    /// 実行できるのは Server のみで、かつ最低人数を満たしている必要がある。
    /// Netcode の SceneManager を使うことで、接続中クライアントにも遷移が同期される。
    /// </summary>
    /// <returns>
    /// true: 遷移要求に成功
    /// false: 実行条件未達（非サーバー、未接続、人数不足など）
    /// </returns>
    public bool TryStartBattle()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogWarning("NetworkManager が見つかりません");
            return false;
        }

        if (!networkManager.IsServer)
        {
            return false;
        }

        if (!networkManager.IsListening)
        {
            Debug.LogWarning("ネットワークが開始されていません");
            return false;
        }

        int playerCount = networkManager.ConnectedClientsList.Count;
        if (playerCount < minPlayersToStart)
        {
            Debug.LogWarning($"プレイヤーが不足しています（現在: {playerCount}人）。{minPlayersToStart}人以上で開始できます");
            RefreshState();
            return false;
        }

        Debug.Log("ゲーム開始!（ネットワーク同期でBattleへ遷移）");
        networkManager.SceneManager.LoadScene(battleSceneName, LoadSceneMode.Single);
        RefreshState();
        return true;
    }

    /// <summary>
    /// サービス破棄時にイベント購読を解除する。
    /// 解除しないと再生成時に重複通知やリークの原因になり得る。
    /// </summary>
    public void Dispose()
    {
        UnregisterNetworkCallbacks();
    }

    void RegisterNetworkCallbacks(NetworkManager networkManager)
    {
        // 多重登録防止: 同じインスタンスに同じコールバックを二度付けない。
        if (callbacksRegistered || networkManager == null) return;

        // 参加/離脱のどちらでも状態再計算したいので同一ハンドラを使う。
        networkManager.OnClientConnectedCallback += OnClientConnectionChanged;
        networkManager.OnClientDisconnectCallback += OnClientConnectionChanged;
        callbacksRegistered = true;
    }

    void UnregisterNetworkCallbacks()
    {
        // 未登録なら何もしない。Dispose の多重呼び出しにも安全。
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
        // 引数はクライアント ID だが、ここでは全体状態の再取得のみが目的。
        RefreshState();
    }

    void RefreshState()
    {
        // 常に Singleton の現状態を読み直し、公開プロパティを同期する。
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            // NetworkManager がないケース（シーン遷移直後や初期化前）では
            // UI が誤表示しないように安全側の既定値へ戻す。
            IsListening = false;
            IsServer = false;
            ConnectedPlayerCount = 0;
            CanStartBattle = false;
            SessionStateChanged?.Invoke();
            return;
        }

        // IsListening が false の場合、ConnectedClientsList 参照を避けるため 0 扱い。
        IsListening = networkManager.IsListening;
        IsServer = networkManager.IsServer;
        ConnectedPlayerCount = IsListening ? networkManager.ConnectedClientsList.Count : 0;

        // Battle 開始可否は「接続中」「サーバー」「人数条件達成」の積集合。
        CanStartBattle = IsListening && IsServer && ConnectedPlayerCount >= minPlayersToStart;

        // UI へ最新状態を通知。
        SessionStateChanged?.Invoke();
    }
}