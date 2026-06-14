using UnityEngine;
using VRC.SDKBase;

namespace io.github.pioka.uzumorizer
{
    /// アバターのルートに付与するマーカーコンポーネント。
    /// 変換するかの判定はアバター内にこのコンポーネントが存在するかで判定するので、厳密にはルートに置かなくてもいい
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
