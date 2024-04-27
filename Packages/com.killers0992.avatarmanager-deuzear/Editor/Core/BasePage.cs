using UnityEngine;
using UnityEngine.UIElements;

namespace AvatarManager.Core
{
    [CreateAssetMenu(fileName = "Page", menuName = "ScriptableObjects/Page", order = 1)]
    public class BasePage : ScriptableObject
    {
        public string Name;
        public string DisplayName;
        public BaseMenu Menu { get; private set; }
        public VisualElement Element { get; private set; }

        public virtual void Initialize(BaseMenu menu, VisualElement element)
        {
            Menu = menu;
            Element = element;
        }

        public void Show()
        {
            OnShow();
            Element?.Show();
        }

        public virtual void OnShow()
        {

        }

        public void Hide()
        {
            Element?.Hide();
            OnHide();
        }

        public virtual void OnHide()
        {

        }
    }
}