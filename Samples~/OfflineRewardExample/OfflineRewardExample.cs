using System;
using Deucarian.Progression;

namespace Deucarian.IdleProgression.Samples
{
    public static class OfflineRewardExample
    {
        public static IdleProgressionResult CalculateOneHour()
        {
            var credits = new CurrencyId("currency.credits");
            var definition = new IdleProgressionDefinition(
                TimeSpan.FromHours(8),
                new[] { new IdleProductionRate(credits, 0.5d) },
                new[] { new IdleCycleReward(credits, new ProgressionAmount(25), TimeSpan.FromMinutes(10)) });

            return IdleProgressionCalculator.Calculate(
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch.AddHours(1),
                definition);
        }
    }
}
