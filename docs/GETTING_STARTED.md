# 起動・操作方法

[作品紹介へ戻る](../README.md) · [設計の説明](ARCHITECTURE.md)

## 必要な環境

- Windows 64bit（主な検証環境）
- Unity HubとUnity **6000.0.43f1**。Windowsビルドを行う場合は対応モジュールも導入します。
- Photon Fusion **2.0.5** SDK。ライセンス付きSDKはこのリポジトリに含めていません。
- オンライン接続を試す場合は、自分のPhoton Fusion用App ID。

## プロジェクトを開く

1. `git clone https://github.com/okamon1234/Elemix.git` で取得します。
2. Unity Hubから取得したフォルダーを追加し、指定バージョンのEditorで開きます。
3. Package Managerの復元を待ちます。URPなどの構成は`Packages/manifest.json`に記録されています。
4. [Photon公式のSDK案内](https://doc.photonengine.com/fusion/current/getting-started/sdk-download)からFusion 2.0.5を入手してインポートします。導入前はFusion関連のコンパイルエラーが出る場合があります。
5. `Assets/RogueSurvivors/Scenes/HomeScene.unity`を開いてPlayします。

SDKの版を変える場合はネットワークPrefabの参照・登録も再確認してください。生成済みのScene・Prefabを含むため、通常の起動でSetupを実行する必要はありません。

## 最初のプレイ

ホームでキャラクター、草原／遺跡、準備時間、討伐対象を選び、「このキャラクターで出撃」から開始します。敵を倒して経験値を集め、3択で武器を獲得・強化します。攻撃は自動です。

| 操作 | 入力 |
|---|---|
| 移動 | WASD / 矢印キー / 左スティック |
| 3択の決定 | クリック / 1・2・3 |
| メニュー | Esc |
| ボスの部位指定 | Q / RB / 部位ボタン / 中クリック |
| 部位指定を解除 | E / LB |

準備時間が終わるとビルドを保存してロビーへ進みます。ホームの「ボス戦の練習」はクラウド接続なしで確認できます。詳しい武器・反応・死亡時のルールは[ゲームガイド](../Assets/RogueSurvivors/README.ja.md)を参照してください。

## オンラインを試す

1. Photon設定画面で自分の**Fusion用App ID**を設定します。PUN2用のSDKは使いません。
2. 同じコード版・同じApp IDを使う2つのクライアントを用意します。EditorとWindowsビルドの組み合わせでも確認できます。
3. それぞれソロでビルドを作り、ロビーで同じ部屋コードへ参加します。
4. 全員が合流したら部屋主がボス戦を開始します。

リージョンは`jp`です。ネットワーク互換性のためAppVersionを設定しているので、全員を同じ版に揃えてください。設定上は最大4人、保存済み検証は2クライアントです。

任意で`Assets/RogueSurvivors/Resources/RogueSurvivors/LocalNetworkSettings.json`を作成すると、その値を優先します。

```json
{"fusionAppId":"YOUR_FUSION_APP_ID"}
```

このファイルとmetaはGit対象外です。個別の接続設定を公開リポジトリへ追加しないでください。

## 検証・ビルド

編集したシーンを保存してから、Unityメニューの`Tools > Rogue Survivors`を使います。

| メニュー | 内容 / 主な結果ファイル |
|---|---|
| Validate | プロジェクト構成の確認 |
| Run Play Mode Smoke Tests | 独自の動作チェック / `Logs/RogueSmokeResult.json` |
| Run Combat Balance Comparison | ソロの自動比較 / `Logs/RogueBalanceResult.json` |
| Run Boss Balance Comparison | ボスの火力・戦闘比較 |
| Build Windows Player | `Builds/RiftSurvivors/RiftSurvivors.exe`を作成 |

現在のビルドメニューは**Development Build**を生成します。ビルド時にはボス用Scene・アート設定の更新処理も実行されます。`Setup`も生成済みScene・Prefabを更新するため、手動編集を加えた場合は事前にコミットまたはバックアップしてください。

## うまく起動しないとき

| 状況 | 確認すること |
|---|---|
| Fusion型が見つからない | Fusion 2.0.5 SDKの導入とUnityの再コンパイル完了を確認 |
| シーンを開いてもホームが出ない | `HomeScene.unity`を開いているか確認 |
| 部屋に合流できない | App ID・部屋コード・ゲームの版が両方で同じか確認 |
| 保存済みビルドが見つからない | ソロを完了したか確認。保存先は`Application.persistentDataPath`内の`rogue-build-v1.json` |

この資料更新では新規PCでのクリーン導入を再実施していません。導入手順は既存の構成とコードに基づきます。過去の実行結果と残件は[検証資料](属性反応と物理の火力・手応え.md)にあります。
