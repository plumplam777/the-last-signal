using System;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// Pure logic of the reactor balance puzzle (StoryBible §6.5). Three coolant valves, each Low/Mid/High.
    /// Output and core temperature are sums of per-valve contributions; the core is stable only when both are in
    /// the green band (40–60%), which happens exactly when every valve is on a different setting.
    /// Output above 75% is an overshoot: alarm, then the valves reset to Mid. No Unity dependencies.
    /// </summary>
    public sealed class ReactorBalance
    {
        public const int ValveCount = 3, Low = 0, Mid = 1, High = 2;
        public const int Base = 10, BandMin = 40, BandMax = 60, OvershootAbove = 75;

        // [valve, setting] — found by search so that "stable ⇔ all settings different" (margin ≥ 5 points).
        private static readonly int[,] OutputTable = { { 9, 13, 33 }, { 1, 6, 29 }, { 1, 6, 24 } };
        private static readonly int[,] TemperatureTable = { { 26, 5, 4 }, { 32, 13, 9 }, { 27, 8, 5 } };

        private readonly int[] _settings = { Mid, Mid, Mid };

        public int Get(int valve) => _settings[valve];

        public void Set(int valve, int setting)
        {
            if (valve < 0 || valve >= ValveCount) throw new ArgumentOutOfRangeException(nameof(valve));
            _settings[valve] = Math.Clamp(setting, Low, High);
        }

        /// <summary>Low → Mid → High → Low.</summary>
        public void Cycle(int valve) => Set(valve, (_settings[valve] + 1) % 3);

        public void Reset()
        {
            for (int i = 0; i < ValveCount; i++) _settings[i] = Mid;
        }

        public int Output => Sum(OutputTable);
        public int Temperature => Sum(TemperatureTable);

        public bool IsStable => InBand(Output) && InBand(Temperature);
        public bool IsOvershoot => Output > OvershootAbove;

        public static bool InBand(int v) => v >= BandMin && v <= BandMax;

        private int Sum(int[,] table)
        {
            int total = Base;
            for (int i = 0; i < ValveCount; i++) total += table[i, _settings[i]];
            return total;
        }
    }
}
