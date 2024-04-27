using AvatarManager.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AvatarManager.Deuzear
{
    [CreateAssetMenu(fileName = "Page", menuName = "ScriptableObjects/Accessories", order = 1)]
    public class AccessoriesPage : BasePage
    {
        public enum AccessoryItemType
        {
            MainCategory,
            SubCategory,
            Item,
        }

        public override void Initialize(BaseMenu menu, VisualElement element)
        {
            base.Initialize(menu, element);

            List = Element.Q<ListView>("List");

            List.makeItem = () =>
            {
                var entry = new VisualElement();
                var logic = new AccessoryHanddler(this);

                entry.userData = logic;

                return entry;
            };

            List.bindItem += (item, index) =>
            {
                (item.userData as AccessoryHanddler).Bind(index, item, Items[index]);
            };

            List.unbindItem += (item, index) =>
            {
                (item.userData as AccessoryHanddler).Unbind();
            };

            List.itemsSource = Items;
        }

        public ListView List { get; private set; }

        public VisualTreeAsset AvailableAsset;
        public VisualTreeAsset CategoryAsset;
        public VisualTreeAsset AccessoryAsset;

        public class AccessoryHanddler
        {
            
            public bool RegisteredEvents = false;
            
            public AccessoryItem Item;

            public Button InstallButton;
            public Button SettingsButton;

            public AccessoriesPage Page;

            public bool IsInstalled => BaseAvatar.Current != null && Item?.Base.Mesh != null;

            public AccessoryHanddler(AccessoriesPage page)
            {
                Page = page;
            }

            public void AvatarChanged()
            {
                UpdateInstallButton();
            }

            public void UpdateInstallButton(int? identifier = null)
            {
                if (!IsInstalled)
                {
                    InstallButton.text = "Website";
                    InstallButton.SetBorderColor(Color.cyan);
                    SettingsButton.SetEnabled(false);
                    return;
                }

                if (BaseAvatar.Current.IsAccessoryInstalled(identifier.HasValue ? identifier.Value : Item.Base.Identifier))
                {
                    SettingsButton.SetEnabled(true);
                    InstallButton.text = "Remove";
                    InstallButton.SetBorderColor(new Color (1f, 0f, 0f, 0.4f));
                }
                else
                {
                    SettingsButton.SetEnabled(false);
                    InstallButton.text = "Install";
                    InstallButton.SetBorderColor(new Color(0.2321882f, 1f, 0f, 0.2f));
                }
            }

            private void AccessoryRemove(int identifier)
            {
                if (Item.Base.Identifier == identifier)
                {
                    SettingsButton.SetEnabled(false);
                    InstallButton.text = "Install";
                    InstallButton.SetBorderColor(new Color(0.2321882f, 1f, 0f, 0.2f));
                }
            }

            public void Unbind()
            {
                if (RegisteredEvents)
                {
                    BaseAvatar.OnAccessoryRemove -= AccessoryRemove;
                    BaseAvatar.OnAvatarChange -= AvatarChanged;
                }

                RegisteredEvents = false;
            }

            public void Bind(int index, VisualElement element, AccessoryItem item)
            {
                switch (item.Type)
                {
                    case AccessoryCategory.Base:
                    case AccessoryCategory.Community:
                    case AccessoryCategory.Dlcs:
                        {
                            var avaAsset = Page.AvailableAsset.Instantiate();

                            avaAsset.style.backgroundColor = new StyleColor()
                            {
                                value = item.Color,
                            };

                            Button btn = avaAsset.Q<Button>();

                            btn.clicked += () =>
                            {
                                item.IsShown = !item.IsShown;

                                if (item.IsShown)
                                {
                                    List<string> categories = new List<string>();

                                    // Setup categories
                                    foreach (var accessory in Page.Menu.Accessories)
                                    {
                                        if (accessory.Type == item.Type)
                                        {
                                            if (categories.Contains(accessory.Category)) continue;

                                            categories.Add(accessory.Category);

                                            int num = Page.Menu.Accessories.Where(acc => acc.Type == item.Type && acc.Category == accessory.Category).Count();

                                            Page.Items.Insert(index + 1, new AccessoryItem()
                                            {
                                                Type = AccessoryCategory.Category,
                                                RootType = item.Type,
                                                Name = $"{accessory.Category} ( {num} )",
                                                Category = accessory.Category,
                                            });
                                        }
                                    }
                                    Page.List.Rebuild();
                                }
                                else
                                {
                                    Page.Items.RemoveAll(item2 => item2.RootType == item.Type);
                                    Page.List.Rebuild();
                                }
                            };

                            Label name = avaAsset.Q<Label>("Name");
                            name.text = $"{item.Name} ( {Page.Menu.Accessories.Where(x => x.Type == item.Type).Count()} )";

                            name.style.color = new StyleColor()
                            {
                                value = item.TextColor,
                            };

                            element.Clear();
                            element.Add(avaAsset);
                        }
                        break;
                    case AccessoryCategory.Category:
                        {
                            var cattegory = Page.CategoryAsset.Instantiate();

                            var button = cattegory.Q<Button>("Button");

                            button.clicked += () =>
                            {
                                item.IsShown = !item.IsShown;

                                if (item.IsShown)
                                {
                                    // Setup items
                                    foreach (var accessory in Page.Menu.Accessories)
                                    {
                                        if (accessory.Type == item.RootType && accessory.Category == item.Category)
                                        {
                                            Page.Items.Insert(index + 1, new AccessoryItem()
                                            {
                                                Type = AccessoryCategory.Item,
                                                RootType = item.RootType,
                                                ParentType = item.Type,
                                                Name = $"{accessory.Name}",
                                                Category = accessory.Category,
                                                Base = accessory,
                                            });
                                        }
                                    }
                                    Page.List.Rebuild();
                                }
                                else
                                {
                                    Page.Items.RemoveAll(item2 => item2.Type == AccessoryCategory.Item && item2.ParentType == item.Type && item2.RootType == item.RootType && item2.Category == item.Category);
                                    Page.List.Rebuild();
                                }
                            };

                            var label = button.Q<Label>("Name");
                            label.text = item.Name;

                            element.Clear();
                            element.Add(cattegory);
                        }

                        break;
                    case AccessoryCategory.Item:
                        {
                            var accessory = Page.AccessoryAsset.Instantiate();

                            var icon = accessory.Q<Button>("Icon");

                            icon.clickable = null;

                            if (item.Base.Icon != null)
                            {
                                icon.clicked += () =>
                                {
                                    PreviewImage.CreatePreview(item.Base.Icon);
                                };
                                icon.style.backgroundImage = new StyleBackground(item.Base.Icon);
                            }

                            var author = accessory.Q<Label>("Author");
                            author.text = item.Base.Author;

                            var label = accessory.Q<Label>("Name");
                            label.text = item.Name;

                            SettingsButton = accessory.Q<Button>("Settings");

                            var install = accessory.Q<Button>("Install");

                            var gumroad = accessory.Q<Button>("Gumroad");
                            gumroad.clickable = null;
                            gumroad.clicked += () =>
                            {
                                Application.OpenURL(Item.Base.WebsiteUrl);
                            };

                            InstallButton = install;
                            Item = item;

                            if (!RegisteredEvents)
                            {
                                BaseAvatar.OnAvatarChange += AvatarChanged;
                                BaseAvatar.OnAccessoryRemove += AccessoryRemove;
                                RegisteredEvents = true;
                            }

                            UpdateInstallButton();

                            install.clickable = null;

                            if (!IsInstalled)
                            {
                                install.clicked += () =>
                                {
                                    Application.OpenURL(Item.Base.WebsiteUrl);
                                };
                            }
                            else
                            {
                                install.clicked += () =>
                                {
                                    BaseAvatar.Current.TryAddOrRemoveAccessory(item.Base, out BaseAccessory _);
                                    UpdateInstallButton();
                                };
                            }

                            element.Clear();
                            element.Add(accessory);
                        }
                        break;
                }
            }
        }

        public class AccessoryItem
        {
            public AccessoryCategory RootType;

            public AccessoryCategory Type;

            public AccessoryCategory ParentType;

            public Color Color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
            public Color TextColor = new Color(1f, 1f, 1f, 0.8f);

            public bool IsShown;

            public string Name;

            public string Category;

            public BaseAccessory Base;
        }

        public List<AccessoryItem> Items = new List<AccessoryItem>()
        {
            new AccessoryItem() { Name = "Base", Type = AccessoryCategory.Base },
            new AccessoryItem() { Name = "Community", Type = AccessoryCategory.Community },
            new AccessoryItem() { Name = "DLCS", Type = AccessoryCategory.Dlcs },
        };

        public class AccessoryInfo
        {
            public bool IsAvailable;
            public string Name;
            public string Category;
        }

        public override void OnShow()
        {
        }
    }
}