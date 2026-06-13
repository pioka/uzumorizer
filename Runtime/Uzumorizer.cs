using UnityEngine;
using VRC.SDKBase;

namespace io.github.pioka.uzumorizer
{
    /// <summary>
    /// アバターのルートに付与するマーカーコンポーネント。
    ///
    /// このコンポーネントが付いたアバターをビルドすると、Uzumorizer の NDMF パスが
    /// アバター内の lilToon マテリアルを うずもれシェーダー (Sigmal00/Uzumore) へ
    /// 非破壊変換します。
    ///
    /// 初版 (v0.1.0) は「存在するだけでアバター全体の lilToon マテリアルを一律変換」する
    /// 仕様で、設定項目はありません。うずもれシェーダー側のパラメータは
    /// シェーダーの既定値がそのまま適用されます。
    ///
    /// <see cref="IEditorOnly"/> を実装しているため、ビルド成果物には残りません
    /// (VRChat SDK によって除去されます)。
    /// </summary>
    [AddComponentMenu("Uzumorizer/Uzumorizer")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/pioka/uzumorizer")]
    public sealed class Uzumorizer : MonoBehaviour, IEditorOnly
    {
        // v0.1.0: 設定項目なし（存在＝全 lilToon マテリアルを変換）。
        //
        // 将来の拡張ポイント:
        //   対象/除外の指定 (Renderer 単位・Material 単位) を追加する場合は、
        //   ここにフィールドを足し、Editor 側の変換パス (UzumoreConversionPass) に
        //   判定を渡す。NDMF パスの骨格は変更不要。
    }
}
