using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRUI;
using CustomUI.BeatSaber;
using CustomUI.Utilities;
using EnhancedSearchAndFilters.SongData;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    class SongDetailsViewController : VRUIViewController
    {
        public Action<IPreviewBeatmapLevel> SelectButtonPressed;
        public Action CompactKeyboardButtonPressed;

        private StandardLevelDetailView _standardLevelDetailView;
        private LevelParamsPanel _levelParamsPanel;
        private TextMeshProUGUI _songNameText;
        private TextMeshProUGUI _detailsText;
        private Button _compactKeyboardButton;

        private static readonly string[] _difficultyStrings = new string[] { "Easy", "Normal", "Hard", "Expert", "Expert+" };
        private static readonly Color _checkmarkColor = new Color(0.8f, 1f, 0.8f);
        private static readonly Color _crossColor = new Color(1f, 0.8f, 0.8f);
        private static Sprite _checkmarkSprite;
        private static Sprite _crossSprite;
        private static Sprite _blankSprite;

        private Dictionary<string, Tuple<TextMeshProUGUI, Image>> _difficultyElements = new Dictionary<string, Tuple<TextMeshProUGUI, Image>>();

        private IPreviewBeatmapLevel _level;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {
                var referenceViewController = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First();
                StandardLevelDetailView reference = referenceViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView");
                RectTransform referenceParent = reference.transform.parent as RectTransform;

                this.rectTransform.anchorMin = referenceParent.anchorMin;
                this.rectTransform.anchorMax = referenceParent.anchorMax;
                this.rectTransform.anchoredPosition = Vector2.zero;
                this.rectTransform.sizeDelta = new Vector2(80f, 0f);

                _standardLevelDetailView = Instantiate(reference, this.transform, false);
                _standardLevelDetailView.gameObject.SetActive(true);
                _standardLevelDetailView.name = "SearchResultLevelDetail";

                _levelParamsPanel = _standardLevelDetailView.GetPrivateField<LevelParamsPanel>("_levelParamsPanel");
                _songNameText = _standardLevelDetailView.GetPrivateField<TextMeshProUGUI>("_songNameText");

                _checkmarkSprite = UIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.checkmark.png");
                _crossSprite = UIUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.cross.png");
                _blankSprite = Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);

                RemoveCustomUIElements(this.rectTransform);
                RemoveSongRequirementsButton();
                ModifyPanelElements();
                ModifyTextElements();
                ModifySelectionElements();
            }
            else
            {
                // stats panel gets disabled when in party mode, so re-enable it here just in case
                RectTransform statsPanel = _standardLevelDetailView.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Stats");
                statsPanel.gameObject.SetActive(true);
                _compactKeyboardButton.gameObject.SetActive(PluginConfig.CompactSearchMode);

                // strings get reset, so they have to be reapplied
                foreach (var str in _difficultyStrings)
                {
                    _difficultyElements[str].Item1.text = str;
                }
            }
        }

        public void SetContent(IPreviewBeatmapLevel level)
        {
            _level = level;

            _songNameText.text = level.songName;
            _levelParamsPanel.bpm = level.beatsPerMinute;
            _standardLevelDetailView.SetTextureAsync(level);
            _levelParamsPanel.duration = level.songDuration;
            _detailsText.text = "";

            foreach (var tuple in _difficultyElements.Values)
            {
                tuple.Item2.sprite = _blankSprite;
                tuple.Item2.color = Color.black;
            }

            if (level is CustomPreviewBeatmapLevel)
            {
#pragma warning disable CS4014
                BeatmapDetailsLoader.Instance.LoadSingleBeatmapAsync(level as CustomPreviewBeatmapLevel,
                    delegate (IBeatmapLevel beatmapLevel)
                    {
                        if (beatmapLevel != null)
                            SetOtherSongDetails(beatmapLevel);
                    });
#pragma warning restore CS4014
            }
            else if (level is IBeatmapLevel)
            {
                SetOtherSongDetails((level as IBeatmapLevel), false);
            }
            else
            {
                foreach (var tuple in _difficultyElements.Values)
                {
                    tuple.Item2.sprite = _crossSprite;
                    tuple.Item2.color = _crossColor;
                }

                _detailsText.text = ": DLC song not yet purchased";
            }
        }

        /// <summary>
        /// Sets the duration and difficulty icons on the UI.
        /// </summary>
        /// <param name="difficultySets"></param>
        private void SetOtherSongDetails(IBeatmapLevel beatmapLevel, bool setDuration=true)
        {
            IBeatmapLevelData beatmapLevelData = beatmapLevel.beatmapLevelData;
            bool hasLightshow = false;

            if (setDuration && beatmapLevelData.audioClip != null)
                _levelParamsPanel.duration = beatmapLevelData.audioClip.length;

            foreach (var tuple in _difficultyElements.Values)
            {
                tuple.Item2.sprite = _crossSprite;
                tuple.Item2.color = _crossColor;
            }

            foreach (var dbs in beatmapLevelData.difficultyBeatmapSets)
            {
                // the colon character looks like a bullet in-game (for whatever reason), so we use that here
                if (dbs.beatmapCharacteristic.serializedName == "LEVEL_ONE_SABER")
                    _detailsText.text += ": Has 'One Saber' mode\n";
                else if (dbs.beatmapCharacteristic.serializedName == "LEVEL_NO_ARROWS")
                    _detailsText.text += ": Has 'No Arrows' mode\n";

                SetDifficultyIcons(dbs.difficultyBeatmaps, ref hasLightshow);
            }

            if (Tweaks.SongDataCoreTweaks.IsRanked(beatmapLevel.levelID, out var ppList))
            {
                if (ppList.Length == 1)
                    _detailsText.text += $": Is Ranked ({ppList[0].ToString("0")}pp)\n";
                else
                    _detailsText.text += $": Is Ranked ({ppList.Min().ToString("0")}pp to {ppList.Max().ToString("0")}pp)\n";
            }

            // on the off chance that the details text will contain one of everything, we'll need to reduce the size of the text so it doesn't overlap with the buttons
            if (_detailsText.text.Count(x => x == '\n') > 3)
                _detailsText.text = "<size=85%>" + _detailsText.text + "</size>";
        }

        private void SetDifficultyIcons(IDifficultyBeatmap[] difficulties, ref bool hasLightshow)
        {
            foreach (var db in difficulties)
            {
                string difficultyName = db.difficulty.Name() == "ExpertPlus" ? "Expert+" : db.difficulty.Name();

                if (!_difficultyElements.TryGetValue(difficultyName, out var tuple))
                    continue;

                Image img = tuple.Item2;
                img.sprite = _checkmarkSprite;
                img.color = _checkmarkColor;

                if (!hasLightshow && db.beatmapData.notesCount == 0)
                {
                    _detailsText.text += ": Has a Lightshow\n";
                    hasLightshow = true;
                }
            }
        }

        private void RemoveCustomUIElements(Transform parent)
        {
            // taken from BeatSaverDownloader
            // https://github.com/andruzzzhka/BeatSaverDownloader/blob/master/BeatSaverDownloader/UI/ViewControllers/SongDetailViewController.cs
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (child.name.StartsWith("CustomUI"))
                {
                    Destroy(child.gameObject);
                }
                if (child.childCount > 0)
                {
                    RemoveCustomUIElements(child);
                }
            }
        }

        private void RemoveSongRequirementsButton()
        {
            // remove SongCore's info button if it exists
            RectTransform[] buttonList = _levelParamsPanel.transform.parent.GetComponentsInChildren<RectTransform>(true)
                .Where(delegate (RectTransform rt)
                {
                    Button btn = rt.GetComponent<Button>();

                    // NOTE: if the button ever gets a proper name, this will need to be changed
                    if (btn == null || btn.name != "PlayButton(Clone)")
                        return false;

                    var text = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                    return text != null ? text.text == "?" : false;
                }).ToArray();

            // there should only be one, but we don't need it so just delete them all anyways
            if (buttonList.Length > 0)
            {
                foreach (RectTransform b in buttonList)
                {
                    Destroy(b.gameObject);
                    Logger.log.Debug("Removed SongCore's requirements info button from StandardLevelDetailView");
                }
            }
            else
            {
                Logger.log.Info("Could not find SongCore's requirements info button custom StandardLevelDetailView");
            }
        }

        // NOTE: also deletes any elements added in by other mods
        private void ModifyPanelElements()
        {
            RectTransform[] rectTransforms = _levelParamsPanel.GetComponentsInChildren<RectTransform>();

            // remove unneeded elements and reposition time and bpm
            foreach (var rt in rectTransforms)
            {
                if (rt.name == "Time" || rt.name == "BPM")
                {
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.sizeDelta = new Vector2(20f, 0f);
                }
                else if ((rt.parent.name == "Time" || rt.parent.name == "BPM") && rt.name == "Icon")
                {
                    rt.anchorMin = new Vector2(0f, 0.5f);
                    rt.anchorMax = new Vector2(0f, 0.5f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.sizeDelta = new Vector2(5f, 5f);
                    rt.anchoredPosition = new Vector2(1f, 0f);
                }
                else if ((rt.parent.name == "Time" || rt.parent.name == "BPM") && rt.name == "ValueText")
                {
                    rt.anchorMin = new Vector2(0f, 0.5f);
                    rt.anchorMax = new Vector2(0f, 0.5f);
                    rt.pivot = new Vector2(0f, 0.5f);
                    rt.sizeDelta = new Vector2(12f, 7f);
                    rt.anchoredPosition = new Vector2(6f, 0f);

                    rt.GetComponentInChildren<TextMeshProUGUI>().fontSize = 5f;
                }
                else if (rt.name != "LevelParamsPanel")
                {
                    Destroy(rt.gameObject);
                }
            }
        }

        private void ModifyTextElements()
        {
            RectTransform statsPanel = _standardLevelDetailView.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Stats");
            statsPanel.gameObject.SetActive(true);

            RectTransform original = statsPanel.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Highscore");
            Instantiate(original, statsPanel.transform, false);
            Instantiate(original, statsPanel.transform, false);

            RectTransform[] rectTransforms = statsPanel.GetComponentsInChildren<RectTransform>(true)
                .Where(x => x.name != "Stats" && x.name != "Title" && x.name != "Value").ToArray();

            float width = 14f;      // parent width = 72u
            float height = 8f;      // parent height = 9u
            float xPos = 1f;
            for (int i = 0; i < rectTransforms.Length; ++i)
            {
                RectTransform rt = rectTransforms[i];
                rt.name = _difficultyStrings[i];

                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(xPos, 1.5f);
                rt.sizeDelta = new Vector2(width, height);
                xPos += width;

                TextMeshProUGUI text = rt.GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "Title");
                text.text = _difficultyStrings[i];
                text.alignment = TextAlignmentOptions.Center;
                text.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                text.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                text.rectTransform.pivot = new Vector2(0.5f, 1f);
                text.rectTransform.anchoredPosition = Vector2.zero;
                text.rectTransform.sizeDelta = rt.sizeDelta;

                Destroy(rt.GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "Value"));

                Image img = new GameObject("Icon").AddComponent<Image>();
                img.rectTransform.SetParent(rt, false);
                img.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                img.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                img.rectTransform.pivot = new Vector2(0.5f, 0f);
                img.rectTransform.anchoredPosition = new Vector2(0f, -0.5f);
                img.rectTransform.sizeDelta = new Vector2(2.5f, 2.5f);
                img.sprite = _crossSprite;
                img.color = _crossColor;

                _difficultyElements.Add(_difficultyStrings[i], new Tuple<TextMeshProUGUI, Image>(text, img));
            }
        }

        private void ModifySelectionElements()
        {
            Button selectButton = _standardLevelDetailView.playButton;
            selectButton.name = "SearchSelectSongButton";
            selectButton.SetButtonText("SELECT SONG");
            selectButton.interactable = true;
            (selectButton.transform as RectTransform).sizeDelta += new Vector2(10f, 0);
            selectButton.ToggleWordWrapping(false);
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(delegate ()
            {
                SelectButtonPressed?.Invoke(_level);
            });

            _compactKeyboardButton = Instantiate(selectButton, selectButton.transform.parent, false);
            _compactKeyboardButton.name = "SearchDisplayKeyboardButton";
            _compactKeyboardButton.SetButtonText("DISPLAY\nKEYBOARD");
            _compactKeyboardButton.SetButtonTextSize(3f);
            (_compactKeyboardButton.transform as RectTransform).sizeDelta += new Vector2(-15f, 0);
            _compactKeyboardButton.onClick.RemoveAllListeners();
            _compactKeyboardButton.onClick.AddListener(delegate ()
            {
                CompactKeyboardButtonPressed?.Invoke();
            });
            _compactKeyboardButton.gameObject.SetActive(PluginConfig.CompactSearchMode);

            Destroy(_standardLevelDetailView.GetComponentInChildren<BeatmapDifficultySegmentedControlController>()?.gameObject);
            Destroy(_standardLevelDetailView.GetComponentInChildren<BeatmapCharacteristicSegmentedControlController>()?.gameObject);
            Destroy(_standardLevelDetailView.practiceButton?.gameObject);

            // transform: PlayButton -> PlayButtons -> PlayContainer
            _detailsText = BeatSaberUI.CreateText(selectButton.transform.parent.parent as RectTransform, "", new Vector2(0f, -2f));
            _detailsText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _detailsText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _detailsText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _detailsText.fontSize = 4f;
        }
    }
}
