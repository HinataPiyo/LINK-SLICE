using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;
using System;

/// <summary>
/// ロビー一覧に1件分の情報を表示するUI。
/// 表示対象の Lobby を保持し、Join ボタン押下時に上位へイベント通知する。
/// </summary>
public class LobbyBanner : MonoBehaviour
{
    // ロビー名表示。
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    // 現在人数 / 最大人数の表示。
    [SerializeField] private TextMeshProUGUI playerCountText;
    // 参加要求を発火するボタン。
    [SerializeField] private Button joinButton;

    // このバナーが表現しているロビー情報。
    public Lobby myLobby { get; private set; }
    // 実際の Join 処理は LobbyManager が持つため、UI からはイベントだけを通知する。
    public static EventHandler<Lobby> OnJoinLobby;

    /// <summary>
    /// ロビー一覧更新時に呼ばれ、表示内容とボタンイベントを初期化する。
    /// </summary>
    public void Init(Lobby lobby)
    {
        myLobby = lobby;
        lobbyNameText.text = lobby.Name;
        playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;

        // 一覧再利用時に同じリスナーが積み増しされないよう、登録前に一度クリアする。
        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(NotifyJoinRequested);
    }

    /// <summary>
    /// このバナーで表しているロビーへの参加要求を通知する。
    /// 実際の参加処理は LobbyManager が持ち、UI は選択結果だけを渡す。
    /// </summary>
    private void NotifyJoinRequested()
    {
        OnJoinLobby?.Invoke(this, myLobby);
        Debug.Log("JoinLobby LobbyID=" + myLobby.Id);
    }
}