using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using HMUI;
using BS_Utils.Utilities;
using BeatSaberMarkupLanguage;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.Utilities;
using Image = UnityEngine.UI.Image;
using BSUtilsUtilities = BS_Utils.Utilities.UIUtilities;
using UIUtilities = EnhancedSearchAndFilters.Utilities.UIUtilities;

namespace EnhancedSearchAndFilters.UI.ViewControllers
{
    internal class SongDetailsViewController : ViewController
    {
        public Action<IPreviewBeatmapLevel> SelectButtonPressed;
        public Action CompactKeyboardButtonPressed;

        public static GameObject ReferenceDetailView { private get; set; }
        public static KawaseBlurRendererSO BlurRenderer { private get; set; }

        private LevelParamsPanel _levelParamsPanel;
        private RawImage _coverImage;
        private TextMeshProUGUI _songNameText;
        private TextMeshProUGUI _detailsText;
        private Button _selectLevelButton;
        private Button _compactKeyboardButton;

        private string _settingCoverImageForLevelId;
        private CancellationTokenSource _cancellationTokenSource;
        private Texture2D _blurredCoverImageTexture;

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
                this.name = "SongDetailsViewController";

                // fallback to using original method of getting StandardLevelDetailView if reference is somehow null
                // if this triggers, there is an issue
                if (ReferenceDetailView == null)
                {
                    Logger.log.Debug("ReferenceDetailView is null. Using fallback method of obtaining StandardLevelDetailView object");

                    var referenceViewController = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First();
                    ReferenceDetailView = referenceViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView").gameObject;
                }

                this.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                this.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                this.rectTransform.anchoredPosition = Vector2.zero;
                this.rectTransform.sizeDelta = new Vector2(80f, 0f);

                var detailView = Instantiate(ReferenceDetailView, this.transform, false);
                detailView.SetActive(true);
                detailView.name = "SearchResultLevelDetail";
                (detailView.transform as RectTransform).anchoredPosition -= new Vector2(0f, -2f);

                _levelParamsPanel = detailView.GetComponentInChildren<LevelParamsPanel>();
                _coverImage = detailView.GetComponentsInChildren<RawImage>().First(x => x.name == "CoverImage");
                _songNameText = detailView.GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "SongNameText");
                _selectLevelButton = detailView.GetComponentsInChildren<Button>().First(x => x.name == "PlayButton");

                _checkmarkSprite = BSUtilsUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.checkmark.png");
                _crossSprite = BSUtilsUtilities.LoadSpriteFromResources("EnhancedSearchAndFilters.Assets.cross.png");
                _blankSprite = Sprite.Create(Texture2D.blackTexture, new Rect(0f, 0f, 1f, 1f), Vector2.zero);

