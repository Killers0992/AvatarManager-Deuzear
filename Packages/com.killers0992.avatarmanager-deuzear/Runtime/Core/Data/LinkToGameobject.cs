using UnityEngine;

namespace AvatarManager.Core.Data
{
    public class LinkToGameobject : MonoBehaviour
    {
        public Component Component;

        public LinkData Data;

        public void OnLink()
        {
            var root = this.gameObject.transform.root.gameObject;

            Data.Apply(root, Component);
        }
    }
}