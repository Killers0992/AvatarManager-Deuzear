using AvatarManager.Core.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

using UnityEngine;
using UnityEngine.Animations;

#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.ScriptableObjects;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
#endif

namespace AvatarManager.Core
{
    [Serializable]
    public struct AccessoryData
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

        [Header("Base")]
        public string Name;

        public AccessoryCategory Type;

        public LocationOnBody Location;

        public string Category;

        public string Author;
        public string WebsiteUrl;

        public Texture2D Icon;

        public bool RequiresVrcSdk;

        [Header("Kit")]
        public bool IsKit;

        public GameObject KitPrefab;
        public string[] WhitelistedObjects;

#if UNITY_EDITOR
        public AnimatorController Animator;
#endif

        public string MenuPath;
#if UNITY_EDITOR && VRC_SDK_VRCSDK3
        public VRCExpressionsMenu Menu;
#endif

        [Header("Other")]

        public Mesh Mesh;

        public Material[] Materials;

        public string AttachToBone;

        public BoneTranslator[] BoneTranslations;

        public AccessoryDefaultBlendshape[] DefaultBlendshapes;

        public AccessoryBlendshape[] Blendshapes;

        public Vector3 IntialPosition;
        public Vector3 IntialRotation;

#if UNITY_EDITOR
        public string MeshPath => AssetDatabase.GetAssetPath(Mesh);
#endif

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

        public GameObject Instantiate(BaseAvatar avatar, bool focus = false)
        {
#if !UNITY_EDITOR
            return null;
#else
            if (RequiresVrcSdk)
            {
#if !VRC_SDK_VRCSDK3
                return null;
#endif
            }

            if (IsKit)
            {
                Vector3 avatarPos = avatar.transform.position;

                avatar.transform.position = Vector3.zero;

                if (KitPrefab != null)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(KitPrefab));

                    Dictionary<string, GameObject> map = new Dictionary<string, GameObject>();

                    map = prefab.transform.GetChildren(prefab, map);

                    Dictionary<string, GameObject> currentDeuzearNewMap = new Dictionary<string, GameObject>();
                    Dictionary<string, GameObject> targetDeuzearNewMap = new Dictionary<string, GameObject>();

                    foreach (var go2 in map.Values)
                    {
                        string str = go2.transform.GetPath();

                        Debug.Log(str);

                        if (!WhitelistedObjects.Any(x => x.StartsWith(str)))
                            continue;

                        if (!currentDeuzearNewMap.ContainsKey(str))
                            currentDeuzearNewMap.Add(str, go2);
                    }

                    map = avatar.transform.GetChildren(avatar.transform.gameObject, new Dictionary<string, GameObject>());

                    foreach (var go2 in map.Values)
                    {
                        string str = go2.transform.GetPath();

                        if (!targetDeuzearNewMap.ContainsKey(str))
                            targetDeuzearNewMap.Add(str, go2);
                    }

                    foreach (var currentDeuzear in currentDeuzearNewMap)
                    {
                        if (targetDeuzearNewMap.ContainsKey(currentDeuzear.Key)) continue;

                        var components = currentDeuzear.Value.GetComponents<Component>().Where(x => x.GetType() != typeof(Transform)).ToArray();

                        CreateGameobjectsFromPath(avatar.gameObject, currentDeuzear.Value.activeSelf, currentDeuzear.Value.transform, currentDeuzear.Key, components);
                    }

                    foreach (var link in avatar.gameObject.GetComponentsInChildren<LinkToGameobject>(true))
                    {
                        link.OnLink();
                        UnityEngine.Object.DestroyImmediate(link);
                    }
                }

#if VRC_SDK_VRCSDK3
                if (Animator?.layers != null)
                {
                    // Clone layers
                    foreach (var layer in Animator.layers)
                    {
                        avatar.Avatar.FxLayer.CreateLayer(layer.name, true, layer.stateMachine, layer.avatarMask, layer.blendingMode, layer.syncedLayerIndex, layer.iKPass, layer.defaultWeight, layer.syncedLayerAffectsTiming);
                    }

                    // Clone parameters
                    foreach (var parameter in Animator.parameters)
                    {
                        var param = avatar.Avatar.FxLayer.GetParameter(parameter.name);
                        if (param == null)
                            avatar.Avatar.FxLayer.CreateParameter(parameter, true, true);
                        else
                        {
                            object val = null;
                            switch (parameter.type)
                            {
                                case AnimatorControllerParameterType.Float:
                                    val =  parameter.defaultFloat;
                                    break;
                                case AnimatorControllerParameterType.Bool:
                                    val =  parameter.defaultBool;
                                    break;
                                case AnimatorControllerParameterType.Int:
                                    val =  parameter.defaultInt;
                                    break;
                            }
                            avatar.Avatar.AddParameter(param.name, val);
                        }
                    }

                    EditorUtility.SetDirty(avatar.Avatar.FxLayer.BaseAnimator);
                }

                if (Menu != null)
                {
                    avatar.Avatar.Menu.controls.Add(new VRCExpressionsMenu.Control()
                    {
                        name = MenuPath,
                        type = VRCExpressionsMenu.Control.ControlType.SubMenu,
                        subMenu = Menu,
                    });

                    EditorUtility.SetDirty(avatar.Avatar.Menu);
                }
#endif

