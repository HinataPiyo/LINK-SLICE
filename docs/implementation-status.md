# Implementation Status

## 1. 現在実装できている内容

このドキュメントでは、現時点のコードとアセットから確認できる実装内容を、システム単位で詳細に整理します。

## 2. プレイヤー関連

### 2.1 移動

実装ファイル:

- `Assets/Scripts/Player/Movement.cs`

実装内容:

- マウス位置追従型の移動
- `NetworkBehaviour` ベースの owner-driven 操作
- 移動範囲の clamp
- `Camera.main` と平面レイキャストを使ったワールド座標化

意味:

- 操作主体が各クライアントの所有オブジェクトに限定されるため、マルチプレイ時の責務が明確
- FollowCamera の可動域設定と直結しており、ステージ境界の概念がすでにある

### 2.2 リンク生成

実装ファイル:

- `Assets/Scripts/Player/Link/LinkController.cs`
- `Assets/Scripts/Player/Link/Link.cs`
- `Assets/Scripts/Player/Link/LinkEffect.cs`

実装内容:

- シーン内の `Movement` を持つオブジェクトをプレイヤーとして収集
- 各プレイヤーペアの距離判定
- 一定距離内のペアにリンク生成
- 順序非依存の pair key による重複防止
- 範囲外では破断アニメーション後に破棄
- 破断中でも線の始点を追従させる設計
- 再接続時は break 状態をキャンセル

意味:

- 複数人プレイにおいても A-B と B-A の二重リンクを避けられる
- 演出中に即時削除しないため、視覚品質が高い
- リンク責務がシーンレベルのコントローラに寄っているため、人数拡張時の調整がしやすい

### 2.3 リンク攻撃

実装ファイル:

- `Assets/Scripts/Player/Attack.cs`
- `Assets/Scripts/Interface/IDamageable.cs`

実装内容:

- リンクの帯状判定で敵を検出
- `IDamageable` 実装先へダメージ適用
- 対象ごとのクールダウン管理
- 共有攻撃力ステータスを参照

意味:

- 攻撃対象や攻撃手段を今後差し替えやすい
- 現状はリンク攻撃に集中しているが、将来は別武器系統も追加可能

## 3. 敵関連

### 3.1 敵 AI

実装ファイル:

- `Assets/Scripts/Enemy/EnemyController.cs`
- `Assets/Scripts/Enemy/Movement.cs`
- `Assets/Scripts/Enemy/GetTarget.cs`
- `Assets/Scripts/Enemy/Attack.cs`
- `Assets/Scripts/Enemy/AttckType/Melee.cs`
- `Assets/Scripts/Enemy/AttckType/LongRange.cs`
- `Assets/Scripts/Enemy/AttckType/Bullet.cs`

実装内容:

- サーバー主導で敵行動を更新
- ターゲット探索
- ターゲットがいれば攻撃、いなければ移動
- 近接型と遠距離型の攻撃バリエーション

意味:

- 敵の意思決定をサーバーに限定しているため、同期の一貫性が高い
- 攻撃タイプが分かれているため、敵バリエーション拡張の余地がある

### 3.2 敵スポーン

実装ファイル:

- `Assets/Scripts/Enemy/EnemySpawnController.cs`
- `Assets/Scripts/ScriptableObjects/EnemySpawnConfig.cs`
- `Assets/Scripts/ScriptableObjects/EnemyContainer.cs`

実装内容:

- ステージ設定を順次読み込むウェーブ制スポーン
- Random / Grouped の 2 種類のスポーンパターン
- composition 単位の spawnCount / spawnInterval 設定
- NetworkObject を持つ敵のサーバー spawn
- 全滅待機後にアップグレード UI 表示

現在の登録アセット:

- `Assets/Datas/Stage/Stage1.asset`
- `Assets/Datas/Stage/Stage2.asset`

Stage1 の内容:

- 近接型中心
- 初期ウェーブとして扱いやすい小規模構成

Stage2 の内容:

- 遠距離型を導入
- grouped 構成の密集出現
- Stage1 より明確に進行度が上がる構成

## 4. Core 関連

実装ファイル:

- `Assets/Scripts/Common/HealthBase.cs`
- `Assets/Scripts/Core/Health.cs`
- `Assets/Scripts/Core/CoreController.cs`

実装内容:

- `HealthBase` に共通体力基盤を集約
- `NetworkVariable<int>` による現在体力と最大体力の同期
- 最大体力 modifier の加算・割合補正
- 死亡時演出の全クライアント通知
- Core の円形エネルギー表示のスケール更新

意味:

- Core 以外のオブジェクトにも同じ Health 基盤を再利用しやすい
- Modifier 方式により、将来的なバフ・デバフ・装備補正を追加しやすい

## 5. アップグレード関連

### 5.1 アップグレード進行管理

実装ファイル:

- `Assets/Scripts/Upgrade/UpgradeManager.cs`
- `Assets/Scripts/Upgrade/UpgradeState.cs`
- `Assets/Scripts/Upgrade/UpgradeContext.cs`

