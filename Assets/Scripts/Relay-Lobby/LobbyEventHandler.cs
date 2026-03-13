using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// ロビー画面の UI 入力を受け取り、LobbyManager へイベントとして中継する窓口。
/// 自身は Lobby Service を直接触らず、表示更新とボタン制御だけを担当する。
/// </summary>
public class LobbyEventHandler : MonoBehaviour
{
    // ロビー一覧1件分の UI プレハブ。
    [SerializeField] private GameObject lobbyBannerPrefab;
    // ロビー一覧の親 Transform。Refresh 時に子要素を作り直す。
    [SerializeField] private Transform lobbyListContent;
    // ロビー作成ボタン。
    [SerializeField] private Button createLobbyButton;
    // ロビー一覧更新ボタン。
    [SerializeField] private Button refreshButton;
    // ホスト用のゲーム開始ボタン。
    [SerializeField] private Button startGameButton;

    // UI からの要求を LobbyManager に渡すためのイベント群。
    public static EventHandler OnCreateLobbyRequest;
    public static EventHandler OnRefreshLobbyListRequest;
    public static EventHandler OnGameStartRequest;

    private void Start()
    {
        createLobbyButton.onClick.AddListener(() =>
        {
            // 二重押下を避けるため、要求送信直前に一旦ボタン状態を絞る。
            SetControlsInteractable(false, true, false);
            OnCreateLobbyRequest?.Invoke(this, EventArgs.Empty);
        });

        refreshButton.onClick.AddListener(() =>
        {
            OnRefreshLobbyListRequest?.Invoke(this, EventArgs.Empty);
        });

        startGameButton.onClick.AddListener(() =>
        {
            // 実際の開始可否判定は LobbyManager 側が担当する。
            Debug.Log("StartGameButton");
            OnGameStartRequest?.Invoke(this, EventArgs.Empty);
        });

        // 初期状態ではロビー作成と一覧更新のみ有効にする。
        SetControlsInteractable(true, true, false);
    }

    /// <summary>
    /// Lobby 一覧 UI を全件作り直す。
    /// Lobby Service からの取得結果をそのまま可視化するため、差分更新ではなく全再構築としている。
    /// </summary>
    public void Refresh(List<Lobby> lobbyList)
    {
        foreach (Transform child in lobbyListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var lobby in lobbyList)
        {
            GameObject lobbyBanner = Instantiate(lobbyBannerPrefab, lobbyListContent);
            lobbyBanner.GetComponent<LobbyBanner>().Init(lobby);
        }
    }

    /// <summary>
    /// 非同期処理中の誤操作を防ぐため、ボタン活性状態を一括で更新する。
    /// </summary>
    public void SetControlsInteractable(bool canCreateLobby, bool canRefreshLobby, bool canStartGame)
    {
        createLobbyButton.interactable = canCreateLobby;
        refreshButton.interactable = canRefreshLobby;
        startGameButton.interactable = canStartGame;
    }
}