using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WizardArena.Combat;

namespace WizardArena.Tests.EditMode.Combat
{
    public sealed class HealthTests
    {
        private readonly List<Object> created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object item in created) Object.DestroyImmediate(item);
            created.Clear();
        }

        [Test]
        public void TakeDamage_ReducesCurrentAndRaisesDamaged()
        {
            Health health = CreateHealth(5, Team.Enemy);
            DamageInfo received = default;
            int damagedCount = 0;
            health.Damaged += damage => { received = damage; damagedCount++; };

            bool applied = health.TakeDamage(Hit(2, Team.Player));

            Assert.IsTrue(applied);
            Assert.AreEqual(3, health.Current);
            Assert.AreEqual(1, damagedCount);
            Assert.AreEqual(2, received.Amount);
        }

        [Test]
        public void Died_IsRaisedOnce_WhenHealthReachesZero()
        {
            Health health = CreateHealth(2, Team.Enemy);
            int diedCount = 0;
            health.Died += () => diedCount++;

            health.TakeDamage(Hit(5, Team.Player));
            bool appliedWhenDead = health.TakeDamage(Hit(1, Team.Player));

            Assert.AreEqual(0, health.Current);
            Assert.IsTrue(health.IsDead);
            Assert.IsFalse(appliedWhenDead);
            Assert.AreEqual(1, diedCount);
        }

        [Test]
        public void Heal_IsCappedAtMax_AndReportsTheAmountHealed()
        {
            Health health = CreateHealth(5, Team.Player);
            health.TakeDamage(Hit(2, Team.Enemy));
            int healed = 0;
            health.Healed += amount => healed = amount;

            health.Heal(10);

            Assert.AreEqual(5, health.Current);
            Assert.AreEqual(2, healed);
        }

        [Test]
        public void Heal_DoesNothing_WhenDead()
        {
            Health health = CreateHealth(1, Team.Player);
            health.TakeDamage(Hit(1, Team.Enemy));

            health.Heal(1);

            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void TakeDamage_IsIgnored_DuringInvulnerabilityWindow()
        {
            Health health = CreateHealth(10, Team.Player, invulnerabilitySeconds: 60f);

            bool first = health.TakeDamage(Hit(2, Team.Enemy));
            bool second = health.TakeDamage(Hit(2, Team.Enemy));

            Assert.IsTrue(first);
            Assert.IsFalse(second);
            Assert.IsTrue(health.IsInvulnerable);
            Assert.AreEqual(8, health.Current);
        }

        [Test]
        public void TakeDamage_AppliesEveryHit_WithoutInvulnerabilityWindow()
        {
            Health health = CreateHealth(3, Team.Enemy);

            health.TakeDamage(Hit(1, Team.Player));
            health.TakeDamage(Hit(1, Team.Player));

            Assert.AreEqual(1, health.Current);
        }

        [Test]
        public void TakeDamage_IsIgnored_FromTheSameTeam()
        {
            Health player = CreateHealth(3, Team.Player);
            Health enemy = CreateHealth(3, Team.Enemy);
            int damagedCount = 0;
            player.Damaged += _ => damagedCount++;
            enemy.Damaged += _ => damagedCount++;

            Assert.IsFalse(player.TakeDamage(Hit(1, Team.Player)));
            Assert.IsFalse(enemy.TakeDamage(Hit(1, Team.Enemy)));
            Assert.AreEqual(3, player.Current);
            Assert.AreEqual(3, enemy.Current);
            Assert.AreEqual(0, damagedCount);
        }

        [Test]
        public void TakeDamage_FromNeutral_HurtsEveryTeam()
        {
            Health player = CreateHealth(3, Team.Player);
            Health enemy = CreateHealth(3, Team.Enemy);
            Health neutral = CreateHealth(3, Team.Neutral);

            Assert.IsTrue(player.TakeDamage(Hit(1, Team.Neutral)));
            Assert.IsTrue(enemy.TakeDamage(Hit(1, Team.Neutral)));
            Assert.IsTrue(neutral.TakeDamage(Hit(1, Team.Neutral)));
            Assert.AreEqual(2, player.Current);
            Assert.AreEqual(2, enemy.Current);
            Assert.AreEqual(2, neutral.Current);
        }

        [Test]
        public void TakeDamage_IsIgnored_WhenDisabledOrAmountIsNotPositive()
        {
            Health health = CreateHealth(3, Team.Enemy);

            Assert.IsFalse(health.TakeDamage(Hit(0, Team.Player)), "zero");
            health.enabled = false;
            Assert.IsFalse(health.TakeDamage(Hit(1, Team.Player)), "disabled");
            Assert.AreEqual(3, health.Current);
        }

        [Test]
        public void Configure_RestoresFullHealth()
        {
            Health health = CreateHealth(3, Team.Enemy);
            health.TakeDamage(Hit(3, Team.Player));

            health.Configure(Track(HealthConfig.Create(4, 0f)), Team.Enemy);

            Assert.AreEqual(4, health.Current);
            Assert.AreEqual(4, health.Max);
            Assert.IsFalse(health.IsDead);
        }

        private Health CreateHealth(int maxHealth, Team team, float invulnerabilitySeconds = 0f)
        {
            GameObject item = Track(new GameObject("Test Health"));
            Health health = item.AddComponent<Health>();
            // Awake does not run in Edit Mode; Configure sets everything up.
            health.Configure(Track(HealthConfig.Create(maxHealth, invulnerabilitySeconds)), team);
            return health;
        }

        private static DamageInfo Hit(int amount, Team team)
        {
            return new DamageInfo(amount, Vector2.zero, null, team);
        }

        private T Track<T>(T item) where T : Object
        {
            created.Add(item);
            return item;
        }
    }
}
