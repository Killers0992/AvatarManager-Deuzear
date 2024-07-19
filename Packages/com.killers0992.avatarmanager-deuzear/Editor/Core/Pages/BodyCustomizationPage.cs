using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AvatarManager.Core
{
    [CreateAssetMenu(fileName = "Page", menuName = "ScriptableObjects/BodyCustomization", order = 1)]
    public class BodyCustomizationPage : BasePage
    {
        public override void Initialize(BaseMenu menu, VisualElement element)
        {
            base.Initialize(menu, element);

            List = Element.Q<ListView>("List");
            Categories = Element.Q<ListView>("Categories");

            Categories.makeItem += () =>
            {
                var item = CategoryItem.Instantiate();

                item.userData = new CategoryInfo(this, item);

                return item;
            };

            Categories.bindItem += (element, index) =>
            {
                (element.userData as CategoryInfo).Bind(CategoriesList[index]);
            };

            Categories.unbindItem += (element, index) =>
            {
                (element.userData as CategoryInfo).Unbind();
            };

            CategoriesList = Menu.Storage.Customizations.Select(x => x.Category).Distinct().ToList();

            Categories.itemsSource = CategoriesList;

            List.makeItem += () =>
            {
                var item = Item.Instantiate();

                item.userData = new CustomInfo(item);

                return item;
            };

            List.bindItem += (element, index) =>
            {
                (element.userData as CustomInfo).Bind(ViewingCustoms[index].Name, ViewingCustoms[index].BlendshapeName, BaseAvatar.Current?.BodyRenderer);
            };

            List.unbindItem += (element, index) =>
            {
                (element.userData as CustomInfo).Unbind();
            };

            ChangedCategory += OnCategoryChanged;

            if (CategoriesList.Count == 0)
                return;

            ChangedCategory?.Invoke(CategoriesList[0]);
        }

        public ListView List { get; private set; }
        public ListView Categories { get; private set; }

        public VisualTreeAsset CategoryItem;
        public VisualTreeAsset Item;

        public class CategoryInfo
        {
            public VisualElement Element;
            public BodyCustomizationPage Page;

            public Button Button;

            public string Category;

            public CategoryInfo(BodyCustomizationPage page, VisualElement element)
            {
                Page = page;
                Element = element;

                Button = Element.Q<Button>("CategoryItem");

                Page.ChangedCategory += OnCategoryChange;
            }

            private void OnCategoryChange(string newCategory)
            {
                if (newCategory == Category)
                    Button.SetBorderColor(Color.white);
                else
                    Button.SetBorderColor(new Color(0.1490196f, 0.1490196f, 0.1490196f, 1f));
            }

            public void Bind(string category)
            {
                Category = category;

                Button.clickable = null;
                Button.clicked += () =>
                {
                    Page.ChangedCategory?.Invoke(Category);
                };

                Text = Category;
            }

            public void Unbind()
            {
                Page.ChangedCategory -= OnCategoryChange;
            }

            public string Text
            {
                get => Button.text;
                set
                {
                    Button.text = value;
                }
            }
        }

        public class CustomInfo
        {
            public bool RegisteredEvents = false;

            public VisualElement Element;

            public Button Text;
            public FloatField FloatField;
            public Slider Slider;

            public string BlendShapeName;
            public SkinnedMeshRenderer Renderer;

            public CustomInfo(VisualElement element)
            {
                Element = element;

                Text = Element.Q<Button>("Label");
                FloatField = Element.Q<FloatField>("FloatField");
                FloatField.RegisterValueChangedCallback(OnFloatChanged);

                Slider = Element.Q<Slider>("SliderField");
                Slider.RegisterValueChangedCallback(OnSliderChanged);
                Slider.lowValue = 0f;
                Slider.highValue = 100f;
            }

            public void UpdateValues(SkinnedMeshRenderer renderer)
            {
                float value = 0f;
                if (renderer != null)
                {
                    Renderer = renderer;
                    value = renderer.GetBlendshapeValue(BlendShapeName);
                }

                FloatField.value = value;
                Slider.value = value;
            }

            public void Bind(string name, string blendShapeName, SkinnedMeshRenderer renderer)
            {
                BlendShapeName = blendShapeName;

                Text.text = name;

                Text.clickable = null;
                Text.clicked += () =>
                {
                    GUIUtility.systemCopyBuffer = blendShapeName;
                };

                if (!RegisteredEvents)
                {
                    BaseAvatar.OnAvatarChange += OnAvatarChanged;
                    RegisteredEvents = true;
                }

                UpdateValues(renderer);
            }

            private void OnAvatarChanged()
            {
                UpdateValues(BaseAvatar.Current?.BodyRenderer);
            }

            public void Unbind()
            {
                if (RegisteredEvents)
                {
                    BaseAvatar.OnAvatarChange -= OnAvatarChanged;
                }
            }

            void OnFloatChanged(ChangeEvent<float> evt)
            {
                float value = Math.Min(100f, evt.newValue);
                Slider.value = value;

                if (Renderer != null)
                {
                    Renderer.SetBlendshapeValue(BlendShapeName, value);
                    BaseAvatar.Current?.UpdateAllBlendshapes(Renderer);
                }
            }

            void OnSliderChanged(ChangeEvent<float> evt)
            {
                FloatField.value = evt.newValue;

                if (Renderer != null)
                {
                    Renderer.SetBlendshapeValue(BlendShapeName, evt.newValue);
                    BaseAvatar.Current?.UpdateAllBlendshapes(Renderer);
                }
            }
        }

        public Action<string> ChangedCategory;

        public List<string> CategoriesList { get; set; } = new List<string>();

        public BaseCustomization[] ViewingCustoms { get; set; } = new BaseCustomization[0];

        void OnCategoryChanged(string category)
        {
            ViewingCustoms = Menu.Storage.Customizations.Where(x => x.Category == category && BaseAvatar.Current?.GetBlendshapeValue(x.BlendshapeName) != null).ToArray();
            List.itemsSource = ViewingCustoms;

            List.Rebuild();
        }
    }
}