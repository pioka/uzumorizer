using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;

namespace io.github.pioka.uzumorizer.Editor
{
    /// ビルド全体で共有する状態。マーカー検出パス(CollectMarkers)から
    /// 変換パス(Convert)へ「このアバターを変換するか」を引き渡すために使う。
    ///
    /// マーカーコンポーネントは AAO より前に削除する（AAO の未知コンポーネント警告を避ける）ため、
    /// 変換パスの時点ではコンポーネントが存在しない。そのため、ここに判定結果を退避しておく。
    internal sealed class UzumorizerState
    {
        public bool Enabled;

        // 将来: 対象/除外などの設定をここにスナップショットして Convert 側で参照する。
    }

    /// <summary>
    /// アバター内の lilToon マテリアルをうずもれシェーダーへ非破壊変換するパス本体。
    ///
    /// AAO 互換性のため 2 パス構成:
    ///   1. CollectMarkers : AAO より前(Transforming)で実行。マーカーを検出して状態に記録し、
    ///      コンポーネントを削除する。→ AAO 処理時にはコンポーネントが無いため警告が出ない。
    ///   2. Convert        : TTT/AAO より後(Optimizing)で実行。記録済みフラグを見て変換する。
    ///
    /// 非破壊性の担保:
    ///   - 元マテリアル（プロジェクトの共有アセット）には一切触れない。
    ///   - 変換対象は new Material(src) で複製し、複製のみを書き換える。
    ///   - 複製した置換マテリアルは ObjectRegistry に登録し、後段プラグイン/エラーレポートが追跡できるようにする。
    ///   - 同一マテリアル参照は 1 回だけ複製してキャッシュ共有（マテリアル数を増やさない）。
    ///   - 実際に変換されなかったマテリアルは複製を残さない。
    ///   - ビルド中に生成された一時マテリアル（IsTemporaryAsset）は共有アセットではないため、
    ///     複製せず in-place で変換する（無駄な複製を避ける）。
    /// </summary>
    internal static class UzumoreConversionPass
    {
        /// うずもれシェーダーの名前に含まれる識別子（既変換マテリアルの除外に使用）。
        private const string UzumoreShaderToken = "Sigmal00/Uzumore";

        // BuildPhase.Transforming: コンポーネントの検出,グローバルでの変換ON/OFFの判定,不要コンポーネントの除去
        public static void CollectMarkers(BuildContext ctx)
        {
            // アバターに含まれるUzumorizerコンポーネントを取得
            var markers = ctx.AvatarRootObject.GetComponentsInChildren<Uzumorizer>(true);

            // 1つでもあればEnabledとしてBuildContextのStateをセット
            ctx.GetState<UzumorizerState>().Enabled = markers.Length > 0;

            // 不要になったUzumorizerコンポーネントを除去
            foreach (var marker in markers) Object.DestroyImmediate(marker);
        }

        /// BuildPhase.Optimizing[TTT・AAO より後] 記録済みフラグを見て lilToon マテリアルを変換する。
        public static void Convert(BuildContext ctx)
        {
            // Transforming時にUzumorizerコンポーネントを検出していなければ何もしない
            if (!ctx.GetState<UzumorizerState>().Enabled) return;

            var root = ctx.AvatarRootObject;
            var proxy = new UzumoreConverterProxy();

            // src -> dst（dst が src と同一参照のときは「変換不要だった」ことを表す）
            var cache = new Dictionary<Material, Material>();

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                // 各メッシュレンダラーの持つマテリアル配列
                var mats = renderer.sharedMaterials;

                // マテリアル配列が変更されたかどうか
                var slotChanged = false;

                for (var i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null) continue;

                    // lilToon 系でなければスキップ。
                    if (!lilToon.lilMaterialUtils.CheckShaderIslilToon(src)) continue;

                    // 既にうずもれシェーダーならスキップ。
                    if (src.shader != null && src.shader.name.Contains(UzumoreShaderToken)) continue;

                    // キャッシュから変換先を解決できない場合、変換先を生成してキャッシュを更新
                    if (!cache.TryGetValue(src, out var dst))
                    {
                        dst = ConvertOne(ctx, proxy, src);
                        cache[src] = dst;
                    }

                    // 変換先の反映
                    if (!ReferenceEquals(dst, src))
                    {
                        mats[i] = dst;
                        slotChanged = true;
                    }
                }

                if (slotChanged) renderer.sharedMaterials = mats;
            }
        }

        /// <summary>
        /// 1 マテリアルを複製して変換を試みる。
        /// 変換された（shader が変化した）場合は複製を一時アセット化して返す。
        /// 変換されなかった場合は複製を破棄し、元マテリアルをそのまま返す。
        /// </summary>
        private static Material ConvertOne(BuildContext ctx, UzumoreConverterProxy proxy, Material src)
        {
            // 変換前マテリアルを複製し、うずもれシェーダーの変換に渡す
            var dst = new Material(src);
            proxy.Convert(dst);

            // 変換前後でシェーダーが同一(何も変換されなかった)場合、変換処理用の複製を破棄し、変換前のマテリアルをそのまま返す
            if (dst.shader == src.shader)
            {
                Object.DestroyImmediate(dst);
                return src;
            }

            // 変換後マテリアルのリネームと保存
            dst.name = src.name + " (Uzumore)";
            ctx.AssetSaver.SaveAsset(dst);

            // (NDMFベストプラクティス準拠)　src → dst の置換関係を登録し、後段プラグインやエラーレポートが追跡できるようにする。
            ObjectRegistry.RegisterReplacedObject(src, dst);

            return dst;
        }
    }
}
