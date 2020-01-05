using System;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedSearchAndFilters.Filters
{
    public interface IFilter
    {
        string Name { get; }
        bool IsAvailable { get; }
        FilterStatus Status { get; }
        bool IsFilterApplied { get; }
        bool HasChanges { get; }
        bool IsStagingDefaultValues { get; }

        event Action SettingChanged;

        void Init(GameObject viewContainer);
        GameObject GetView();

        void SetDefaultValuesToStaging();
        void SetAppliedValuesToStaging();
        void ApplyStagingValues();
        void ApplyDefaultValues();

        void FilterSongList(ref List<SongData.BeatmapDetails> detailsList);

        string SerializeFromStaging();
        void DeserializeToStaging(string serializedSettings);
    }

    public enum FilterStatus
    {
        NotApplied,
        NotAppliedAndChanged,
        Applied,
        AppliedAndChanged
    }
}
