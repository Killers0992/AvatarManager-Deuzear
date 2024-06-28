using UnityEditor;
using UnityEngine;

namespace AvatarManager.Core
{
    public class PreviewImage : EditorWindow
    {
        public static void CreatePreview(Texture2D texture)
        {
            var window = GetWindow<PreviewImage>(true, "Preview", true);
            window.minSize = new Vector2(256, 256);
            window.maxSize = new Vector2(256, 256);

            window.Texture = texture;
            window.ShowUtility();
        }

        public Texture2D Texture;

        public void OnGUI()
        {
            GUI.Box(new Rect(0f, 0f, position.width, position.height), Texture);
        }
    }
}