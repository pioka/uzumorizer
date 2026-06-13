using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;

namespace io.github.pioka.uzumorizer.Editor
{
    /// <summary>
    /// ビルド全体で共有する状態。マーカー検出パス(CollectMarkers)から
    /// 変換パス(Convert)へ「このアバターを変換するか」を引き渡すために使う。
    ///
    /// マーカーコンポーネントは AAO より前に削除する（AAO の未知コンポーネント警告を避ける）ため、
    /// 変換パスの時点ではコンポーネントが存在しない。そのため、ここに判定結果を退避しておく。
    /// </summary>
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
    ///   - 元マテリアル（共有アセット）には一切触れない。
    ///   - 変換対象は new Material(src) で複製し、複製のみを書き換える。
    ///   - 同一マテリアル参照は 1 回だけ複製してキャッシュ共有（マテリアル数を増やさない）。
    ///   - 実際に変換されなかったマテリアルは複製を残さない。
    /// </summary>
    internal static class UzumoreConversionPass
    {
        /// <summary>うずもれシェーダーの名前に含まれる識別子（既変換マテリアルの除外に使用）。</summary>
        private const string UzumoreShaderToken = "Sigmal00/Uzumore";

        /// <summary>
        /// [Transforming / AAO より前] マーカーを検出して状態へ記録し、コンポーネントを削除する。
        ///
        /// 実行時に動作しないマーカーのため、AAO が処理する前に削除しておくのが
        /// AAO 公式が推奨する最善の互換方法。
        /// （参照: AAO「コンポーネントにAvatar Optimizerとの互換性をもたせる」/ コンポーネントを削除する）
        /// </summary>
        public static void CollectMarkers(BuildContext ctx)
        {
            var markers = ctx.AvatarRootObject.GetComponentsInChildren<Uzumorizer>(true);

            ctx.GetState<UzumorizerState>().Enabled = markers.Length > 0;

            // 将来: ここで markers の設定値を UzumorizerState にスナップショットする。

            foreach (var marker in markers)
            {
                if (marker != null) Object.DestroyImmediate(marker);
            }
        }

        /// <summary>
        /// [Optimizing / TTT・AAO より後] 記録済みフラグを見て lilToon マテリアルを変換する。
        /// </summary>
        public static void Convert(BuildContext ctx, IMaterialConversionStrategy strategy)
        {
            if (!ctx.GetState<UzumorizerState>().Enabled) return;

            var root = ctx.AvatarRootObject;
            var proxy = new UzumoreConverterProxy();

            // src -> dst（dst が src と同一参照のときは「変換不要だった」ことを表す）
            var cache = new Dictionary<Material, Material>();

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.sharedMaterials;
                var slotChanged = false;

                for (var i = 0; i < mats.Length; i++)
                {
                    var src = mats[i];
                    if (src == null) continue;
                    if (!strategy.ShouldConvert(renderer, i, src)) continue;

                    // 一次フィルタ: lilToon 系でなければ複製すらしない。
                    if (!lilToon.lilMaterialUtils.CheckShaderIslilToon(src)) continue;

                    // 既にうずもれシェーダーなら除外（無駄な複製を避ける）。
                    if (src.shader != null && src.shader.name.Contains(UzumoreShaderToken)) continue;

                    if (!cache.TryGetValue(src, out var dst))
                    {
                        dst = ConvertOne(ctx, proxy, src);
                        cache[src] = dst;
                    }

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
            var dst = new Material(src);
            proxy.Convert(dst);

            if (dst.shader == src.shader)
            {
                // 標準 lilToon のどの派生にも一致しなかった（= 変換対象外）。複製は残さない。
                Object.DestroyImmediate(dst);
                return src;
            }

            dst.name = src.name + " (Uzumore)";
            ctx.AssetSaver.SaveAsset(dst);
            return dst;
        }
    }
}
