﻿namespace AutoEvo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

/// <summary>
///   A node for the Miche Tree
/// </summary>
/// <remarks>
///   <para>
///     The Miche class forms a tree by storing a list of child instances of Miche Nodes. If a Miche has no children
///     it is considered a leaf node and can have a species Occupant instead (otherwise Occupant should be null).
///     This class handles insertion into the tree through scores from the selection pressure it contains.
///     For a fuller explanation see docs/auto_evo.md
///   </para>
/// </remarks>
/// <remarks>
///   <para>
///     This is partially saved to be able to display info about the auto-evo run in the editor after loading a save.
///     This has to be a reference to work with the parent miche reference. This uses Thrive serializer as the fields
///     can already refer back to this object.
///   </para>
/// </remarks>
[JsonObject(IsReference = true)]
[UseThriveSerializer]
public class Miche
{
    [JsonProperty]
    public readonly SelectionPressure Pressure;

    [JsonProperty]
    public readonly List<Miche> Children = new();

    [JsonProperty]
    public Miche? Parent;

    /// <summary>
    ///   The species that currently occupies this Miche
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     Occupant should always be null if this Miche is not a leaf node (children is not empty).
    ///   </para>
    /// </remarks>
    [JsonProperty]
    public Species? Occupant;

    private bool locked;

    public Miche(SelectionPressure pressure)
    {
        Pressure = pressure;
    }

    public bool IsLeafNode()
    {
        return Children.Count == 0;
    }

    public void GetLeafNodes(List<Miche> nodes)
    {
        if (IsLeafNode())
        {
            nodes.Add(this);
            return;
        }

        foreach (var child in Children)
        {
            child.GetLeafNodes(nodes);
        }
    }

    public void GetLeafNodes(List<Miche> nodes, Func<Miche, bool> criteria)
    {
        if (IsLeafNode() && criteria(this))
        {
            nodes.Add(this);
            return;
        }

        foreach (var child in Children)
        {
            child.GetLeafNodes(nodes, criteria);
        }
    }

    public void GetLeafNodes(List<Miche> passingNodes, List<Miche> failingNodes, Func<Miche, bool> criteria)
    {
        if (IsLeafNode())
        {
            if (criteria(this))
            {
                passingNodes.Add(this);
            }
            else
            {
                failingNodes.Add(this);
            }

            return;
        }

        foreach (var child in Children)
        {
            child.GetLeafNodes(passingNodes, failingNodes, criteria);
        }
    }

    /// <summary>
    ///   Adds occupants of this and all child miches to the set. Does not clear the set before adding.
    /// </summary>
    /// <param name="occupantsSet">Where to *append* the results</param>
    public void GetOccupants(HashSet<Species> occupantsSet)
    {
        if (Occupant != null)
        {
            occupantsSet.Add(Occupant);
            return;
        }

        foreach (var child in Children)
        {
            child.GetOccupants(occupantsSet);
        }
    }

    public IEnumerable<Miche> BackTraversal(List<Miche> currentTraversal)
    {
        currentTraversal.Insert(0, this);

        if (Parent == null)
            return currentTraversal;

        return Parent.BackTraversal(currentTraversal);
    }

    public void AddChild(Miche newChild)
    {
        ThrowIfLocked();

        Children.Add(newChild);
        newChild.Parent = this;
    }

