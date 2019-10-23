using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMUI;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using UEImage = UnityEngine.UI.Image;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    // NOTE: some of the fields included in CustomListViewController are not used
    class SearchResultsListViewController : CustomListViewController
    {
        public Action<IPreviewBeatmapLevel> SongSelected;

        private IPreviewBeatmapLevel[] _beatmapLevels = new IPreviewBeatmapLevel[0];

        private LevelListTableCell _tableCellInstance;

        public new string reuseIdentifier = "SearchListTableCell";

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            base.DidActivate(firstActivation, activationType);

            if (firstActivation)
            {
                _tableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                // fix for autoscrolling when a gamepad is attached
                this.GetComponentInChildren<ScrollRect>().vertical = false;

                this.DidSelectRowEvent += RowSelected;
            }

            UpdateSize();
        }

        public void UpdateSize()
        {
            if (!this.isActivated)
                return;

            var container = (this.transform.Find("CustomListContainer") as RectTransform);
            if (PluginConfig.CompactSearchMode)
            {
                this.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                this.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                this.rectTransform.sizeDelta = new Vector2(55f, 0f);
                this.rectTransform.pivot = new Vector2(0.45f, 0.5f);

                container.sizeDelta = new Vector2(50f, 0f);
            }
            else
            {
                // make the list view narrower and with a slight rightward bias
                // to fit details view controller and back button
                this.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                this.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                this.rectTransform.sizeDelta = new Vector2(70f, 0f);
                this.rectTransform.pivot = new Vector2(0.45f, 0.5f);

                container.sizeDelta = new Vector2(60f, 0f);
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

        /// <summary>
        /// Used when the "Display Keyboard" button on the SongDetailsViewController is pressed, 
        /// so the player will be able to select the same song and have the details show up.
        /// </summary>
        public void DeselectSong()
        {
            if (this.isActivated)
                _customListTableView.ClearSelection();
        }

        public override TableCell CellForIdx(TableView tableView, int idx)
        {
            // adapted from CustomUI's CustomListViewController
            LevelListTableCell tableCell = (LevelListTableCell)_customListTableView.DequeueReusableCellForIdentifier(reuseIdentifier);

            if (!tableCell)
            {
                tableCell = Instantiate(_tableCellInstance);

                // force text to take up full width (minus cover image)
                var cellText = tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText");
                cellText.rectTransform.sizeDelta += new Vector2(14f, 0f);
                cellText.rectTransform.anchoredPosition += new Vector2(7f, 0f);
                cellText = tableCell.GetPrivateField<TextMeshProUGUI>("_authorText");
                cellText.rectTransform.sizeDelta += new Vector2(12f, 0f);
                cellText.rectTransform.anchoredPosition += new Vector2(6f, 0f);

                foreach (UEImage i in tableCell.GetPrivateField<UEImage[]>("_beatmapCharacteristicImages"))
                    i.enabled = false;
                tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
                tableCell.SetPrivateField("_beatmapCharacteristicImages", new UEImage[0]);
                tableCell.reuseIdentifier = reuseIdentifier;
            }

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

            Texture2D texture = await level.GetCoverImageTexture2DAsync(CancellationToken.None);
            coverImage.texture = texture;
            coverImage.color = Color.white;
        }

        public override int NumberOfCells()
        {
            return _beatmapLevels.Length;
        }
    }
}
