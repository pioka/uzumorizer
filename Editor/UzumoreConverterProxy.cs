#if UZUMORIZER_HAS_UZUMORE
using UnityEngine;

namespace io.github.pioka.uzumorizer.Editor
{
    /// <summary>
    /// うずもれシェーダー公式の変換ロジックをそのまま再利用するための薄いプロキシ。
    ///
    /// lilToon.UzumoreInspector は lilToon.lilToonInspector を継承しており、
    /// 実体の変換処理 ConvertMaterialToCustomShader(Material) は lilToonInspector 側の
    /// protected メソッド。UzumoreInspector が override する ReplaceToCustomShaders() が
    /// 差し替え先を Sigmal00/Uzumore/... に設定する。
    ///
    /// ここではそれを継承し、protected メソッドを public で呼べるように露出するだけ。
    /// うずもれ公式メニューと同じく new UzumoreConverterProxy() のみで動作する
    /// （MaterialEditor などのインスペクタ状態は不要）。
    /// </summary>
    internal sealed class UzumoreConverterProxy : lilToon.UzumoreInspector
    {
        /// <summary>
        /// 渡されたマテリアルの shader を、対応する Sigmal00/Uzumore/... へ in-place で差し替える。
        /// 標準 lilToon のいずれの派生にも一致しない場合は何もしない（shader は変化しない）。
        /// renderQueue は保持される。
        ///
        /// 注意: in-place 書き換えなので、共有アセットそのものではなく
        /// 複製したマテリアルを渡すこと（非破壊性は呼び出し側で担保する）。
        /// </summary>
        public void Convert(Material material)
        {
            ConvertMaterialToCustomShader(material);
        }
    }
}
#endif
