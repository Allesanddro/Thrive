﻿using System;

[JSONAlwaysDynamicType]
public class BehaviourActionData : EditorCombinableActionData
{
    public float NewValue;
    public float OldValue;
    public BehaviouralValueType Type;

    public BehaviourActionData(float newValue, float oldValue, BehaviouralValueType type)
    {
        OldValue = oldValue;
        NewValue = newValue;
        Type = type;
    }

    public override bool WantsMergeWith(CombinableActionData other)
    {
        return other is BehaviourActionData;
    }

    protected override double CalculateCostInternal()
    {
        // TODO: should this be free?
        return 0;
    }

    protected override ActionInterferenceMode GetInterferenceModeWithGuaranteed(CombinableActionData other)
    {
        if (other is BehaviourActionData behaviourChangeActionData && behaviourChangeActionData.Type == Type)
        {
            // If the value has been changed back to a previous value
            if (Math.Abs(NewValue - behaviourChangeActionData.OldValue) < MathUtils.EPSILON &&
                Math.Abs(behaviourChangeActionData.NewValue - OldValue) < MathUtils.EPSILON)
                return ActionInterferenceMode.CancelsOut;

            // If the value has been changed twice
            if (Math.Abs(NewValue - behaviourChangeActionData.OldValue) < MathUtils.EPSILON ||
                Math.Abs(behaviourChangeActionData.NewValue - OldValue) < MathUtils.EPSILON)
                return ActionInterferenceMode.Combinable;
        }

        return ActionInterferenceMode.NoInterference;
    }

    protected override CombinableActionData CombineGuaranteed(CombinableActionData other)
    {
        var behaviourChangeActionData = (BehaviourActionData)other;
        if (Math.Abs(OldValue - behaviourChangeActionData.NewValue) < MathUtils.EPSILON)
            return new BehaviourActionData(behaviourChangeActionData.OldValue, NewValue, Type);

        return new BehaviourActionData(behaviourChangeActionData.NewValue, OldValue, Type);
    }

    protected override void MergeGuaranteed(CombinableActionData other)
    {
        var behaviourChangeActionData = (BehaviourActionData)other;

        if (Math.Abs(OldValue - behaviourChangeActionData.NewValue) < MathUtils.EPSILON)
        {
            // Handle cancels out
            if (Math.Abs(NewValue - behaviourChangeActionData.OldValue) < MathUtils.EPSILON)
            {
                NewValue = behaviourChangeActionData.NewValue;
                return;
            }

            OldValue = behaviourChangeActionData.OldValue;
            return;
        }

        NewValue = behaviourChangeActionData.NewValue;
    }
}
