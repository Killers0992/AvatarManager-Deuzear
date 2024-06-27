#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
#endif
using AnimatorControllerLayer = UnityEditor.Animations.AnimatorControllerLayer;
using AnimatorControllerParameter = UnityEngine.AnimatorControllerParameter;
using AnimatorLayerBlendingMode = UnityEditor.Animations.AnimatorLayerBlendingMode;
using BlendTree = UnityEditor.Animations.BlendTree;

namespace AvatarManager.Core.Helpers
{
    public class AvatarAnimationController
    {
        private Dictionary<AnimatorControllerLayer, AnimatorLayer> _baseAnimatorToCustom = new Dictionary<AnimatorControllerLayer, AnimatorLayer>();

        public AnimatorController BaseAnimator { get; private set; }

#if VRC_SDK_VRCSDK3
        public VRChatAvatar Avatar { get; private set; }

        public static AvatarAnimationController Init(VRChatAvatar avatar, AnimatorController controller)
        {
            AvatarAnimationController animationController = new AvatarAnimationController();

            animationController.BaseAnimator = controller;
            animationController.Avatar = avatar;

            return animationController;
        }
#endif

        public AnimatorControllerParameter GetParameter(string name) => BaseAnimator.parameters.FirstOrDefault(x => x.name == name);
        
        public void CreateParameter(AnimatorControllerParameter parameter, bool createVrcParameter = false, bool overrideExisting = true)
        {
            object value = null;
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Bool:
                    value = parameter.defaultBool;
                    break;
                case AnimatorControllerParameterType.Int:
                    value = parameter.defaultInt;
                    break;
                case AnimatorControllerParameterType.Float:
                    value = parameter.defaultFloat;
                    break;
            }

            CreateParameter(parameter.name, value, parameter.type, createVrcParameter, overrideExisting);
        }

        public void CreateParameter(string name, object defaultValue, AnimatorControllerParameterType type = AnimatorControllerParameterType.Trigger, bool createVrcParameter = false, bool overrideExisting = true)
        {
            AnimatorControllerParameter parameter = new AnimatorControllerParameter()
            {
                name = name,
                type = type,
            };

            switch (defaultValue)
            {
                case bool _bool:
                    parameter.defaultBool = _bool;
#if VRC_SDK_VRCSDK3
                    if (createVrcParameter) Avatar.AddBoolParameter(name, _bool);
#endif
                    if (type == AnimatorControllerParameterType.Trigger) parameter.type = AnimatorControllerParameterType.Bool;
                    break;
                case float _float:
                    parameter.defaultFloat = _float;
#if VRC_SDK_VRCSDK3
                    if (createVrcParameter) Avatar.AddFloatParameter(name, _float);
#endif
                    if (type == AnimatorControllerParameterType.Trigger) parameter.type = AnimatorControllerParameterType.Float;
                    break;
                case int _int:
                    parameter.defaultInt = _int;
#if VRC_SDK_VRCSDK3
                    if (createVrcParameter) Avatar.AddIntegerParameter(name, _int);
#endif
                    if (type == AnimatorControllerParameterType.Trigger) parameter.type = AnimatorControllerParameterType.Int;
                    break;
            }

                    var param = GetParameter(name);

            if (overrideExisting && param != null)
                BaseAnimator.RemoveParameter(param);

            BaseAnimator.AddParameter(parameter);
        }

        public void CreateBoolParameter(string name, bool defaultValue = true, bool overrideExisting = true) => CreateParameter(name, defaultValue, AnimatorControllerParameterType.Bool, overrideExisting);
        public void CreateFloatParameter(string name, float defaultValue = 0f, bool overrideExisting = true) => CreateParameter(name, defaultValue, AnimatorControllerParameterType.Float, overrideExisting);
        public void CreateIntParameter(string name, int defaultValue = 0, bool overrideExisting = true) => CreateParameter(name, defaultValue, AnimatorControllerParameterType.Int, overrideExisting);

