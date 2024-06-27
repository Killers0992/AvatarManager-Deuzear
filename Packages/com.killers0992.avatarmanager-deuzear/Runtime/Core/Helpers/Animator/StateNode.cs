#if UNITY_EDITOR
using UnityEditor.Animations;

namespace AvatarManager.Core.Helpers
{
    public class StateNode
    {
        public AnimatorState BaseState { get; private set; }
        public AnimatorLayer Layer { get; set; }

        public static StateNode Init(AnimatorLayer layer, AnimatorState state)
        {
            StateNode node = new StateNode();
            node.BaseState = state;
            node.Layer = layer;

            return node;
        }

        public StateNode CreateNode(string name, AnimationControllerParameter condition = null, bool hasExitTime = false, bool hasFixedDuration = false, bool setDefaultExitTime = false)
        {
            StateNode node = Layer.CreateNode(name);

            Connect(node, condition, hasExitTime, hasFixedDuration, setDefaultExitTime);
          
            return node;
        }

        public void Connect(StateNode node, AnimationControllerParameter condition = null, bool hasExitTime = false, bool hasFixedDuration = false, bool setDefaultExitTime = false, float duration = 0f)
        {
            AnimatorStateTransition transition = new AnimatorStateTransition()
            {
                destinationState = node.BaseState,
                hasExitTime = hasExitTime,
                hasFixedDuration = hasFixedDuration,
                duration = duration,
            };

            if (condition != null)
            {
                Layer.AnimationController.CreateParameter(condition.Name, condition.DefaultValue, createVrcParameter: condition.CreateVrcParameter);

                float value = 0f;
                switch (condition.ConditionValue)
                {
                    case bool _bool:
                        value = _bool ? 1f : 0f;
                        break;
                    case float _float:
                        value = _float;
                        break;
                    case int _int:
                        value = _int;
                        break;
                }

                transition.AddCondition(condition.ConditionMode.Value, value, condition.Name);
            }

            if (setDefaultExitTime)
            {
                transition.hasExitTime = true;
                transition.duration = 0.25f;
                transition.exitTime = 0.75f;
            }

            BaseState.AddTransition(transition);
        }
    }
}
#endif