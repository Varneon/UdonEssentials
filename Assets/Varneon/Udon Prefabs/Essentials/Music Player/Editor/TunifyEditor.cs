using UnityEditor;
using UdonSharpEditor;
using UnityEngine;

namespace Varneon.UdonPrefabs.Essentials
{
    [CustomEditor(typeof(Tunify))]
    public class TunifyEditor : Editor
    {
        private Tunify tunify;

        private bool invalidPrefabOrBehaviour;

        private void OnEnable()
        {
            tunify = (Tunify)target;

            invalidPrefabOrBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(tunify) == null || tunify.transform.childCount < 1;
        }

        public override void OnInspectorGUI()
        {
            DrawBanner();

            if (invalidPrefabOrBehaviour)
            {
                EditorGUILayout.HelpBox("Use the Tunify prefab provided in Assets/Varneon/Udon Prefabs/Essentials/Music Player", MessageType.Error);

                return;
            }

            if (tunify.transform.localScale.normalized != Vector3.one.normalized || tunify.transform.GetChild(0).localScale.normalized != Vector3.one.normalized) { DrawScaleWarning(); }

            EditorGUILayout.Space();

            base.OnInspectorGUI();
        }

        private void DrawBanner()
        {
            GUI.color = new Color(0f, 0.5f, 1f);

            GUILayout.BeginVertical(EditorStyles.helpBox);

            GUI.color = Color.white;

            GUILayout.Label("Varneon's UdonEssentials - Tunify", EditorStyles.whiteLargeLabel);

            GUILayout.BeginHorizontal();

            GUILayout.Label("Find more Udon prefabs at:", EditorStyles.whiteLabel, GUILayout.Width(160));

            if (GUILayout.Button("https://github.com/Varneon", EditorStyles.whiteLabel, GUILayout.Width(165)))
            {
                Application.OpenURL("https://github.com/Varneon");
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUILayout.Label($"[Library] Songs: {tunify.Urls.Length} | Playlists: {tunify.PlaylistIndices.Length}");

            GUILayout.EndHorizontal();
        }

        private void DrawScaleWarning()
        {
            GUI.color = Color.yellow;

            GUILayout.BeginHorizontal(EditorStyles.helpBox);

            GUI.color = Color.white;

            GUILayout.Label("Scale of the root prefab or the Canvas is not even! If you want to resize the music player, please adjust the scale on each axis evenly or adjust the Width and Height on the Canvas RectTransform.", EditorStyles.wordWrappedLabel);

            GUILayout.EndHorizontal();
        }
    }
}
