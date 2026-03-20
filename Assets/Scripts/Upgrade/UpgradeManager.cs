namespace Upgrade
{
    using System;
    using System.Collections.Generic;
    using System.Collections;
    using PlayerSystem.Link;
    using UI.Module;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.UI;

    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager I { get; private set; }
        const int UPGRADE_OPTION_COUNT = 3;    // アップグレードの選択肢の数
        [SerializeField] UpgradeModuleController uiCtrl;
        [SerializeField] UpgradeDatabase upgradeDatabase;
        [SerializeField] float selectedResultDisplaySeconds = 2.5f;
        readonly Dictionary<UpgradeDefinition, UpgradeState> upgradeStates = new Dictionary<UpgradeDefinition, UpgradeState>();
        readonly Dictionary<ulong, int> selectedUpgradeByClientId = new Dictionary<ulong, int>();
        Entry[] currentEntries;
        int[] selectedPlayerCounts;
        int expectedSelectionCount;
        Coroutine resolveSelectionCoroutine;

        public bool IsUpgraded { get; private set; } = false;    // アップグレードが適用されたかどうかを示すフラグ

        // 今後、アップグレードの適用にプレイヤーや武器、移動などの参照が必要になった場合は、
        // このクラスにそれらの参照も持たせる。
        Core.Health coreHealth;
        LinkRuntimeStats linkRuntimeStats;

        public class Entry
        {
            public UpgradeDefinition data;
            public int currentLevel;
            public int offeredLevel;
            public Action onClickAction;
        }

        public void ChangeUpgradeFlag(bool value) => IsUpgraded = value;

        public void Awake()
        {
            if (I == null) I = this;
            coreHealth = FindAnyObjectByType<Core.Health>();
            linkRuntimeStats = FindAnyObjectByType<LinkRuntimeStats>();
        }

        /// <summary>
        /// 現在提示可能なアップグレード候補を選び、UI に必要な情報へ変換して表示する。
        /// 候補選出は定義側の CanOffer を使って行うため、要素追加時にこのメソッドへ個別分岐を足す必要がない。
        /// </summary>
        public void OnShowUpgradeUI()
        {
            UpgradeDefinition[] upgradeDefinitions = GetOfferableUpgradeDefinitions(UPGRADE_OPTION_COUNT);
            if (upgradeDefinitions.Length == 0)
            {
                // 提示可能な候補が尽きた場合に待機ループへ入ったままにならないよう、ここで明示的にフラグを戻す。
                IsUpgraded = false;
                Debug.LogWarning("提示可能なアップグレード候補が存在しないため、Upgrade UI の表示をスキップしました。");
                return;
            }

            IsUpgraded = true;    // 候補生成に成功したタイミングで待機フラグを立てる
            expectedSelectionCount = GetExpectedSelectionCount();
            List<Entry> entries = new List<Entry>();
            for (int i = 0; i < upgradeDefinitions.Length; i++)
            {
                UpgradeState state = GetOrCreateState(upgradeDefinitions[i]);
                Entry entry = new Entry();
                entry.data = upgradeDefinitions[i];
                entry.currentLevel = state.Level;
                entry.offeredLevel = state.Level + 1;
                entry.onClickAction = () => ApplyUpgrade(entry.data);   // クリックされたときにそのUpgradeを適用するアクションを設定する
                entries.Add(entry);
            }

            currentEntries = entries.ToArray();
            selectedUpgradeByClientId.Clear();
            selectedPlayerCounts = new int[currentEntries.Length];

            uiCtrl.ShowUpgradeSelection(currentEntries, expectedSelectionCount);   // UIを表示して、選出されたUpgradeの内容を渡す
            uiCtrl.UpdateUpgradeSelectionCounts(selectedPlayerCounts, expectedSelectionCount);
        }

        /// <summary>
        /// プレイヤーごとのアップグレード投票を受け付ける。
        /// </summary>
        /// <param name="selectedIndex">UIから渡される選択されたアップグレードのインデックス</param>
        /// <param name="clientId">選択したクライアントID</param>
        public void SubmitUpgradeSelection(int selectedIndex, ulong clientId)
        {
            if (!IsUpgraded || currentEntries == null) return;

            if (selectedIndex < 0 || selectedIndex >= currentEntries.Length)
            {
                Debug.LogWarning($"無効なアップグレード選択です: {selectedIndex}");
                return;
            }

            if (selectedUpgradeByClientId.ContainsKey(clientId)) return;

            selectedUpgradeByClientId.Add(clientId, selectedIndex);
            selectedPlayerCounts[selectedIndex]++;
            uiCtrl.UpdateUpgradeSelectionCounts(selectedPlayerCounts, expectedSelectionCount);

            if (selectedUpgradeByClientId.Count < expectedSelectionCount) return;
            if (resolveSelectionCoroutine != null) return;

            resolveSelectionCoroutine = StartCoroutine(ResolveUpgradeSelectionRoutine());
        }

        /// <summary>
        /// 選択されたアップグレードを適用する。
        /// 実行内容そのものは UpgradeDefinition 側へ委譲し、この Manager は状態更新と UI 制御に専念する。
        /// </summary>
        void ApplyUpgrade(UpgradeDefinition data)
        {
            if (data == null) return;

            UpgradeContext context = BuildContext();        // 適用に必要な参照を UpgradeContext としてまとめて渡す
            UpgradeState state = GetOrCreateState(data);
            int offeredLevel = state.Level + 1;
            if (!data.Apply(context, offeredLevel))
            {
                Debug.LogWarning($"アップグレード適用に失敗しました: {data.name}");
                return;
            }

            state.LevelUp();

            ChangeUpgradeFlag(false);   // アップグレードが適用された後、UpgradeManagerのフラグをfalseにする
            currentEntries = null;
            selectedPlayerCounts = null;
            expectedSelectionCount = 0;
            selectedUpgradeByClientId.Clear();
            resolveSelectionCoroutine = null;

            if (uiCtrl == null) return;

            // サーバーであれば、UI制御もサーバー側で行う。
            // クライアントであれば、クライアント側でUIを非表示にするためのClientRpcを呼び出す。
            if (uiCtrl.IsSpawned && uiCtrl.IsServer)
            {
                uiCtrl.HideUpgradeSelection();
            }
            else    // サーバーでない場合は、クライアント側でUIを非表示にするためのClientRpcを呼び出す
            {
                uiCtrl.m_SelectUpgradeElement.Hide();
            }
        }

        /// <summary>
        /// アップグレード選択の結果を集計して、実際に適用するアップグレードを決定するルーチン。
        /// </summary>
        IEnumerator ResolveUpgradeSelectionRoutine()
        {
            List<int> selectedIndices = new List<int>();
            foreach (int selectedIndex in selectedUpgradeByClientId.Values)
            {
                if (!selectedIndices.Contains(selectedIndex))
                {
                    selectedIndices.Add(selectedIndex);
                }
            }

            if (selectedIndices.Count == 0)
            {
                Debug.LogWarning("アップグレード投票結果が空のため、解決処理を中断しました。");
                ChangeUpgradeFlag(false);
                yield break;
            }

            int randomIndex = UnityEngine.Random.Range(0, selectedIndices.Count);
            int selectedUpgradeIndex = selectedIndices[randomIndex];
            uiCtrl.ShowUpgradeSelectionResult(selectedUpgradeIndex);

            yield return new WaitForSeconds(selectedResultDisplaySeconds);

            ApplyUpgrade(currentEntries[selectedUpgradeIndex].data);
        }

        /// <summary>
        /// 提示可能なアップグレード定義を選び出す。
        /// 定義ごとの CanOffer を使って選出するため、要素追加時にこのメソッドへ個別分岐を足す必要がない。
        /// </summary>
        /// <param name="count">提示するアップグレードの数</param>
        /// <returns>提示可能なアップグレード定義の配列</returns>
        UpgradeDefinition[] GetOfferableUpgradeDefinitions(int count)
        {
            // upgradeDatabase が null の場合は空の配列を返す
            if (upgradeDatabase == null) return Array.Empty<UpgradeDefinition>();

            UpgradeContext context = BuildContext();        // 定義の選出に必要な参照を UpgradeContext としてまとめて渡す
            List<UpgradeDefinition> candidates = new List<UpgradeDefinition>();     // 提示可能なアップグレード定義の候補リスト

            // upgradeDatabase 内のすべての定義に対して、定義ごとの CanOffer を使って提示可能かを判定し、候補リストを作成する
            foreach (UpgradeDefinition definition in upgradeDatabase.Upgrades)
            {
                if (definition == null) continue;

                // 定義に対応する状態を取得し、定義の CanOffer を呼び出して提示可能かを判定する
                UpgradeState state = GetOrCreateState(definition);

                // 定義の CanOffer を呼び出して提示可能かを判定する。提示可能な場合は候補リストに追加する。
                if (definition.CanOffer(context, state.Level))
                {
                    candidates.Add(definition);
                }
            }

            Shuffle(candidates);        // 候補リストをシャッフルして、提示されるアップグレードの順番にランダム性を持たせる
            if (candidates.Count <= count) return candidates.ToArray();     // 候補が提示数以下の場合はすべてを返す

            return candidates.GetRange(0, count).ToArray();     // 候補リストから提示数分だけを取得して配列で返す
        }

        /// <summary>
        /// UpgradeDefinition に対応する UpgradeState のインデックスを UpgradeDatabase から取得する。
        /// </summary>
        /// <param name="definition">対応する UpgradeState のインデックスを取得する UpgradeDefinition</param>
        /// <returns> 対応する UpgradeState のインデックス。見つからない場合は -1 を返す。</returns>
        public int GetUpgradeDefinitionIndex(UpgradeDefinition definition)
        {
            // upgradeDatabase が null の場合は -1 を返す
            if (upgradeDatabase == null || definition == null) return -1;

            return upgradeDatabase.IndexOf(definition);     // UpgradeDatabase から UpgradeDefinition に対応するインデックスを取得して返す
        }

        /// <summary>
        /// UpgradeDefinition のインデックスと現在レベルの配列から、UIに表示するための ViewData の配列を作成して返す。
        /// </summary>
        /// <param name="definitionIndices"> UpgradeDefinition のインデックスの配列。UIはこのインデックスを使用して、表示するUpgradeの内容をUpgradeManagerから取得する。</param>
        /// <param name="currentLevels"> 各Upgradeの現在レベルの配列。UIはこの情報から、今回提示されるレベルと説明文を再構築する。</param>
        /// <returns> UIに表示するための ViewData の配列</returns>
        public SelectUpgradeElement.ViewData[] CreateViewData(int[] definitionIndices, int[] currentLevels)
        {
            // upgradeDatabase が null の場合や definitionIndices が null の場合は空の配列を返す
            if (upgradeDatabase == null || definitionIndices == null) return Array.Empty<SelectUpgradeElement.ViewData>();

            // definitionIndices と currentLevels の長さが異なる場合は、処理を続行する前に警告をログに出す
            SelectUpgradeElement.ViewData[] viewData = new SelectUpgradeElement.ViewData[definitionIndices.Length];

            // definitionIndices の各インデックスに対して、UpgradeDatabase から対応する UpgradeDefinition を取得し、currentLevels から現在レベルを取得して、ViewData を作成する
            for (int i = 0; i < definitionIndices.Length; i++)
            {
                // UpgradeDatabase から definitionIndices[i] に対応する UpgradeDefinition を取得する
                UpgradeDefinition definition = upgradeDatabase.GetUpgradeDefinitionAt(definitionIndices[i]);

                // currentLevels から現在レベルを取得する。currentLevels が null でない場合は currentLevels[i] を使用し、null の場合は 0 を使用する。
                int currentLevel = currentLevels != null && i < currentLevels.Length ? currentLevels[i] : 0;

                if (definition == null)
                {
                    Debug.LogWarning($"UpgradeDefinition が見つかりません: index={definitionIndices[i]}");
                    continue;
                }

                // definition と currentLevel を使用して、UIに表示するための ViewData を作成する。
                // ViewData には、定義のアイコン、今回提示されるレベル、定義の名前、定義の説明文を設定する。
                viewData[i] = new SelectUpgradeElement.ViewData(
                    definition.icon,
                    currentLevel + 1,
                    definition.upgradeName,
                    definition.GetDescription(currentLevel + 1));
            }

            return viewData;
        }

        /// <summary>
        /// UpgradeDefinition に対応する UpgradeState を取得する。存在しない場合は新規作成して辞書に追加する。
        /// </summary>
        /// <param name="definition">対応する UpgradeState を取得または作成する UpgradeDefinition</param>
        /// <returns> 対応する UpgradeState</returns>
        UpgradeState GetOrCreateState(UpgradeDefinition definition)
        {
            if (definition == null) return null;
            // 定義に対応する状態が既に存在する場合はそれを返し、存在しない場合は新規作成して辞書に追加する
            if (upgradeStates.TryGetValue(definition, out UpgradeState state)) return state;

            state = new UpgradeState(definition);       // 定義に対応する状態が存在しない場合は新規作成して辞書に追加する
            upgradeStates.Add(definition, state);
            return state;
        }

        /// <summary>
        /// UpgradeDefinition の適用に必要な参照を UpgradeContext としてまとめて生成する。
        /// </summary>
        /// <returns> UpgradeDefinition の適用に必要な参照をまとめた UpgradeContext</returns>
        UpgradeContext BuildContext()
        {
            if (coreHealth == null)
                coreHealth = FindAnyObjectByType<Core.Health>();
            if (linkRuntimeStats == null)
                linkRuntimeStats = FindAnyObjectByType<LinkRuntimeStats>();

            // 今後プレイヤー、武器、移動などの参照を追加する場合も、この生成箇所だけを拡張すればよい。
            return new UpgradeContext(coreHealth, linkRuntimeStats);
        }

        int GetExpectedSelectionCount()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsListening && networkManager.ConnectedClientsIds != null)
            {
                return Mathf.Max(1, networkManager.ConnectedClientsIds.Count);
            }

            return 1;
        }

        /// <summary>
        /// リストをシャッフルする処理。
        /// Fisher-Yatesアルゴリズムを使用して、リストの要素をランダムな順序に並べ替える。
        /// </summary>
        /// <typeparam name="T">シャッフルするリストの要素の型</typeparam>
        /// <param name="list">シャッフルするリスト</param>
        void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }
    }
}