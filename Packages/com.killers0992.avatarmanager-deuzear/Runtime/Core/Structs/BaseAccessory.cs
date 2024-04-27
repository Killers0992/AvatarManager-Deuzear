using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.AI;
using UnityEngine.Rendering;

namespace AvatarManager.Core
{
    [Serializable]
    public struct BaseAccessory
    {
        public int Identifier
        {
            get
            {
                if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Author))
                    return 0;

                return Name.GetHashCode() + Author.GetHashCode();
            }
        }


        public string Name;

        public AccessoryCategory Type;

        public LocationOnBody Location;

        public string Category;

        public string Author;
        public string WebsiteUrl;

        public Texture2D Icon;

        public Mesh Mesh;
        public Material[] Materials;

        public string AttachToBone;

        public BoneTranslator[] BoneTranslations;

        public AccessoryDefaultBlendshape[] DefaultBlendshapes;

        public AccessoryBlendshape[] Blendshapes;

        public Vector3 IntialPosition;
        public Vector3 IntialRotation;

        public string MeshPath => AssetDatabase.GetAssetPath(Mesh);

        public GameObject Object;

        public bool IsAvailable() => Mesh != null && BaseAvatar.Current != null;

        public SkinnedMeshRenderer GetRenderer(BaseAvatar avatar)
        {
            foreach(var renderer in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh == Mesh)
                    return renderer;
            }

            return null;
        }

        public void Delete(BaseAvatar avatar)
        {
            List<string> blendsToRevert = DefaultBlendshapes != null ? DefaultBlendshapes.Select(x => x.BlendshapeName).ToList() : new List<string>();

            foreach(var accessory in avatar.Accesories)
            {
                if (accessory.Identifier == Identifier) continue;

                if (accessory.DefaultBlendshapes == null) continue;

                foreach(var blend in accessory.DefaultBlendshapes)
                {
                    bool skipRemove = false;
                    if (blendsToRevert.Contains(blend.BlendshapeName))
                    {
                        foreach(var setting in accessory.Blendshapes)
                        {
                            if (blend.TargetLocation == LocationOnBody.Unknown) continue;

                            var val = avatar.GetBlendshapeValue(setting.BlendShapeName);

                            if (setting.ZeroMeansActivation ? val == 0 : val != 0)
                            {
                                skipRemove = true;
                            }
                        }
                    }

                    foreach(var blendShape in Blendshapes)
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
                foreach (var blend in DefaultBlendshapes.Where(x => blendsToRevert.Contains(x.BlendshapeName)))
                {
                    avatar.SetBlendshapeValue(blend.BlendshapeName, blend.UsePrevValueThanDefault ? blend.PrevBlendshapeValue : blend.DefaultValue);
                }
            }

            avatar.Accesories.Remove(this);
            GameObject.DestroyImmediate(Object);
        }

        public GameObject Instantiate(BaseAvatar avatar, bool focus = false)
        {
            GameObject targetAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MeshPath);

            if (targetAsset == null)
            {
                Logger.Error($"Fbx <color=green>{MeshPath}</color> is invalid for accessory <color=green>{GetType().FullName}</color>!");
                return null;
            }

            GameObject targetGameObject = targetAsset;

            if (targetAsset.transform.childCount != 0)
            {
                targetGameObject = targetAsset.GetGameObject(Mesh.name);

                if (targetGameObject == null)
                {
                    Logger.Error($"Mesh <color=green>{Mesh}</color> is invalid for accessory <color=green>{GetType().FullName}</color>!");
                    return null;
                }
            }

            bool setCustomIntial = false;
            Transform targetTransform = null;
            Vector3 pos = Vector3.zero;
            Vector3 rot = Vector3.zero;

            string bone = AttachToBone;

            if (!string.IsNullOrEmpty(bone))
            {
                targetTransform = avatar.BodyRenderer.rootBone.GetComponentsInChildren<Transform>().Where(x => x.name == bone).FirstOrDefault();

                if (targetTransform != null)
                {
                    pos = IntialPosition;
                    rot = IntialRotation;
                    setCustomIntial = true;
                }
                else
                    Logger.Error($"Can't find bone <color=green>{bone}</color>");
            }

            if (targetTransform == null)
            {
                targetTransform = avatar.gameObject.GetOrCreateGameObject("Accessories").transform;
                pos = avatar.transform.position;
                rot = avatar.transform.eulerAngles;
            }

            var go = setCustomIntial ? UnityEngine.Object.Instantiate(targetGameObject, targetTransform, false) : UnityEngine.Object.Instantiate(targetGameObject, pos, Quaternion.Euler(rot), targetTransform);

            if (setCustomIntial)
            {
                go.transform.localPosition = pos;
                go.transform.localRotation = Quaternion.Euler(rot);
            }

            if (focus)
            {
                Selection.activeTransform = go.transform;
                SceneView.FrameLastActiveSceneView();
            }

            go.name = Path.GetFileNameWithoutExtension(Mesh.name);

            SkinnedMeshRenderer renderer = go.GetComponent<SkinnedMeshRenderer>();

            if (renderer != null)
            {
                if (renderer.bones.Length != 0)
                    avatar.BodyRenderer.MergeAccessory(renderer, BoneTranslations);

                var avatarBlendshapes = avatar.GetBlendshapes();

                if (Blendshapes != null)
                {
                    var tempLocation = Location;

                    foreach (var accessory in avatar.Accesories.ToArray())
                    {
                        if (accessory.Location.HasAny(tempLocation))
                        {
                            if (accessory.Blendshapes.Any(x => x.Location.HasAny(tempLocation)))
                            {
                                foreach (var blendShape in accessory.Blendshapes)
                                {
                                    if (!blendShape.Location.HasAny(tempLocation)) continue;

                                    avatar.SetBlendshapeValue(blendShape.BlendShapeName, blendShape.ZeroMeansActivation ? 100f : 0f);
                                    Logger.Info($"Set blendshape <color=green>{blendShape.Name}</color> to <color=green>{(blendShape.ZeroMeansActivation ? 100 : 0)}</color> for <color=green>{accessory.Name}</color> because is coldiing with <color=green>{Name}</color>!");
                                }
                            }
                            else
                            {
                                bool fixedProblem = false;
                                for(int x = 0; x < Blendshapes.Length; x++)
                                {
                                    // If current accessory dont have any location from x then contnue.
                                    if (!accessory.Location.HasAny(Blendshapes[x].Location)) continue;

                                    avatar.SetBlendshapeValue(Blendshapes[x].BlendShapeName, Blendshapes[x].ZeroMeansActivation ? 100f : 0f);
                                    fixedProblem = true;
                                }

                                if (fixedProblem) continue;

                                avatar.RemoveAccessory(accessory);
                                Logger.Info($"Removed accessory <color=green>{accessory.Name}</color> because is colliding with <color=green>{Name}</color>!");
                            }
                        }
                    }
                }

                if (DefaultBlendshapes != null)
                {
                    AccessoryDefaultBlendshape[] tempDefaultBlendshapes = DefaultBlendshapes;

                    for (int x = 0; x < tempDefaultBlendshapes.Length;x++)
                    {
                        RunBlendshapeCondition(avatar, x, tempDefaultBlendshapes[x]);
                    }
                }

                avatar.BodyRenderer.SyncBlendshapes(renderer);

                if (Materials.Length == 1)
                {
                    renderer.sharedMaterial = Materials[0];
                }
            }

            return go;
        }

        public void SetDefaultIfPossible(BaseAvatar avatar, int index, string blendshapeName)
        {
            float? value = avatar.GetBlendshapeValue(blendshapeName);

            if (value.HasValue)
                DefaultBlendshapes[index].PrevBlendshapeValue = value.Value;
        }

        public void RunBlendshapeCondition(BaseAvatar avatar, int index, AccessoryDefaultBlendshape blendshape)
        {
            switch (blendshape.Condition)
            {
                case Condition.None:
                    SetDefaultIfPossible(avatar, index, blendshape.BlendshapeName);
                    avatar.SetBlendshapeValue(blendshape.BlendshapeName, blendshape.BlendshapeValue);
                    break;
                case Condition.IfAccessoryIsDisabled when !avatar.IsAccessoryInstalled(blendshape.TargetAccessory):
                    SetDefaultIfPossible(avatar, index, blendshape.BlendshapeName);
                    avatar.SetBlendshapeValue(blendshape.BlendshapeName, blendshape.BlendshapeValue);
                    break;
                case Condition.IfBlendShapeIsAt:
                    var val = avatar.GetBlendshapeValue(blendshape.TargetBlendShape);

                    if (!val.HasValue) break;

                    if (val.Value != blendshape.TargetBlendShapeValue) break;

                    SetDefaultIfPossible(avatar, index, blendshape.BlendshapeName);
                    avatar.SetBlendshapeValue(blendshape.BlendshapeName, blendshape.BlendshapeValue);
                    break;
                case Condition.IfAccessoryAtLocationIsDisabled:
                case Condition.IfAccessoryAtLocationIsEnabled:

                    bool foundAny = false;

                    foreach (var accessory in avatar.Accesories)
                    {
                        if (accessory.Location.HasAny(blendshape.TargetLocation))
                            foundAny = true;
                    }

                    if (foundAny && blendshape.Condition == Condition.IfAccessoryAtLocationIsEnabled)
                    {
                        SetDefaultIfPossible(avatar, index, blendshape.BlendshapeName);
                        avatar.SetBlendshapeValue(blendshape.BlendshapeName, blendshape.BlendshapeValue);
                    }
                    else if (!foundAny)
                    {
                        if (blendshape.Condition == Condition.IfAccessoryAtLocationIsDisabled)
                        {
                            SetDefaultIfPossible(avatar, index, blendshape.BlendshapeName);
                            avatar.SetBlendshapeValue(blendshape.BlendshapeName, blendshape.BlendshapeValue);
                        }
                        else if (blendshape.Condition == Condition.IfAccessoryAtLocationIsEnabled && blendshape.InvertValueIfConditionNotPassed)
                        {
                            SetDefaultIfPossible(avatar, index, blendshape.BlendshapeName);
                            avatar.SetBlendshapeValue(blendshape.BlendshapeName, blendshape.BlendshapeValue == 100f ? 0f : 100f);
                        }
                    }
                    break;
            }
        }
    }
}