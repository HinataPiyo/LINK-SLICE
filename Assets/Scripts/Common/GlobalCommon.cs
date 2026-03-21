public static class GlobalCommon
{
    public const string LoadingSceneName = "Load";
    public const string DefaultGameplaySceneName = "Battle";

    public const string LAYER_INVISIBLE = "Invisible";    // 敵が攻撃を受けないレイヤー
    public const string LAYER_ENEMY = "Enemy";    // 敵のレイヤー

    public static string NextSceneName { get; set; }
}