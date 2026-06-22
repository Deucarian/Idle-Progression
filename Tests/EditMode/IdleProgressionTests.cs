using System;
using Deucarian.Progression;
using NUnit.Framework;

namespace Deucarian.IdleProgression.Tests
{
    public sealed class IdleProgressionTests
    {
        private static readonly CurrencyId Credits = new CurrencyId("currency.credits");
        private static readonly CurrencyId Scrap = new CurrencyId("currency.scrap");
        private static readonly DateTimeOffset Start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [Test]
        public void ZeroElapsedReturnsNoElapsedReward()
        {
            IdleProgressionResult result = IdleProgressionCalculator.Calculate(Start, Start, Definition());

            Assert.AreEqual(IdleProgressionResultCode.NoElapsedTime, result.Code);
            Assert.AreEqual(TimeSpan.Zero, result.EffectiveElapsed);
            Assert.AreEqual(0, result.Reward.CurrencyLines.Count);
        }

        [Test]
        public void PositiveElapsedReturnsSuccessAndReward()
        {
            IdleProgressionResult result = IdleProgressionCalculator.Calculate(Start, Start.AddSeconds(10), Definition());

            Assert.AreEqual(IdleProgressionResultCode.Success, result.Code);
            Assert.AreEqual(TimeSpan.FromSeconds(10), result.EffectiveElapsed);
            AssertCurrency(result, Credits, 20);
        }

        [Test]
        public void CappedElapsedUsesMaximumDuration()
        {
            IdleProgressionResult result = IdleProgressionCalculator.Calculate(Start, Start.AddHours(5), Definition(maximumSeconds: 60));

            Assert.AreEqual(IdleProgressionResultCode.Capped, result.Code);
            Assert.IsTrue(result.Capped);
            Assert.AreEqual(TimeSpan.FromSeconds(60), result.EffectiveElapsed);
            AssertCurrency(result, Credits, 120);
        }

        [Test]
        public void NegativeElapsedReturnsClockRollbackWithoutReward()
        {
            IdleProgressionResult result = IdleProgressionCalculator.Calculate(Start, Start.AddSeconds(-1), Definition());

            Assert.AreEqual(IdleProgressionResultCode.ClockRollback, result.Code);
            Assert.AreEqual(TimeSpan.Zero, result.EffectiveElapsed);
            Assert.AreEqual(0, result.Reward.CurrencyLines.Count);
        }

        [Test]
        public void HugeElapsedIsCappedAndDoesNotRunaway()
        {
            IdleProgressionDefinition definition = Definition(
                maximumSeconds: 86_400,
                rates: new[] { new IdleProductionRate(Credits, 100) });

            IdleProgressionResult result = IdleProgressionCalculator.Calculate(Start, Start.AddYears(100), definition);

            Assert.AreEqual(IdleProgressionResultCode.Capped, result.Code);
            Assert.AreEqual(TimeSpan.FromDays(1), result.EffectiveElapsed);
            AssertCurrency(result, Credits, 8_640_000);
        }

        [Test]
        public void RateCalculationFloorsFractionalCurrency()
        {
            IdleProgressionDefinition definition = Definition(rates: new[] { new IdleProductionRate(Credits, 1.5) });

            IdleProgressionResult result = IdleProgressionCalculator.Calculate(Start, Start.AddSeconds(3), definition);

            AssertCurrency(result, Credits, 4);
        }

        [Test]
        public void CycleCalculationUsesOnlyCompletedCycles()
        {
            IdleProgressionDefinition definition = Definition(
                rates: Array.Empty<IdleProductionRate>(),
                cycles: new[] { new IdleCycleReward(Scrap, new ProgressionAmount(7), TimeSpan.FromSeconds(5)) });

            IdleProgressionResult result = IdleProgressionCalculator.Calculate(Start, Start.AddSeconds(14), definition);

            AssertCurrency(result, Scrap, 14);
        }

        [Test]
        public void RewardCalculationCombinesRatesAndCyclesByCurrency()
        {
            IdleProgressionDefinition definition = Definition(
                rates: new[] { new IdleProductionRate(Credits, 2), new IdleProductionRate(Scrap, 1) },
                cycles: new[] { new IdleCycleReward(Credits, new ProgressionAmount(5), TimeSpan.FromSeconds(10)) });

            IdleProgressionResult result = IdleProgressionCalculator.Calculate(Start, Start.AddSeconds(20), definition);

            AssertCurrency(result, Credits, 50);
            AssertCurrency(result, Scrap, 20);
        }

