using UnityEngine;

namespace OutpostZero.Combat
{
    /// <summary>
    /// Counts live combat flashes so a leak shows on the watch page.
    /// Returning more than were borrowed does not go below zero.
    /// </summary>
    public static class VfxLedger
    {
        public static int Live { get; private set; }
        public static int Peak { get; private set; }
        public static int Born { get; private set; }

        public static void Reset()
        {
            Live = 0;
            Peak = 0;
            Born = 0;
        }

        public static void Borrow()
        {
            Born++;
            Live++;
            if (Live > Peak) Peak = Live;
        }

        public static void Return()
        {
            if (Live > 0) Live--;
        }

        public static string Line()
        {
            return Line("en");
        }

        public static string Line(string language)
        {
            string live = string.IsNullOrEmpty(language) ? OutpostZero.Shell.Loc.T("watch.vfx") : OutpostZero.Shell.Loc.T("watch.vfx", language);
            string peak = string.IsNullOrEmpty(language) ? OutpostZero.Shell.Loc.T("watch.peak") : OutpostZero.Shell.Loc.T("watch.peak", language);
            return live + " " + Live + "  " + peak + " " + Peak;
        }
    }

    public class VfxSeat : MonoBehaviour
    {
        private void OnEnable()
        {
            VfxLedger.Borrow();
        }

        private void OnDestroy()
        {
            VfxLedger.Return();
        }
    }
}
