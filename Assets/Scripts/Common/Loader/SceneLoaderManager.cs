namespace Common
{
    using System;
    using System.Collections;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// シーンの非同期ロードを管理するクラス。
    /// 
    /// 主な役割は次の 3 点です。
    /// 1. 指定されたシーンをバックグラウンドで読み込む。
    /// 2. 読み込み進捗を外部から参照できるようにする。
    /// 3. 読み込み完了後に安全なタイミングで次のシーンへ切り替える。
    /// 
    /// ロード画面用のシーンに配置し、Start 時に自動で遷移させる使い方と、
    /// ボタンや別スクリプトから明示的に呼び出す使い方の両方を想定している。
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneLoaderManager : MonoBehaviour
    {
        [Header("ロード設定")]
        [SerializeField] bool loadOnStart = false;
        [SerializeField] float minimumLoadingSeconds = 0.5f;

        /// <summary>
        /// 現在ロード中かどうか。
        /// UI 側でボタン連打を防いだり、ローディング表示を切り替える材料に使う。
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// 0.0 から 1.0 の範囲で扱う読み込み進捗。
        /// Unity の AsyncOperation.progress は 0.9 で止まるため、
        /// このプロパティでは 0.9 を 1.0 に正規化して外部へ公開する。
        /// </summary>
        public float Progress { get; private set; }

        /// <summary>
        /// 現在ロード対象になっているシーン名。
        /// まだロードしていない場合は、共有値または既定値から解決したシーン名を返す。
        /// </summary>
        public string CurrentTargetSceneName => string.IsNullOrWhiteSpace(loadingSceneName) ? ResolveTargetSceneName() : loadingSceneName;

        /// <summary>
        /// 進捗が更新されたタイミングで通知するイベント。
        /// ローディングバーやテキストの更新に使える。
        /// </summary>
        public event Action<float> OnProgressChanged;

        /// <summary>
        /// ロード開始時に通知するイベント。
        /// 引数には読み込み対象のシーン名が入る。
        /// </summary>
        public event Action<string> OnLoadStarted;

        /// <summary>
        /// ロード完了時に通知するイベント。
        /// シーン切り替え完了後に呼ばれる。
        /// </summary>
        public event Action<string> OnLoadCompleted;

        AsyncOperation currentLoadOperation;
        string loadingSceneName;

        void Start()
        {
            // ロード画面シーンに置いた場合、設定次第で自動的に遷移を始める。
            // 未設定のまま動かしても原因が分かるよう、エラーではなく警告に留める。
            if (!loadOnStart)
            {
                return;
            }

            LoadConfiguredScene();
        }

        /// <summary>
        /// Inspector の nextSceneName に設定されたシーンを読み込む。
        /// ボタンの OnClick から呼びやすいよう、引数なしメソッドも用意している。
        /// </summary>
        public void LoadConfiguredScene()
        {
            LoadScene(ResolveTargetSceneName());
        }

        /// <summary>
        /// 指定名のシーンを非同期で読み込む。
        /// 既にロード中の場合は多重実行を防ぐため処理を開始しない。
        /// </summary>
        /// <param name="sceneName">Build Settings に登録されているシーン名。</param>
        public void LoadScene(string sceneName)
        {
            if (IsLoading)
            {
                Debug.LogWarning("SceneLoaderManager は既にシーンをロード中です", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("ロード先のシーン名が未設定です", this);
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>
        /// 現在のロードをコルーチンで管理する。
        /// Unity のシーンロード API はフレームをまたいで進むため、
        /// MonoBehaviour ではコルーチンが最も分かりやすく扱いやすい。
        /// </summary>
        IEnumerator LoadSceneRoutine(string sceneName)
        {
            IsLoading = true;
            SetProgress(0f);
            loadingSceneName = sceneName;
            OnLoadStarted?.Invoke(sceneName);

            NetworkManager networkManager = NetworkManager.Singleton;
            bool shouldUseNetworkSceneManager = networkManager != null && networkManager.IsListening;

            if (shouldUseNetworkSceneManager)
            {
                yield return LoadWithNetworkSceneManagerRoutine(sceneName, networkManager);

                string completedNetworkSceneName = loadingSceneName;
                ResetLoadingState();
                OnLoadCompleted?.Invoke(completedNetworkSceneName);
                yield break;
            }

            yield return LoadWithLocalSceneManagerRoutine(sceneName);

            string completedSceneName = loadingSceneName;
            ResetLoadingState();
            OnLoadCompleted?.Invoke(completedSceneName);
        }

        /// <summary>
        /// Netcode の SceneManager を使って最終シーンへ進めるルート。
        /// 
        /// Host だけが実際のシーン遷移を開始し、Client は Load シーン上で待機する。
        /// こうすることで、マルチプレイ時のシーン同期を崩さずに
        /// 「Battle の前に Load を挟む」構成を実現できる。
        /// </summary>
        IEnumerator LoadWithNetworkSceneManagerRoutine(string sceneName, NetworkManager networkManager)
        {
            if (networkManager.SceneManager == null)
            {
                Debug.LogWarning("NetworkManager.SceneManager が利用できないため、通常の SceneManager にフォールバックします", this);
                yield return LoadWithLocalSceneManagerRoutine(sceneName);
                yield break;
            }

            if (!networkManager.IsServer)
            {
                yield return WaitForHostSceneTransitionRoutine();
                yield break;
            }

            float elapsedSeconds = 0f;
            float waitSeconds = Mathf.Max(0f, minimumLoadingSeconds);

            while (elapsedSeconds < waitSeconds)
            {
                elapsedSeconds += Time.unscaledDeltaTime;

                if (waitSeconds <= 0f)
                {
                    SetProgress(0.9f);
                }
                else
                {
                    float waitingProgress = Mathf.Clamp01(elapsedSeconds / waitSeconds) * 0.9f;
                    SetProgress(waitingProgress);
                }

                yield return null;
            }

            SetProgress(1f);
            SceneEventProgressStatus status = networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            if (status != SceneEventProgressStatus.Started)
            {
                Debug.LogWarning("ゲームシーンへの遷移開始に失敗しました: " + status + " scene=" + sceneName, this);
                yield break;
            }

            // 次回以降の参照が前回の値に引っ張られないよう、発行後にクリアする。
            GlobalCommon.NextSceneName = null;
        }

        /// <summary>
        /// 非ネットワーク時に使う通常の非同期ロード処理。
        /// 読み込み進捗をそのまま UI に反映できるため、単機能テスト時にも扱いやすい。
        /// </summary>
        IEnumerator LoadWithLocalSceneManagerRoutine(string sceneName)
        {
            currentLoadOperation = SceneManager.LoadSceneAsync(sceneName);
            if (currentLoadOperation == null)
            {
                Debug.LogWarning("シーンロードの開始に失敗しました: " + sceneName, this);
                ResetLoadingState();
                yield break;
            }

            currentLoadOperation.allowSceneActivation = false;

            float elapsedSeconds = 0f;
            while (!currentLoadOperation.isDone)
            {
                elapsedSeconds += Time.unscaledDeltaTime;

                float normalizedProgress = Mathf.Clamp01(currentLoadOperation.progress / 0.9f);
                SetProgress(normalizedProgress);

                bool hasFinishedLoading = currentLoadOperation.progress >= 0.9f;
                bool hasReachedMinimumTime = elapsedSeconds >= minimumLoadingSeconds;

                if (hasFinishedLoading && hasReachedMinimumTime)
                {
                    SetProgress(1f);
                    currentLoadOperation.allowSceneActivation = true;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Client 側は自分でシーン切り替えを発行せず、Host からの同期を待つ。
        /// 進捗値は固定ではなくゆっくり 0.9 まで進め、待機中であることを見た目に出す。
        /// </summary>
        IEnumerator WaitForHostSceneTransitionRoutine()
        {
            float displayedProgress = 0f;
            float waitStartTime = Time.unscaledTime;

            while (SceneManager.GetActiveScene().name == GlobalCommon.LoadingSceneName)
            {
                float elapsedSeconds = Time.unscaledTime - waitStartTime;
                float targetProgress = minimumLoadingSeconds <= 0f
                    ? 0.9f
                    : Mathf.Clamp01(elapsedSeconds / minimumLoadingSeconds) * 0.9f;

                displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime);
                SetProgress(displayedProgress);
                yield return null;
            }

            SetProgress(1f);
        }

        /// <summary>
        /// ロード対象のシーン名を解決する。
        /// 共有値が空なら Battle を既定値として使い、Load シーン単体でも動くようにする。
        /// </summary>
        string ResolveTargetSceneName()
        {
            if (!string.IsNullOrWhiteSpace(GlobalCommon.NextSceneName))
            {
                return GlobalCommon.NextSceneName;
            }

            return GlobalCommon.DefaultGameplaySceneName;
        }

        /// <summary>
        /// 進捗値を更新し、UI 連携用イベントも同時に通知する。
        /// 更新経路を 1 か所にまとめることで、進捗の扱いを追いやすくしている。
        /// </summary>
        /// <param name="value">0.0 ～ 1.0 の進捗値。</param>
        void SetProgress(float value)
        {
            Progress = Mathf.Clamp01(value);
            OnProgressChanged?.Invoke(Progress);
        }

        /// <summary>
        /// ロード終了後に内部状態を初期化する。
        /// 次回ロード時に前回の AsyncOperation やシーン名が残らないようにする。
        /// </summary>
        void ResetLoadingState()
        {
            currentLoadOperation = null;
            loadingSceneName = null;
            IsLoading = false;
        }
    }
}