using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] Button hostButton;
    [SerializeField] Button cliantButton;
    [SerializeField] Button battleStartButton;
    [SerializeField] string battleSceneName = "Battle";
    [SerializeField] int minPlayersToStart = 2;

    NetworkSessionService sessionService;

    void Awake()
    {
        if (battleStartButton != null) battleStartButton.interactable = false;

        hostButton.onClick.AddListener(() =>
        {
            if (sessionService == null)
            {
                Debug.LogWarning("NetworkSessionService が未初期化です");
                return;
            }

            sessionService.TryStartHost();
        });

        cliantButton.onClick.AddListener(() =>
        {
            if (sessionService == null)
            {
                Debug.LogWarning("NetworkSessionService が未初期化です");
                return;
            }

            sessionService.TryStartClient();
        });

        battleStartButton.onClick.AddListener(() =>
        {
            if (sessionService == null)
            {
                Debug.LogWarning("NetworkSessionService が未初期化です");
                return;
            }

            sessionService.TryStartBattle();
        });
    }

    void Start()
    {
        sessionService = new NetworkSessionService(minPlayersToStart, battleSceneName);
        sessionService.SessionStateChanged += RefreshUiState;
        RefreshUiState();
    }

    void OnDestroy()
    {
        if (sessionService != null)
        {
            sessionService.SessionStateChanged -= RefreshUiState;
            sessionService.Dispose();
            sessionService = null;
        }
    }

    /// <summary>
    /// UI の状態をセッションの状態に応じて更新します。
    /// </summary>
    void RefreshUiState()
    {
        if (sessionService == null)
        {
            if (battleStartButton != null) battleStartButton.interactable = false;
            if (hostButton != null) hostButton.interactable = true;
            if (cliantButton != null) cliantButton.interactable = true;
            return;
        }

        bool canChooseRole = !sessionService.IsListening;
        if (hostButton != null) hostButton.interactable = canChooseRole;
        if (cliantButton != null) cliantButton.interactable = canChooseRole;
        if (battleStartButton != null) battleStartButton.interactable = sessionService.CanStartBattle;

        if (sessionService.IsListening)
        {
            Debug.Log($"接続プレイヤー数: {sessionService.ConnectedPlayerCount}");
        }
    }
}