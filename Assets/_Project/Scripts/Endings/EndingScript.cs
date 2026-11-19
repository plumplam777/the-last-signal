using System.Collections.Generic;
using LastSignal.Core;

namespace LastSignal.Endings
{
    public enum EndingType
    {
        None,
        /// <summary>A — overload the reactor, destroy Lantern Station.</summary>
        Destroy,
        /// <summary>B — lift the lockout, return the station to CAIRN.</summary>
        Restore,
        /// <summary>C (hidden) — silence the Array, release the pod beacon.</summary>
        Silence,
    }

    public enum EndingBeat
    {
        /// <summary>CAIRN speaks (subtitle with speaker).</summary>
        Cairn,
        /// <summary>Screen/caption text without speaker.</summary>
        Caption,
        /// <summary>The visual event of the ending (explosion / lights on / Murmur fades).</summary>
        Event,
        Epilogue,
    }

    public readonly struct EndingLine
    {
        public readonly EndingBeat Beat;
        public readonly string Key;
        public EndingLine(EndingBeat beat, string key) { Beat = beat; Key = key; }
    }

    /// <summary>
    /// The scripted lines of each ending (StoryBible §9.3–9.5) chosen by trust. Trust never changes which ending
    /// plays, only the lines and the epilogue: trust ≥ 50 uses the "high" variants, below 50 the "low" ones
    /// (the bible defines only two epilogue variants, so neutral trust is split at the starting value).
    /// </summary>
    public static class EndingScript
    {
        public const string ChoiceKey = "choice.final";
        public const int EpilogueSplit = 50;
        /// <summary>Ending C requires every audio log (StoryBible §8: all 14).</summary>
        public const int HiddenEndingLogs = 14;

        public static string Id(EndingType type) => type switch
        {
            EndingType.Destroy => "destroy",
            EndingType.Restore => "restore",
            EndingType.Silence => "silence",
            _ => string.Empty,
        };

        public static EndingType FromId(string id) => id switch
        {
            "destroy" => EndingType.Destroy,
            "restore" => EndingType.Restore,
            "silence" => EndingType.Silence,
            _ => EndingType.None,
        };

        public static bool HighTrust(int trust) => trust >= EpilogueSplit;

        public static bool HiddenEndingAvailable(int logsCollected) => logsCollected >= HiddenEndingLogs;

        public static List<EndingLine> Lines(EndingType type, int trust)
        {
            bool high = HighTrust(trust);
            var l = new List<EndingLine>();
            switch (type)
            {
                case EndingType.Destroy:
                    l.Add(new(EndingBeat.Cairn, "end.a.01"));
                    if (high)
                    {
                        l.Add(new(EndingBeat.Cairn, "end.a.02_high"));
                        l.Add(new(EndingBeat.Cairn, "end.a.03_high"));
                    }
                    else
                    {
                        l.Add(new(EndingBeat.Cairn, "end.a.02_low"));
                        l.Add(new(EndingBeat.Cairn, "end.a.03_low"));
                    }
                    l.Add(new(EndingBeat.Cairn, "end.a.04"));
                    l.Add(new(EndingBeat.Event, null));
                    l.Add(new(EndingBeat.Caption, "end.a.05"));
                    l.Add(new(EndingBeat.Epilogue, high ? "epi.a_high" : "epi.a_low"));
                    break;
                case EndingType.Restore:
                    l.Add(new(EndingBeat.Event, null));
                    l.Add(new(EndingBeat.Cairn, "end.b.01"));
                    l.Add(new(EndingBeat.Cairn, "end.b.02"));
                    l.Add(new(EndingBeat.Cairn, high ? "end.b.03_high" : "end.b.03_low"));
                    l.Add(new(EndingBeat.Caption, "end.b.04"));
                    l.Add(new(EndingBeat.Epilogue, high ? "epi.b_high" : "epi.b_low"));
                    break;
                case EndingType.Silence:
                    l.Add(new(EndingBeat.Cairn, "end.c.01"));
                    l.Add(new(EndingBeat.Cairn, "end.c.02"));
                    l.Add(new(EndingBeat.Caption, "end.c.03"));
                    l.Add(new(EndingBeat.Cairn, "end.c.04"));
                    l.Add(new(EndingBeat.Cairn, high ? "end.c.05_high" : "end.c.05_low"));
                    l.Add(new(EndingBeat.Event, null));
                    l.Add(new(EndingBeat.Caption, "end.c.06"));
                    l.Add(new(EndingBeat.Epilogue, "epi.c"));
                    break;
            }
            return l;
        }

        /// <summary>Stores the decision and marks the run as finished (read by the Ending scene and saved later).</summary>
        public static void Commit(WorldState world, EndingType type)
        {
            world.SetString(ChoiceKey, Id(type));
            world.SetBool(WorldKeys.Story("ending_reached"), true);
        }
    }
}