        public AnimatorLayer GetLayer(string name)
        {
            AnimatorControllerLayer layer = null;
            int layerIndex = 0;
            for (int x = 0; x < BaseAnimator.layers.Length; x++)
            {
                if (BaseAnimator.layers[x].name == name)
                {
                    layer = BaseAnimator.layers[x];
                    layerIndex = x;
                    break;
                }
            }

            if (layer == null) return null;

            if (!_baseAnimatorToCustom.TryGetValue(layer, out AnimatorLayer animatorLayer)) 
            {
                animatorLayer = AnimatorLayer.Init(this, layer);
                animatorLayer.Index = layerIndex;
                _baseAnimatorToCustom.Add(layer, animatorLayer);
            }

            return animatorLayer;
        }

        public void RemoveLayer(string name)
        {
            var layer = GetLayer(name);

            if (layer == null) return;

            BaseAnimator.RemoveLayer(layer.Index);
            _baseAnimatorToCustom.Clear();
        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L117
        public AnimatorStateMachine CloneStateMachine(AnimatorStateMachine stateMachine)
        {
            AnimatorStateMachine machine = new AnimatorStateMachine()
            {
                name = stateMachine.name,
                hideFlags = stateMachine.hideFlags,
                anyStatePosition = stateMachine.anyStatePosition,
                entryPosition = stateMachine.entryPosition,
                exitPosition = stateMachine.exitPosition,
                parentStateMachinePosition = stateMachine.parentStateMachinePosition,
                stateMachines = stateMachine.stateMachines.Select(CloneChildAnimatorStateMachine).ToArray(),
                states = stateMachine.states.Select(CloneChildAnimatorState).ToArray(),
            };

            AssetDatabase.AddObjectToAsset(machine, BaseAnimator);
            machine.defaultState = FindState(stateMachine.defaultState, stateMachine, machine);

            foreach (var oldb in stateMachine.behaviours)
            {
                var behaviour = machine.AddStateMachineBehaviour(oldb.GetType());
                CloneBehaviourParameters(oldb, behaviour);
            }

            return machine;
        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L352
        public AnimatorState FindState(AnimatorState original, AnimatorStateMachine old, AnimatorStateMachine n)
        {
            AnimatorState[] oldStates = GetStatesRecursive(old).ToArray();
            AnimatorState[] newStates = GetStatesRecursive(n).ToArray();
            for (int i = 0; i < oldStates.Length; i++)
                if (oldStates[i] == original)
                    return newStates[i];

            return null;
        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L380
        public List<AnimatorState> GetStatesRecursive(AnimatorStateMachine sm)
        {
            List<AnimatorState> childrenStates = sm.states.Select(x => x.state).ToList();
            foreach (var child in sm.stateMachines.Select(x => x.stateMachine))
                childrenStates.AddRange(GetStatesRecursive(child));

            return childrenStates;
        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L142
        public ChildAnimatorStateMachine CloneChildAnimatorStateMachine(ChildAnimatorStateMachine animatorState) => new ChildAnimatorStateMachine
        {
            position = animatorState.position,
            stateMachine = CloneStateMachine(animatorState.stateMachine)
        };

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L152
        public ChildAnimatorState CloneChildAnimatorState(ChildAnimatorState animatorState)
        {
            ChildAnimatorState childState = new ChildAnimatorState
            {
                position = animatorState.position,
                state = CloneAnimatorState(animatorState.state)
            };

            foreach (var oldb in animatorState.state.behaviours)
            {
                var behaviour = childState.state.AddStateMachineBehaviour(oldb.GetType());
                CloneBehaviourParameters(oldb, behaviour);
            }
            return childState;
        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L254
        public void CloneBehaviourParameters(StateMachineBehaviour old, StateMachineBehaviour n)
        {
            if (old.GetType() != n.GetType())
            {
                throw new ArgumentException("2 state machine behaviours that should be of the same type are not.");
            }
#if VRC_SDK3
            switch (n)
            {
                case VRCAnimatorLayerControl l:
                    {
                        var o = old as VRCAnimatorLayerControl;
                        l.ApplySettings = o.ApplySettings;
                        l.blendDuration = o.blendDuration;
                        l.debugString = o.debugString;
                        l.goalWeight = o.goalWeight;
                        l.layer = o.layer;
                        l.playable = o.playable;
                        break;
                    }
                case VRCAnimatorLocomotionControl l:
                    {
                        var o = old as VRCAnimatorLocomotionControl;
                        l.ApplySettings = o.ApplySettings;
                        l.debugString = o.debugString;
                        l.disableLocomotion = o.disableLocomotion;
                        break;
                    }
                case VRCAnimatorTemporaryPoseSpace l:
                    {
                        var o = old as VRCAnimatorTemporaryPoseSpace;
                        l.ApplySettings = o.ApplySettings;
                        l.debugString = o.debugString;
                        l.delayTime = o.delayTime;
                        l.enterPoseSpace = o.enterPoseSpace;
                        l.fixedDelay = o.fixedDelay;
                        break;
                    }
                case VRCAnimatorTrackingControl l:
                    {
                        var o = old as VRCAnimatorTrackingControl;
                        l.ApplySettings = o.ApplySettings;
                        l.debugString = o.debugString;
                        l.trackingEyes = o.trackingEyes;
                        l.trackingHead = o.trackingHead;
                        l.trackingHip = o.trackingHip;
                        l.trackingLeftFingers = o.trackingLeftFingers;
                        l.trackingLeftFoot = o.trackingLeftFoot;
                        l.trackingLeftHand = o.trackingLeftHand;
                        l.trackingMouth = o.trackingMouth;
                        l.trackingRightFingers = o.trackingRightFingers;
                        l.trackingRightFoot = o.trackingRightFoot;
                        l.trackingRightHand = o.trackingRightHand;
                        break;
                    }
                case VRCAvatarParameterDriver l:
                    {
                        var d = old as VRCAvatarParameterDriver;
                        l.debugString = d.debugString;
                        l.localOnly = d.localOnly;
                        l.isLocalPlayer = d.isLocalPlayer;
                        l.initialized = d.initialized;
                        l.parameters = d.parameters.ConvertAll(p =>
                        {
                            string name = p.name;
                            return new VRC_AvatarParameterDriver.Parameter
                            {
                                name = name,
                                value = p.value,
                                chance = p.chance,
                                valueMin = p.valueMin,
                                valueMax = p.valueMax,
                                type = p.type,
                                source = p.source,
                                convertRange = p.convertRange,
                                destMax = p.destMax,
                                destMin = p.destMin,
                                destParam = p.destParam,
                                sourceMax = p.sourceMax,
                                sourceMin = p.sourceMin,
                                sourceParam = p.sourceParam
                            };
                        });
                        break;
                    }
                case VRCPlayableLayerControl l:
                    {
                        var o = old as VRCPlayableLayerControl;
                        l.ApplySettings = o.ApplySettings;
                        l.blendDuration = o.blendDuration;
                        l.debugString = o.debugString;
                        l.goalWeight = o.goalWeight;
                        l.layer = o.layer;
                        l.outputParamHash = o.outputParamHash;
                        break;
                    }
            }
#endif
        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L167
        public AnimatorState CloneAnimatorState(AnimatorState old)
        {
            Motion motion = old.motion;
            if (motion is UnityEditor.Animations.BlendTree oldTree)
            {
                var tree = CloneBlendTree(null, oldTree);
                motion = tree;
                tree.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(motion, BaseAnimator);
            }

            var n = new AnimatorState
            {
                cycleOffset = old.cycleOffset,
                cycleOffsetParameter = old.cycleOffsetParameter,
                cycleOffsetParameterActive = old.cycleOffsetParameterActive,
                hideFlags = old.hideFlags,
                iKOnFeet = old.iKOnFeet,
                mirror = old.mirror,
                mirrorParameter = old.mirrorParameter,
                mirrorParameterActive = old.mirrorParameterActive,
                motion = motion,
                name = old.name,
                speed = old.speed,
                speedParameter = old.speedParameter,
                speedParameterActive = old.speedParameterActive,
                tag = old.tag,
                timeParameter = old.timeParameter,
                timeParameterActive = old.timeParameterActive,
                writeDefaultValues = old.writeDefaultValues
            };
            AssetDatabase.AddObjectToAsset(n, BaseAnimator);
            return n;
        }

        // Taken from here: https://gist.github.com/phosphoer/93ca8dcbf925fc006e4e9f6b799c13b0
        public BlendTree CloneBlendTree(BlendTree parentTree, BlendTree oldTree)
        {
            BlendTree pastedTree = new BlendTree();
            pastedTree.name = oldTree.name;
            pastedTree.blendType = oldTree.blendType;
            pastedTree.blendParameter = oldTree.blendParameter;
            pastedTree.blendParameterY = oldTree.blendParameterY;
            pastedTree.minThreshold = oldTree.minThreshold;
            pastedTree.maxThreshold = oldTree.maxThreshold;
            pastedTree.useAutomaticThresholds = oldTree.useAutomaticThresholds;

            foreach (var child in oldTree.children)
            {
                var children = pastedTree.children;

                var childMotion = new ChildMotion
                {
                    timeScale = child.timeScale,
                    position = child.position,
                    cycleOffset = child.cycleOffset,
                    mirror = child.mirror,
                    threshold = child.threshold,
                    directBlendParameter = child.directBlendParameter
                };

                if (child.motion is BlendTree tree)
                {
                    var childTree = CloneBlendTree(pastedTree, tree);
                    childMotion.motion = childTree;
                    childTree.hideFlags = HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(childTree, BaseAnimator);
                }
                else
                {
                    childMotion.motion = child.motion;
                }

                ArrayUtility.Add(ref children, childMotion);
                pastedTree.children = children;
            }

            return pastedTree;
        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L423
        public void CloneTransitions(AnimatorStateMachine old, AnimatorStateMachine n)
        {
            List<AnimatorState> oldStates = GetStatesRecursive(old);
            List<AnimatorState> newStates = GetStatesRecursive(n);
            var newAnimatorsByChildren = new Dictionary<AnimatorStateMachine, AnimatorStateMachine>();
            var oldAnimatorsByChildren = new Dictionary<AnimatorStateMachine, AnimatorStateMachine>();
            List<AnimatorStateMachine> oldStateMachines = GetStateMachinesRecursive(old, oldAnimatorsByChildren);
            List<AnimatorStateMachine> newStateMachines = GetStateMachinesRecursive(n, newAnimatorsByChildren);
            // Generate state transitions
            for (int i = 0; i < oldStates.Count; i++)
            {
                foreach (var transition in oldStates[i].transitions)
                {
                    AnimatorStateTransition newTransition = null;
                    if (transition.isExit && transition.destinationState == null && transition.destinationStateMachine == null)
                    {
                        newTransition = newStates[i].AddExitTransition();
                    }
                    else if (transition.destinationState != null)
                    {
                        var dstState = FindMatchingState(oldStates, newStates, transition);
                        if (dstState != null)
                            newTransition = newStates[i].AddTransition(dstState);
                    }
                    else if (transition.destinationStateMachine != null)
                    {
                        var dstState = FindMatchingStateMachine(oldStateMachines, newStateMachines, transition);
                        if (dstState != null)
                            newTransition = newStates[i].AddTransition(dstState);
                    }

                    if (newTransition != null)
                        ApplyTransitionSettings(transition, newTransition);
                }
            }

            for (int i = 0; i < oldStateMachines.Count; i++)
            {
                if (oldAnimatorsByChildren.ContainsKey(oldStateMachines[i]) && newAnimatorsByChildren.ContainsKey(newStateMachines[i]))
                {
                    foreach (var transition in oldAnimatorsByChildren[oldStateMachines[i]].GetStateMachineTransitions(oldStateMachines[i]))
                    {
                        AnimatorTransition newTransition = null;
                        if (transition.isExit && transition.destinationState == null && transition.destinationStateMachine == null)
                        {
                            newTransition = newAnimatorsByChildren[newStateMachines[i]].AddStateMachineExitTransition(newStateMachines[i]);
                        }
                        else if (transition.destinationState != null)
                        {
                            var dstState = FindMatchingState(oldStates, newStates, transition);
                            if (dstState != null)
                                newTransition = newAnimatorsByChildren[newStateMachines[i]].AddStateMachineTransition(newStateMachines[i], dstState);
                        }
                        else if (transition.destinationStateMachine != null)
                        {
                            var dstState = FindMatchingStateMachine(oldStateMachines, newStateMachines, transition);
                            if (dstState != null)
                                newTransition = newAnimatorsByChildren[newStateMachines[i]].AddStateMachineTransition(newStateMachines[i], dstState);
                        }

                        if (newTransition != null)
                            ApplyTransitionSettings(transition, newTransition);
                    }
                }
                GenerateStateMachineBaseTransitions(oldStateMachines[i], newStateMachines[i], oldStates, newStates, oldStateMachines, newStateMachines);
            }
        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L389C29-L389C29
        public List<AnimatorStateMachine> GetStateMachinesRecursive(AnimatorStateMachine sm,
    IDictionary<AnimatorStateMachine, AnimatorStateMachine> newAnimatorsByChildren = null)
        {
            List<AnimatorStateMachine> childrenSm = sm.stateMachines.Select(x => x.stateMachine).ToList();

            List<AnimatorStateMachine> gcsm = new List<AnimatorStateMachine>();
            gcsm.Add(sm);
            foreach (var child in childrenSm)
            {
                newAnimatorsByChildren?.Add(child, sm);
                gcsm.AddRange(GetStateMachinesRecursive(child, newAnimatorsByChildren));
            }

            return gcsm;
        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L405
        private AnimatorState FindMatchingState(List<AnimatorState> old, List<AnimatorState> n, AnimatorTransitionBase transition)
        {
            for (int i = 0; i < old.Count; i++)
                if (transition.destinationState == old[i])
                    return n[i];

            return null;
        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L405
        public AnimatorStateMachine FindMatchingStateMachine(List<AnimatorStateMachine> old, List<AnimatorStateMachine> n, AnimatorTransitionBase transition)
        {
            for (int i = 0; i < old.Count; i++)
                if (transition.destinationStateMachine == old[i])
                    return n[i];

            return null;
        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L492
        private void GenerateStateMachineBaseTransitions(AnimatorStateMachine old, AnimatorStateMachine n, List<AnimatorState> oldStates,
    List<AnimatorState> newStates, List<AnimatorStateMachine> oldStateMachines, List<AnimatorStateMachine> newStateMachines)
        {
            foreach (var transition in old.anyStateTransitions)
            {
                AnimatorStateTransition newTransition = null;
                if (transition.destinationState != null)
                {
                    var dstState = FindMatchingState(oldStates, newStates, transition);
                    if (dstState != null)
                        newTransition = n.AddAnyStateTransition(dstState);
                }
                else if (transition.destinationStateMachine != null)
                {
                    var dstState = FindMatchingStateMachine(oldStateMachines, newStateMachines, transition);
                    if (dstState != null)
                        newTransition = n.AddAnyStateTransition(dstState);
                }

                if (newTransition != null)
                    ApplyTransitionSettings(transition, newTransition);
            }

            // Generate EntryState transitions
            foreach (var transition in old.entryTransitions)
            {
                AnimatorTransition newTransition = null;
                if (transition.destinationState != null)
                {
                    var dstState = FindMatchingState(oldStates, newStates, transition);
                    if (dstState != null)
                        newTransition = n.AddEntryTransition(dstState);
                }
                else if (transition.destinationStateMachine != null)
                {
                    var dstState = FindMatchingStateMachine(oldStateMachines, newStateMachines, transition);
                    if (dstState != null)
                        newTransition = n.AddEntryTransition(dstState);
                }

                if (newTransition != null)
                    ApplyTransitionSettings(transition, newTransition);
            }
        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L537
        private void ApplyTransitionSettings(AnimatorStateTransition transition, AnimatorStateTransition newTransition)
        {
            newTransition.canTransitionToSelf = transition.canTransitionToSelf;
            newTransition.duration = transition.duration;
            newTransition.exitTime = transition.exitTime;
            newTransition.hasExitTime = transition.hasExitTime;
            newTransition.hasFixedDuration = transition.hasFixedDuration;
            newTransition.hideFlags = transition.hideFlags;
            newTransition.isExit = transition.isExit;
            newTransition.mute = transition.mute;
            newTransition.name = transition.name;
            newTransition.offset = transition.offset;
            newTransition.interruptionSource = transition.interruptionSource;
            newTransition.orderedInterruption = transition.orderedInterruption;
            newTransition.solo = transition.solo;
            foreach (var condition in transition.conditions)
                newTransition.AddCondition(condition.mode, condition.threshold, condition.parameter);

        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L557C9-L557C9
        private void ApplyTransitionSettings(AnimatorTransition transition, AnimatorTransition newTransition)
        {
            newTransition.hideFlags = transition.hideFlags;
            newTransition.isExit = transition.isExit;
            newTransition.mute = transition.mute;
            newTransition.name = transition.name;
            newTransition.solo = transition.solo;
            foreach (var condition in transition.conditions)
                newTransition.AddCondition(condition.mode, condition.threshold, condition.parameter);

        }

        // Taken from here: https://github.com/VRLabs/Avatars-3.0-Manager/blob/9bb513216d4761f764ac60a17560f18835e5fade/Editor/AnimatorCloner.cs#L414
        public AnimatorLayer CreateLayer(string name, bool overrideOldLayer = false, AnimatorStateMachine stateMachine = null, AvatarMask avatarMask = null, AnimatorLayerBlendingMode blendingMode = AnimatorLayerBlendingMode.Override, int syncedLayerIndex = -1, bool ikPass = false, float weight = 1f, bool syncedLayerAffectsTiming = false)
        {
            if (stateMachine == null)
            {
                stateMachine = new AnimatorStateMachine()
                {
                    name = name,
                };
                AssetDatabase.AddObjectToAsset(stateMachine, BaseAnimator);
            }
            else
            {
                var newState = CloneStateMachine(stateMachine);
                CloneTransitions(stateMachine, newState);
                stateMachine = newState;
            }

            AnimatorControllerLayer baseLayer = new AnimatorControllerLayer()
            {
                name = name,
                stateMachine = stateMachine,
                avatarMask = avatarMask,
                blendingMode = blendingMode,
                syncedLayerIndex = syncedLayerIndex,
                iKPass = ikPass,
                defaultWeight = weight,
                syncedLayerAffectsTiming = syncedLayerAffectsTiming,
            };

            if (overrideOldLayer)
                RemoveLayer(name);

            BaseAnimator.AddLayer(baseLayer);

            return GetLayer(name);
        }

        public AnimatorLayer GetOrCreateLayer(string name, AnimatorStateMachine stateMachine = null, AvatarMask avatarMask = null, AnimatorLayerBlendingMode blendingMode = AnimatorLayerBlendingMode.Override, int syncedLayerIndex = -1, bool ikPass = false, float weight = 1f, bool syncedLayerAffectsTiming = false)
        {
            var layer = GetLayer(name);
            if (layer != null)
                return layer;

            return CreateLayer(name, false, stateMachine, avatarMask, blendingMode, syncedLayerIndex, ikPass, weight, syncedLayerAffectsTiming);
        }
    }
}
#endif