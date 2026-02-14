# フェーズ1：基盤構築プラン

現在、プロジェクトは新規のUnity URPプロジェクトで、Input SystemとNavigationパッケージがインストールされた状態です。フェーズ1の目標（プレイアブル・プロトタイプ）を達成するために、直ちにキャラクターコントローラーと環境構築ツールのセットアップが必要です。

## 1. 初期セットアップ（ユーザー対応項目）

以下のパッケージはAIが自動でインストールできないため、Unityエディタ上で手動インストールをお願いします。

1.  **"Starter Assets - Third Person Character Controller" のインストール**
    *   `Window > Package Manager` を開く。
    *   `Packages: My Assets` を選択。
    *   "Starter Assets - Third Person Character Controller" を検索（必要ならダウンロード）し、**Import** してください。
    *   *注:* Input System関連の再起動を求められた場合は「Yes」を選択してください。
2.  **"ProBuilder" のインストール**
    *   Package Managerで `Packages: Unity Registry` に切り替え。
    *   "ProBuilder" を検索し、**Install** してください。
    *   *目的:* テスト用レベル（グレイボックス）の作成に使用します。

## 2. 実装タスク（AI担当項目）

上記のインストールが完了次第、AIは以下の作業を開始します。

### ステップ1: プロジェクト整理
プロジェクトを綺麗に保つため、以下のフォルダ構造を作成します：
*   `Assets/Scripts/Core` (マネージャー類)
*   `Assets/Scripts/Player` (プレイヤーロジック)
*   `Assets/Scripts/Environment` (昼夜システム等)
*   `Assets/_Game/Prefabs`

### ステップ2: コアスクリプトの実装
開発計画書で定義された以下のスクリプトを作成します：
*   **`PlayerStats.cs`:** ブラッドゲージ、耐久度、日光弱点を管理。
*   **`TimeManager.cs`:** 昼夜サイクルとイベント通知を管理。
*   **`GameManager.cs`:** 全体の進行管理。

### ステップ3: 統合（ユーザー支援）
*   `PlayerStats` をStarter AssetのプレイヤーPrefabにアタッチ。
*   UI Canvasを作成し、`PlayerStats` の値を表示。

## 直近3日間のスケジュール
*   **Day 1:** パッケージインストール（ユーザー） & フォルダ整理（AI）。
*   **Day 2:** `PlayerStats` と `TimeManager` のスクリプト作成（AI）。
*   **Day 3:** UI実装と動作テスト（AI/ユーザー）。
