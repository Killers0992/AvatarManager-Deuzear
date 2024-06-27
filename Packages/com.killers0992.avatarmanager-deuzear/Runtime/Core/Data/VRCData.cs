using UnityEngine;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace AvatarManager.Core.Data
{
    public class VRCData : LinkData
    {
        public string RootBone;

        public override void Apply(GameObject root, Component comp)
        {
            GameObject rootBone = string.IsNullOrEmpty(RootBone) ? null : RootBone.GetGameObjectFromPath(root);

            if (rootBone == null) return;

            switch (comp)
            {
                case VRCContactReceiver receiver:
                    receiver.rootTransform = rootBone.transform;
                    break;
                case VRCContactSender sender:
                    sender.rootTransform = rootBone.transform;
                    break;
                case VRCPhysBone phys:
                    phys.rootTransform = rootBone.transform;
                    break;
            }
        }
    }
}