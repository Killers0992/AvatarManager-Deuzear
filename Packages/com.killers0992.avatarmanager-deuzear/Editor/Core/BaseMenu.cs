using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace AvatarManager.Core
{
    [CreateAssetMenu(fileName = "Menu", menuName = "ScriptableObjects/Menu", order = 1)]
    public class BaseMenu : ScriptableObject
    {
        public string Title;

        public Vector2 MinSize;
        public Vector2 MaxSize;

        public string FilterScenePrefix;

        public VisualTreeAsset MenuAsset;

        public VisualTreeAsset ButtonAsset;

        public List<Mesh> AvatarModelFbxs;

        public BasePage[] Pages;

        public BaseAccessory[] Accessories;

        public BaseCustomization[] Customizations;

        public BaseAvatar AvatarComponent;

        public virtual void OnInitialize(EditorWindow window)
        {
            InitializePages();
            InitializeFooter();
            InitializeDropdowns();

            window.rootVisualElement.Add(MenuAssetInstance);

            window.titleContent.text = $"Avatar Manager | {Title}";
            window.minSize = MinSize;
            window.maxSize = MaxSize;
        }

        void InitializePages()
        {
            VisualElement buttons = MenuAssetInstance.Q("Buttons");
            buttons.Clear();

            VisualElement pages = MenuAssetInstance.Q("Pages");

            foreach (var page in Pages)
            {
                if (string.IsNullOrEmpty(page.Name)) continue;

                VisualElement pageElement = pages.Q(page.Name);

                if (pageElement == null) continue;

                page.Initialize(this, pageElement);

                var buttonInstance = ButtonAsset.Instantiate();

                var button = buttonInstance.Q<Button>();

                button.text = page.DisplayName;

                button.clicked += () =>
                {
                    if (CurrentPage == page) return;

                    if (CurrentPage != null)
                    {
                        CurrentPage.Hide();
                    }

                    CurrentPage = page;

                    page.Show();
                };

                buttons.Add(button);
            }

            foreach(var page in Pages)
            {
                page.Hide();
            }

            CurrentPage = Pages[0];
            Pages[0].Show();
        }

        void InitializeFooter()
        {
            VisualElement footer = MenuAssetInstance.Q("Footer");
            VisualElement socials = footer.Q("Socials");

            socials.Q<Button>("Github").RedirectToUrl("https://github.com/killers0992");
            socials.Q<Button>("Discord").RedirectToUrl("https://discord.gg/czQCAsDMHa");
        }

        void InitializeDropdowns()
        {
            VisualElement sceneElement = MenuAssetInstance.Q("Scene");
            SceneDropdownField = sceneElement.Q<DropdownField>("Dropdown");

            RefreshScenes();

            SceneDropdownField.RegisterValueChangedCallback(callback =>
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;

                int index = SceneDropdownField.choices.IndexOf(callback.newValue);
                EditorSceneManager.OpenScene(scenesPaths[index]);
                RefreshEditing();
            });

            VisualElement editingElement = MenuAssetInstance.Q("Editing");
            EditingDropdownField = editingElement.Q<DropdownField>("Dropdown");

            RefreshEditing();

            EditingDropdownField.RegisterValueChangedCallback(callback =>
            {
                int index = EditingDropdownField.choices.IndexOf(callback.newValue);

                BaseAvatar.Current = avatars[index];

                Selection.activeTransform = avatars[index].transform;
                SceneView.FrameLastActiveSceneView();
            });
        }

        void RefreshScenes()
        {
            List<string> choices = new List<string>();
            List<string> paths = new List<string>();

            string currentScene = Path.GetFileNameWithoutExtension(EditorSceneManager.GetActiveScene().path);

            string value = null;

            foreach (var scene in Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories))
            {
                string name = Path.GetFileNameWithoutExtension(scene);

                if (!name.StartsWith(FilterScenePrefix)) continue;

                int num = choices.Count + 1;

                string finalName = $"{num} | " + name.Replace(FilterScenePrefix, string.Empty).Trim();

                choices.Add(finalName);
                paths.Add(scene);

                if (name == currentScene)
                    value = finalName;
            }

            SceneDropdownField.choices = choices;
            scenesPaths = paths.ToArray();

            if (choices.Count == 0)
                return;

            SceneDropdownField.value = value ?? choices[0];
        }

        void RefreshEditing()
        {
            List<string> choices = new List<string>();
            List<BaseAvatar> avis = new List<BaseAvatar>();

            foreach (var renderer in UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>())
            {
                if (!AvatarModelFbxs.Contains(renderer.sharedMesh)) continue;

                Transform root = renderer.transform.root;

                BaseAvatar avatar = root.gameObject.GetComponent<BaseAvatar>();

                if (avatar == null)
                {
                    avatar = (BaseAvatar)root.gameObject.AddComponent(AvatarComponent.GetType());

                    foreach(var accessory in Accessories)
                    {
                        if (avatar.TryGetAccessory(accessory, out var acc))
                        {
                            if (acc.Object == null)
                                avatar.RemoveAccessory(acc);
                        }
                    }

                }

                avis.Add(avatar);
                choices.Add(root.gameObject.name);
            }

            EditingDropdownField.choices = choices;
            avatars = avis.ToArray();

            if (choices.Count == 0) return;

            EditingDropdownField.value = choices[0];
            BaseAvatar.Current = avis[0];
        }

        public virtual void OnCreateGUI(EditorWindow window)
        {
            OnInitialize(window);
        }

        public virtual void OnUpdate(EditorWindow window)
        {

        }

        private VisualElement _menuInstance;

        public static BaseMenu Instance;

        string[] scenesPaths = Array.Empty<string>();
        BaseAvatar[] avatars = Array.Empty<BaseAvatar>();

        public BasePage CurrentPage { get; private set; }

        public VisualElement MenuAssetInstance
        {
            get
            {
                if (_menuInstance == null)
                {
                    _menuInstance = MenuAsset.Instantiate();
                }

                return _menuInstance;
            }
        }

        public DropdownField SceneDropdownField { get; private set; }
        public DropdownField EditingDropdownField { get; private set; }

        public Button[] Buttons { get; private set; }
    }
}
