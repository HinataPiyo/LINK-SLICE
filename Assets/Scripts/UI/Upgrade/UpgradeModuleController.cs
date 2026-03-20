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

        /// <summary>
        /// サーバーからクライアントにUpgrade選択UIを表示するためのClientRpcを呼び出す。サーバーであれば、直接UIを表示する。
        /// </summary>
        /// <param name="entries"> サーバー側で表示するUpgradeの内容を含むEntryの配列。クライアントはこの情報を使用して、UIに表示するUpgradeの内容を決定する。</param>
        public void ShowUpgradeSelection(UpgradeManager.Entry[] entries)
        {
            if (!IsServer || entries == null) return;

            int[] definitionIndices = new int[entries.Length];      // UpgradeDefinitionのインデックスをクライアントに送るための配列
            int[] currentLevels = new int[entries.Length];          // 各Upgradeの現在レベルをクライアントへ送るための配列
            for (int i = 0; i < entries.Length; i++)
            {
                // クライアント側でも同じ候補内容を再構築できるよう、定義の参照情報と現在レベルだけを送る。
                definitionIndices[i] = UpgradeManager.I.GetUpgradeDefinitionIndex(entries[i].data);
                currentLevels[i] = entries[i].currentLevel;
            }

            // クライアントにUpgrade選択UIを表示するためのClientRpcを呼び出す
            ShowUpgradeSelectionClientRpc(definitionIndices, currentLevels);
        }

        /// <summary>
        /// クライアントにUpgrade選択UIを非表示にするためのClientRpcを呼び出す
        /// </summary>
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
        /// <param name="currentLevels"> 各Upgradeの現在レベルの配列。クライアントはこの情報から、今回提示されるレベルと説明文を再構築する。</param>
        [ClientRpc]
        void ShowUpgradeSelectionClientRpc(int[] definitionIndices, int[] currentLevels)
        {
            if (!IsClient) return;

            Initialize();
            // クライアント側でUpgrade選択UIを表示するための処理をここに実装する
            SelectUpgradeElement.ViewData[] viewData = UpgradeManager.I.CreateViewData(definitionIndices, currentLevels);
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