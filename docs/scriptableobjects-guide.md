# ScriptableObjects Guide

## 1. このプロジェクトでの ScriptableObject の位置付け

このプロジェクトでは、ScriptableObject は「実行時ロジックそのもの」ではなく、「ゲームバランスや定義情報を保持するデータ資産」として使われています。

現時点で確認できる主な用途は以下です。

- プレイヤー基本設定
- 敵スポーン設定
- アップグレード定義
- アップグレード定義の一覧管理
- 敵プレハブ対応表
- 音声データ管理

この分離によって、以下の利点があります。

- 数値調整をコード改修なしで行いやすい
- 新しい要素追加時のコード変更範囲を小さくできる
- Git 上でデータ変更とロジック変更を分けて追いやすい

## 2. 現在存在する ScriptableObject クラス

### 2.1 PlayerConfig

スクリプト:

- `Assets/Scripts/ScriptableObjects/PlayerConfig.cs`

アセット:

- `Assets/Datas/PlayerConfig.asset`

役割:

- Core の基本体力
- Link の prefab
- Link の基本攻撃力
- Link の接続距離
- Link の攻撃間隔
- Link の描画パラメータ

現在の使われ方:

- `LinkController` が起動時に `LinkRuntimeStats.Initialize(playerConfig)` を呼ぶ
- `Link` と `Player Attack` がリンク関連の基礎値を参照する

### 2.2 EnemySpawnConfig

スクリプト:

- `Assets/Scripts/ScriptableObjects/EnemySpawnConfig.cs`

アセット:

- `Assets/Datas/Stage/Stage1.asset`
- `Assets/Datas/Stage/Stage2.asset`

役割:

- ウェーブごとの敵出現構成定義
- 敵タイプ
- スポーンパターン
- 出現数
- 出現間隔

現在の使われ方:

- `EnemySpawnController` が `spawnConfigs` 配列を順次処理する

### 2.3 UpgradeDefinition

スクリプト:

- `Assets/Scripts/ScriptableObjects/UpgradeDefinition.cs`

役割:

- 個々のアップグレード定義の抽象基底
- 表示名
- アイコン
- 最大レベル
- 提示条件
- 説明文
- 効果適用処理

現在の派生クラス:

- `Assets/Scripts/ScriptableObjects/UpgradeElements/CoreHealthUp.cs`
- `Assets/Scripts/ScriptableObjects/UpgradeElements/LinkStrengthUp.cs`

### 2.4 UpgradeDatabase

スクリプト:

- `Assets/Scripts/ScriptableObjects/UpgradeElements/UpgradeDatabase.cs`

アセット:

- `Assets/Datas/UpgradeDatabase.asset`

役割:

- 使用可能な `UpgradeDefinition` 一覧を保持する

現在の使われ方:

- `UpgradeManager` がここから提示候補を抽出する

### 2.5 EnemyContainer

スクリプト:

- `Assets/Scripts/ScriptableObjects/EnemyContainer.cs`

アセット:

- `Assets/Datas/EnemyContainer.asset`

役割:

- `EnemyType` と実際の敵プレハブの対応表を提供する

### 2.6 AudioData

スクリプト:

- `Assets/Scripts/ScriptableObjects/AudioData.cs`

役割:

- 効果音や音声管理用のデータ定義

## 3. ScriptableObject を新規作成する基本手順

### 3.1 既存クラスのアセットを作る場合

例として、新しい `EnemySpawnConfig` や `PlayerConfig` を作る場合です。

1. Unity Editor の Project ビューで保存先フォルダを開く
2. 右クリックする
3. `Create` メニューを開く
4. 各クラスの `CreateAssetMenu` で定義された項目を選ぶ
5. 生成された asset にわかりやすい名前を付ける
6. Inspector で値を設定する
7. その asset を参照先コンポーネントへ割り当てる

本プロジェクトの例:

- `Config/EnemySpawnConfig`
- `Config/UpgradeDatabase`
- `UpgradeData/CoreHealthUp`
- `UpgradeData/LinkStrengthUp`

`PlayerConfig` は `menuName = "PlayerConfig"` なので、Create メニュー上では単独項目として出ます。

### 3.2 新しい種類の ScriptableObject クラスを作る場合

1. `Assets/Scripts/ScriptableObjects` 配下などに新しい C# クラスを作成する
2. `ScriptableObject` を継承する
3. `CreateAssetMenu` 属性を付与する
4. 必要な public / SerializeField を定義する
5. Unity に戻って asset を生成する

最小例:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ExampleConfig", menuName = "Config/ExampleConfig")]
public class ExampleConfig : ScriptableObject
{
    [SerializeField] int value = 1;

