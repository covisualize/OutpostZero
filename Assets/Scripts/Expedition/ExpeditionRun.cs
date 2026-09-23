using System;
using System.Collections.Generic;
using OutpostZero.Graphics;
using OutpostZero.Shell;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// What the street is entered with: the district, who leads, what they carry, the weather,
    /// and the world seed the street is scattered from.
    /// </summary>
    [Serializable]
    public struct ExpeditionContext
    {
        public string district;
        public string leaderId;
        public string leaderName;
        public string[] loadout;
        public WeatherKind weather;
        public int seed;
        public int day;
        public int difficulty;

        public bool Open => !string.IsNullOrEmpty(district) || !string.IsNullOrEmpty(leaderId);
    }

    public enum ExpeditionEnd
    {
        None,
        Extracted,
        Victory,
        Dragged,
        Succession,
        Wiped
    }

    /// <summary>
    /// What the street gives back to camp: how it ended, the haul against the quota, and the time out.
    /// </summary>
    [Serializable]
    public struct ExpeditionOutcome
    {
        public string district;
        public string leaderId;
        public ExpeditionEnd end;
        public int kills;
        public int killGoal;
        public int scrap;
        public int scrapGoal;
        public float seconds;
        public string[] loadout;
        public int combatShifts;
        public int scavengeShifts;
        public int combatLevel;
        public int scavengeLevel;

        public bool LeaderCameHome => end == ExpeditionEnd.Extracted || end == ExpeditionEnd.Victory || end == ExpeditionEnd.Dragged;
        public bool QuotaMet => kills >= killGoal && scrap >= scrapGoal;
    }

    public static class ExpeditionLedger
    {
        public static ExpeditionContext Open(string district, string leaderId, string leaderName, IEnumerable<string> carried, WeatherKind weather, int seed, int day, int difficulty)
        {
            return new ExpeditionContext
            {
                district = district ?? "",
                leaderId = leaderId ?? "",
                leaderName = leaderName ?? "",
                loadout = Loadout(carried),
                weather = weather,
                seed = seed,
                day = day < 1 ? 1 : day,
                difficulty = difficulty,
            };
        }

        public static string[] Loadout(IEnumerable<string> carried)
        {
            var seen = new SortedSet<string>(StringComparer.Ordinal);
            if (carried != null)
            {
                foreach (var id in carried)
                {
                    if (!string.IsNullOrEmpty(id)) seen.Add(id);
                }
            }
            var list = new string[seen.Count];
            seen.CopyTo(list);
            return list;
        }

        public static ExpeditionOutcome Close(ExpeditionContext context, ExpeditionEnd end, int kills, int killGoal, int scrap, int scrapGoal, float seconds)
        {
            return new ExpeditionOutcome
            {
                district = context.district ?? "",
                leaderId = context.leaderId ?? "",
                end = end,
                kills = kills < 0 ? 0 : kills,
                killGoal = killGoal < 1 ? 1 : killGoal,
                scrap = scrap < 0 ? 0 : scrap,
                scrapGoal = scrapGoal < 1 ? 1 : scrapGoal,
                seconds = seconds < 0f ? 0f : seconds,
                loadout = context.loadout ?? Array.Empty<string>(),
            };
        }

        /// <summary>Where the flow goes once the street ends.</summary>
        public static FlowStep After(ExpeditionEnd end)
        {
            switch (end)
            {
                case ExpeditionEnd.Extracted:
                case ExpeditionEnd.Victory:
                    return FlowStep.Results;
                case ExpeditionEnd.Dragged:
                    return FlowStep.Sanctuary;
                default:
                    return FlowStep.Results;
            }
        }

        public static string Clock(float seconds)
        {
            int whole = seconds <= 0f ? 0 : (int)seconds;
            return (whole / 60) + ":" + (whole % 60).ToString("00");
        }

        /// <summary>The line camp shows when the leader walks back in, e.g. "Back from Old Market: 12 kills, 40 scrap, 6:05 out."</summary>
        public static string CampLine(ExpeditionOutcome outcome, string language)
        {
            string place = ExtractSlip.Place(outcome.district, language);
            string text = Word("run.home", language);
            return text
                .Replace("{place}", place)
                .Replace("{kills}", outcome.kills.ToString())
                .Replace("{scrap}", outcome.scrap.ToString())
                .Replace("{time}", Clock(outcome.seconds));
        }

        public static string TimeLine(ExpeditionOutcome outcome, string language)
        {
            return Word("run.time", language) + " " + Clock(outcome.seconds);
        }

        private static string Word(string key, string language)
        {
            return string.IsNullOrEmpty(language) ? Loc.T(key) : Loc.T(key, language);
        }
    }
}
