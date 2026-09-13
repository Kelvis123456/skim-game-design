using System.Collections.Generic;
using UnityEngine;

// Basic i18n (fase10 §5.6): every static UI string plus the dynamic
// format strings the controllers build at runtime. Keys are grouped by
// screen; values are (es, en) pairs looked up by the current language.
public class LocalizationSystemImpl : MonoBehaviour, ILocalizationSystem
{
    const string PREF_KEY = "skim_language";

    public string Language { get; private set; } = "es";
    public event System.Action OnLanguageChanged;

    static readonly Dictionary<string, (string es, string en)> TABLE = new()
    {
        // Main menu
        ["menu.subtitle"]        = ("Una piedra. Un flick. El océano entero.", "One stone. One flick. The whole ocean."),
        ["menu.climate_actual"]  = ("CLIMA ACTUAL", "CURRENT CLIMATE"),
        ["menu.best_throw"]      = ("TU MEJOR TIRADA", "YOUR BEST THROW"),
        ["menu.record_format"]  = ("RÉCORD: {0}m", "RECORD: {0}m"),
        ["menu.launch"]          = ("LANZAR", "LAUNCH"),

        // Tabs
        ["tab.collection"]   = ("Colección", "Collection"),
        ["tab.climate"]      = ("Clima", "Climate"),
        ["tab.challenges"]   = ("Desafíos", "Challenges"),
        ["tab.achievements"] = ("Logros", "Achievements"),
        ["tab.settings"]     = ("Config", "Settings"),

        // Shared
        ["common.back"]     = ("‹ Volver", "‹ Back"),
        ["row.locked"]      = ("BLOQUEADA", "LOCKED"),
        ["row.equipped"]    = ("EQUIPADA", "EQUIPPED"),

        // Collection (stones)
        ["collection.title"]    = ("COLECCIÓN", "COLLECTION"),
        ["collection.subtitle"] = ("Cada piedra cambia el rebote y la sensibilidad al spin", "Each stone changes the rebound and spin sensitivity"),
        ["stone.desc_unlocked"] = ("Rebote: {0}  Spin: {1}  ·  Récord: {2}m", "Rebound: {0}  Spin: {1}  ·  Record: {2}m"),
        ["stone.desc_locked"]   = ("Desbloquea a {0} acumulados", "Unlocks at {0} accumulated"),
        ["stone.name.Guijarro"] = ("Guijarro", "Pebble"),
        ["stone.name.Esquisto"] = ("Esquisto", "Schist"),
        ["stone.name.Basalto"]  = ("Basalto", "Basalt"),
        ["stone.name.Cuarzo"]   = ("Cuarzo", "Quartz"),

        // Climates
        ["climate.title"]           = ("CLIMAS", "CLIMATES"),
        ["climate.unlocked_count"]  = ("{0}/{1} niveles", "{0}/{1} levels"),
        ["climate.desc_unlocked"]   = ("{0} olas  ×{1} score  ·  Récord: {2}m", "{0} waves  ×{1} score  ·  Record: {2}m"),
        ["climate.desc_locked"]     = ("Desbloquea a {0}", "Unlocks at {0}"),
        ["climate.name.Calma"]      = ("Calma", "Calm"),
        ["climate.name.Brisa"]      = ("Brisa", "Breeze"),
        ["climate.name.Viento"]     = ("Viento", "Wind"),
        ["climate.name.Marejada"]   = ("Marejada", "Swell"),
        ["climate.name.Tormenta"]   = ("Tormenta", "Storm"),

        // Settings
        ["settings.title"]          = ("CONFIGURACIÓN", "SETTINGS"),
        ["settings.audio"]          = ("AUDIO", "AUDIO"),
        ["settings.accessibility"]  = ("ACCESIBILIDAD", "ACCESSIBILITY"),
        ["settings.language"]       = ("IDIOMA", "LANGUAGE"),
        ["settings.music"]         = ("Música", "Music"),
        ["settings.effects"]        = ("Efectos", "Effects"),
        ["settings.vibration"]      = ("Vibración", "Vibration"),
        ["settings.reduce_effects"] = ("Reducir efectos", "Reduce effects"),
        ["settings.always_assist"]  = ("Asistir siempre", "Always assist"),
        ["settings.delete_data"]    = ("Eliminar datos", "Delete data"),
        ["settings.delete_title"]   = ("¿Eliminar todos tus datos?", "Delete all your data?"),
        ["settings.delete_body"]    = ("Perderás tu progreso, récords y piedras/climas desbloqueados. Esta acción no se puede deshacer.",
                                       "You'll lose your progress, records, and unlocked stones/climates. This can't be undone."),
        ["settings.cancel"]         = ("CANCELAR", "CANCEL"),
        ["settings.confirm_delete"] = ("ELIMINAR", "DELETE"),
        ["settings.lang_es"]        = ("Español", "Español"),
        ["settings.lang_en"]        = ("English", "English"),

        // Daily challenge
        ["challenge.title"]     = ("DESAFÍO DIARIO", "DAILY CHALLENGE"),
        ["challenge.completed"] = ("¡Completado!", "Completed!"),
        ["challenge.reward"]    = ("+{0} conchas", "+{0} shells"),
        ["challenge.reset"]     = ("Renueva en {0}h {1}m", "Resets in {0}h {1}m"),
        ["challenge.desc.0"]    = ("Recorre 100m en una sola tirada", "Travel 100m in a single throw"),
        ["challenge.desc.1"]    = ("Logra 15 rebotes en una sola tirada", "Land 15 skips in a single throw"),
        ["challenge.desc.2"]    = ("Acumula 300m de distancia hoy", "Accumulate 300m of distance today"),

        // Achievements
        ["achievements.title"]    = ("LOGROS", "ACHIEVEMENTS"),
        ["achievements.subtitle"] = ("Se desbloquean solos mientras juegas", "Unlock automatically as you play"),
        ["achievements.unlocked"] = ("DESBLOQUEADO", "UNLOCKED"),
        ["ach.first_throw.name"]  = ("Primer lanzamiento", "First throw"),
        ["ach.first_throw.desc"]  = ("Completa tu primera tirada", "Complete your first throw"),
        ["ach.dist_100.name"]     = ("Cien metros", "One hundred meters"),
        ["ach.dist_100.desc"]     = ("Acumula 100m de distancia total", "Accumulate 100m of total distance"),
        ["ach.dist_1000.name"]    = ("Kilómetro completo", "Full kilometer"),
        ["ach.dist_1000.desc"]    = ("Acumula 1000m de distancia total", "Accumulate 1000m of total distance"),
        ["ach.best_50.name"]      = ("Media centuria", "Half century"),
        ["ach.best_50.desc"]      = ("Logra 50m en una sola tirada", "Land 50m in a single throw"),
        ["ach.skips_15.name"]     = ("Rebotador", "Skipper"),
        ["ach.skips_15.desc"]     = ("Logra 15 rebotes en una sola tirada", "Land 15 skips in a single throw"),
        ["ach.challenges_5.name"] = ("Constante", "Consistent"),
        ["ach.challenges_5.desc"] = ("Completa 5 desafíos diarios", "Complete 5 daily challenges"),
        ["ach.all_stones.name"]   = ("Coleccionista", "Collector"),
        ["ach.all_stones.desc"]   = ("Desbloquea todas las piedras", "Unlock every stone"),
        ["ach.all_climates.name"] = ("Todoterreno", "All-terrain"),
        ["ach.all_climates.desc"] = ("Desbloquea todos los climas", "Unlock every climate"),

        // HUD
        ["hud.points_caption"]     = ("PUNTOS", "POINTS"),
        ["hud.tutorial_hint"]      = ("Arrastrá y soltá en cualquier parte de la pantalla para lanzar la piedra.\nNo hay que perder — cada tirada es un intento. ¡Superá tu récord!",
                                      "Drag and release anywhere on screen to launch the stone.\nThere's no losing — every throw is just another attempt. Beat your record!"),
        ["hud.achievement_toast"] = ("¡Logro desbloqueado! {0}", "Achievement unlocked! {0}"),

        // Post-launch
        ["postlaunch.distance_caption"] = ("DISTANCIA", "DISTANCE"),
        ["postlaunch.skips_caption"]    = ("SALTOS", "SKIPS"),
        ["postlaunch.skips_unit"]       = ("saltos", "skips"),
        ["postlaunch.new_record"]       = ("¡NUEVO RÉCORD!", "NEW RECORD!"),
        ["postlaunch.session_best"]     = ("¡MEJOR DE SESIÓN!", "SESSION BEST!"),
        ["postlaunch.retry"]            = ("OTRA VEZ", "PLAY AGAIN"),
    };

    void Awake() => Language = PlayerPrefs.GetString(PREF_KEY, "es");

    public void SetLanguage(string language)
    {
        if (language != "es" && language != "en") return;
        if (language == Language) return;
        Language = language;
        PlayerPrefs.SetString(PREF_KEY, language);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public string Get(string key)
    {
        if (TABLE.TryGetValue(key, out var pair)) return Language == "en" ? pair.en : pair.es;
        Debug.LogWarning($"[Localization] Missing key: {key}");
        return key;
    }
}
