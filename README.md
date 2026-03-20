# LINK-SLICE

LINK-SLICE は、複数プレイヤー同士をリンクで接続し、そのリンク自体を攻撃手段として使いながらウェーブを耐え抜く、マルチプレイ前提の 2D アクションゲームです。

この README は入口です。実装内容の詳細は文量を分割し、以下のドキュメントへ整理しています。

## ドキュメント一覧

- [docs/project-overview.md](docs/project-overview.md)
	- ゲーム概要、現状の完成範囲、シーン構成、ゲームループ、採用技術
- [docs/implementation-status.md](docs/implementation-status.md)
	- 現在実装できている機能の詳細、アセット構成、既知の仕様と注意点
- [docs/script-architecture.md](docs/script-architecture.md)
	- スクリプトの責務、ディレクトリ単位の意味、依存関係、拡張ポイント
- [docs/scriptableobjects-guide.md](docs/scriptableobjects-guide.md)
	- ScriptableObject の役割、作成方法、登録方法、導入手順
- [docs/future-roadmap.md](docs/future-roadmap.md)
	- 拡張性、将来性、推奨追加機能、技術的改善方針

## プロジェクト概要

- プレイヤーは個別に移動し、一定距離内の他プレイヤーと自動でリンクされます
- リンクは LineRenderer と当たり判定を持ち、敵に継続ダメージを与えます
- 敵はウェーブ制でスポーンし、全滅後にアップグレード選択が発生します
- アップグレードは ScriptableObject 定義駆動で実装されており、将来的な追加に強い構成です
- Lobby / Relay / Netcode for GameObjects によるマルチプレイ導線が実装されています

## 現在の主要シーン

- `Lobby`
	- ロビー作成、一覧更新、参加、ゲーム開始を担当
- `Load`
	- ゲーム本編へ入る前の中継ロードシーン
- `Battle`
	- 実際のプレイが進行する戦闘シーン
- `Title`
	- シーンは存在するが、コード上の主導線は Lobby 起点です

## 現時点の実装ハイライト

- プレイヤーの owner-driven 移動
- 複数プレイヤー間の距離判定によるリンク生成と破断演出
- リンクによる敵攻撃と共有攻撃力ステータス
- 敵ウェーブスポーンと撃破待機
- ウェーブ間アップグレード UI と投票型選択
- Core の体力管理と見た目同期
- Lobby / Relay / Netcode を使った Host / Client 起動とシーン遷移

## リポジトリ構成

- `Assets/Scripts/Common`
	- 共通基盤、体力、ロード制御
- `Assets/Scripts/Core`
	- コアの体力とビジュアル
- `Assets/Scripts/Player`
	- プレイヤー移動、リンク、リンク攻撃
- `Assets/Scripts/Enemy`
	- 敵 AI、攻撃、スポーン
- `Assets/Scripts/Upgrade`
	- アップグレード進行管理
- `Assets/Scripts/ScriptableObjects`
	- 設定データとアップグレード定義
- `Assets/Scripts/Relay-Lobby`
	- Unity Services を使ったロビー・Relay 導線
- `Assets/Scripts/UI`
	- UI Toolkit ベースのアップグレード UI と補助 UI

## 技術スタック

- Unity
- Netcode for GameObjects
- Unity Relay
- Unity Lobby
- Unity Authentication
- Unity Transport
- UI Toolkit
- Input System
- URP

詳細なパッケージは `Packages/manifest.json` を参照してください。

## 読み進める順番

1. まず [docs/project-overview.md](docs/project-overview.md) でゲーム全体像を把握
2. 次に [docs/implementation-status.md](docs/implementation-status.md) で現状機能を確認
3. 実装拡張や保守を行う場合は [docs/script-architecture.md](docs/script-architecture.md) を確認
4. データ追加やバランス調整を行う場合は [docs/scriptableobjects-guide.md](docs/scriptableobjects-guide.md) を確認
5. 中長期の改修計画は [docs/future-roadmap.md](docs/future-roadmap.md) を参照

