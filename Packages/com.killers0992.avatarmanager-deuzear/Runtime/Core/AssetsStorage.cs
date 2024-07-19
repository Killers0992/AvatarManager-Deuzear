using UnityEngine;

namespace AvatarManager.Core
{
    [CreateAssetMenu(fileName = "Storage", menuName = "ScriptableObjects/Storage", order = 1)]
    public class AssetsStorage : ScriptableObject
    {
        public static AssetsStorage Instance;

        public AccessoryData[] Accessories;

        public BaseCustomization[] Customizations;
    }
}