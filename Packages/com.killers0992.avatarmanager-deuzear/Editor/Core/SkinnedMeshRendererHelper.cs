using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AvatarManager.Core
{
    [CustomEditor(typeof(SkinnedMeshRenderer), true)]
    [CanEditMultipleObjects]
    public class SkinnedMeshRendererHelper : Editor
    {
        Vector2 _scroll = Vector2.zero;
        Editor defaultEditor;
        SkinnedMeshRenderer renderer;

        private void OnEnable()
        {
            defaultEditor = Editor.CreateEditor(targets, Type.GetType("UnityEditor.SkinnedMeshRendererEditor, UnityEditor"));
            renderer = (target as SkinnedMeshRenderer);
        }

        private void OnDisable()
        {
            MethodInfo disableMethod = defaultEditor.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (disableMethod != null)
                disableMethod.Invoke(defaultEditor, null);

            DestroyImmediate(defaultEditor);
        }

        public override void OnInspectorGUI()
        {
            GUILayout.Label($"Total bones {renderer.bones.Length} | Valid {renderer.bones.Where(x => x != null).Count()}/{renderer.bones.Length}");
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.MaxHeight(150));

            for(int x = 0; x < renderer.bones.Length; x++)
            {
                if (renderer.bones[x] == null)
                    GUI.color = Color.red;

                GUILayout.BeginHorizontal("box");
                GUILayout.Label($"#{x}");
                EditorGUILayout.ObjectField(renderer.bones[x], typeof(Transform), true);
                GUILayout.EndHorizontal();
                GUI.color = Color.white;
            }

            GUILayout.EndScrollView();

            defaultEditor.OnInspectorGUI();
        }
    }
}