    /// <summary>
    ///   Inserts a species into any spots on the tree where the species is a better fit than any current occupants
    /// </summary>
    /// <param name="species">Species to try to insert</param>
    /// <param name="patch">Patch this miche is in for calculating scores</param>
    /// <param name="scoresSoFar">
    ///   Scores generated so far. If not called recursively just pass in null. Not modified by this method.
    /// </param>
    /// <param name="cache">Data cache for faster calculation</param>
    /// <param name="dry">If true the species is not inserted but only checked if it could be inserted</param>
    /// <param name="workingMemory">Temporary working memory to use by this method, automatically cleared</param>
    /// <param name="depth">Depth in the miche tree</param>
    /// <returns>
    ///   Returns a bool based on if the species was inserted into a leaf node
    /// </returns>
    public bool InsertSpecies(Species species, Patch patch, Dictionary<Species, float>? scoresSoFar,
        SimulationCache cache, bool dry, InsertWorkingMemory workingMemory, int depth = 0)
    {
        var myScore = Pressure.Score(species, patch, cache);

        // Prune branch if species fails any pressures
        if (myScore <= 0)
            return false;

        if (IsLeafNode() && Occupant == null)
        {
            if (!dry)
                Occupant = species;

            return true;
        }

        var newScores = workingMemory.GetScoresAtDepth(depth);

        workingMemory.WorkingHashSet.Clear();
        GetOccupants(workingMemory.WorkingHashSet);

        // Build new scores on top of previous values
        if (scoresSoFar == null)
        {
            // Initial call, not recursive

            foreach (var currentSpecies in workingMemory.WorkingHashSet)
            {
                newScores[currentSpecies] =
                    Pressure.WeightedComparedScores(myScore, Pressure.Score(currentSpecies, patch, cache));
            }
        }
        else
        {
            foreach (var currentSpecies in workingMemory.WorkingHashSet)
            {
                var addedScoreAmount =
                    Pressure.WeightedComparedScores(myScore, Pressure.Score(currentSpecies, patch, cache));

                // If some species doesn't have a score yet, the score it starts off with is 0
                if (scoresSoFar.TryGetValue(currentSpecies, out var score))
                {
                    newScores[currentSpecies] = score + addedScoreAmount;
                }
                else
                {
                    newScores[currentSpecies] = addedScoreAmount;
                }
            }
        }

        // We check here to see if scores more than 0, because
        // scores is relative to the inserted species
        if (IsLeafNode() && newScores[Occupant!] > 0)
        {
            if (!dry)
                Occupant = species;

            return true;
        }

        var inserted = false;
        foreach (var child in Children)
        {
            if (child.InsertSpecies(species, patch, newScores, cache, dry, workingMemory, depth + 1))
            {
                inserted = true;

                if (dry)
                    return true;
            }
        }

        return inserted;
    }

    public Miche DeepCopy()
    {
        // This doesn't copy pressures, but it shouldn't need to as pressures should not have any state outside init

        if (IsLeafNode())
        {
            return new Miche(Pressure)
            {
                Occupant = Occupant,
            };
        }

        var newChildren = new List<Miche>(Children).Select(child => child.DeepCopy()).ToList();

        var newMiche = new Miche(Pressure);
        newMiche.Children.AddRange(newChildren);

        return newMiche;
    }

    /// <summary>
    ///   Mark miche as added to the simulation, locks this from having the children modified
    /// </summary>
    public void Lock()
    {
        if (locked)
            throw new InvalidOperationException("Miche is already locked");

        locked = true;
    }

    public override int GetHashCode()
    {
        var parentHash = Parent != null ? Parent.GetHashCode() : 53;

        // TODO: as Occupant can change it should not be used as part of the hash code
        return Pressure.GetHashCode() * 131 ^ parentHash * 587 ^
            (Occupant == null ? 17 : Occupant.GetHashCode()) * 5171;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfLocked()
    {
        if (locked)
        {
            throw new InvalidOperationException(
                "This operation cannot be done after miche is added to the simulation (locked)");
        }
    }

    /// <summary>
    ///   Working memory used to reduce memory allocations in <see cref="Miche.InsertSpecies"/>
    /// </summary>
    public class InsertWorkingMemory
    {
        public readonly HashSet<Species> WorkingHashSet = new();

        private readonly List<Dictionary<Species, float>> scoreDictionaries = new();

        public Dictionary<Species, float> GetScoresAtDepth(int depth)
        {
            while (scoreDictionaries.Count <= depth)
                scoreDictionaries.Add(new Dictionary<Species, float>());

            var result = scoreDictionaries[depth];

            // Clear any previous data before returning to make calling this method simpler
            result.Clear();

            return result;
        }
    }
}
