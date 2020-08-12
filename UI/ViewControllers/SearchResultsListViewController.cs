using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMUI;
using UEImage = UnityEngine.UI.Image;
using BS_Utils.Utilities;
using EnhancedSearchAndFilters.Utilities;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    internal class SearchResultsListViewController : ViewController, TableView.IDataSource
    {
        public Action<IPreviewBeatmapLevel> SongSelected;

        private IPreviewBeatmapLevel[] _beatmapLevels = new IPreviewBeatmapLevel[0];

        private RectTransform _container;
        private TableView _tableView;
        private LevelListTableCell _tableCellInstance;

        private const string ReuseIdentifier = "EnhancedSearchListTableCell";

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                name = "SearchResultsListViewController";
                _tableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                this.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                this.rectTransform.anchorMax = new Vector2(0.5f, 1f);

                _container = new GameObject("SearchResultsListContainer", typeof(RectTransform)).transform as RectTransform;
                _container.anchorMin = Vector2.zero;
                _container.anchorMax = Vector2.one;
                _container.sizeDelta = Vector2.zero;
                _container.SetParent(this.transform, false);

                // tableview setup
                var tableViewGO = new GameObject("SearchTableView");
                tableViewGO.SetActive(false);
                tableViewGO.transform.SetParent(_container, false);
                _tableView = tableViewGO.AddComponent<TableView>();

                _tableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);
                _tableView.SetPrivateField("_isInitialized", false);
                _tableView.SetPrivateField("_hideScrollButtonsIfNotNeeded", false);

                var viewport = new GameObject("Viewport").AddComponent<RectTransform>();
                viewport.SetParent(tableViewGO.transform, false);

                viewport.gameObject.AddComponent<RectMask2D>();

                var scrollRect = tableViewGO.GetComponent<ScrollRect>();
                scrollRect.viewport = viewport;

                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.sizeDelta = Vector2.zero;
                viewport.anchoredPosition = Vector2.zero;
                (tableViewGO.transform as RectTransform).anchorMin = Vector2.zero;
                (tableViewGO.transform as RectTransform).anchorMax = Vector2.one;
                (tableViewGO.transform as RectTransform).sizeDelta = new Vector2(0f, -20f);
                (tableViewGO.transform as RectTransform).anchoredPosition = Vector2.zero;

                // page up/down button setup
                var buttonPrefab = Resources.FindObjectsOfTypeAll<NoTransitionsButton>().Where(x => x.name == "PageUpButton").Last();
                var buttonGO = Instantiate(buttonPrefab.gameObject, tableViewGO.transform, false);
                buttonGO.name = "PageUpButton";
                (buttonGO.transform as RectTransform).anchorMin = new Vector2(0f, 1f);
                (buttonGO.transform as RectTransform).anchorMax = Vector2.one;
                (buttonGO.transform as RectTransform).pivot = new Vector2(0.5f, 1f);
                (buttonGO.transform as RectTransform).anchoredPosition = Vector2.zero;
                (buttonGO.transform as RectTransform).sizeDelta = new Vector2(0f, 10.25f);
                (buttonGO.transform as RectTransform).localRotation = Quaternion.Euler(0f, 0f, 180f);
                _tableView.SetPrivateField("_pageUpButton", buttonGO.GetComponent<NoTransitionsButton>());
                buttonGO.SetActive(true);

                buttonGO = Instantiate(buttonPrefab.gameObject, tableViewGO.transform, false);
                buttonGO.name = "PageDownButton";
                (buttonGO.transform as RectTransform).anchorMin = Vector2.zero;
                (buttonGO.transform as RectTransform).anchorMax = new Vector2(1f, 0f);
                (buttonGO.transform as RectTransform).pivot = new Vector2(0.5f, 1f);
                (buttonGO.transform as RectTransform).anchoredPosition = Vector2.zero;
                (buttonGO.transform as RectTransform).sizeDelta = new Vector2(0f, 10.25f);
                (buttonGO.transform as RectTransform).localRotation = Quaternion.Euler(0f, 0f, 0f);
                _tableView.SetPrivateField("_pageDownButton", buttonGO.GetComponent<NoTransitionsButton>());
                buttonGO.SetActive(true);

                _tableView.dataSource = this;
                _tableView.didSelectCellWithIdxEvent += RowSelected;
                tableViewGO.SetActive(true);

                // fix for autoscrolling when a gamepad is attached
               scrollRect.vertical = false;
            }

            UpdateSize();
        }

        public void UpdateSize()
        {
            if (!this.isActivated)
                return;

            if (PluginConfig.SearchKeyboard == SearchKeyboardType.Compact)
            {
                this.rectTransform.sizeDelta = new Vector2(60f, 0f);

                _container.sizeDelta = Vector2.zero;
                _container.anchoredPosition = new Vector2(-5f, 0f);
            }
            else
            {
                this.rectTransform.sizeDelta = new Vector2(70f, 0f);

                _container.sizeDelta = new Vector2(-5f, 0f);
                _container.anchoredPosition = Vector2.zero;
            }
        }

        public void UpdateSongs(IPreviewBeatmapLevel[] beatmapLevels)
        {
            if (beatmapLevels == null)
                beatmapLevels = Array.Empty<IPreviewBeatmapLevel>();

            _beatmapLevels = beatmapLevels;
            if (this.isActivated)
            {
                _tableView.ReloadData();
                if (beatmapLevels.Length != 0)
                    _tableView.ScrollToCellWithIdx(0, TableViewScroller.ScrollPositionType.Beginning, false);
            }
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
                _tableView.ClearSelection();
        }

        public TableCell CellForIdx(TableView tableView, int idx)
        {
            // adapted from CustomUI's CustomListViewController
            LevelListTableCell tableCell = (LevelListTableCell)_tableView.DequeueReusableCellForIdentifier(ReuseIdentifier);

            if (tableCell == null)
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
                tableCell.SetPrivateField("_beatmapCharacteristicImages", new UEImage[0]);
                tableCell.transform.Find("FavoritesIcon").gameObject.SetActive(false);
                tableCell.reuseIdentifier = ReuseIdentifier;
            }

            IPreviewBeatmapLevel level = _beatmapLevels[idx];

            string contributorsText = "";
            if (!string.IsNullOrEmpty(level.levelAuthorName))
            {
                contributorsText = $"[{level.levelAuthorName}]";
                contributorsText = (string.IsNullOrEmpty(level.songAuthorName) ? "" : " ") + contributorsText;
            }

            tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText").text = $"{level.songName.EscapeTextMeshProTags()} <size=80%>{level.songSubName.EscapeTextMeshProTags()}</size>";
            tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = $"{level.songAuthorName.EscapeTextMeshProTags()}<size=80%>{contributorsText.EscapeTextMeshProTags()}</size>";
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

        public int NumberOfCells()
        {
            return _beatmapLevels.Length;
        }

        public float CellSize()
        {
            return 8f;
        }
    }
}
