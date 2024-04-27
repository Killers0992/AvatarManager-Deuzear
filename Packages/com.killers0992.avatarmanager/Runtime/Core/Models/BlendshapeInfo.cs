using UnityEngine;

namespace AvatarManager.Core
{
    public class BlendshapeInfo
    {
        private float _value;

        public BlendshapeInfo(int index, string name, float value, SkinnedMeshRenderer renderer)
        {
            Index = index;
            Name = name;
            Renderer = renderer;
            Value = value;
        }

        public int Index { get; }

        public string Name { get; }

        public float Value
        {
            get => _value;
            set
            {
                Renderer.SetBlendShapeWeight(Index, value);
                _value = value;
            }
        }

        public SkinnedMeshRenderer Renderer { get; }
    }
}