        [Test]
        public void CapPreventsRunawayReward()
        {
            IdleProgressionResult capped = IdleProgressionCalculator.Calculate(Start, Start.AddHours(2), Definition(maximumSeconds: 30));
            IdleProgressionResult withinCap = IdleProgressionCalculator.Calculate(Start, Start.AddSeconds(30), Definition(maximumSeconds: 30));

            AssertCurrency(capped, Credits, CurrencyAmount(withinCap, Credits));
        }

        [Test]
        public void CalculationIsDeterministic()
        {
            IdleProgressionDefinition definition = Definition(cycles: new[] { new IdleCycleReward(Scrap, new ProgressionAmount(3), TimeSpan.FromSeconds(4)) });

            IdleProgressionResult first = IdleProgressionCalculator.Calculate(Start, Start.AddSeconds(29), definition);
            IdleProgressionResult second = IdleProgressionCalculator.Calculate(Start, Start.AddSeconds(29), definition);

            Assert.AreEqual(first.Code, second.Code);
            Assert.AreEqual(first.EffectiveElapsed, second.EffectiveElapsed);
            Assert.AreEqual(CurrencyAmount(first, Credits), CurrencyAmount(second, Credits));
            Assert.AreEqual(CurrencyAmount(first, Scrap), CurrencyAmount(second, Scrap));
        }

        [Test]
        public void InvalidRateIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IdleProductionRate(Credits, -0.1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new IdleProductionRate(Credits, double.NaN));
            Assert.Throws<ArgumentException>(() => new IdleProductionRate(default, 1));
        }

        [Test]
        public void InvalidCapIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new IdleProgressionDefinition(TimeSpan.Zero));
            Assert.Throws<ArgumentOutOfRangeException>(() => new IdleProgressionDefinition(TimeSpan.FromSeconds(-1)));
        }

        [Test]
        public void ProgressionRewardBundleCanBeAppliedByApplicationLayer()
        {
            var catalog = new ProgressionCatalog(new[] { new CurrencyDefinition(Credits, new ProgressionAmount(500)) });
            var state = new ProgressionState();
            IdleProgressionResult result = IdleProgressionCalculator.Calculate(Start, Start.AddSeconds(30), Definition());

            ProgressionResult applied = state.ApplyReward(catalog, new ProgressionOperationId("idle.reward.1"), result.Reward);

            Assert.IsTrue(applied.Succeeded);
            Assert.AreEqual(60, state.GetBalance(Credits).Value);
        }

        [Test]
        public void CalculatorDoesNotSimulateIndividualEnemies()
        {
            IdleProgressionDefinition definition = Definition(
                rates: new[] { new IdleProductionRate(Credits, 5) },
                cycles: new[] { new IdleCycleReward(Scrap, new ProgressionAmount(1), TimeSpan.FromSeconds(2)) });

            IdleProgressionResult result = IdleProgressionCalculator.Calculate(Start, Start.AddSeconds(8), definition);

            Assert.AreEqual(2, result.Reward.CurrencyLines.Count);
            AssertCurrency(result, Credits, 40);
            AssertCurrency(result, Scrap, 4);
        }

        private static IdleProgressionDefinition Definition(
            int maximumSeconds = 3600,
            IdleProductionRate[] rates = null,
            IdleCycleReward[] cycles = null)
        {
            return new IdleProgressionDefinition(
                TimeSpan.FromSeconds(maximumSeconds),
                rates ?? new[] { new IdleProductionRate(Credits, 2) },
                cycles ?? Array.Empty<IdleCycleReward>());
        }

        private static void AssertCurrency(IdleProgressionResult result, CurrencyId id, long amount)
        {
            Assert.AreEqual(amount, CurrencyAmount(result, id));
        }

        private static long CurrencyAmount(IdleProgressionResult result, CurrencyId id)
        {
            for (int index = 0; index < result.Reward.CurrencyLines.Count; index++)
            {
                CurrencyLine line = result.Reward.CurrencyLines[index];
                if (line.CurrencyId.Equals(id))
                {
                    Assert.IsTrue(line.IsCredit);
                    return line.Amount.Value;
                }
            }

            return 0;
        }
    }
}
