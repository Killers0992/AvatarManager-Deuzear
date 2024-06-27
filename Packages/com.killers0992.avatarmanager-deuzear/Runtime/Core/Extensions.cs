using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC.Utility;

namespace AvatarManager.Core
{
    public static class RuntimeExtensions
    {
        public static bool HasAny<TEnum>(this TEnum me, TEnum other)
where TEnum : Enum, IConvertible
        {
            return (me.ToInt32(null) & other.ToInt32(null)) != 0;
        }

        public static Dictionary<string, GameObject> GetChildren(this Transform transform, GameObject relativePath, Dictionary<string, GameObject> dict, bool simple = false)
        {
            if (simple && !dict.ContainsKey(transform.name))
            {
                dict.Add(transform.name, transform.gameObject);
            }
            else if (!simple)
                dict.Add(transform.GetGameObjectPath(), transform.gameObject);

            for (int x = 0; x < transform.childCount; x++)
                GetChildren(transform.GetChild(x), relativePath, dict, simple);

            return dict;
        }

        public static GameObject GetGameObjectFromPath(this string path, GameObject target, bool create = true)
        {
            var sp = path.Split('/').ToList();

            sp.RemoveAt(0);

            foreach (var s in sp)
            {
                var findGo = GetGameObjectByName(s, target.transform);

                if (findGo == null)
                {
                    if (!create)
                        return null;

                    var go = new GameObject(s);
                    go.transform.parent = target.transform;
                    target = go;
                }
                else
                    target = findGo;
            }
            return target;
        }

        public static GameObject GetGameObjectByName(this string name, Transform t)
        {
            for (int x = 0; x < t.childCount; x++)
            {
                var child = t.GetChild(x);

                if (child.name == name)
                    return child.gameObject;
            }
            return null;
        }

        public static string GetPath(this Transform t, string str = "")
        {
            if (t == null || t.IsRoot())
                return str;

            str = $"/{t.name}" + str;

            if (t.parent != null)
                str = GetPath(t.parent, str);

            return str;
        }

        public static string GetGameObjectPath(this Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;

                if (transform.parent != null)
                    path = transform.name + "/" + path;
            }
            return path;
        }

        public static float GetBlendshapeValue(this SkinnedMeshRenderer renderer, string name)
        {
            var blend = renderer.GetBlendshapeByName(name);

            if (blend == null) return 0f;

            return blend.Value;
        }

        public static void SetBlendshapeValue(this SkinnedMeshRenderer renderer, string name, float value)
        {
            var blend = renderer.GetBlendshapeByName(name);

            if (blend == null) return;

            blend.Value = value;
        }

        public static List<BlendshapeInfo> GetBlendshapes(this SkinnedMeshRenderer renderer)
        {
            List<BlendshapeInfo> blends = new List<BlendshapeInfo>();

            for(int x = 0; x < renderer.sharedMesh.blendShapeCount; x++)
            {
                blends.Add(new BlendshapeInfo(x, renderer.sharedMesh.GetBlendShapeName(x), renderer.GetBlendShapeWeight(x), renderer));
            }

            return blends;
        }

        public static BlendshapeInfo GetBlendshapeByName(this SkinnedMeshRenderer renderer, string name)
        {
            for (int x = 0; x < renderer.sharedMesh.blendShapeCount; x++)
            {
                string blendName = renderer.sharedMesh.GetBlendShapeName(x);

                if (string.IsNullOrWhiteSpace(blendName))
                    continue;

                if (blendName != name) continue;

                return new BlendshapeInfo(x, blendName, renderer.GetBlendShapeWeight(x), renderer);
            }

            return null;
        }

        public static List<BlendshapeInfo> GetBlendshapesByName(this List<SkinnedMeshRenderer> renderers, string name) 
        {
            List<BlendshapeInfo> blends = new List<BlendshapeInfo>();

            foreach(var renderer in renderers)
            {
                var blend = renderer.GetBlendshapeByName(name);

                if (blend == null) continue;

                blends.Add(blend);
            }

            return blends;
        }

        public static Dictionary<string, BlendshapeInfo> GetBlendshapesNamesDictionary(this SkinnedMeshRenderer renderer)
        {
            Dictionary<string, BlendshapeInfo> blends = new Dictionary<string, BlendshapeInfo>();

            if (renderer.sharedMesh == null)
                return blends;

            for (int x = 0; x < renderer.sharedMesh.blendShapeCount; x++)
            {
                string name = renderer.sharedMesh.GetBlendShapeName(x);

                blends.Add(name, new BlendshapeInfo(x, name, renderer.GetBlendShapeWeight(x), renderer));
            }

            return blends;
        }

        public static void SyncBlendshapes(this SkinnedMeshRenderer renderer, SkinnedMeshRenderer target)
        {
            var targetBlends = GetBlendshapesNamesDictionary(target);

            var currentBlends = renderer.GetBlendshapes();

            for(int x = 0; currentBlends.Count > x; x++)
            {
                if (!targetBlends.TryGetValue(currentBlends[x].Name, out BlendshapeInfo info)) continue;

                target.SetBlendShapeWeight(info.Index, currentBlends[x].Value);
            }
        }

        public static void MergeAccessory(this SkinnedMeshRenderer body, SkinnedMeshRenderer accessory, BoneTranslator[] boneTranslations)
        {
            Transform[] newBones = new Transform[accessory.bones.Length];

            Transform[] bodyBones = body.rootBone.GetComponentsInChildren<Transform>();

            for (int i = 0; i < accessory.bones.Length; i++)
            {
                string fallbackname = accessory.bones[i].name;
                string targetName = boneTranslations.Where(x => x.OrginalName == fallbackname).FirstOrDefault().NewName;

                Transform target = bodyBones.Where(x => x.name == targetName).FirstOrDefault();

                if (target != null)
                {
                    newBones[i] = target;
                }
                else
                {
                    newBones[i] = bodyBones.Where(x => x.name == fallbackname).FirstOrDefault();
                }
            }

            accessory.bones = newBones;
            accessory.rootBone = body.rootBone;
            accessory.probeAnchor = body.rootBone;
            accessory.localBounds = body.localBounds;
        }

        public static GameObject GetOrCreateGameObject(this GameObject parent, string name)
        {
            var go = parent.GetGameObject(name);
            if (go != null) return go;

            GameObject gmb = new GameObject(name);
            gmb.transform.parent = parent.transform;
            return gmb;
        }

        public static GameObject GetGameObject(this GameObject parent, string name)
        {
            if (parent == null) return null;

            for (int x = 0; x < parent.transform.childCount; x++)
            {
                var go = parent.transform.GetChild(x);
                if (go.name == name)
                    return go.gameObject;
            }

            return null;
        }
    }
}