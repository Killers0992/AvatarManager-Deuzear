#if VRC_SDK_VRCSDK3
using System.Linq;
using UnityEditor.Animations;
using VRC;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace AvatarManager.Core.Helpers
{
    public class VRChatAvatar
    {
        public static VRChatAvatar EditingAvatar;

        private AvatarAnimationController _baseLayer;
        private AvatarAnimationController _additiveLayer;
        private AvatarAnimationController _gestureLayer;
        private AvatarAnimationController _actionLayer;
        private AvatarAnimationController _fxLayer;

        public VRCAvatarDescriptor BaseDescriptor { get; private set; }

        public AvatarAnimationController BaseLayer
        {
            get
            {
                if (BaseDescriptor.baseAnimationLayers[0].animatorController is AnimatorController anim)
                    _baseLayer = AvatarAnimationController.Init(this, anim);

                return _baseLayer;
            }
        }

        public AvatarAnimationController AdditiveLayer
        {
            get
            {
                if (BaseDescriptor.baseAnimationLayers[1].animatorController is AnimatorController anim)
                    _additiveLayer = AvatarAnimationController.Init(this, anim);

                return _additiveLayer;
            }
        }

        public AvatarAnimationController GestureLayer
        {
            get
            {
                if (BaseDescriptor.baseAnimationLayers[2].animatorController is AnimatorController anim)
                    _gestureLayer = AvatarAnimationController.Init(this, anim);

                return _gestureLayer;
            }
        }

        public AvatarAnimationController ActionLayer
        {
            get
            {
                if (BaseDescriptor.baseAnimationLayers[3].animatorController is AnimatorController anim)
                    _actionLayer = AvatarAnimationController.Init(this, anim);

                return _actionLayer;
            }
        }
        public AvatarAnimationController FxLayer
        {
            get
            {
                _fxLayer = InitializeAnimationLayer(4);

                if (BaseDescriptor.baseAnimationLayers[4].isEnabled)
                {

                }

                    if (BaseDescriptor.baseAnimationLayers[4].animatorController is AnimatorController anim)
                    _fxLayer = AvatarAnimationController.Init(this, anim);


                return _fxLayer;
            }
        }

        public VRCExpressionsMenu Menu => BaseDescriptor.expressionsMenu;

        public VRCExpressionParameters Parameters => BaseDescriptor.expressionParameters;

        public static VRChatAvatar Init(VRCAvatarDescriptor descriptor)
        {
            VRChatAvatar avatar = new VRChatAvatar();
            avatar.BaseDescriptor = descriptor;

            EditingAvatar = avatar;

            return avatar;
        }

        private AvatarAnimationController InitializeAnimationLayer(int index)
        {
            BaseDescriptor.baseAnimationLayers[index].isEnabled = true;
            BaseDescriptor.baseAnimationLayers[index].isDefault = false;
            BaseDescriptor.baseAnimationLayers[index].animatorController = CreateAnimatorController($"Controller {index}");

            if (BaseDescriptor.baseAnimationLayers[index].animatorController is AnimatorController anim)
                return AvatarAnimationController.Init(this, anim);

            return null;
        }

        public AnimatorController CreateAnimatorController(string name)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath($"Assets/{name}.controller");

            return controller;
        }

        public (int, VRCExpressionParameters.Parameter) GetParameter(string name)
        {
            VRCExpressionParameters.Parameter parameter = null;
            int index = -1;

            for (int x = 0; x < Parameters.parameters.Length; x++)
            {
                if (Parameters.parameters[x].name != name) continue;

                parameter = Parameters.parameters[x];
                index = x;
                break;
            }

            return (index, parameter);
        }

        public void AddFloatParameter(string name, float defaultValue = 0f, bool saved = true, bool networkSynced = true) => AddParameter(name, (float)defaultValue, saved, networkSynced);
        public void AddIntegerParameter(string name, int defaultValue = 0, bool saved = true, bool networkSynced = true) => AddParameter(name, (int)defaultValue, saved, networkSynced);
        public void AddBoolParameter(string name, bool defaultValue = false, bool saved = true, bool networkSynced = true) => AddParameter(name, (bool)defaultValue, saved, networkSynced);

        public void AddParameter(string name, object defaultValue, bool saved = true, bool networkSynced = true)
        {
            float defaultValueFloat = 0f;
            VRCExpressionParameters.ValueType type = VRCExpressionParameters.ValueType.Bool;

            switch (defaultValue)
            {
                case bool _bool:
                    defaultValueFloat = _bool ? 1f : 0f;
                    break;
                case int _int:
                    defaultValueFloat = _int;
                    type = VRCExpressionParameters.ValueType.Int;
                    break;
                case float _float:
                    defaultValueFloat = _float;
                    type = VRCExpressionParameters.ValueType.Float;
                    break;

            }

            var parameterFound = GetParameter(name);

            if (parameterFound.Item1 != -1)
            {
                Parameters.parameters[parameterFound.Item1] = new VRCExpressionParameters.Parameter()
                {
                    name = name,
                    saved = saved,
                    defaultValue = defaultValueFloat,
                    networkSynced = networkSynced,
                    valueType = type,
                };
            }
            else
            {
                Parameters.parameters = Parameters.parameters.Append(new VRCExpressionParameters.Parameter()
                {
                    name = name,
                    saved = saved,
                    defaultValue = defaultValueFloat,
                    networkSynced = networkSynced,
                    valueType = type,
                }).ToArray();
            }

            Parameters.MarkDirty();
        }
    }
}
#endif