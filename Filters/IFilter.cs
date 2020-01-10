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

        /// <summary>
        /// Initialize the view associated with this filter.
        /// </summary>
        /// <param name="viewContainer">The parent GameObject that will contain the view for this filter.</param>
        void Init(GameObject viewContainer);

        /// <summary>
        /// Destroy the view and other created objects associated with this filter.
        /// </summary>
        void Cleanup();

        /// <summary>
        /// Gets the GameObject which represents the user interface view.
        /// </summary>
        /// <returns>A GameObject that contains all the controls for this filter.</returns>
        GameObject GetView();

        void SetDefaultValuesToStaging();
        void SetAppliedValuesToStaging();
        void ApplyStagingValues();
        void ApplyDefaultValues();

        /// <summary>
        /// Filters the provided list according to the applied settings of this filter.
        /// </summary>
        /// <param name="detailsList">A reference to a list of songs to filter.</param>
        void FilterSongList(ref List<SongData.BeatmapDetails> detailsList);

        /// <summary>
        /// Serialize the applied values to a string.
        /// </summary>
        /// <returns>A string that represents the applied values.</returns>
        string SerializeFromAppliedValues();

        /// <summary>
        /// Set the staging values according to a serialized string.
        /// </summary>
        /// <param name="serializedSettings">A string that represents the values to be staged to this filter's settings.</param>
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
