using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace AvatarManager.Core.Data
{
    public class ConstraintData : LinkData
    {
        public Vector3 PositionAtRest;
        public Vector3 RotationAtRest;
        public Vector3[] PositionOffsets;
        public Vector3[] RotationOffsets;

        public Dictionary<string, float> Sources = new Dictionary<string, float>();

        public override void Apply(GameObject root, Component comp)
        {
            var icon = (comp as IConstraint);

            List<ConstraintSource> sources = new List<ConstraintSource>();

            foreach (var go in Sources)
            {
                var gameObject = go.Key.GetGameObjectFromPath(root);
                sources.Add(new ConstraintSource()
                {
                    sourceTransform = gameObject.transform,
                    weight = go.Value
                });
            }

            switch (comp)
            {
                case ParentConstraint ps:
                    ps.translationAtRest = PositionAtRest;
                    ps.rotationAtRest = RotationAtRest;
                    ps.translationOffsets = PositionOffsets;
                    ps.rotationOffsets = RotationOffsets;
                    break;
            }

            icon.SetSources(sources);
        }
    }
}