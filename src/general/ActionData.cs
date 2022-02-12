﻿using System;

public abstract class ActionData
{
    /// <summary>
    ///   Does this action cancel out with the <paramref name="other"/> action?
    /// </summary>
    /// <returns>
    ///   Returns the interference mode with <paramref name="other"/>
    /// </returns>
    /// <para>Do not call with itself</para>
    public abstract MicrobeActionInterferenceMode GetInterferenceModeWith(ActionData other);

    /// <summary>
    ///   Combines two actions to one if possible.
    ///   Call <see cref="GetInterferenceModeWith"/> first and check if it returns
    ///   <see cref="MicrobeActionInterferenceMode.Combinable"/>
    /// </summary>
    /// <param name="other">The action this should be combined with</param>
    /// <returns>Returns the combined action</returns>
    /// <exception cref="NotSupportedException">Thrown when combination is not possible</exception>
    public ActionData Combine(ActionData other)
    {
        if (GetInterferenceModeWith(other) != MicrobeActionInterferenceMode.Combinable)
            throw new NotSupportedException();

        return CombineGuaranteed(other);
    }

    /// <summary>
    ///   Combines two actions to one
    /// </summary>
    /// <param name="other">The action this should be combined with. Guaranteed to be combinable</param>
    /// <returns>Returns the combined action</returns>
    protected abstract ActionData CombineGuaranteed(ActionData other);
}
