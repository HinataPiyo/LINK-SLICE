using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class TestUIManager : MonoBehaviour
{
    public static TestUIManager I { get; private set; }
    [SerializeField] VisualTreeAsset temp_uiAsset;
    [SerializeField] int maxDebugUiCount = 10;
    UIDocument uIDocument;
    readonly Queue<VisualElement> createdDebugUis = new Queue<VisualElement>();
    Label signalInfomation;
    string currentSignalText = string.Empty;
    string currentCameraTargetText = "CameraTarget: 未設定";


    private void Awake() {
        if(I == null)
        {
            I = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        uIDocument = GetComponent<UIDocument>();
        signalInfomation = uIDocument.rootVisualElement.Q<Label>("SignalInformation");
    }

    public void SetSignalInformation(string text)
    {
        currentSignalText = text ?? string.Empty;
        RefreshSignalInformation();
    }

    public void SetCameraTargetInformation(string text)
    {
        currentCameraTargetText = string.IsNullOrEmpty(text) ? "CameraTarget: 未設定" : text;
        RefreshSignalInformation();
    }

    void RefreshSignalInformation()
    {
        if (signalInfomation == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(currentSignalText))
        {
            signalInfomation.text = currentCameraTargetText;
            return;
        }

        signalInfomation.text = currentSignalText + "\n" + currentCameraTargetText;
    }

    public void CreateDebugUI(string text)
    {
        VisualElement parent = uIDocument.rootVisualElement.Q("DebugUIParent");
        if (parent == null || temp_uiAsset == null)
        {
            return;
        }

        VisualElement temp = temp_uiAsset.Instantiate();
        Label label = temp.Q<Label>();
        if (label != null)
        {
            label.text = text;
        }
        parent.Add(temp);

        createdDebugUis.Enqueue(temp);
        int limit = Mathf.Max(1, maxDebugUiCount);
        while (createdDebugUis.Count > limit)
        {
            VisualElement oldest = createdDebugUis.Dequeue();
            oldest?.RemoveFromHierarchy();
        }
    }
}