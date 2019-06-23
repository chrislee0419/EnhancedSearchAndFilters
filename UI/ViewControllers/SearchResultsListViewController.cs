using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMUI;
using CustomUI.BeatSaber;
using CustomUI.Utilities;

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

            if (firstActivation)
            {
                // make the list view narrower and with a slight rightward bias
                // to fit details view controller and back button
                this.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                this.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                this.rectTransform.sizeDelta = new Vector2(74f, 0f);
                this.rectTransform.pivot = new Vector2(0.4f, 0.5f);

                this.DidSelectRowEvent += RowSelected;
            }
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

            string contributorsText = "";
            if (!string.IsNullOrEmpty(level.levelAuthorName))
            {
                contributorsText = $"[{level.levelAuthorName}]";
                contributorsText = (string.IsNullOrEmpty(level.songAuthorName) ? "" : " ") + contributorsText;
            }

            tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText").text = $"{level.songName} <size=80%>{level.songSubName}</size>";
            tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = $"{level.songAuthorName}<size=80%>{contributorsText}</size>";
            tableCell.SetPrivateField("_bought", true);

            // black placeholder image
            RawImage coverImage = tableCell.GetPrivateField<RawImage>("_coverRawImage");
            coverImage.texture = Texture2D.blackTexture;
            coverImage.color = Color.black;

            SetBaseGameCoverImageAsync(tableCell, level);

            return tableCell;
        }

        private async void SetBaseGameCoverImageAsync(TableCell tableCell, IPreviewBeatmapLevel level)
        {
            RawImage coverImage = tableCell.GetPrivateField<RawImage>("_coverRawImage");
            CancellationToken token = new CancellationToken();

            try
            {
                Texture2D texture = await level.GetCoverImageTexture2DAsync(token);
                coverImage.texture = texture;
                coverImage.color = Color.white;
            }
            catch (OperationCanceledException) { }
        }

        public override int NumberOfCells()
        {
            return _beatmapLevels.Length;
        }
    }
}
