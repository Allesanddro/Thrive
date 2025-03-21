﻿namespace AutoEvo;

using System;
using System.Collections.Generic;

public class ChangeMembraneType : IMutationStrategy<MicrobeSpecies>
{
    private MembraneType membraneType;

    public ChangeMembraneType(string membraneType)
    {
        this.membraneType = SimulationParameters.Instance.GetMembrane(membraneType);
    }

    public bool Repeatable => false;

    public List<Tuple<MicrobeSpecies, double>>? MutationsOf(MicrobeSpecies baseSpecies, double mp, bool lawk,
        Random random, BiomeConditions biomeToConsider)
    {
        if (baseSpecies.MembraneType == membraneType)
            return null;

        if (mp < membraneType.EditorCost)
            return null;

        var newSpecies = (MicrobeSpecies)baseSpecies.Clone();

        newSpecies.MembraneType = membraneType;

        return [Tuple.Create(newSpecies, mp - membraneType.EditorCost)];
    }
}
