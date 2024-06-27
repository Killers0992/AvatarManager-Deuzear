using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AvatarManager.Core.Data
{
    public class RendererData : LinkData
    {
        public string RootBone;
        public string ProbeAnchor;
        public List<string> Bones;

        public override void Apply(GameObject root, Component comp)
        {
            var renderer = (comp as SkinnedMeshRenderer);

            var bodyRenderer = root.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(x => x.name == "Body");

            GameObject rootBone = string.IsNullOrEmpty(RootBone) ? null : RootBone.GetGameObjectFromPath(root);
            GameObject probeAnchor = string.IsNullOrEmpty(ProbeAnchor) ? null : ProbeAnchor.GetGameObjectFromPath(root);

            List<Transform> bones = new List<Transform>();

            foreach (var bone in Bones)
            {
                var boneGo = bone.GetGameObjectFromPath(root, false);

                if (boneGo == null)
                {
                    Debug.LogError("Can't find bone " + bone);
                    continue;
                }

                bones.Add(boneGo.transform);
            }

            renderer.bones = bones.ToArray();
            renderer.rootBone = rootBone?.transform;
            renderer.probeAnchor = probeAnchor?.transform;
        }
    }
}