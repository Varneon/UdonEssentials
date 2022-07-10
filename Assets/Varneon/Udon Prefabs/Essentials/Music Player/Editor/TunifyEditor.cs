using UnityEditor;
using UdonSharpEditor;
using UnityEngine;
using Varneon.UdonPrefabs.Essentials.MusicPlayerEditor;

namespace Varneon.UdonPrefabs.Essentials
{
    [CustomEditor(typeof(Tunify))]
    public class TunifyEditor : Editor
    {
        private Tunify tunify;

        private bool invalidPrefabOrBehaviour;

        private RectTransform canvasTransform;

        private bool showCanvasSettings;

        private void OnEnable()
        {
            tunify = (Tunify)target;

            invalidPrefabOrBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(tunify) == null || tunify.transform.childCount < 1;

            canvasTransform = tunify.GetComponentInChildren<Canvas>(true).GetComponent<RectTransform>();
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

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUI.color = Color.white;

                GUILayout.Label("Varneon's UdonEssentials - Tunify", EditorStyles.whiteLargeLabel);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Find more Udon prefabs at:", EditorStyles.whiteLabel, GUILayout.Width(160));

                    if (GUILayout.Button("https://github.com/Varneon", EditorStyles.whiteLabel, GUILayout.Width(165)))
                    {
                        Application.OpenURL("https://github.com/Varneon");
                    }
                }
            }

            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label($"[Library] Songs: {tunify.Urls.Length} | Playlists: {tunify.PlaylistIndices.Length}");

                if (GUILayout.Button("Open Music Player Manager", GUILayout.ExpandWidth(false)))
                {
                    MusicPlayerManager.Init();
                }
            }

            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUI.IndentLevelScope(1))
                {
                    if (showCanvasSettings = EditorGUILayout.Foldout(showCanvasSettings, "Window Options", true))
                    {
                        using (var scope = new EditorGUI.ChangeCheckScope())
                        {
                            Vector2 size = EditorGUILayout.Vector2Field("Resolution", canvasTransform.sizeDelta);

                            if (scope.changed)
                            {
                                Undo.RecordObject(canvasTransform, "Change Canvas Resolution");

                                canvasTransform.sizeDelta = size;
                            }
                        }

                        using (var scope = new EditorGUI.ChangeCheckScope())
                        {
                            float scale = EditorGUILayout.FloatField("Scale", canvasTransform.localScale.x);

                            if (scope.changed)
                            {
                                Undo.RecordObject(canvasTransform, "Change Canvas Scale");

                                canvasTransform.localScale = new Vector3(scale, scale, scale);
                            }
                        }
                    }
                }
            }
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
