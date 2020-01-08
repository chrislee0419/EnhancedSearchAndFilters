using BeatSaberMarkupLanguage.Attributes;

namespace EnhancedSearchAndFilters.UI
{
    internal class SettingsMenu : PersistentSingleton<SettingsMenu>
    {
        [UIValue("disable-search")]
        public bool DisableSearch
        {
            get => PluginConfig.DisableSearch;
            set => PluginConfig.DisableSearch = value;
        }

        [UIValue("disable-filters")]
        public bool DisableFilters
        {
            get => PluginConfig.DisableFilters;
            set => PluginConfig.DisableFilters = value;
        }
    }
}
