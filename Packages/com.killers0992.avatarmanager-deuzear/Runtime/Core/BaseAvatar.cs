using UnityEngine;
using System.Collections.Generic;
using System;
using AvatarManager.Core.Helpers;
using VRC.SDK3.Avatars.Components;


#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
#endif

namespace AvatarManager.Core
{
    public class BaseAvatar : MonoBehaviour
#if VRC_SDK_VRCSDK3
        , IEditorOnly
#endif
    {
        static BaseAvatar _currentAvatar;
        public static BaseAvatar Current
        {
            get
            {
                return _currentAvatar;
            }
            set
            {
                _currentAvatar = value;
                OnAvatarChange?.Invoke();
            }
        }

        public static Action<int> OnAccessoryRemove;
        public static Action OnAvatarChange;

        private SkinnedMeshRenderer _bodyRenderer;

        private VRCAvatarDescriptor _descriptor;

        private VRChatAvatar _avatar;

        public List<BaseAccessory> Accesories = new List<BaseAccessory>();

        public VRCAvatarDescriptor Descriptor
        {
            get
            {
                if (_descriptor != null)
                    return _descriptor;

                _descriptor = this.GetComponentInChildren<VRCAvatarDescriptor>();
                return _descriptor;
            }
        }

        public VRChatAvatar Avatar
        {
            get
            {
                if (_avatar != null && _avatar.BaseDescriptor != null)
                    return _avatar;

                _avatar = VRChatAvatar.Init(Descriptor);
                return _avatar;
            }
        }

        public SkinnedMeshRenderer BodyRenderer
        {
            get
            {
                if (_bodyRenderer != null)
                    return _bodyRenderer;

                var go = this.gameObject.GetGameObject("Body");

                if (go == null) return null;

                _bodyRenderer = go.GetComponent<SkinnedMeshRenderer>();

                return _bodyRenderer;
            }
        }

        public float? GetBlendshapeValue(string blendshapeName)
        {
            var blends = GetBlendshapesWithName(blendshapeName);
            
            return blends.Count > 0 ? blends[0].Value : null;
        }

        public void SetBlendshapeValue(string blendshapeName, float blendshapeValue)
        {
            foreach(var blend in GetBlendshapesWithName(blendshapeName))
            {
                blend.Value = blendshapeValue;
            }
        }

        public List<BlendshapeInfo> GetBlendshapesWithName(string name)
        {
            List<BlendshapeInfo> blends = new List<BlendshapeInfo>();

            foreach (var blend in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var meshBlends = blend.GetBlendshapesNamesDictionary();

                foreach (var bl in meshBlends)
                {
                    if (bl.Key != name) continue;

                    blends.Add(bl.Value);
                }
            }

            return blends;
        }

        public Dictionary<string, List<BlendshapeInfo>> GetBlendshapes()
        {
            Dictionary<string, List<BlendshapeInfo>> blends = new Dictionary<string, List<BlendshapeInfo>>();

            foreach (var blend in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var meshBlends = blend.GetBlendshapesNamesDictionary();

                foreach(var bl in meshBlends)
                {
                    if (blends.ContainsKey(bl.Key))
                        blends[bl.Key].Add(bl.Value);
                    else
                        blends.Add(bl.Key, new List<BlendshapeInfo>() { bl.Value });
                }
            }

            return blends;
        }

        public void UpdateAllBlendshapes(SkinnedMeshRenderer from)
        {
            if (from == null) return;

            foreach(var blend in GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (blend == null || blend == from) continue;

                from.SyncBlendshapes(blend);
            }
        }

        public void RemoveAccessory(BaseAccessory acc)
        {
            if (TryGetAccessory(acc, out BaseAccessory found))
            {
                OnAccessoryRemove?.Invoke(acc.Identifier);

                found.Delete(this);
            }
        }

        public bool TryAddOrRemoveAccessory(BaseAccessory acc, out BaseAccessory accessory)
        {
            if (TryGetAccessory(acc, out BaseAccessory found))
            {
                found.Delete(this);
                accessory = default(BaseAccessory);
                return true;
            }
            else
            {
                GameObject go = acc.Instantiate(this);

                acc.Object = go;

                Accesories.Add(acc);
                accessory = acc;
                return false;
            }
        }

        public bool IsAccessoryInstalled(int identifier) => TryGetAccessoryByIdentifier(identifier, out var _);

        public bool IsAccessoryInstalled(BaseAccessory acc) => TryGetAccessory(acc, out BaseAccessory _);

        public bool TryGetAccessory(BaseAccessory acc, out BaseAccessory accessory)
        {
            accessory = default;

            if (acc.Mesh == null)
                return false;


            var renderer = acc.GetRenderer(this);

            if (renderer == null)
                return false;


            if (renderer.gameObject.tag == "EditorOnly")
            {
                DestroyImmediate(renderer.gameObject);

                return false;
            }

            if (!TryGetAccessoryByIdentifier(acc.Identifier, out accessory))
            {
                accessory = acc;
                acc.Object = renderer.gameObject;
                Accesories.Add(acc);
            }

            return true;
        }

        public bool TryGetAccessoryByIdentifier(int identifier, out BaseAccessory acc)
        {
            acc = default;

            foreach(var accessory in Accesories.ToArray())
            {
                if (accessory.Identifier == identifier)
                {
                    acc = accessory;
                    return true;
                }
            }

            acc = default;
            return false;
        }
    }
}