    public int Value => value;
}
```

## 4. このプロジェクトでの導入方法

ScriptableObject は、作っただけでは使われません。どこかのコンポーネントやデータベースに登録して初めて動作に反映されます。

### 4.1 PlayerConfig の導入

導入先:

- `LinkController`
- `Link`
- `Player Attack`

実務上の手順:

1. `PlayerConfig.asset` を作成する
2. Link prefab を設定する
3. strength、distance、attack interval、line width 系を設定する
4. Battle シーン上の `LinkController` の `playerConfig` に割り当てる
5. Link prefab 側の `Link` / `Attack` が同じ設定を参照していることを確認する

注意点:

- Link prefab が未設定だとリンク生成が行えません
- target layer が誤っていると敵へヒットしません

### 4.2 EnemySpawnConfig の導入

導入先:

- `EnemySpawnController`

実務上の手順:

1. `EnemySpawnConfig` asset を作成する
2. entries にウェーブを追加する
3. compositions に敵タイプと出現条件を設定する
4. Battle シーン上の `EnemySpawnController` の `spawnConfigs` に追加する

注意点:

- `EnemyContainer` に対応敵 prefab がない enemyType は出せません
- grouped を使う場合は composition 内のまとまりを意識して設計する必要があります

### 4.3 UpgradeDefinition の導入

導入先:

- `UpgradeDatabase`
- 間接的に `UpgradeManager`

実務上の手順:

1. `UpgradeDefinition` 派生クラスを作る
2. `CreateAssetMenu` を付ける
3. `GetDescription()` と `Apply()` を実装する
4. Unity Editor で asset を生成する
5. `Assets/Datas/UpgradeDatabase.asset` に追加する
6. `UpgradeManager` が参照している `UpgradeDatabase` にその asset が含まれていることを確認する

重要:

- 新しいアップグレードを作っても `UpgradeDatabase` に登録しなければ候補に出ません
- `Apply()` は対象参照が無い場合に false を返す設計なので、文脈依存先が存在することを確認してください

## 5. 新しいアップグレードを追加する詳細手順

ここが将来的に最も頻繁に拡張されるポイントです。

### 5.1 手順の全体像

1. 派生クラスを作る
2. 効果の対象を `UpgradeContext` から取得する
3. `Apply()` で対象へ modifier を設定する
4. asset を作る
5. `UpgradeDatabase` へ登録する
6. UI 表示を確認する

### 5.2 例: プレイヤー移動速度アップを追加したい場合

現状の `UpgradeContext` には移動速度対象が入っていないため、次の拡張が必要です。

1. ランタイム移動ステータスを作る
2. `UpgradeContext` にその参照を追加する
3. `UpgradeManager.BuildContext()` で参照を構築する
4. `MoveSpeedUp : UpgradeDefinition` を作る
5. `Apply()` で移動速度 modifier を設定する
6. asset を作成し `UpgradeDatabase` に追加する

このように、ScriptableObject だけで完結する場合と、コンテキスト側の拡張が必要な場合があります。

## 6. ScriptableObject 設計上のルール

このプロジェクトで今後守るとよいルールは以下です。

- ScriptableObject は基礎定義を持つ
- 実行時に変化する値は MonoBehaviour 側に持つ
- `Apply()` は「最終値の直書き」よりも modifier 設定を優先する
- 追加した定義は必ず database 型 asset に登録する
- シーン側の参照が必要なら、導入手順を README に残す

## 7. 現在の設計が拡張に強い理由

### 7.1 定義追加時に Manager を増改築しにくくてよい

`UpgradeManager` が `CanOffer()` と `Apply()` を各定義に委譲しているため、アップグレードを増やしても manager の型分岐が肥大化しません。

### 7.2 データ差し替えでプレイ感を変えられる

敵ステージや PlayerConfig を差し替えるだけで、難易度やテンポを大きく変えられます。

### 7.3 テストしやすい

データとロジックが分かれているため、問題が数値起因か処理起因か切り分けやすくなります。

## 8. 実運用でおすすめの整理方法

今後 asset が増えるなら、以下のようなフォルダ整理が有効です。

- `Assets/Datas/Player/`
- `Assets/Datas/Enemy/`
- `Assets/Datas/Stage/`
- `Assets/Datas/Upgrade/Definitions/`
- `Assets/Datas/Upgrade/Databases/`
- `Assets/Datas/Audio/`

現状でも `Assets/Datas/Stage` と `Assets/Datas/UpgradeDatas` は存在しているため、この方向に寄せるとより見通しが良くなります。

## 9. まとめ

このプロジェクトにおける ScriptableObject は、単なる設定置き場ではなく、今後のゲーム拡張を支えるコンテンツ追加基盤です。特にアップグレードとステージ構成は、ScriptableObject を中心に伸ばしていくのが最も自然です。