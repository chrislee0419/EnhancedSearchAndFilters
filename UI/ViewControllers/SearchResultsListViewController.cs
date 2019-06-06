using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMUI;
using CustomUI.BeatSaber;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    // NOTE: some of the fields included in CustomListViewController are not used
    class SearchResultsListViewController : CustomListViewController
    {
        public Action<IPreviewBeatmapLevel> SongSelected;

        private IPreviewBeatmapLevel[] _beatmapLevels = new IPreviewBeatmapLevel[0];

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            base.DidActivate(firstActivation, activationType);

            // make the list view narrower and with a slight rightward bias
            // to fit details view controller and back button
            this.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            this.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            this.rectTransform.sizeDelta = new Vector2(74f, 0f);
            this.rectTransform.pivot = new Vector2(0.4f, 0.5f);

            this.DidSelectRowEvent += RowSelected;
        }

        public void UpdateSongs(IPreviewBeatmapLevel[] beatmapLevels)
        {
            _beatmapLevels = beatmapLevels;
            if (this.isActivated)
                _customListTableView.ReloadData();
        }

        private void RowSelected(TableView unused, int row)
        {
            SongSelected?.Invoke(_beatmapLevels[row]);
        }

        public override TableCell CellForIdx(int idx)
        {
            LevelListTableCell tableCell = GetTableCell();

            IPreviewBeatmapLevel level = _beatmapLevels[idx];

            tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText").text = $"{level.songName} <size=80%>{level.songSubName}</size>";
            tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = level.songAuthorName;
            tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
            tableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);
            tableCell.SetPrivateField("_bought", true);

            if (level is CustomLevel)
            {
                CustomLevel customLevel = level as CustomLevel;
                RawImage coverImage = tableCell.GetPrivateField<RawImage>("_coverRawImage");

                // can't check if the cover image is the same as the CustomSongsIcon, since it is internal to SongLoaderPlugin
                // but since loading cover images from disk the first time also stores it in memory for future use
                // and a player will probably eventually see the image, might as well just load it every time without the check
                SongLoader.LoadSprite($"{customLevel.customSongInfo.path}/{customLevel.customSongInfo.coverImagePath}", customLevel);

                coverImage.texture = customLevel.coverImageTexture2D;
                coverImage.color = Color.white;
                tableCell.SetPrivateField("_coverRawImage", coverImage);
            }
            else
            {
                SetBaseGameCoverImageAsync(tableCell, level);

                //tableCell.SetDataFromLevelAsync(_beatmapLevels[idx]);
            }

            return tableCell;
        }

        private async void SetBaseGameCoverImageAsync(TableCell tableCell, IPreviewBeatmapLevel level)
        {
            RawImage coverImage = tableCell.GetPrivateField<RawImage>("_coverRawImage");
            CancellationToken token = new CancellationToken();

            Texture2D texture = await level.GetCoverImageTexture2DAsync(token);
            coverImage.texture = texture;
            coverImage.color = Color.white;

            tableCell.SetPrivateField("_coverRawImage", coverImage);
        }

        public override int NumberOfCells()
        {
            return _beatmapLevels.Length;
        }
    }
}
