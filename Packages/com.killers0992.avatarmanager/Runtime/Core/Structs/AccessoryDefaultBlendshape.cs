using System;
using UnityEngine;

namespace AvatarManager.Core
{
    [Serializable]
    public struct AccessoryDefaultBlendshape
    {
        public string BlendshapeName;

        public float BlendshapeValue;

        public bool UsePrevValueThanDefault;

        public float DefaultValue;

#if UNITY_EDITOR
        [HideInInspector]
#endif
        public float PrevBlendshapeValue;

        public Condition Condition;

        public bool InvertValueIfConditionNotPassed;

        public int TargetAccessory;

        public string TargetBlendShape;

        public LocationOnBody TargetLocation;
        public float TargetBlendShapeValue;
    }
}