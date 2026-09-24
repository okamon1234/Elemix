# Elemix / 2Droguelike

ひとりで育て、仲間と挑む。ソロで作ったビルドを持ち寄り、属性反応・物理武器の進化・巨大ボスの部位破壊で攻略する2D協力ローグライトです。UIとゲーム内ガイドは日本語です。

## 動作環境

- Unity **6000.0.43f1**、2D Universal Render Pipeline **17.0.4**
- Windowsを主な検証対象としています。
- オンライン：**Photon Fusion 2.0.5 / Shared Mode**、最大4人、日本リージョン
- PUN2は使用しません。

## プロジェクトを開く

1. このリポジトリをcloneし、Unity Hubにフォルダーを追加します。
2. Unity 6000.0.43f1で開き、Package Managerの復元を待ちます。
3. [Photon公式](https://doc.photonengine.com/fusion/current/getting-started/sdk-download)からFusion 2.0.5のSDKを入手し、Unityへインポートします。SDK自体はこのリポジトリに同梱していません。インポート前はFusion関連のコンパイルエラーが出る場合があります。
4. Photonの設定画面で**自分のFusion用App ID**を設定してください。個別のApp IDはGitに含めません。オフラインのソロとボス練習ではクラウド接続は不要です。
5. `Assets/RogueSurvivors/Scenes/HomeScene.unity`を開いてPlayします。

SDKの版を変える場合はネットワークPrefabの参照・登録を再確認してください。メニュー `Tools > Rogue Survivors` にセットアップ・テスト・ビルドがあります。**Setupは生成済みScene/Prefabを更新するため、手動編集後はバックアップしてから実行してください。**

## 遊び方

ホームでキャラクター、準備時間、討伐対象を選びます。ソロ終了時にビルドを保存し、ロビーから同じ部屋コードで合流します。部屋主がボス戦を開始します。「ボス戦の練習」はオフライン用です。

| 操作 | 入力 |
|---|---|
| 移動 | WASD / 矢印キー / 左スティック |
| 攻撃 | 自動 |
| レベルアップ3択 | クリック / 1・2・3 |
| メニュー | Esc |

武器は物理9種・属性9種。合計6枠、属性は最大4つ、武器レベルは8まで。ソロ・ボス戦ともに死亡すると直近3回の強化を失って復活します。育成、反応、進化の詳しい説明は[ゲームガイド](Assets/RogueSurvivors/README.ja.md)を参照してください。

## 現在の構成

- ソロ準備、経験値、レベルアップ3択、時間経過で強くなる敵5種。
- アーチャー、ウォリアー、ナイト、メイジ、ランサーの5キャラクター。
- 付着を残す拡散、結晶の一撃バリア、物理9進化と光・闇の同時進化。
- 進化相手が出やすい3択、近接撃破の経験値25%増加と吸引、Lv3・5・8で変化する杖。
- ゴーレム、大グモ、ドラゴン、森の守護者の4ボス×3形態。部位を壊すたびに強くなる24種の攻撃。
- 四方向の装甲、広い決戦エリア、ミニマップ、自動回復と応急手当。
- Fusionによるプレイヤー・ボス・攻撃・勝利の同期。

## ディレクトリ

| 場所 | 内容 |
|---|---|
| `Assets/RogueSurvivors/Scripts` | ゲーム本体、UI、同期、開発用検証 |
| `Assets/RogueSurvivors/Editor` | Scene/Prefab/画像の構築、テスト起動、Windowsビルド |
| `Assets/RogueSurvivors/Scenes` | HomeScene / SoloScene / LobbyScene / MultiBossScene |
| `Assets/RogueSurvivors/Resources` | 実行時に読み込むPrefab・アート |
| `Packages` | Unityパッケージ構成 |
| `ProjectSettings` | タグ、レイヤー、衝突行列、ビルド設定 |

## 検証と開発状況

PlayModeテストは `Tools > Rogue Survivors > Run Play Mode Smoke Tests`、Windows版は `Build Windows Player` で作成します。出力先は `Builds/RiftSurvivors` です。

今回の戦闘版はローカル189項目、Windowsビルド（エラー・警告0）、同一PCの独立2クライアントによる結晶・部位破壊・3形態・勝利同期を確認済みです。別回線、4人の長時間プレイ、ホスト交代は検証範囲外です。最新の結果と制限は変更・検証記録にまとめています。

今回の改良内容と測定結果は[変更・検証記録](docs/変更と検証.md)、全クラスの責務は[スクリプト役割一覧](docs/スクリプト役割一覧.md)を参照してください。脳波を使う研究連携は将来案であり、現時点では未実装です。

## 配布範囲と個別設定

Unityのキャッシュ、ビルド、ログ、セーブ、移行バックアップ、Photon SDK、個別の接続設定はGit対象外です。PhotonのライセンスはSDKの提供条件に従ってください。このリポジトリにはオープンソースライセンスをまだ設定していません。

必要に応じて `Assets/RogueSurvivors/Resources/RogueSurvivors/LocalNetworkSettings.json` に `{"fusionAppId":"YOUR_FUSION_APP_ID"}` を作成するとローカル設定を優先します。このファイルとmetaはコミットされません。
