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

        public void ShowUpgradeSelection(UpgradeManager.Entry[] entries)
        {
            if (!IsServer || entries == null) return;

            int[] definitionIndices = new int[entries.Length];      // UpgradeDefinitionのインデックスをクライアントに送るための配列
            int[] activeCounts = new int[entries.Length];           // 各Upgradeの適用回数をクライアントに送るための配列
            for (int i = 0; i < entries.Length; i++)
            {
                // UpgradeManagerのEntryからUpgradeDefinitionのインデックスと適用回数を抽出して配列に格納する
                definitionIndices[i] = UpgradeManager.I.GetUpgradeDefinitionIndex(entries[i].data);
                activeCounts[i] = entries[i].activeCount;       // クライアント側でUIを表示する際に、Upgradeの内容と適用回数を正確に反映させるために必要な情報を送る
            }

            // クライアントにUpgrade選択UIを表示するためのClientRpcを呼び出す
            ShowUpgradeSelectionClientRpc(definitionIndices, activeCounts);
        }

        // クライアントにUpgrade選択UIを非表示にするためのClientRpcを呼び出す
        public void HideUpgradeSelection()
        {
            if (!IsServer) return;

            // クライアントにUpgrade選択UIを非表示にするためのClientRpcを呼び出す
            HideUpgradeSelectionClientRpc();
        }

        /// <summary>
        /// クライアントにUpgrade選択UIを表示するためのClientRpc。サーバーから呼び出される。
        /// </summary>
        /// <param name="definitionIndices"> UpgradeDefinitionのインデックスの配列。クライアントはこのインデックスを使用して、表示するUpgradeの内容をUpgradeManagerから取得する。</param>
        /// <param name="activeCounts"> 各Upgradeの適用回数の配列。クライアントはこの情報を使用して、UIに適用回数を表示するなど、Upgradeの内容を正確に反映させる。</param>
        [ClientRpc]
        void ShowUpgradeSelectionClientRpc(int[] definitionIndices, int[] activeCounts)
        {
            if (!IsClient) return;

            Initialize();
            // クライアント側でUpgrade選択UIを表示するための処理をここに実装する
            SelectUpgradeElement.ViewData[] viewData = UpgradeManager.I.CreateViewData(definitionIndices, activeCounts);
            m_SelectUpgradeElement.Show(viewData, HandleUpgradeSelected);       // UIの表示と、Upgradeが選択されたときのコールバックを設定する
        }

        /// <summary>
        /// クライアントにUpgrade選択UIを非表示にするためのClientRpc。サーバーから呼び出される。
        /// </summary>
        [ClientRpc]
        void HideUpgradeSelectionClientRpc()
        {
            if (!IsClient) return;

            Initialize();
            m_SelectUpgradeElement.Hide();
        }

        /// <summary>
        /// Upgradeが選択されたときの処理。クライアント側で呼び出される。
        /// </summary>
        void HandleUpgradeSelected(int selectedIndex)
        {
            if (IsServer)
            {
                // サーバーであれば、直接Upgradeを適用する
                UpgradeManager.I.ApplyUpgradeAt(selectedIndex);
                return;
            }

            SelectUpgradeServerRpc(selectedIndex);      // クライアントからサーバーに選択されたUpgradeのインデックスを送るためのServerRpcを呼び出す
        }

        /// <summary>
        /// クライアントからサーバーに選択されたUpgradeのインデックスを送るためのServerRpc。クライアントから呼び出される。
        /// </summary>
        /// <param name="selectedIndex"> クライアントが選択したUpgradeのインデックス。サーバーはこのインデックスを使用して、UpgradeManagerからUpgradeの内容を取得し、適用する。</param>
        /// <param name="serverRpcParams"> ServerRpcのパラメータ。必要に応じて、呼び出し元のクライアントの情報などを取得するために使用できる。</param>
        [ServerRpc(InvokePermission = RpcInvokePermission.Owner)]
        void SelectUpgradeServerRpc(int selectedIndex, ServerRpcParams serverRpcParams = default)
        {
            UpgradeManager.I.ApplyUpgradeAt(selectedIndex);
        }
    }
}