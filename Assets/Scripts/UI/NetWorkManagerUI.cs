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

    void Awake()
    {
        if (battleStartButton != null)
        {
            battleStartButton.interactable = false;
        }

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

            if (battleStartButton != null)
            {
                battleStartButton.interactable = true;
            }
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
            }
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

            Debug.Log("ゲーム開始!（ネットワーク同期でBattleへ遷移）");
            networkManager.SceneManager.LoadScene(battleSceneName, LoadSceneMode.Single);
        });
    }
}