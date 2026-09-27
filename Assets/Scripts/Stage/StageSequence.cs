using System;
using System.Collections.Generic;
using UnityEngine;

namespace WizardArena.Stage
{
    // The ordered run of stages. StageRunner plays them front to back; clearing the last one
    // ends the run in Victory instead of StageCleared.
    [CreateAssetMenu(menuName = "Wizard Arena/Stage/Stage Sequence", fileName = "StageSequence")]
    public sealed class StageSequence : ScriptableObject
    {
        [SerializeField] private StageDefinition[] stages = Array.Empty<StageDefinition>();

        public IReadOnlyList<StageDefinition> Stages => stages;

        // For the scene builder and tests.
        public static StageSequence Create(StageDefinition[] stages)
        {
            StageSequence sequence = CreateInstance<StageSequence>();
            sequence.stages = stages ?? Array.Empty<StageDefinition>();
            return sequence;
        }
    }
}
