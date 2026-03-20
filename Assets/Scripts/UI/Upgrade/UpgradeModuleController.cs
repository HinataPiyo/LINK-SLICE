namespace UI.Module
{
    using System;
    using Unity.Netcode;
    using UI.Base;
    using Upgrade;
    using UnityEngine.UIElements;

    public class UpgradeModuleController : ModuleControllerBase
    {
        UIDocument uiDocument;

        public SelectUpgradeElement m_SelectUpgradeElement { get; private set; }

        public const string MODULE_SELECT_UPGRADE_ELEMENT = "SelectUpgradeElements";


        void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
            m_SelectUpgradeElement = GetComponent<SelectUpgradeElement>();
        }

        /// <summary>
        /// ネットワークスパン時にUIの初期化を行う。クライアントとサーバーの両方で呼び出される。
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Initialize();
        }

        protected override void Initialize()
        {
            VisualElement root = uiDocument.rootVisualElement;
            Initialize(m_SelectUpgradeElement, MODULE_SELECT_UPGRADE_ELEMENT, root);
        }

        void EnsureInitialized()
        {
            if (m_SelectUpgradeElement != null && m_SelectUpgradeElement.IsInitialized) return;

            Initialize();
        }

        /// <summary>
        /// サーバーからクライアントにUpgrade選択UIを表示するためのClientRpcを呼び出す。サーバーであれば、直接UIを表示する。
        /// </summary>
        /// <param name="entries"> サーバー側で表示するUpgradeの内容を含むEntryの配列。クライアントはこの情報を使用して、UIに表示するUpgradeの内容を決定する。</param>
        public void ShowUpgradeSelection(UpgradeManager.Entry[] entries, int totalPlayerCount)
        {
            if (entries == null) return;

            int[] definitionIndices = new int[entries.Length];      // UpgradeDefinitionのインデックスをクライアントに送るための配列
            int[] currentLevels = new int[entries.Length];          // 各Upgradeの現在レベルをクライアントへ送るための配列
            for (int i = 0; i < entries.Length; i++)
            {
                // クライアント側でも同じ候補内容を再構築できるよう、定義の参照情報と現在レベルだけを送る。
                definitionIndices[i] = UpgradeManager.I.GetUpgradeDefinitionIndex(entries[i].data);
                currentLevels[i] = entries[i].currentLevel;
            }

            if (IsSpawned)
            {
                if (!IsServer) return;

                // クライアントにUpgrade選択UIを表示するためのClientRpcを呼び出す
                ShowUpgradeSelectionClientRpc(definitionIndices, currentLevels, totalPlayerCount);
                return;
            }

            // ネットワーク非同期モードではない場合は、直接UIを表示する
            ShowUpgradeSelectionLocal(definitionIndices, currentLevels, totalPlayerCount);
        }

        /// <summary>
        /// クライアントにUpgrade選択UIを非表示にするためのClientRpcを呼び出す
        /// </summary>
        public void HideUpgradeSelection()
        {
            if (IsSpawned)
            {
                if (!IsServer) return;

                HideUpgradeSelectionClientRpc();
                return;
            }

            EnsureInitialized();
            m_SelectUpgradeElement.Hide();
        }

        /// <summary>
        /// クライアントにUpgrade選択UIの選択状況を更新するためのClientRpcを呼び出す。サーバーから呼び出される。
        /// </summary>
        /// <param name="selectedCounts"> 各Upgradeが現在何人のプレイヤーに選択されているかを示す配列。クライアントはこの情報を使用して、UIに選択状況を反映する。</param>
        /// <param name="totalPlayerCount"> プレイヤーの総数。クライアントはこの情報を使用して、UIに選択状況を反映する際の分母として使用する。</param>
        public void UpdateUpgradeSelectionCounts(int[] selectedCounts, int totalPlayerCount)
        {
            if (selectedCounts == null) return;

            if (IsSpawned)
            {
                if (!IsServer) return;

                // クライアントにUpgrade選択UIの選択状況を更新するためのClientRpcを呼び出す
                UpdateUpgradeSelectionCountsClientRpc(selectedCounts, totalPlayerCount);
                return;
            }

            EnsureInitialized();
            // ネットワーク非同期モードではない場合は、直接UIを表示する
            m_SelectUpgradeElement.UpdateSelectedPlayerCounts(selectedCounts, totalPlayerCount);
        }

        /// <summary>
        /// クライアントにUpgrade選択UIの選択結果を表示するためのClientRpcを呼び出す。サーバーから呼び出される。
        /// </summary>
        /// <param name="selectedIndex"> クライアントが選択したUpgradeのインデックス。クライアントはこの情報を使用して、UIに選択結果を反映する。</param>
        public void ShowUpgradeSelectionResult(int selectedIndex)
        {
            if (IsSpawned)
            {
                if (!IsServer) return;

                // クライアントにUpgrade選択UIの選択結果を表示するためのClientRpcを呼び出す
                ShowUpgradeSelectionResultClientRpc(selectedIndex);
                return;
            }

            EnsureInitialized();
            m_SelectUpgradeElement.ShowSelectedResult(selectedIndex);       // ネットワーク非同期モードではない場合は、直接UIを表示する
        }

        /// <summary>
        /// クライアントにUpgrade選択UIを表示するためのClientRpc。サーバーから呼び出される。
        /// </summary>
        /// <param name="definitionIndices"> UpgradeDefinitionのインデックスの配列。クライアントはこのインデックスを使用して、表示するUpgradeの内容をUpgradeManagerから取得する。</param>
        /// <param name="currentLevels"> 各Upgradeの現在レベルの配列。クライアントはこの情報から、今回提示されるレベルと説明文を再構築する。</param>
        [ClientRpc]
        void ShowUpgradeSelectionClientRpc(int[] definitionIndices, int[] currentLevels, int totalPlayerCount)
        {
            if (!IsClient) return;

            ShowUpgradeSelectionLocal(definitionIndices, currentLevels, totalPlayerCount);
        }

        /// <summary>
        /// クライアントにUpgrade選択UIを非表示にするためのClientRpc。サーバーから呼び出される。
        /// </summary>
        [ClientRpc]
        void HideUpgradeSelectionClientRpc()
        {
            if (!IsClient) return;

            EnsureInitialized();
            m_SelectUpgradeElement.Hide();
        }

        /// <summary>
        /// クライアントにUpgrade選択UIの選択状況を更新するためのClientRpc。サーバーから呼び出される。
        /// </summary>
        /// <param name="selectedCounts"> 各Upgradeが現在何人のプレイヤーに選択されているかを示す配列。クライアントはこの情報を使用して、UIに選択状況を反映する。</param>
        /// <param name="totalPlayerCount"> プレイヤーの総数。クライアントはこの情報を使用して、UIに選択状況を反映する際の分母として使用する。</param>
        [ClientRpc]
        void UpdateUpgradeSelectionCountsClientRpc(int[] selectedCounts, int totalPlayerCount)
        {
            if (!IsClient) return;

            EnsureInitialized();
            m_SelectUpgradeElement.UpdateSelectedPlayerCounts(selectedCounts, totalPlayerCount);
        }

        /// <summary>
        /// クライアントにUpgrade選択UIの選択結果を表示するためのClientRpc。サーバーから呼び出される。
        /// </summary>
        /// <param name="selectedIndex"> クライアントが選択したUpgradeのインデックス。クライアントはこの情報を使用して、UIに選択結果を反映する。</param>
        [ClientRpc]
        void ShowUpgradeSelectionResultClientRpc(int selectedIndex)
        {
            if (!IsClient) return;

            EnsureInitialized();
            m_SelectUpgradeElement.ShowSelectedResult(selectedIndex);       // ネットワーク非同期モードではない場合は、直接UIを表示する
        }

        /// <summary>
        /// Upgradeが選択されたときの処理。クライアント側で呼び出される。
        /// </summary>
        /// <param name="selectedIndex"> クライアントが選択したUpgradeのインデックス。サーバーはこのインデックスを使用して、UpgradeManagerからUpgradeの内容を取得し、適用する。</param>
        void HandleUpgradeSelected(int selectedIndex)
        {
            if (!IsSpawned || IsServer)
            {
                UpgradeManager.I.SubmitUpgradeSelection(selectedIndex, NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0);
                return;
            }

            SelectUpgradeServerRpc(selectedIndex);      // クライアントからサーバーに選択されたUpgradeのインデックスを送るためのRpcを呼び出す
        }

        /// <summary>
        /// クライアントからサーバーに選択されたUpgradeのインデックスを送るためのRpc。クライアントから呼び出される。
        /// </summary>
        /// <param name="selectedIndex"> クライアントが選択したUpgradeのインデックス。サーバーはこのインデックスを使用して、UpgradeManagerからUpgradeの内容を取得し、適用する。</param>
        /// <param name="rpcParams"> RpcParams。クライアントのIDを取得するために使用される。</param>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void SelectUpgradeServerRpc(int selectedIndex, RpcParams rpcParams = default)
        {
            UpgradeManager.I.SubmitUpgradeSelection(selectedIndex, rpcParams.Receive.SenderClientId);
        }

        /// <summary>
        /// Upgrade選択UIを表示するためのローカルメソッド。ネットワーク非同期モードではない場合や、クライアント側でClientRpcが呼び出されたときに使用される。
        /// </summary>
        void ShowUpgradeSelectionLocal(int[] definitionIndices, int[] currentLevels, int totalPlayerCount)
        {
            EnsureInitialized();
            SelectUpgradeElement.ViewData[] viewData = UpgradeManager.I.CreateViewData(definitionIndices, currentLevels);
            m_SelectUpgradeElement.Show(viewData, totalPlayerCount, HandleUpgradeSelected);
        }
    }
}