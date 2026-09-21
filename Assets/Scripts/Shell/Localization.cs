using System.Collections.Generic;
using OutpostZero.Core;

namespace OutpostZero.Shell
{
    public static class Loc
    {
        private static readonly Dictionary<string, string> english = new Dictionary<string, string>
        {
            { "hud.expedition", "OUTPOST ZERO — EXPEDITION" },
            { "hud.health", "Health" },
            { "hud.stamina", "Stamina" },
            { "hud.extract", "Reach the sanctuary gate to extract" },
            { "menu.pause", "PAUSED" },
            { "menu.resume", "Resume" },
            { "menu.save", "Save" },
            { "menu.camp", "Sanctuary" },
            { "menu.settings", "Settings" },
            { "menu.main", "Main Menu" },
            { "camp.title", "SANCTUARY" },
            { "result.title", "EXPEDITION COMPLETE" },
            { "gameover.title", "THE OUTPOST FALLS" }
        };

        private static readonly Dictionary<string, string> spanish = new Dictionary<string, string>
        {
            { "hud.expedition", "PUESTO CERO — EXPEDICIÓN" },
            { "hud.health", "Salud" },
            { "hud.stamina", "Aguante" },
            { "hud.extract", "Llega a la puerta del santuario para extraer" },
            { "menu.pause", "PAUSA" },
            { "menu.resume", "Continuar" },
            { "menu.save", "Guardar" },
            { "menu.camp", "Santuario" },
            { "menu.settings", "Ajustes" },
            { "menu.main", "Menú principal" },
            { "camp.title", "SANTUARIO" },
            { "result.title", "EXPEDICIÓN COMPLETA" },
            { "gameover.title", "EL PUESTO CAE" }
        };

        public static string T(string key)
        {
            string language = SettingsService.Instance != null ? SettingsService.Instance.Language : "en";
            var table = language == "es" ? spanish : english;
            return table.TryGetValue(key, out var value) ? value : key;
        }
    }
}
