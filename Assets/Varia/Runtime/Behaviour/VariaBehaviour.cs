using UnityEngine;

namespace Varia
{

    /// <summary>
    /// Base class for all varia components that actually do something.
    /// Simply inherit, and override the Apply() method to make a new component.
    /// </summary>
    public abstract class VariaBehaviour : MonoBehaviour
    {
        VariaContext context;

        public VariaConditionList conditionList = new VariaConditionList();

        protected void OnEnable()
        {
            context = VariaContext.current;
        }

        private void Start()
        {
            // Disable if prototype set
            var prototype = GetComponentInParent<VariaPrototype>();
            if (prototype == null || prototype.enabled == false)
            {
                context.ConditionalApply(this);
            }
        }

        /// <summary>
        /// Override this to control what happens when all the conditions are met
        /// </summary>
        /// <param name="context"></param>
        public virtual void Apply(VariaContext context)
        {
        }

        /// <summary>
        /// Override this to control what happens when a condition is missed
        /// </summary>
        /// <param name="context"></param>
        public virtual void NoApply(VariaContext context)
        {
        }
    }
}