using UnityEngine;

namespace io.github.pioka.uzumorizer.Editor
{
    /// <summary>
    /// 「このマテリアルスロットをうずもれシェーダーへ変換すべきか」を判定する戦略。
    /// 初版は <see cref="ConvertAllStrategy"/>（常に true）のみ。
    /// 将来、対象/除外指定を入れる際はこのインターフェースの別実装に差し替える。
    /// </summary>
    internal interface IMaterialConversionStrategy
    {
        bool ShouldConvert(Renderer renderer, int slotIndex, Material material);
    }

    /// <summary>
    /// v0.1.0 用。lilToon マテリアルであれば一律変換する。
    /// （lilToon 系かどうかの一次判定は呼び出し側のパスで行う）
    /// </summary>
    internal sealed class ConvertAllStrategy : IMaterialConversionStrategy
    {
        public bool ShouldConvert(Renderer renderer, int slotIndex, Material material) => true;
    }
}