実装内容:

- 候補抽選
- 提示条件判定
- 現在レベル管理
- クライアントごとの選択受付
- 全員選択後の解決
- 適用処理の実行

現状仕様上のポイント:

- Manager は定義そのものを知らず、状態管理に寄っている
- 実際の効果適用は `UpgradeDefinition.Apply()` に委譲されている

### 5.2 現在実装済みアップグレード

実装ファイル:

- `Assets/Scripts/ScriptableObjects/UpgradeElements/CoreHealthUp.cs`
- `Assets/Scripts/ScriptableObjects/UpgradeElements/LinkStrengthUp.cs`

登録アセット:

- `Assets/Datas/UpgradeDatas/CoreHealthUp.asset`
- `Assets/Datas/UpgradeDatas/LinkStrengthUp.asset`
- `Assets/Datas/UpgradeDatabase.asset`

現在の内容:

- コア最大体力増強
- リンク攻撃力増強

どちらも割合 modifier を定義ごとの sourceId で上書きする設計です。これにより、同じアップグレードを再取得したときも累積の扱いが明確です。

## 6. UI 関連

実装ファイル:

- `Assets/Scripts/UI/Upgrade/UpgradeModuleController.cs`
- `Assets/Scripts/UI/Upgrade/Module/SelectUpgradeElement.cs`
- `Assets/Scripts/UI/ModuleControllerBase.cs`

UI アセット:

- `Assets/UI Toolkit/Main_Upgrade.uxml`
- `Assets/UI Toolkit/Temp_UpgradeElement.uxml`
- `Assets/UI Toolkit/Style/UpgradeElement_Style.uss`

実装内容:

- UI Toolkit ベースのアップグレード選択 UI
- 候補 3 枚表示
- レベル、名称、説明、選択人数の表示
- 選択後の二重送信防止
- サーバー主導の ClientRpc 更新

意味:

- 今後、別のモジュール UI を追加しても同じ枠組みに乗せやすい
- ネットワーク下でも UI 情報を軽量に再構築できる

## 7. ネットワーク・ロビー関連

実装ファイル:

- `Assets/Scripts/Relay-Lobby/LobbyManager.cs`
- `Assets/Scripts/Relay-Lobby/LobbyApiClient.cs`
- `Assets/Scripts/Relay-Lobby/LobbyEventHandler.cs`
- `Assets/Scripts/Relay-Lobby/LobbyBanner.cs`
- `Assets/Scripts/Relay-Lobby/LobbyUiStatePolicy.cs`
- `Assets/Scripts/Relay-Lobby/RelayTest.cs`
- `Assets/Scripts/Relay-Lobby/NetcodeSceneTransitionCoordinator.cs`

実装内容:

- Unity Services 初期化
- 匿名認証
- ロビー作成
- ロビー一覧取得
- ロビー参加
- Relay セッション作成 / 参加
- Host / Client 起動
- 接続人数待機
- Load シーン経由の Battle 遷移
- Battle 到達後の PlayerObject 手動生成

意味:

- 単なるローカル試作ではなく、オンライン接続まで視野に入った構成
- ロビー API、UI 状態、シーン遷移が分離されており、保守しやすい

## 8. 現在のデータアセット

確認できた主要アセット:

- `Assets/Datas/PlayerConfig.asset`
- `Assets/Datas/EnemyContainer.asset`
- `Assets/Datas/UpgradeDatabase.asset`
- `Assets/Datas/UpgradeDatas/CoreHealthUp.asset`
- `Assets/Datas/UpgradeDatas/LinkStrengthUp.asset`
- `Assets/Datas/Stage/Stage1.asset`
- `Assets/Datas/Stage/Stage2.asset`

PlayerConfig の現在値:

- Core maxHealth: 100
- Link strength: 1
- Link distance: 8
- attack interval: 0.5
- max line width: 0.15
- grow duration: 0.2
- shrink duration: 0.12

## 9. 既知の仕様と注意点

コードと既存メモから、少なくとも以下は重要です。

- Lobby から Battle へは Load シーン経由が前提
- Netcode の二重起動を避けるため `IsListening` ガードが重要
- Battle で PlayerObject を生成する都合上、接続承認で自動生成を止めている
- リンク破断中に target を雑に null 化すると見た目が崩れる
- Core のような scene object の見た目同期は Transform 変更だけでは不十分で、状態同期が必要

## 10. まだ薄い部分

現時点で基盤はある一方、以下は今後の伸びしろです。

- UI の種類が限定的
- アップグレード数がまだ少ない
- 敵タイプが少ない
- Battle 中のメタ情報表示が薄い
- セッション異常系のハンドリングは今後の改善余地がある

## 11. 総評

現状は「遊びの核」と「今後の拡張土台」が両方存在する状態です。特に、ScriptableObject 駆動のアップグレードとウェーブ構成、そしてネットワーク導線がすでにある点は大きく、単発プロトタイプではなく継続開発に向いた構造になっています。