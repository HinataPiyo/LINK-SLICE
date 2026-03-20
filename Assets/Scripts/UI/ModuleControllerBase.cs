using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Base
{
    public abstract class ModuleControllerBase : NetworkBehaviour
    {
        [SerializeField] protected PlayerConfig playerConfig;
        
        /// <summary>
        /// 各モジュールの初期化を行う
        /// </summary>
        /// <param name="uI">Moduleの初期化に必要なInterface</param>
        /// <param name="name">UIBuilderで設定している名前</param>
        protected void Initialize(IUIModuleHandler uI, string name, VisualElement root)
        {
            VisualElement moduleRoot = root.Q(name);

            if (moduleRoot != null)
            {
                uI.Initialize(moduleRoot);
            }
            else
            {
                Debug.LogError($"[ {name} ] モジュールのルート要素が見つかりません。");
            }
        }

        /// <summary>
        /// UIDocumentの再構築: GameObjectを非アクティブ→アクティブにした際
        /// UIDocumentが再構築されるため各モジュールの初期化を行う
        /// </summary>
        protected abstract void Initialize();
        protected virtual void OnEnable() => Initialize();
    }
}