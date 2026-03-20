namespace UI.Module
{
    using UnityEngine;
    using UI.Base;
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

        protected override void Initialize()
        {
            VisualElement root = uiDocument.rootVisualElement;

            Initialize(m_SelectUpgradeElement, MODULE_SELECT_UPGRADE_ELEMENT, root);
        }
    }
}