                RemoveCustomUIElements(this.rectTransform);
                ModifyPanelElements(detailView);
                ModifyTextElements(detailView);
                ModifySelectionElements(detailView);
            }
            else
            {
                //// stats panel gets disabled when in party mode, so re-enable it here just in case
                //RectTransform statsPanel = _standardLevelDetailView.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Stats");
                //statsPanel.gameObject.SetActive(true);
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
            SetCoverImageAsync(level);
            _levelParamsPanel.duration = level.songDuration;
            _detailsText.text = "";

            foreach (var tuple in _difficultyElements.Values)
            {
                tuple.Item2.sprite = _blankSprite;
                tuple.Item2.color = new Color(0f, 0f, 0f, 0f);
            }

            if (level is CustomPreviewBeatmapLevel)
            {
#pragma warning disable CS4014
                BeatmapDetailsLoader.instance.LoadSingleBeatmapAsync(level as CustomPreviewBeatmapLevel,
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

        private async void SetCoverImageAsync(IPreviewBeatmapLevel level)
        {
            string levelID = level.levelID;
            if (_settingCoverImageForLevelId == levelID)
                return;

            try
            {
                _settingCoverImageForLevelId = levelID;

                if (_cancellationTokenSource != null)
                    _cancellationTokenSource.Cancel();

                _cancellationTokenSource = new CancellationTokenSource();
                _coverImage.enabled = false;
                _coverImage.texture = null;

                Texture2D coverImageTexture = await level.GetCoverImageTexture2DAsync(_cancellationTokenSource.Token);
                _cancellationTokenSource.Token.ThrowIfCancellationRequested();

                if (_blurredCoverImageTexture != null)
                    Destroy(_blurredCoverImageTexture);

                _blurredCoverImageTexture = BlurRenderer.Blur(coverImageTexture, KawaseBlurRendererSO.KernelSize.Kernel7);
                _coverImage.texture = _blurredCoverImageTexture;
                _coverImage.enabled = true;

                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
            catch (OperationCanceledException)
            { }
            finally
            {
                if (_settingCoverImageForLevelId == levelID)
                    _settingCoverImageForLevelId = null;
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
                if (dbs.beatmapCharacteristic.serializedName == "OneSaber")
                    _detailsText.text += ": Has 'One Saber' mode\n";
                else if (dbs.beatmapCharacteristic.serializedName == "NoArrows")
                    _detailsText.text += ": Has 'No Arrows' mode\n";

                // set difficulty icons and lightshow text (if not already present)
                foreach (var db in dbs.difficultyBeatmaps)
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

            if (Tweaks.SongDataCoreTweaks.IsRanked(beatmapLevel.levelID, out var ppList))
            {
                if (ppList.Length == 1)
                    _detailsText.text += $": Is Ranked ({ppList[0].ToString("0")}pp)\n";
                else
                    _detailsText.text += $": Is Ranked ({ppList.Min().ToString("0")}pp to {ppList.Max().ToString("0")}pp)\n";
            }

            // if we can't find the PlayerData object for whatever reason, assume the level has been played before
            if (!(PlayerDataHelper.Instance?.HasCompletedLevel(beatmapLevel.levelID) ?? true))
                _detailsText.text += ": Has never been completed\n";

            // on the off chance that the details text will contain one of everything, we'll need to reduce the size of the text so it doesn't overlap with the buttons
            int lineCount = _detailsText.text.Count(x => x == '\n');
            if (lineCount > 4)
                _detailsText.text = "<size=68%>" + _detailsText.text + "</size>";
            else if (lineCount > 3)
                _detailsText.text = "<size=85%>" + _detailsText.text + "</size>";
        }

        private void RemoveCustomUIElements(Transform parent)
        {
            // taken from BeatSaverDownloader
            // https://github.com/andruzzzhka/BeatSaverDownloader/blob/master/BeatSaverDownloader/UI/ViewControllers/SongDetailViewController.cs
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (child.name.StartsWith("CustomUI") || child.name.StartsWith("BSML"))
                {
                    Destroy(child.gameObject);
                }
                if (child.childCount > 0)
                {
                    RemoveCustomUIElements(child);
                }
            }
        }

        private void ModifyPanelElements(GameObject detailViewGO)
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

                    // remove the hoverhint, otherwise we get null exceptions from onpointerenter/exit
                    Destroy(rt.GetComponentInChildren<HoverHint>());
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

            // remove favorites toggle
            Destroy(detailViewGO.transform.Find("LevelInfo/FavoritesToggle").gameObject);
        }

        private void ModifyTextElements(GameObject detailViewGO)
        {
            RectTransform statsPanel = detailViewGO.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Stats");
            statsPanel.gameObject.SetActive(true);

            Func<RectTransform, bool> getStatsRectTransforms = x => x.name != "Stats" && x.name != "Title" && x.name != "Value";

            // ensure that there are only 5 stats rect transforms, representing each difficulty
            // another mod could add/remove these, which would mess up the layout for our use
            RectTransform prefab = statsPanel.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Highscore");

            foreach (var statsRectTransform in statsPanel.GetComponentsInChildren<RectTransform>().Where(x => x.parent == statsPanel))
            {
                if (statsRectTransform != prefab)
                    Destroy(statsRectTransform.gameObject);
            }
            List<RectTransform> rectTransforms = new List<RectTransform>(_difficultyStrings.Length);
            rectTransforms.Add(prefab);

            while (rectTransforms.Count < _difficultyStrings.Length)
                rectTransforms.Add(Instantiate(prefab, statsPanel.transform, false));

            float width = 16f;      // parent width = 80u
            float height = 8f;      // parent height = 9u
            float xPos = 1f;
            for (int i = 0; i < rectTransforms.Count; ++i)
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
                img.material = UIUtilities.NoGlowMaterial;

                _difficultyElements.Add(_difficultyStrings[i], new Tuple<TextMeshProUGUI, Image>(text, img));
            }
        }

        private void ModifySelectionElements(GameObject detailViewGO)
        {
            _selectLevelButton.name = "SearchSelectSongButton";
            _selectLevelButton.SetButtonText("SELECT SONG");
            _selectLevelButton.interactable = true;
            _selectLevelButton.GetComponentInChildren<Image>().color = new Color(0f, 0.706f, 1f, 0.784f);
            (_selectLevelButton.transform as RectTransform).sizeDelta += new Vector2(10f, 0);
            _selectLevelButton.ToggleWordWrapping(false);
            _selectLevelButton.onClick.RemoveAllListeners();
            _selectLevelButton.onClick.AddListener(delegate ()
            {
                SelectButtonPressed?.Invoke(_level);
            });

            _compactKeyboardButton = Instantiate(_selectLevelButton, _selectLevelButton.transform.parent, false);
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

            Destroy(detailViewGO.GetComponentInChildren<BeatmapDifficultySegmentedControlController>()?.gameObject);
            Destroy(detailViewGO.GetComponentInChildren<BeatmapCharacteristicSegmentedControlController>()?.gameObject);
            Destroy(detailViewGO.GetComponentsInChildren<Button>().FirstOrDefault(x => x.name == "PracticeButton")?.gameObject);

            // transform: PlayButton -> PlayButtons -> PlayContainer
            _detailsText = BeatSaberUI.CreateText(_selectLevelButton.transform.parent.parent as RectTransform, "", new Vector2(0f, -2f));
            _detailsText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            _detailsText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _detailsText.rectTransform.pivot = new Vector2(0.5f, 1f);
            _detailsText.fontSize = 4f;
        }
    }
}
