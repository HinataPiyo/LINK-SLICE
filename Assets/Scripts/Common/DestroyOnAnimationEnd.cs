using UnityEngine;

public class DestroyOnAnimationEnd : MonoBehaviour
{
    [SerializeField] GameObject parent;

    public void DestroyParent()
    {
        Destroy(parent);
    }
    /// <summary>
    /// アニメーション終了時呼び出される
    /// </summary>
    public void DestroyThisObject()
    {
        Destroy(gameObject);
    }
    
}