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
        BindButtons();
        SetControlsInteractable(true, true, false);
    }

    /// <summary>
    /// ボタン入力をイベントへ変換する登録処理をまとめる。
    /// Start を短く保ちつつ、UI 入力の入口をひと目で追えるようにする。
    /// </summary>
    private void BindButtons()
    {
        if (createLobbyButton != null)
        {
            createLobbyButton.onClick.AddListener(HandleCreateLobbyClicked);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(HandleRefreshClicked);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(HandleStartGameClicked);
        }
    }

    /// <summary>
    /// ロビー作成ボタン押下を上位へ通知する。
    /// 送信直前に一度ボタンを絞り、連打による多重実行を見た目でも防ぐ。
    /// </summary>
    private void HandleCreateLobbyClicked()
    {
        SetControlsInteractable(false, true, false);
        OnCreateLobbyRequest?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// ロビー一覧更新ボタン押下を通知する。
    /// 実際の取得処理は LobbyManager 側が担当する。
    /// </summary>
    private void HandleRefreshClicked()
    {
        OnRefreshLobbyListRequest?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// ゲーム開始ボタン押下を通知する。
    /// 実際の開始可否判定は LobbyManager 側へ委譲する。
    /// </summary>
    private void HandleStartGameClicked()
    {
        OnGameStartRequest?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Lobby 一覧 UI を全件作り直す。
    /// Lobby Service からの取得結果をそのまま可視化するため、差分更新ではなく全再構築としている。
    /// </summary>
    public void Refresh(List<Lobby> lobbyList)
    {
        if (lobbyListContent == null || lobbyBannerPrefab == null)
        {
            return;
        }
        
        foreach (Transform child in lobbyListContent)
        {
            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbyList)
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
        SetButtonInteractable(createLobbyButton, canCreateLobby);
        SetButtonInteractable(refreshButton, canRefreshLobby);
        SetButtonInteractable(startGameButton, canStartGame);
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = interactable;
    }
}