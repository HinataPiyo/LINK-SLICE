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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            Initialize();
        }

        protected override void Initialize()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (m_SelectUpgradeElement == null)
            {
                m_SelectUpgradeElement = GetComponent<SelectUpgradeElement>();
            }

            if (uiDocument == null || m_SelectUpgradeElement == null)
            {
                return;
            }

            VisualElement root = uiDocument.rootVisualElement;
            if (root == null)
            {
                return;
            }

            Initialize(m_SelectUpgradeElement, MODULE_SELECT_UPGRADE_ELEMENT, root);
        }

        public void ShowUpgradeSelection(UpgradeManager.Entry[] entries)
        {
            if (!IsServer || entries == null)
            {
                return;
            }

            int[] definitionIndices = new int[entries.Length];
            int[] activeCounts = new int[entries.Length];
            for (int i = 0; i < entries.Length; i++)
            {
                definitionIndices[i] = UpgradeManager.I.GetUpgradeDefinitionIndex(entries[i].data);
                activeCounts[i] = entries[i].activeCount;
            }

            ShowUpgradeSelectionClientRpc(definitionIndices, activeCounts);
        }

        public void HideUpgradeSelection()
        {
            if (!IsServer)
            {
                return;
            }

            HideUpgradeSelectionClientRpc();
        }

        [ClientRpc]
        void ShowUpgradeSelectionClientRpc(int[] definitionIndices, int[] activeCounts)
        {
            if (!IsClient)
            {
                return;
            }

            Initialize();
            SelectUpgradeElement.ViewData[] viewData = UpgradeManager.I.CreateViewData(definitionIndices, activeCounts);
            m_SelectUpgradeElement.Show(viewData, HandleUpgradeSelected);
        }

        [ClientRpc]
        void HideUpgradeSelectionClientRpc()
        {
            if (!IsClient)
            {
                return;
            }

            Initialize();
            m_SelectUpgradeElement.Hide();
        }

        void HandleUpgradeSelected(int selectedIndex)
        {
            if (IsServer)
            {
                UpgradeManager.I.ApplyUpgradeAt(selectedIndex);
                return;
            }

            SelectUpgradeServerRpc(selectedIndex);
        }

        [ServerRpc(InvokePermission = RpcInvokePermission.Owner)]
        void SelectUpgradeServerRpc(int selectedIndex, ServerRpcParams serverRpcParams = default)
        {
            UpgradeManager.I.ApplyUpgradeAt(selectedIndex);
        }
    }
}