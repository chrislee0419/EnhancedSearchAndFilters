using System;
using System.Collections.Generic;

namespace EnhancedSearchAndFilters.Filters
{
    public interface IFilter
    {
        string FilterName { get; }
        bool IsAvailable { get; }
        FilterStatus Status { get; }
        bool ApplyFilter { get; set; }
        FilterControl[] Controls { get; }

        event Action SettingChanged;

        void Init();

        void SetDefaultValues();
        void ResetValues();

        void FilterSongList(ref List<SongData.BeatmapDetails> detailsList);
    }

    public enum FilterStatus
    {
        NotAppliedAndDefault,
        NotAppliedAndChanged,
        Applied,
        AppliedAndChanged
    }
}
