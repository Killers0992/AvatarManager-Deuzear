using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Animations;

namespace AvatarManager.Core.Helpers
{
    public class AnimationControllerParameter
    {
        public AnimationControllerParameter(string name, object defaultValue, bool createVrcParameter, AnimatorConditionMode? condition = null, object conditionValue = null)
        {
            Name = name;
            DefaultValue = defaultValue;
            CreateVrcParameter = createVrcParameter;
            if (condition.HasValue) ConditionMode = condition;
            ConditionValue = conditionValue;
        }

        public string Name { get; set; }
        public object DefaultValue { get; set; }
        public bool CreateVrcParameter { get; set; } = true;
        public AnimatorConditionMode? ConditionMode { get; set; }
        public object ConditionValue { get; set; }
    }
}
