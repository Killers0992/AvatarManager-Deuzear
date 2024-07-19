using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AvatarManager.Core
{
    [Serializable]
    public struct AccessoryInfo
    {
        public int Identifier;
        public GameObject Object;

        public AccessoryData Data
        {
            get
            {
                int id = Identifier;

                return AssetsStorage.Instance.Accessories.FirstOrDefault(x => x.Identifier == id);
            }
        }

        public void Delete(BaseAvatar avatar)
        {
            List<string> blendsToRevert = Data.DefaultBlendshapes != null ? Data.DefaultBlendshapes.Select(x => x.BlendshapeName).ToList() : new List<string>();

            foreach (var accessory in avatar.Accesories)
            {
                if (accessory.Identifier == Identifier) continue;

                if (accessory.Data.DefaultBlendshapes == null) continue;

                foreach (var blend in accessory.Data.DefaultBlendshapes)
                {
                    bool skipRemove = false;
                    if (blendsToRevert.Contains(blend.BlendshapeName))
                    {
                        foreach (var setting in accessory.Data.Blendshapes)
                        {
                            if (blend.TargetLocation == LocationOnBody.Unknown) continue;

                            var val = avatar.GetBlendshapeValue(setting.BlendShapeName);

                            if (setting.ZeroMeansActivation ? val == 0 : val != 0)
                            {
                                skipRemove = true;
                            }
                        }
                    }

                    foreach (var blendShape in Data.Blendshapes)
                    {
                        if (blendShape.BlendShapeName == blend.TargetBlendShape)
                            skipRemove = true;
                    }

                    if (skipRemove) continue;

                    blendsToRevert.Remove(blend.BlendshapeName);
                }
            }

            if (blendsToRevert.Count != 0)
            {
                foreach (var blend in Data.DefaultBlendshapes.Where(x => blendsToRevert.Contains(x.BlendshapeName)))
                {
                    avatar.SetBlendshapeValue(blend.BlendshapeName, blend.UsePrevValueThanDefault ? blend.PrevBlendshapeValue : blend.DefaultValue);
                }
            }

            avatar.Accesories.Remove(this);
            GameObject.DestroyImmediate(Object);
        }
    }
}