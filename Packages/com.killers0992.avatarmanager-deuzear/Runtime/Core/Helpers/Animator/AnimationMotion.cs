using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AvatarManager.Core.Helpers
{
    public class AnimationMotion
    {
        public List<IFrameKey> Frames = new List<IFrameKey>();

        public string Name;
        public WrapMode WrapMode;

        public AnimationMotion(string name, WrapMode wrapMode = WrapMode.Loop)
        {
            Name = name;
            WrapMode = wrapMode;
        }

        public AnimationClip Build()
        {
            AnimationClip clip = new AnimationClip();
            clip.name = $"{Name}";
            clip.wrapMode = WrapMode;

            List<Keyframe> frames = new List<Keyframe>();

            foreach(var frame in Frames)
            {
                switch (frame.Value)
                {
                    case bool _bool:
                        frames.Add(new Keyframe(frame.Time, _bool ? 1f : 0f));
                        break;
                }
            }

            AnimationCurve animCurve = new AnimationCurve(frames.ToArray());

            foreach (var frame in Frames)
            {
                clip.SetCurve(frame.Path, frame.Type, frame.Property, animCurve);
            }

            AssetDatabase.CreateAsset(clip, $"Assets/Animation_{Guid.NewGuid().ToString("N")}.anim");

            return clip;
        }

        public interface IFrameKey
        {
            string Property { get; }
            Type Type { get; }
            object Value { get; }
            float Time { get; }
            string Path { get; }
        }

        public class BaseFrame : IFrameKey
        {
            public virtual string Property { get; set; }
            public virtual Type Type { get; set; }
            public virtual object Value { get; set; }
            public virtual float Time { get; set; }
            public virtual string Path { get; set; }
        }

        public class SkinnedMeshRenderer : BaseFrame
        {
            public override string Property { get; set; } = "m_Enabled";
            public override Type Type { get; set; } = typeof(UnityEngine.SkinnedMeshRenderer);
        }

        public class GameObject : BaseFrame
        {
            public override string Property { get; set; } = "m_IsActive";
            public override Type Type { get; set; } = typeof(UnityEngine.GameObject);
        }
    }   

    public static class AnimationMotionExtensions
    {
        /*public static void SetActiveFrame(this UnityEngine.GameObject go, AnimationMotion motion, bool state, float time = 0f)
        {
            string path = go.transform.GetGameObjectPath().Replace(VRChatAvatar.EditingAvatar.BaseDescriptor.transform.GetGameObjectPath(), "");

            motion.SetActiveGameObject(state, path, time);
        }

        public static void SetActiveGameObject(this AnimationMotion motion, bool state, string path, float time = 0f)
        {
            AnimationMotion.GameObject clone = new AnimationMotion.GameObject();
            clone.Value = state;
            clone.Time = time;
            clone.Path = path;

            motion.Frames.Add(clone);
        }

        public static void SetActiveFrame(this UnityEngine.SkinnedMeshRenderer renderer, AnimationMotion motion, bool state, float time = 0f)
        {
            string path = renderer.transform.GetGameObjectPath().Replace(VRChatAvatar.EditingAvatar.BaseDescriptor.transform.GetGameObjectPath(), "");

            motion.SetActiveSkinnedMeshRenderer(state, path, time);
        }

        public static void SetActiveSkinnedMeshRenderer(this AnimationMotion motion, bool state, string path, float time = 0f)
        {
            AnimationMotion.SkinnedMeshRenderer clone = new AnimationMotion.SkinnedMeshRenderer();
            clone.Value = state;
            clone.Time = time;
            clone.Path = path;

            motion.Frames.Add(clone);
        }*/
    }
}
