using System;

namespace AssistantAi.Helpers
{
    /// <summary>
    /// Rough token count used for the pre-send budget check. This is a character
    /// heuristic, not a real tokenizer, so treat the number as an upper-bound guess.
    /// </summary>
    public static class TokenEstimator
    {
        private const int AverageCharactersPerToken = 4;

        public static int CountTokens(string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            return (int)Math.Ceiling((double)input.Length / AverageCharactersPerToken);
        }
    }
}
