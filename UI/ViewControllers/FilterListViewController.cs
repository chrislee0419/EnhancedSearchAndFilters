using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UEImage = UnityEngine.UI.Image;
using TMPro;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using EnhancedSearchAndFilters.Filters;
using HMUI;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    class FilterListViewController : CustomListViewController
    {
        public Action<IFilter, IFilter> FilterSelected;

        public List<IFilter> FilterList { get; private set; } = new List<IFilter>();
        public int CurrentRow { get; private set; } = 0;
        public IFilter CurrentFilter { get { return FilterList[CurrentRow]; } }

        public new string reuseIdentifier = "FilterListTableCell";

        private LevelListTableCell _songListTableCellInstance;

        private static readonly Color DefaultFilterColor = new Color(1f, 0.2f, 0.2f);
        private static readonly Color PendingFilterColor = new Color(1f, 1f, 0f);
        private static readonly Color AppliedFilterColor = new Color(0.2f, 1f, 0.2f);
        private static readonly Color AppliedPendingFilterColor = new Color(0.2f, 0.5f, 1f);

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation)
            {
                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));
            }

            base.DidActivate(firstActivation, type);

            if (firstActivation)
            {
                RectTransform rt = this._customListTableView.transform as RectTransform;
                rt.sizeDelta = Vector2.zero;
                (rt.parent as RectTransform).sizeDelta = new Vector2(50f, 45f);

                rt = rt.GetComponent<RectMask2D>().rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;

                rt = this._pageUpButton.transform as RectTransform;
                rt.SetParent(this.transform, false);
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 0f);       // for whatever reason, the pivot on this button is upside down (rotated 180 degrees? vertically scaled by -1?)
                rt.sizeDelta = new Vector2(40f, 6f);
                rt.anchoredPosition = Vector2.zero;

                rt = this._pageDownButton.transform as RectTransform;
                rt.SetParent(this.transform, false);
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(40f, 6f);
                rt.anchoredPosition = Vector2.zero;

                this.DidSelectRowEvent += RowSelected;
            }
        }

        public void RefreshTable(bool useCallback = false)
        {
            _customListTableView.ReloadData();

            // since ReloadData() clears the cell selection, we re-select the current row (callback disabled)
            _customListTableView.SelectCellWithIdx(CurrentRow, useCallback);
        }

        private void RowSelected(TableView unused, int idx)
        {
            int oldIdx = CurrentRow;
            CurrentRow = idx;

            FilterSelected?.Invoke(FilterList[oldIdx], FilterList[idx]);
        }

        public override TableCell CellForIdx(int idx)
        {
            // adapted from CustomUI's CustomListViewController
            LevelListTableCell tableCell = (LevelListTableCell)_customListTableView.DequeueReusableCellForIdentifier(reuseIdentifier);
            TextMeshProUGUI cellText;
            UEImage statusImg;

            if (!tableCell)
            {
                tableCell = Instantiate(_songListTableCellInstance);

                // remove unused elements
                Destroy(tableCell.GetPrivateField<RawImage>("_coverRawImage").gameObject);
                Destroy(tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").gameObject);

                cellText = tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText");
                cellText.fontSize = 5f;
                cellText.alignment = TextAlignmentOptions.Left;
                cellText.rectTransform.anchorMin = new Vector2(0.15f, 0f);
                cellText.rectTransform.anchorMax = Vector2.one;
                cellText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                cellText.rectTransform.sizeDelta = Vector2.zero;
                cellText.rectTransform.anchoredPosition = Vector2.zero;

                UEImage bgImg = tableCell.GetComponentsInChildren<UEImage>().First(x => x.name == "BG");
                bgImg.rectTransform.anchorMin = Vector2.zero;
                bgImg.rectTransform.anchorMax = Vector2.one;
                bgImg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                bgImg.rectTransform.anchoredPosition = Vector2.zero;
                bgImg.rectTransform.sizeDelta = Vector2.zero;
                UEImage highlightImg = tableCell.GetComponentsInChildren<UEImage>().First(x => x.name == "Highlight");
                highlightImg.rectTransform.anchorMin = Vector2.zero;
                highlightImg.rectTransform.anchorMax = Vector2.one;
                highlightImg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                highlightImg.rectTransform.anchoredPosition = Vector2.zero;
                highlightImg.rectTransform.sizeDelta = Vector2.zero;

                statusImg = new GameObject("StatusImage").AddComponent<UEImage>();
                statusImg.transform.SetParent(tableCell.transform, false);
                statusImg.rectTransform.anchorMin = Vector2.zero;
                statusImg.rectTransform.anchorMax = new Vector2(0.03f, 1f);
                statusImg.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                statusImg.rectTransform.sizeDelta = new Vector2(0.5f, 0.5f);
                statusImg.rectTransform.anchoredPosition = Vector2.zero;
                statusImg.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
                statusImg.color = DefaultFilterColor;

                foreach (UEImage i in tableCell.GetPrivateField<UEImage[]>("_beatmapCharacteristicImages"))
                    i.enabled = false;
                tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
                tableCell.SetPrivateField("_beatmapCharacteristicImages", new UEImage[0]);
                tableCell.reuseIdentifier = reuseIdentifier;
            }

            IFilter filter = FilterList[idx];
            cellText = tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText");
            statusImg = tableCell.GetComponentsInChildren<UEImage>().First(x => x.name == "StatusImage");

            cellText.text = filter.FilterName;

            if (filter.Status == FilterStatus.NotAppliedAndDefault)
                statusImg.color = DefaultFilterColor;
            else if (filter.Status == FilterStatus.NotAppliedAndChanged)
                statusImg.color = PendingFilterColor;
            else if (filter.Status == FilterStatus.AppliedAndChanged)
                statusImg.color = AppliedPendingFilterColor;
            else
                statusImg.color = AppliedFilterColor;

            return tableCell;
        }

        public override int NumberOfCells()
        {
            return FilterList.Count;
        }
    }
}
