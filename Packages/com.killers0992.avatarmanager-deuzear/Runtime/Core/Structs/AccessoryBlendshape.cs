using System;

namespace AvatarManager.Core
{
    [Serializable]
    public struct AccessoryBlendshape
    {
        public string Name;

        public LocationOnBody Location;

        public string BlendShapeName;

        public float DefaultValue;

        public bool IsToggle;

        public bool ZeroMeansActivation;
    }
}