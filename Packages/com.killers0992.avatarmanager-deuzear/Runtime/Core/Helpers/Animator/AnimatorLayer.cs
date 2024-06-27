#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Animations;

namespace AvatarManager.Core.Helpers
{
    public class AnimatorLayer
    {
        private Dictionary<AnimatorState, StateNode> _baseNodeToCustom = new Dictionary<AnimatorState, StateNode>();

        public AnimatorControllerLayer BaseLayer { get; private set; }
        public AvatarAnimationController AnimationController { get; private set; }
        public int Index { get; set; }

        public static AnimatorLayer Init(AvatarAnimationController controller, AnimatorControllerLayer layer)
        {
            AnimatorLayer animatorLayer = new AnimatorLayer();
            animatorLayer.BaseLayer = layer;
            animatorLayer.AnimationController = controller;
    
            return animatorLayer;
        }

        public void SyncNodes(ChildAnimatorState[] states)
        {
            BaseLayer.stateMachine.states = states;
        }

        public StateNode CreateNode(string name)
        {
            AnimatorState animState = BaseLayer.stateMachine.AddState(name);

            StateNode node = StateNode.Init(this, animState);

            _baseNodeToCustom.Add(animState, node);

            return node;
        }

        public StateNode GetNode(string name)
        {
            ChildAnimatorState state = BaseLayer.stateMachine.states.FirstOrDefault(x => x.state.name == name);

            if (state.state == null) return null;

            if (!_baseNodeToCustom.TryGetValue(state.state, out StateNode node))
            {
                node = StateNode.Init(this, state.state);
                _baseNodeToCustom.Add(state.state, node);
            }

            return node;
        }

        public void Clear()
        {
            BaseLayer.stateMachine.states = new ChildAnimatorState[0];
        }
    }
}
#endif