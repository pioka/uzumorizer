using UnityEditor;
using UnityEngine;

namespace io.github.pioka.uzumorizer.Editor
{
    /// Uzumorizer コンポーネント用の最小インスペクタ。
    /// 設定項目は無いため、何をするコンポーネントかの説明のみ表示する。
    [CustomEditor(typeof(Uzumorizer))]
    internal sealed class UzumorizerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "このアバターをビルドすると、内部の lilToon マテリアルを\n" +
                "うずもれシェーダー (Sigmal00/Uzumore) へ非破壊変換します。\n\n" +
                "・パラメータはうずもれシェーダーの既定値が適用されます。\n" +
                "・変換は AvatarOptimizer の前に実行されます。\n" +
                "・元のマテリアルやアバターは変更されません。",
                MessageType.Info);
        }
    }
}
