using System.Collections.Generic;
using nadena.dev.ndmf;
using UnityEngine;

namespace io.github.pioka.uzumorizer.Editor
{
    /// アバター内の lilToon マテリアルをうずもれシェーダーへ非破壊変換するパス本体。
    ///
    /// 単一パス構成（Optimizing フェーズ・AAO より前）:
    ///   マーカー検出 → 変換 → マーカー削除 を 1 パスで行う。
    ///   AAO より前にマーカーを削除する。
    ///
    /// 非破壊性の担保:
    ///   - 元マテリアル（プロジェクトの共有アセット）には一切触れない。
    ///   - 基本的に変換対象は new Material(src) で複製し、複製のみを書き換える
    ///   - ただし既に一時アセットとして取り扱われている場合はin-placeで書き換える
    ///   - 複製した置換マテリアルは ObjectRegistry に登録し、後段プラグイン/エラーレポートが追跡できるようにする。
    ///   - 同一マテリアル参照は 1 回だけ複製してキャッシュ共有（マテリアル数を増やさない）。
    ///   - 実際に変換されなかったマテリアルは複製を残さない。

    internal static class UzumoreConversionPass
    {
        /// うずもれシェーダーの名前に含まれる識別子（既変換マテリアルの除外に使用）。
        private const string UzumoreShaderToken = "Sigmal00/Uzumore";

        /// <summary>
        /// パス本体。マーカーを検出して lilToon マテリアルを変換し、マーカーを削除する。
        /// AAO より前で実行されるため、ここでマーカーを除去すれば AAO の警告は出ない。
        /// </summary>
        public static void Execute(BuildContext ctx)
        {
            var root = ctx.AvatarRootObject;

            // アバターに含まれる Uzumorizer マーカーを取得。1 つも無ければ何もしない。
            var markers = root.GetComponentsInChildren<Uzumorizer>(true);
            if (markers.Length == 0) return;

            var proxy = new UzumoreConverterProxy();

            // src -> dst（dst が src と同一参照のときは「変換不要だった / in-place 変換した」ことを表す）
            var materialCache = new Dictionary<Material, Material>();

            var renderers = root.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                // 各メッシュレンダラーの持つマテリアル配列
                var materials = renderer.sharedMaterials;

                // マテリアル配列が変更されたかどうか
                var modified = false;

                for (var i = 0; i < materials.Length; i++)
                {
                    var src = materials[i];
                    if (src == null) continue;

                    // lilToon 系でなければスキップ。
                    if (!lilToon.lilMaterialUtils.CheckShaderIslilToon(src)) continue;

                    // 既にうずもれシェーダーならスキップ。
                    if (src.shader != null && src.shader.name.Contains(UzumoreShaderToken)) continue;

                    // キャッシュから変換先を解決できない場合、変換先を生成してキャッシュを更新
                    if (!materialCache.TryGetValue(src, out var dst))
                    {
                        dst = ConvertOne(ctx, proxy, src);
                        materialCache[src] = dst;
                    }

                    // 変換先の反映
                    if (!ReferenceEquals(dst, src))
                    {
                        materials[i] = dst;
                        modified = true;
                    }
                }

                if (modified) renderer.sharedMaterials = materials;
            }

            // 変換完了後、マーカーを除去して後段(AAO)の考慮対象から外す。
            foreach (var marker in markers) Object.DestroyImmediate(marker);
        }

        /// <summary>
        /// 1 マテリアルを変換する。
        ///
        /// 一時マテリアル（IsTemporaryAsset）は共有アセットではないため複製せず in-place で変換し、
        /// 参照を変えずにそのまま返す（呼び出し側でスロット差し替えは発生しない）。
        ///
        /// 共有アセットは複製して変換を試み、変換された（shader が変化した）場合は複製を一時アセット化して返す。
        /// 変換されなかった場合は複製を破棄し、元マテリアルをそのまま返す。
        /// </summary>
        private static Material ConvertOne(BuildContext ctx, UzumoreConverterProxy proxy, Material src)
        {
            // 一時アセットの場合、複製せず in-place でConvert()を呼び出して変換する。(NDMFベストプラクティス準拠)
            if (ctx.IsTemporaryAsset(src))
            {
                proxy.Convert(src);
                return src;
            }

            // 共有アセットは複製し、複製したものをConvert()で変換する。
            var dst = new Material(src);
            proxy.Convert(dst);

            // 変換前後でシェーダーが同一(何も変換されなかった)場合、変換処理用の複製を破棄し、変換前のマテリアルをそのまま返す
            if (dst.shader == src.shader)
            {
                Object.DestroyImmediate(dst);
                return src;
            }

            // 変換後マテリアルの保存
            ctx.AssetSaver.SaveAsset(dst);

            // src → dst の置換関係を登録し、後段プラグインやエラーレポートが追跡できるようにする。(NDMFベストプラクティス準拠)
            ObjectRegistry.RegisterReplacedObject(src, dst);

            return dst;
        }
    }
}
