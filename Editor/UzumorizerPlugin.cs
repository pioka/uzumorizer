using io.github.pioka.uzumorizer.Editor;
using nadena.dev.ndmf;

[assembly: ExportsPlugin(typeof(UzumorizerPlugin))]

namespace io.github.pioka.uzumorizer.Editor
{
    /// Uzumorizer の NDMF プラグイン。
    ///
    /// AAO 互換性のため 2 パス構成にしている:
    ///
    ///   (UzumoreConversionPass.CollectMarkers)
    ///   Transforming フェーズ（= Optimizing より必ず前）でマーカーを検出・記録し、
    ///   コンポーネントを削除する。
    ///
    ///   (UzumoreConversionPass.Convert)
    ///   Optimizing フェーズの、TexTransTool と AvatarOptimizer の「後」で変換を実行する。
    ///       - TTT / AAO は lilToon のシェーダー名を見てアトラス化・テクスチャ最適化を行うため、
    ///         先に Sigmal00/Uzumore へ変換すると未知シェーダー扱いになり最適化が劣化/スキップされる。
    ///       - また、AAOによる最適化が終わったあとの方がマテリアル変換の呼び出しを少なくできる。
    ///       - よって両者の処理が終わってから、純粋なシェーダー差し替えとして最後に変換する。
    internal sealed class UzumorizerPlugin : Plugin<UzumorizerPlugin>
    {
        public override string QualifiedName => "io.github.pioka.uzumorizer";
        public override string DisplayName => "Uzumorizer";

        protected override void Configure()
        {
            // (1) AAO より前: マーカーを記録して削除する。
            InPhase(BuildPhase.Transforming)
                .Run("Collect Uzumorizer marker", UzumoreConversionPass.CollectMarkers);

            // (2) TTT/AAO より後: 記録済みフラグを見て変換する。
            InPhase(BuildPhase.Optimizing)
                .AfterPlugin("net.rs64.tex-trans-tool")        // TexTransTool (AtlasTexture)
                .AfterPlugin("com.anatawa12.avatar-optimizer") // AAO: Avatar Optimizer
                .Run("Convert lilToon to UzumoreShader", UzumoreConversionPass.Convert);
        }
    }
}
