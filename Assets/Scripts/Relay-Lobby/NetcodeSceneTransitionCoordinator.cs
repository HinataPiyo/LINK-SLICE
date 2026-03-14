using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Netcode の接続待機とシーン遷移を担当する。
/// </summary>
public class NetcodeSceneTransitionCoordinator
{
    public async Task WaitForExpectedClientsAndLoadSceneAsync(string sceneName, int expectedPlayers)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
        {
            Debug.LogWarning("接続待機を開始できません。NetworkManager が未起動か Server ではありません");
            return;
        }

        expectedPlayers = Mathf.Max(1, expectedPlayers);
        const int waitStepMs = 200;
        const int timeoutMs = 15000;
        int elapsedMs = 0;

        while (elapsedMs < timeoutMs)
        {
            int connectedCount = networkManager.ConnectedClientsList != null ? networkManager.ConnectedClientsList.Count : 0;
            if (connectedCount >= expectedPlayers)
            {
                Debug.Log($"接続人数が揃いました: connected={connectedCount} expected={expectedPlayers}");
                break;
            }

            await Task.Delay(waitStepMs);
            elapsedMs += waitStepMs;
        }

        int finalConnected = networkManager.ConnectedClientsList != null ? networkManager.ConnectedClientsList.Count : 0;
        if (finalConnected < expectedPlayers)
        {
            Debug.LogWarning($"接続待機がタイムアウトしました: connected={finalConnected} expected={expectedPlayers} timeoutMs={timeoutMs}");
        }

        TryLoadGameplayScene(sceneName);
    }

    private void TryLoadGameplayScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("gameplaySceneName が未設定です");
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
        {
            Debug.LogWarning("NetworkManager が未起動か、Server ではありません");
            return;
        }

        if (networkManager.SceneManager == null)
        {
            Debug.LogWarning("NetworkManager.SceneManager が利用できません。Enable Scene Management を確認してください");
            return;
        }

        SceneEventProgressStatus status = networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        if (status != SceneEventProgressStatus.Started)
        {
            Debug.LogWarning("シーン遷移開始に失敗しました: " + status + " scene=" + sceneName);
            return;
        }

        Debug.Log("シーン遷移開始: " + sceneName);
    }
}
