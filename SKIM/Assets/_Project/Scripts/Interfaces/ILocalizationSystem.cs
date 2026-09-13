public interface ILocalizationSystem
{
    string Language { get; }
    void SetLanguage(string language);
    string Get(string key);
    event System.Action OnLanguageChanged;
}
