# Dracul [drei] 開発スケジュール (ガントチャート)

`DevelopmentPlan.md` に基づく1年間の開発工程表です。

```mermaid
gantt
    title Dracul [drei] 1-Year Development Schedule
    dateFormat  YYYY-MM-DD
    axisFormat  %m月

    section Phase 1: 基盤構築
    プロジェクトセットアップ & 基本操作 :active, p1_1, 2026-02-01, 30d
    ブラッドゲージ & 昼夜サイクル        :       p1_2, after p1_1, 30d
    日光ダメージ & UI基礎               :       p1_3, after p1_2, 30d

    section Phase 2: コアメカニクス
    戦闘システム基礎 (近接・吸血)       :       p2_1, after p1_3, 30d
    敵AI v1 (徘徊・追跡) & 変異体        :       p2_2, after p2_1, 30d
    ダメージ・出血・死亡処理             :       p2_3, after p2_2, 30d

    section Phase 3: 世界構築と成長
    マップ作成 (地形・廃墟)             :       p3_1, after p2_3, 30d
    成長システム & 記録収集             :       p3_2, after p3_1, 30d
    武器追加 & 上位敵 (メカ)            :       p3_3, after p3_2, 30d

    section Phase 4: ポリッシュ
    乗り物プロトタイプ (Optional)        :       p4_1, after p3_3, 30d
    UI・サウンド・演出強化              :       p4_2, after p4_1, 30d
    最適化・バグ修正・調整              :       p4_3, after p4_2, 30d
```

## 各フェーズ詳細

### Phase 1: Foundation (Months 1-3)
*   **Month 1:** プロジェクト作成、キャラコン導入、グレイボクシング。
*   **Month 2:** 飢餓システム（ブラッドゲージ）、時間経過システム。
*   **Month 3:** 環境ダメージ（日光）、HUD実装。

### Phase 2: Core Mechanics (Months 4-6)
*   **Month 4:** 攻撃モーション、ヒットストップ、吸血アクション。
*   **Month 5:** NavMesh導入、ステートマシンAI、雑魚敵実装。
*   **Month 6:** ヘルス管理、状態異常（出血）、ゲームオーバー処理。

### Phase 3: World & Progression (Months 7-9)
*   **Month 7:** Terrain作成、アセット配置、レベルデザイン。
*   **Month 8:** インタラクトUI、ステータス強化ロジック、スキル解放。
*   **Month 9:** 銃撃戦、近接コンボ、中ボス級AI。

### Phase 4: Polish (Months 10-12)
*   **Month 10:** （余裕があれば）車両挙動の実装。
*   **Month 11:** メニュー画面、SE/BGM実装、ポストプロセス調整。
*   **Month 12:** プロファイラーによる負荷軽減、デバッグ、難易度調整。
