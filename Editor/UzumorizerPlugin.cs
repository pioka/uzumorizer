using io.github.pioka.uzumorizer.Editor;
using nadena.dev.ndmf;

[assembly: ExportsPlugin(typeof(UzumorizerPlugin))]

namespace io.github.pioka.uzumorizer.Editor
{
    /// Uzumorizer の NDMF プラグイン。
    ///
    /// 単一パス構成（Optimizing フェーズ・AAO より前）:
    ///   (UzumoreConversionPass.Execute)
    ///   マーカー検出 → lilToon マテリアルの変換 → マーカー削除 を 1 パスで実行する。
    ///       - AAO より前にマーカーを削除し、警告を回避する。
    internal sealed class UzumorizerPlugin : Plugin<UzumorizerPlugin>
    {
        public override string QualifiedName => "io.github.pioka.uzumorizer";
        public override string DisplayName => "Uzumorizer";

        protected override void Configure()
        {
            InPhase(BuildPhase.Optimizing)
                .BeforePlugin("com.anatawa12.avatar-optimizer") // AAO: Avatar Optimizer
                .Run("Convert lilToon to UzumoreShader", UzumoreConversionPass.Execute);
        }
    }
}
