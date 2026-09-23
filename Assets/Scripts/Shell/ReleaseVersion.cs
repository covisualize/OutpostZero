using System;
using System.Text.RegularExpressions;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Semantic version stamped into players: tags are "v1.2.3" or "v1.2.3-rc.1"; dev builds append "+sha".
    /// </summary>
    public struct ReleaseVersion : IComparable<ReleaseVersion>
    {
        private static readonly Regex Pattern = new Regex(@"^v?(\d+)\.(\d+)\.(\d+)(?:-([0-9A-Za-z.-]+))?(?:\+([0-9A-Za-z.-]+))?$");

        public int Major;
        public int Minor;
        public int Patch;
        public string Pre;
        public string Meta;

        public bool IsPrerelease => !string.IsNullOrEmpty(Pre);

        public static bool TryParse(string text, out ReleaseVersion version)
        {
            version = default;
            if (string.IsNullOrEmpty(text)) return false;
            var match = Pattern.Match(text.Trim());
            if (!match.Success) return false;
            version = new ReleaseVersion
            {
                Major = int.Parse(match.Groups[1].Value),
                Minor = int.Parse(match.Groups[2].Value),
                Patch = int.Parse(match.Groups[3].Value),
                Pre = match.Groups[4].Success ? match.Groups[4].Value : "",
                Meta = match.Groups[5].Success ? match.Groups[5].Value : "",
            };
            return true;
        }

        /// <summary>
        /// The version a build carries. A release takes the tag as it is. Anything else is a dev build of the
        /// tag (or the fallback when there is no tag) with "-dev" and the short commit.
        /// </summary>
        public static ReleaseVersion Stamp(string tag, string fallback, string sha, bool release)
        {
            if (!TryParse(tag, out var version) && !TryParse(fallback, out version)) version = new ReleaseVersion();
            version.Meta = "";
            string shortSha = Short(sha);
            if (!release)
            {
                if (string.IsNullOrEmpty(version.Pre)) version.Pre = "dev";
                version.Meta = shortSha;
            }
            return version;
        }

        public static string Short(string sha)
        {
            if (string.IsNullOrEmpty(sha)) return "";
            string clean = sha.Trim();
            return clean.Length > 7 ? clean.Substring(0, 7) : clean;
        }

        /// <summary>The numeric part only, which is what platform bundle versions accept.</summary>
        public string Core => Major + "." + Minor + "." + Patch;

        public override string ToString()
        {
            string text = Core;
            if (!string.IsNullOrEmpty(Pre)) text += "-" + Pre;
            if (!string.IsNullOrEmpty(Meta)) text += "+" + Meta;
            return text;
        }

        public string Json(string sha, string unity, string channel)
        {
            return "{\n"
                + "  \"version\": \"" + Escape(ToString()) + "\",\n"
                + "  \"commit\": \"" + Escape(Short(sha)) + "\",\n"
                + "  \"unity\": \"" + Escape(unity ?? "") + "\",\n"
                + "  \"channel\": \"" + Escape(channel ?? "") + "\"\n"
                + "}\n";
        }

        /// <summary>SemVer precedence: a prerelease sorts below its release, and build metadata is ignored.</summary>
        public int CompareTo(ReleaseVersion other)
        {
            int c = Major.CompareTo(other.Major);
            if (c == 0) c = Minor.CompareTo(other.Minor);
            if (c == 0) c = Patch.CompareTo(other.Patch);
            if (c != 0) return c;
            bool mine = string.IsNullOrEmpty(Pre);
            bool theirs = string.IsNullOrEmpty(other.Pre);
            if (mine && theirs) return 0;
            if (mine) return 1;
            if (theirs) return -1;
            return ComparePre(Pre, other.Pre);
        }

        private static int ComparePre(string a, string b)
        {
            var left = a.Split('.');
            var right = b.Split('.');
            int n = Math.Min(left.Length, right.Length);
            for (int i = 0; i < n; i++)
            {
                bool ln = int.TryParse(left[i], out int li);
                bool rn = int.TryParse(right[i], out int ri);
                int c;
                if (ln && rn) c = li.CompareTo(ri);
                else if (ln) c = -1;
                else if (rn) c = 1;
                else c = string.CompareOrdinal(left[i], right[i]);
                if (c != 0) return c;
            }
            return left.Length.CompareTo(right.Length);
        }

        private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
