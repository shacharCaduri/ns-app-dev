using System.Collections.Generic;
using NUnit.Framework;
using WizardArena.Enemies;

namespace WizardArena.Tests.EditMode.Enemies
{
    public sealed class EnemyStateMachineTests
    {
        private readonly List<string> calls = new List<string>();

        [SetUp]
        public void SetUp()
        {
            calls.Clear();
        }

        [Test]
        public void ChangeTo_ExitsTheOldState_BeforeEnteringTheNewOne()
        {
            EnemyStateMachine machine = new EnemyStateMachine();
            RecordingState a = new RecordingState("A", calls);
            RecordingState b = new RecordingState("B", calls);

            machine.ChangeTo(a);
            machine.ChangeTo(b);

            CollectionAssert.AreEqual(new[] { "A.Enter", "A.Exit", "B.Enter" }, calls);
            Assert.AreSame(b, machine.Current);
        }

        [Test]
        public void Tick_OnlyTicksTheCurrentState()
        {
            EnemyStateMachine machine = new EnemyStateMachine();
            RecordingState a = new RecordingState("A", calls);
            RecordingState b = new RecordingState("B", calls);
            machine.ChangeTo(a);
            machine.ChangeTo(b);
            calls.Clear();

            machine.Tick(0.1f);

            CollectionAssert.AreEqual(new[] { "B.Tick" }, calls);
        }

        [Test]
        public void ChangeTo_ReentersTheSameState()
        {
            EnemyStateMachine machine = new EnemyStateMachine();
            RecordingState a = new RecordingState("A", calls);

            machine.ChangeTo(a);
            machine.ChangeTo(a);

            CollectionAssert.AreEqual(new[] { "A.Enter", "A.Exit", "A.Enter" }, calls);
        }

        [Test]
        public void Tick_DoesNothing_WithoutAState()
        {
            EnemyStateMachine machine = new EnemyStateMachine();

            Assert.DoesNotThrow(() => machine.Tick(0.1f));
            Assert.IsNull(machine.Current);
        }

        private sealed class RecordingState : IEnemyState
        {
            private readonly string name;
            private readonly List<string> log;

            public RecordingState(string name, List<string> log)
            {
                this.name = name;
                this.log = log;
            }

            public void Enter() => log.Add(name + ".Enter");
            public void Tick(float deltaTime) => log.Add(name + ".Tick");
            public void Exit() => log.Add(name + ".Exit");
        }
    }
}