                AssetDatabase.SaveAssets();
                avatar.transform.position = avatarPos;
                return null;
            }

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
                        if (accessory.Data.Location.HasAny(tempLocation))
                        {
                            if (accessory.Data.Blendshapes.Any(x => x.Location.HasAny(tempLocation)))
                            {
                                foreach (var blendShape in accessory.Data.Blendshapes)
                                {
                                    if (!blendShape.Location.HasAny(tempLocation)) continue;

                                    avatar.SetBlendshapeValue(blendShape.BlendShapeName, blendShape.ZeroMeansActivation ? 100f : 0f);
                                    Logger.Info($"Set blendshape <color=green>{blendShape.Name}</color> to <color=green>{(blendShape.ZeroMeansActivation ? 100 : 0)}</color> for <color=green>{accessory.Data.Name}</color> because is coldiing with <color=green>{Name}</color>!");
                                }
                            }
                            else
                            {
                                bool fixedProblem = false;
                                for(int x = 0; x < Blendshapes.Length; x++)
                                {
                                    // If current accessory dont have any location from x then contnue.
                                    if (!accessory.Data.Location.HasAny(Blendshapes[x].Location)) continue;

                                    avatar.SetBlendshapeValue(Blendshapes[x].BlendShapeName, Blendshapes[x].ZeroMeansActivation ? 100f : 0f);
                                    fixedProblem = true;
                                }

                                if (fixedProblem) continue;

                                avatar.RemoveAccessory(accessory);
                                Logger.Info($"Removed accessory <color=green>{accessory.Data.Name}</color> because is colliding with <color=green>{Name}</color>!");
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
#endif
            }

#if UNITY_EDITOR
            public void CreateGameobjectsFromPath(GameObject target, bool isActive, Transform transform, string path, Component[] components)
        {
            var sp = path.Split('/').ToList();

            sp.RemoveAt(0);

            GameObject root = target;

            GameObject current = target;

            int index = 0;

            foreach (var s in sp)
            {
                var findGo = s.GetGameObjectByName(current.transform);

                if (findGo == null)
                {
                    GameObject go = null;
                    if (index == sp.Count -1)
                    {
                        go = new GameObject(s, components.Select(x => x.GetType()).Where(x => x != typeof(Transform)).ToArray());
                        EditorUtility.SetDirty(go);
                        go.SetActive(isActive);
                        go.transform.parent = current.transform;
                        go.transform.position = transform.position;
                        go.transform.rotation = transform.rotation;
                        go.transform.localScale = transform.localScale;

                        var comps = go.GetComponents<Component>().Where(x => x.GetType() != typeof(Transform)).ToArray();

                        for (int x = 0; x < components.Length; x++)
                        {
                            var comp = components[x];

                            string componentPath = comp.transform.GetPath();

                            EditorUtility.CopySerialized(components[x], comps[x]);

                            switch (comp)
                            {
                                case SkinnedMeshRenderer renderer:
                                    {
                                        string rootBonePath = renderer.rootBone.GetPath();
                                        GameObject targetGo = rootBonePath.GetGameObjectFromPath(root, false);

                                        var link = go.AddComponent<LinkToGameobject>();
                                        link.Component = comps[x];

                                        RendererData data = new RendererData();

                                        data.RootBone = renderer.rootBone == null ? null : renderer.rootBone.GetPath();
                                        data.ProbeAnchor = renderer.probeAnchor == null ? null : renderer.probeAnchor.GetPath();

                                        data.Bones = renderer.bones.Select(y => y.GetPath()).ToList();

                                        link.Data = data;
                                    }
                                    break;
#if VRC_SDK_VRCSDK3
                                case VRCContactReceiver receiver:
                                    {
                                        var link = go.AddComponent<LinkToGameobject>();
                                        link.Component = comps[x];

                                        VRCData data = new VRCData();

                                        data.RootBone = receiver.rootTransform == null ? null : receiver.rootTransform.GetPath();

                                        link.Data = data;
                                    }
                                    break;
                                case VRCContactSender sender:
                                    {
                                        var link = go.AddComponent<LinkToGameobject>();
                                        link.Component = comps[x];
                                        VRCData data = new VRCData();

                                        data.RootBone = sender.rootTransform == null ? null : sender.rootTransform.GetPath();

                                        link.Data = data;
                                    }
                                    break;
                                case VRCPhysBone phys:
                                    {
                                        EditorUtility.CopySerialized(components[x], comps[x]);
                                        var link = go.AddComponent<LinkToGameobject>();
                                        link.Component = comps[x];
                                        VRCData data = new VRCData();

                                        data.RootBone = phys.rootTransform == null ? null : phys.rootTransform.GetPath();

                                        link.Data = data;
                                    }
                                    break;
#endif
                                case IConstraint parent:
                                    {
                                        if (parent.GetSource(0).sourceTransform.name == "WorldRoot")
                                            continue;

                                        var link = go.AddComponent<LinkToGameobject>();
                                        link.Component = comps[x];

                                        List<ConstraintSource> sources = new List<ConstraintSource>();

                                        parent.GetSources(sources);

                                        ConstraintData data = new ConstraintData();

                                        switch (parent)
                                        {
                                            case ParentConstraint pc:
                                                data.PositionAtRest = pc.translationAtRest;
                                                data.RotationAtRest = pc.rotationAtRest;
                                                data.PositionOffsets = pc.translationOffsets;
                                                data.RotationOffsets = pc.rotationOffsets;
                                                break;
                                        }

                                        sources.ForEach(y =>
                                        {
                                            data.Sources.Add(y.sourceTransform.GetPath(), y.weight);
                                        });
                                        link.Data = data;
                                    }
                                    break;
                            }
                        }
                    }
                    else
                    {
                        go = new GameObject(s);
                        EditorUtility.SetDirty(go);
                        go.transform.parent = current.transform;
                    }

                    current = go;
                }
                else
                {
                    current = findGo;
                }
                index++;
            }

        }
#endif

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
                        if (accessory.Data.Location.HasAny(blendshape.TargetLocation))
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