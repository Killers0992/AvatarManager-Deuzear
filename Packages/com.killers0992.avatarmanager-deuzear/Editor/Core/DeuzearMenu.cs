using UnityEditor;

namespace AvatarManager.Core
{
    public class DeuzearMenu : EditorWindow
    {
        private BaseMenu _menuInstance;

        public  BaseMenu MenuInstance
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

        [MenuItem("AvatarManager/Open Deuzear Menu")]
        public static void OpenWindow()
        {
            GetWindow<DeuzearMenu>();
        }

        public void OnGUI()
        {
            if (MenuInstance.MenuAssetInstance == null)
                MenuInstance.OnInitialize(this);

            MenuInstance.OnUpdate(this);
        }

        public void CreateGUI()
        {
            MenuInstance.OnCreateGUI(this);
        }
    }
}