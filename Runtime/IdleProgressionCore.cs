using System;
using System.Collections.Generic;
using Deucarian.Progression;

namespace Deucarian.IdleProgression
{
    /// <summary>Result category for an idle progression calculation.</summary>
    public enum IdleProgressionResultCode { Success = 0, Capped = 1, ClockRollback = 2, NoElapsedTime = 3 }

    /// <summary>Continuous currency production expressed as a currency amount per elapsed second.</summary>
    public readonly struct IdleProductionRate
    {
        /// <summary>Creates a production rate.</summary>
        public IdleProductionRate(CurrencyId currencyId, double amountPerSecond)
        {
            if (currencyId.IsEmpty) throw new ArgumentException("Currency id cannot be empty.", nameof(currencyId));
            RequireFiniteNonNegative(amountPerSecond, nameof(amountPerSecond));
            CurrencyId = currencyId; AmountPerSecond = amountPerSecond;
        }
        /// <summary>Gets the produced currency.</summary>
        public CurrencyId CurrencyId { get; }
        /// <summary>Gets the non-negative amount produced per elapsed second.</summary>
        public double AmountPerSecond { get; }
        internal static void RequireFiniteNonNegative(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d) throw new ArgumentOutOfRangeException(name);
        }
    }

    /// <summary>Discrete currency reward granted once for each completed elapsed cycle.</summary>
    public readonly struct IdleCycleReward
    {
        /// <summary>Creates a cycle reward.</summary>
        public IdleCycleReward(CurrencyId currencyId, ProgressionAmount amountPerCycle, TimeSpan cycleDuration)
        {
            if (currencyId.IsEmpty) throw new ArgumentException("Currency id cannot be empty.", nameof(currencyId));
            if (cycleDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(cycleDuration));
            CurrencyId = currencyId; AmountPerCycle = amountPerCycle; CycleDuration = cycleDuration;
        }
        /// <summary>Gets the rewarded currency.</summary>
        public CurrencyId CurrencyId { get; }
        /// <summary>Gets the non-negative amount granted for each completed cycle.</summary>
        public ProgressionAmount AmountPerCycle { get; }
        /// <summary>Gets the duration required to complete one reward cycle.</summary>
        public TimeSpan CycleDuration { get; }
    }

    /// <summary>Immutable offline reward definition used by the calculator.</summary>
    public sealed class IdleProgressionDefinition
    {
        /// <summary>Creates an idle progression definition.</summary>
        public IdleProgressionDefinition(TimeSpan maximumOfflineDuration, IReadOnlyList<IdleProductionRate> productionRates = null, IReadOnlyList<IdleCycleReward> cycleRewards = null)
        {
            if (maximumOfflineDuration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maximumOfflineDuration));
            MaximumOfflineDuration = maximumOfflineDuration;
            ProductionRates = Copy(productionRates);
            CycleRewards = Copy(cycleRewards);
        }
        /// <summary>Gets the maximum elapsed time that can contribute rewards.</summary>
        public TimeSpan MaximumOfflineDuration { get; }
        /// <summary>Gets copied continuous production rates.</summary>
        public IReadOnlyList<IdleProductionRate> ProductionRates { get; }
        /// <summary>Gets copied cycle rewards.</summary>
        public IReadOnlyList<IdleCycleReward> CycleRewards { get; }
        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null) return Array.Empty<T>();
            var copy = new T[source.Count];
            for (int i = 0; i < source.Count; i++) copy[i] = source[i];
            return copy;
        }
    }

    /// <summary>Immutable result of an idle progression calculation.</summary>
    public sealed class IdleProgressionResult
    {
        /// <summary>Creates a calculation result.</summary>
        public IdleProgressionResult(IdleProgressionResultCode code, TimeSpan rawElapsed, TimeSpan effectiveElapsed, bool capped, RewardBundle reward)
        {
            Code = code; RawElapsed = rawElapsed; EffectiveElapsed = effectiveElapsed; Capped = capped; Reward = reward ?? new RewardBundle();
        }
        /// <summary>Gets the result category.</summary>
        public IdleProgressionResultCode Code { get; }
        /// <summary>Gets the raw elapsed time before caps or rollback handling.</summary>
        public TimeSpan RawElapsed { get; }
        /// <summary>Gets the elapsed time used for reward calculation.</summary>
        public TimeSpan EffectiveElapsed { get; }
        /// <summary>Gets whether the raw elapsed duration exceeded the configured cap.</summary>
        public bool Capped { get; }
        /// <summary>Gets the calculated Progression reward bundle.</summary>
        public RewardBundle Reward { get; }
    }

    /// <summary>Pure calculator for capped offline reward bundles.</summary>
    public static class IdleProgressionCalculator
    {
        /// <summary>Calculates a deterministic reward bundle from supplied timestamps and an immutable definition.</summary>
        public static IdleProgressionResult Calculate(DateTimeOffset lastSeenUtc, DateTimeOffset nowUtc, IdleProgressionDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            TimeSpan raw = nowUtc - lastSeenUtc;
            if (raw < TimeSpan.Zero) return new IdleProgressionResult(IdleProgressionResultCode.ClockRollback, raw, TimeSpan.Zero, false, new RewardBundle());
            if (raw == TimeSpan.Zero) return new IdleProgressionResult(IdleProgressionResultCode.NoElapsedTime, raw, TimeSpan.Zero, false, new RewardBundle());
            bool capped = raw > definition.MaximumOfflineDuration;
            TimeSpan effective = capped ? definition.MaximumOfflineDuration : raw;
            RewardBundle reward = CalculateReward(effective, definition);
            return new IdleProgressionResult(capped ? IdleProgressionResultCode.Capped : IdleProgressionResultCode.Success, raw, effective, capped, reward);
        }

        private static RewardBundle CalculateReward(TimeSpan elapsed, IdleProgressionDefinition definition)
        {
            var amounts = new SortedDictionary<CurrencyId, long>();
            for (int i = 0; i < definition.ProductionRates.Count; i++)
            {
                IdleProductionRate rate = definition.ProductionRates[i];
                Add(amounts, rate.CurrencyId, ToLongFloor(rate.AmountPerSecond * elapsed.TotalSeconds));
            }
            for (int i = 0; i < definition.CycleRewards.Count; i++)
            {
                IdleCycleReward cycle = definition.CycleRewards[i];
                long completed = (long)Math.Floor(elapsed.TotalSeconds / cycle.CycleDuration.TotalSeconds);
                Add(amounts, cycle.CurrencyId, SaturatingMultiply(completed, cycle.AmountPerCycle.Value));
            }
            var lines = new List<CurrencyLine>();
            foreach (KeyValuePair<CurrencyId, long> pair in amounts)
                if (pair.Value > 0) lines.Add(new CurrencyLine(pair.Key, new ProgressionAmount(pair.Value), true));
            return new RewardBundle(lines);
        }

        private static long ToLongFloor(double value)
        {
            if (value <= 0d) return 0;
            if (value > long.MaxValue) return long.MaxValue;
            return (long)Math.Floor(value);
        }

        private static void Add(SortedDictionary<CurrencyId, long> amounts, CurrencyId id, long amount)
        {
            if (amount <= 0) return;
            amounts.TryGetValue(id, out long current);
            amounts[id] = SaturatingAdd(current, amount);
        }

        private static long SaturatingAdd(long left, long right)
        {
            if (left > long.MaxValue - right) return long.MaxValue;
            return left + right;
        }

        private static long SaturatingMultiply(long left, long right)
        {
            if (left <= 0 || right <= 0) return 0;
            if (left > long.MaxValue / right) return long.MaxValue;
            return left * right;
        }
    }
}
