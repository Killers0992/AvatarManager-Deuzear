using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using AvatarManager.Core.Helpers;
#endif

#if VRC_SDK_VRCSDK3
using VRC.SDKBase;
using VRC.SDK3.Avatars.Components;
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

#if VRC_SDK_VRCSDK3
        private VRCAvatarDescriptor _descriptor;
#endif

#if UNITY_EDITOR && VRC_SDK_VRCSDK3
        private VRChatAvatar _avatar;
#endif

        public List<AccessoryInfo> Accesories = new List<AccessoryInfo>();

#if VRC_SDK_VRCSDK3
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
#endif

#if UNITY_EDITOR && VRC_SDK_VRCSDK3
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
#endif

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

        public void RemoveAccessory(AccessoryInfo acc)
        {
            OnAccessoryRemove?.Invoke(acc.Identifier);
            acc.Delete(this);
        }

        public bool TryAddOrRemoveAccessory(AccessoryData acc)
        {
            if (TryGetAccessory(acc, out AccessoryInfo found))
            {
                found.Delete(this);
                return true;
            }
            else
            {
                GameObject go = acc.Instantiate(this);

                AccessoryInfo acInfo = new AccessoryInfo()
                {
                    Identifier = acc.Identifier,
                    Object = go,
                };

                Accesories.Add(acInfo);
                return false;
            }
        }

        public bool IsAccessoryInstalled(int identifier) => TryGetAccessoryByIdentifier(identifier, out var _);

        public bool IsAccessoryInstalled(AccessoryData acc) => TryGetAccessory(acc, out AccessoryInfo _);

        public bool TryGetAccessory(AccessoryData acc, out AccessoryInfo accessory)
        {
            SkinnedMeshRenderer renderer;

            if (TryGetAccessoryByIdentifier(acc.Identifier, out accessory))
            {
                if (accessory.Object == null)
                {
                    renderer = acc.GetRenderer(this);

                    if (renderer != null)
                    {
                        accessory.Object = renderer.gameObject;
                        return true;
                    }

                    return false;
                }

                return true;
            }
            else
            {
                renderer = acc.GetRenderer(this);

                if (renderer == null)
                    return false;

                if (renderer.gameObject.tag == "EditorOnly")
                {
                    DestroyImmediate(renderer.gameObject);

                    return false;
                }

                accessory = new AccessoryInfo()
                {
                    Identifier = acc.Identifier,
                    Object = renderer.gameObject,
                };

                Accesories.Add(accessory);
            }

            return true;
        }

        public bool TryGetAccessoryByIdentifier(int identifier, out AccessoryInfo acc)
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