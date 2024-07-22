using UnityEditor;
using UnityEngine;

namespace AvatarManager.Core
{
    public class DeuzearMenu : EditorWindow
    {
#if AVATAR_MANAGER
        private BaseMenu _menuInstance;

        public BaseMenu MenuInstance
        {
            get
            {
                if (_menuInstance == null)
                {
                    rootVisualElement.Clear();

                    _menuInstance = AssetDatabase.LoadAssetAtPath<BaseMenu>("Packages/com.killers0992.avatarmanager-deuzear/DeuzearMenu.asset");

                    _menuInstance.OnInitialize(this);
                }

                return _menuInstance;
            }
        }
#endif

        [MenuItem("AvatarManager/Open Deuzear Menu")]
        public static void OpenWindow()
        {
            var window = GetWindow<DeuzearMenu>();

#if !AVATAR_MANAGER
            window.minSize = new Vector2(250f, 60f);
            window.maxSize = window.minSize;
#endif
        }

        public void OnGUI()
        {
#if AVATAR_MANAGER
            if (MenuInstance.MenuAssetInstance == null)
                MenuInstance.OnInitialize(this);

            MenuInstance.OnUpdate(this);
#else
            GUILayout.Label("Missing dependencies:");
            GUILayout.Label(" - AvatarManager");
            if (GUILayout.Button("Install"))
            {
                Install();
            }
#endif
        }

        void Install()
        {
            Application.OpenURL("https://avatarmanager.killers.dev/install");
        }

        public void CreateGUI()
        {
#if AVATAR_MANAGER
            MenuInstance.OnCreateGUI(this);
#endif
        }
    }
}