using System.Collections.Generic;

namespace AssistantAi.Models
{
    /// <summary>
    /// Per-1K-token prices used for the pre-send cost estimate.
    ///
    /// WARNING: this table only covers gpt-3.5 / gpt-4 era models, so every model
    /// currently in <see cref="ModelCatalog.ChatModels"/> falls through to 0.0 and
    /// the UI reports "Estimated Cost = $0.00". Fill in current prices from
    /// https://platform.openai.com/docs/pricing to make the estimate meaningful.
    /// </summary>
    public static class PricingTable
    {
        private static readonly Dictionary<string, (double InputPrice, double OutputPrice)> Prices =
            new Dictionary<string, (double, double)>
            {
                { "gpt-3.5-turbo-1106", (0.0010, 0.0020) },
                { "gpt-3.5-turbo",      (0.0010, 0.0020) },
                { "gpt-3.5-turbo-16k",  (0.0010, 0.0020) },
                { "gpt-4",              (0.03,   0.06)   }
            };

        /// <summary>
        /// Estimated dollar cost of <paramref name="tokens"/> against
        /// <paramref name="modelName"/>, or 0.0 when the model has no entry.
        /// </summary>
        public static double EstimateCost(int tokens, string? modelName)
        {
            if (string.IsNullOrEmpty(modelName))
                return 0.0;

            if (!Prices.TryGetValue(modelName.ToLower(), out var prices))
                return 0.0;

            // Charges the token count at both the input and output rate.
            return (tokens / 1000.0) * (prices.InputPrice + prices.OutputPrice);
        }
    }
}
