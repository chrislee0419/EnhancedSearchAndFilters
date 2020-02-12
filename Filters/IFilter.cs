﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        /// Convert the currently applied values to strings and stored them as key-value pairs. 
        /// Both the key and value should only contain alphanumeric characters.
        /// </summary>
        /// <returns>A List of FilterSettingsKeyValuePairs that represent the applied values.</returns>
        List<FilterSettingsKeyValuePair> GetAppliedValuesAsPairs();

        /// <summary>
        /// Set the staging values according to key-value pairs.
        /// </summary>
        /// <param name="settingsList">A string that represents the values to be staged to this filter's settings.</param>
        void SetStagingValuesFromPairs(List<FilterSettingsKeyValuePair> settingsList);
    }

    public enum FilterStatus
    {
        NotApplied,
        NotAppliedAndChanged,
        Applied,
        AppliedAndChanged
    }

    public class FilterSettingsKeyValuePair
    {
        private string _key;
        public string Key
        {
            get => _key;
            set
            {
                if (string.IsNullOrEmpty(value) || !AlphanumericRegex.IsMatch(value))
                    throw new ArgumentException($"The key of a filter setting should only contain alphanumeric characters (got '{value ?? "null"}').");

                _key = value;
            }
        }

        private string _value;
        public string Value {
            get => _value;
            set
            {
                if (string.IsNullOrEmpty(value) || !AlphanumericRegex.IsMatch(value))
                    throw new ArgumentException($"The value of a filter setting should only contain alphanumeric characters (got '{value ?? "null"}').");

                _value = value;
            }
        }

        public static readonly Regex AlphanumericRegex = new Regex("^[A-Za-z0-9]+$");
        public const char SeparatorCharacter = ':';

        public FilterSettingsKeyValuePair(string key, object value)
        {
            Key = key;
            Value = value.ToString();
        }

        public FilterSettingsKeyValuePair(string key, string value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// Create a List of key-value pairs that represent a filter's settings and their associated values.
        /// </summary>
        /// <param name="items">A list of keys (string) and their associated values (objects). 
        /// The parameters should be provided so that each key string is followed immediately by its associated value.</param>
        /// <returns>A list of FilterSettingsKeyValuePair objects.</returns>
        public static List<FilterSettingsKeyValuePair> CreateFilterSettingsList(params object[] items)
        {
            List<FilterSettingsKeyValuePair> settingsList = new List<FilterSettingsKeyValuePair>(items.Length / 2);

            for (int k = 0, v = 1; k < items.Length && v < items.Length; k += 2, v += 2)
                settingsList.Add(new FilterSettingsKeyValuePair(items[k].ToString(), items[v]));

            return settingsList;
        }

        public override string ToString() => $"{Key}{SeparatorCharacter}{Value}";

        /// <summary>
        /// Parse a string to a FilterSettingsKeyValuePair object.
        /// </summary>
        /// <param name="kvPairString">String to parse.</param>
        /// <returns>A FilterSettingsKeyValuePair object.</returns>
        public static FilterSettingsKeyValuePair FromString(string kvPairString)
        {
            if (string.IsNullOrEmpty(kvPairString))
                return null;

            string[] pair = kvPairString.Split(SeparatorCharacter);

            if (pair.Length != 2)
                return null;
            else
                return new FilterSettingsKeyValuePair(pair[0], pair[1]);
        }
    }
}
