using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IPA.Utilities;
using BeatSaberMarkupLanguage;
using EnhancedSearchAndFilters.SongData;
using EnhancedSearchAndFilters.Utilities;

namespace EnhancedSearchAndFilters.UI.Components
{
    [RequireComponent(typeof(RectTransform))]
    internal class SongDetailsDisplay : MonoBehaviour
    {
        public event Action SelectButtonPressed;
        public event Action KeyboardButtonPressed;

        public RectTransform rectTransform { get; private set; }
        public bool KeyboardButtonActive { set => _keyboardButton?.gameObject.SetActive(value); }

        private RawImage _coverImage;
        private TextMeshProUGUI _songNameText;
        private TextMeshProUGUI _timeText;
        private TextMeshProUGUI _bpmText;
        private TextMeshProUGUI _detailsText;
        private Dictionary<string, Image> _difficultyElements = new Dictionary<string, Image>(5);

        private Button _selectButton;
        private Button _keyboardButton;

        private string _settingCoverImageForLevelId;
        private CancellationTokenSource _cancellationTokenSource;
        private Texture2D _blurredCoverImageTexture;
        private static KawaseBlurRendererSO _blurRenderer;

        private static readonly Color CheckmarkColour = new Color(0.8f, 1f, 0.8f);
        private static readonly Color CrossColour = new Color(1f, 0.8f, 0.8f);

        private void Awake()
        {
            rectTransform = this.GetComponent<RectTransform>();

            const float HalfPoint = 0.4f;

            // cover image background
            var mask = new GameObject("CoverImageMask").AddComponent<Mask>();
            mask.transform.SetParent(this.transform, false);
            mask.rectTransform.anchorMin = Vector2.zero;
            mask.rectTransform.anchorMax = Vector2.one;
            mask.rectTransform.anchoredPosition = Vector2.zero;
            mask.rectTransform.sizeDelta = Vector2.zero;

            var maskImage = mask.gameObject.AddComponent<Image>();
            maskImage.sprite = UIUtilities.RoundRectPanelSprite;
            maskImage.material = UIUtilities.NoGlowMaterial;
            maskImage.color = UIUtilities.RoundRectDefaultColour;
            maskImage.type = Image.Type.Sliced;

            _coverImage = new GameObject("CoverImage").AddComponent<RawImage>();
            _coverImage.transform.SetParent(mask.transform, false);
            _coverImage.rectTransform.anchorMin = Vector2.zero;
            _coverImage.rectTransform.anchorMax = Vector2.one;
            _coverImage.rectTransform.anchoredPosition = Vector2.zero;
            _coverImage.rectTransform.sizeDelta = new Vector2(5f, 5f);

            _coverImage.enabled = false;
            _coverImage.texture = null;

            // song name text
            var nameBG = CreateBackground(
                "SongName",
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(0f, -1f),
                new Vector2(-4f, 8f),
                new Vector2(0.5f, 1f));

            _songNameText = BeatSaberUI.CreateText(nameBG.rectTransform, "", Vector2.zero, Vector2.zero);
            _songNameText.name = "SongNameText";
            _songNameText.rectTransform.anchorMin = Vector2.zero;
            _songNameText.rectTransform.anchorMax = Vector2.one;

            _songNameText.alignment = TextAlignmentOptions.Center;
            _songNameText.overflowMode = TextOverflowModes.Ellipsis;
            _songNameText.enableWordWrapping = false;
            _songNameText.fontSize = 3.5f;

            // song info panel
            var songInfoBG = CreateBackground(
                "SongInfo",
                new Vector2(0f, 1f),
                new Vector2(HalfPoint, 1f),
                new Vector2(0f, -11f),
                new Vector2(-4f, 14f),
                new Vector2(0.5f, 1f));

            songInfoBG.sprite = UIUtilities.RoundRectPanelSprite;

            var timeIcon = new GameObject("TimeIcon").AddComponent<Image>();
            timeIcon.rectTransform.SetParent(songInfoBG.transform, false);
            timeIcon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            timeIcon.rectTransform.anchorMax = new Vector2(0.4f, 1f);
            timeIcon.rectTransform.anchoredPosition = Vector2.zero;
            timeIcon.rectTransform.sizeDelta = new Vector2(-2f, -1f);

            timeIcon.sprite = Resources.FindObjectsOfTypeAll<Image>().First(x => x.name == "Icon" && x.sprite?.name == "ClockIcon").sprite;
            timeIcon.preserveAspect = true;

            _timeText = BeatSaberUI.CreateText(songInfoBG.rectTransform, "", Vector2.zero, new Vector2(-2f, -1f));
            _timeText.name = "TimeText";
            _timeText.rectTransform.anchorMin = new Vector2(0.4f, 0.5f);
            _timeText.rectTransform.anchorMax = new Vector2(1f, 1f);

            _timeText.alignment = TextAlignmentOptions.Center;
            _timeText.overflowMode = TextOverflowModes.Truncate;
            _timeText.enableWordWrapping = false;
            _timeText.fontSize = 2.8f;

            var bpmIcon = new GameObject("BPMIcon").AddComponent<Image>();
            bpmIcon.rectTransform.SetParent(songInfoBG.transform, false);
            bpmIcon.rectTransform.anchorMin = new Vector2(0f, 0f);
            bpmIcon.rectTransform.anchorMax = new Vector2(0.4f, 0.5f);
            bpmIcon.rectTransform.anchoredPosition = Vector2.zero;
            bpmIcon.rectTransform.sizeDelta = new Vector2(-2f, -1f);

            bpmIcon.sprite = Resources.FindObjectsOfTypeAll<Image>().First(x => x.name == "Icon" && x.sprite?.name == "MetronomeIcon").sprite;
            bpmIcon.preserveAspect = true;

            _bpmText = BeatSaberUI.CreateText(songInfoBG.rectTransform, "", Vector2.zero, new Vector2(-2f, -1f));
            _bpmText.name = "BPMText";
            _bpmText.rectTransform.anchorMin = new Vector2(0.4f, 0f);
            _bpmText.rectTransform.anchorMax = new Vector2(1f, 0.5f);

            _bpmText.alignment = TextAlignmentOptions.Center;
            _bpmText.overflowMode = TextOverflowModes.Truncate;
            _bpmText.enableWordWrapping = false;
            _bpmText.fontSize = 2.8f;

            // difficulties panel
            var difficultiesBG = CreateBackground(
                "Difficulties",
                new Vector2(0f, 1f),
                new Vector2(HalfPoint, 1f),
                new Vector2(0f, -27f),
                new Vector2(-4f, 38f),
                new Vector2(0.5f, 1f));
            string[] DifficultyStrings = { "Easy", "Normal", "Hard", "Expert", "Expert+" };
            string[] DifficultyTexts =
            {
                "<color=#55CC55>Easy</color>",
                "<color=#2299EE>Normal</color>",
                "<color=#FF9900>Hard</color>",
                "<color=#FF4433>Expert</color>",
                "<color=#AA22BB>Expert+</color>"
            };
            float divisionSize = 1f / DifficultyStrings.Length;
            for (int i = 0; i < DifficultyStrings.Length; ++i)
            {
                float minYAnchor = 1f - divisionSize * (i + 1);
                float maxYAnchor = 1f - divisionSize * i;

                var text = BeatSaberUI.CreateText(difficultiesBG.rectTransform, DifficultyTexts[i], Vector2.zero, Vector2.zero);
                text.rectTransform.anchorMin = new Vector2(0f, minYAnchor);
                text.rectTransform.anchorMax = new Vector2(0.5f, maxYAnchor);

                text.alignment = TextAlignmentOptions.Center;
                text.overflowMode = TextOverflowModes.Truncate;
                text.enableWordWrapping = false;
                text.fontSize = 2.5f;

                var image = new GameObject("Icon").AddComponent<Image>();
                image.rectTransform.SetParent(difficultiesBG.transform, false);
                image.rectTransform.anchorMin = new Vector2(0.5f, minYAnchor);
                image.rectTransform.anchorMax = new Vector2(1f, maxYAnchor);
                image.rectTransform.anchoredPosition = Vector2.zero;
                image.rectTransform.sizeDelta = Vector2.zero;

                image.sprite = UIUtilities.BlankSprite;
                image.color = Color.black;
                image.material = UIUtilities.NoGlowMaterial;

                _difficultyElements.Add(DifficultyStrings[i], image);
            }

            // details text
            var detailsTextContainer = new GameObject("DetailsContainer").AddComponent<RectTransform>();
            detailsTextContainer.SetParent(this.transform, false);
            detailsTextContainer.anchorMin = new Vector2(HalfPoint, 1f);
            detailsTextContainer.anchorMax = Vector2.one;
            detailsTextContainer.anchoredPosition = new Vector2(0f, -11f);
            detailsTextContainer.sizeDelta = new Vector2(-4f, 56f);
            detailsTextContainer.pivot = new Vector2(0.5f, 1f);

            _detailsText = BeatSaberUI.CreateText(detailsTextContainer, "", Vector2.zero, Vector2.zero);
            _detailsText.name = "DetailsText";
            _detailsText.rectTransform.anchorMin = Vector2.zero;
            _detailsText.rectTransform.anchorMax = Vector2.one;

            _detailsText.alignment = TextAlignmentOptions.TopLeft;
            _detailsText.overflowMode = TextOverflowModes.Truncate;
            _detailsText.enableWordWrapping = true;
            _detailsText.fontSize = 2.8f;

            // buttons
            var buttonsContainer = new GameObject("ButtonsContainer").AddComponent<RectTransform>();
            buttonsContainer.SetParent(this.transform, false);
            buttonsContainer.anchorMin = Vector2.zero;
            buttonsContainer.anchorMax = new Vector2(1f, 0f);
            buttonsContainer.pivot = new Vector2(0.5f, 0f);
            buttonsContainer.anchoredPosition = new Vector2(0f, 1f);
            buttonsContainer.sizeDelta = new Vector2(0f, 8f);

            var hlg = buttonsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 2f;

            var buttonPrefab = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "PlayButton");

            _selectButton = Instantiate(buttonPrefab, buttonsContainer, false);
            _selectButton.name = "SelectSongButton";
            _selectButton.SetButtonText("SELECT SONG");
            _selectButton.interactable = true;
            _selectButton.GetComponentInChildren<Image>().color = new Color(0f, 0.706f, 1f, 0.784f);
            (_selectButton.transform as RectTransform).sizeDelta += new Vector2(10f, 0f);
            _selectButton.ToggleWordWrapping(false);
            _selectButton.onClick.RemoveAllListeners();
            _selectButton.onClick.AddListener(() => SelectButtonPressed?.Invoke());

            _keyboardButton = Instantiate(buttonPrefab, buttonsContainer, false);
            _keyboardButton.name = "DisplayKeyboardButton";
            _keyboardButton.SetButtonText("DISPLAY\nKEYBOARD");
            _keyboardButton.SetButtonTextSize(3f);
            (_keyboardButton.transform as RectTransform).sizeDelta += new Vector2(-5f, 0);
            _keyboardButton.onClick.RemoveAllListeners();
            _keyboardButton.onClick.AddListener(() => KeyboardButtonPressed?.Invoke());
            _keyboardButton.gameObject.SetActive(PluginConfig.CompactSearchMode);

            var detailView = Resources.FindObjectsOfTypeAll<StandardLevelDetailView>().First();
            _blurRenderer = detailView.GetField<KawaseBlurRendererSO, StandardLevelDetailView>("_kawaseBlurRenderer");
        }

        private Image CreateBackground(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2? pivot = null)
        {
            var bg = new GameObject(name).AddComponent<Image>();
            bg.rectTransform.SetParent(this.transform, false);
            bg.rectTransform.anchorMin = anchorMin;
            bg.rectTransform.anchorMax = anchorMax;
            bg.rectTransform.anchoredPosition = anchoredPosition;
            bg.rectTransform.sizeDelta = sizeDelta;

            if (pivot.HasValue)
                bg.rectTransform.pivot = pivot.Value;

            bg.sprite = UIUtilities.RoundRectPanelSprite;
            bg.material = UIUtilities.NoGlowMaterial;
            bg.color = UIUtilities.RoundRectDefaultColour;
            bg.type = Image.Type.Sliced;

            return bg;
        }

        public void SetContent(IPreviewBeatmapLevel level)
        {
            _songNameText.text = level.songName;
            _bpmText.text = level.beatsPerMinute.ToString();
            SetCoverImageAsync(level);
            _timeText.text = FloatToTimeString(level.songDuration);

            // clear info that will be loaded later
            _detailsText.text = "";
            foreach (var image in _difficultyElements.Values)
            {
                image.sprite = UIUtilities.BlankSprite;
                image.color = new Color(0f, 0f, 0f, 0f);
            }

            if (level is CustomPreviewBeatmapLevel)
            {
#pragma warning disable CS4014
                BeatmapDetailsLoader.instance.LoadSingleBeatmapAsync(level as CustomPreviewBeatmapLevel,
                    delegate (IBeatmapLevel beatmapLevel)
                    {
                        if (beatmapLevel != null)
                            SetLoadedSongDetails(beatmapLevel);
                    });
#pragma warning restore CS4014
            }
            else if (level is IBeatmapLevel)
            {
                SetLoadedSongDetails((level as IBeatmapLevel), false);
            }
            else
            {
                foreach (var image in _difficultyElements.Values)
                {
                    image.sprite = UIUtilities.CrossSprite;
                    image.color = CrossColour;
                }

                _detailsText.text = ": DLC song not yet purchased";
            }
        }

        public void SetCoverImage(Texture texture)
        {
            if (texture == null)
            {
                _coverImage.enabled = false;
                _coverImage.texture = null;
            }
            else
            {
                _coverImage.texture = texture;
                _coverImage.enabled = true;
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

                _blurredCoverImageTexture = _blurRenderer.Blur(coverImageTexture, KawaseBlurRendererSO.KernelSize.Kernel7);
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

        private void SetLoadedSongDetails(IBeatmapLevel beatmapLevel, bool setDuration = true)
        {
            IBeatmapLevelData beatmapLevelData = beatmapLevel.beatmapLevelData;
            bool hasLightshow = false;

            if (setDuration && beatmapLevelData.audioClip != null)
                _timeText.text = FloatToTimeString(beatmapLevelData.audioClip.length);

            foreach (var image in _difficultyElements.Values)
            {
                image.sprite = UIUtilities.CrossSprite;
                image.color = CrossColour;
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

                    if (!_difficultyElements.TryGetValue(difficultyName, out var image))
                        continue;

                    image.sprite = UIUtilities.CheckmarkSprite;
                    image.color = CheckmarkColour;

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

        private string FloatToTimeString(float time)
        {
            if (time < 0f)
                return "0:00";

            float seconds = time % 60;
            time -= seconds;

            float minutes = (time / 60) % 60;
            time -= minutes * 60;

            float hours = time;

            StringBuilder sb = new StringBuilder();
            if (hours > 0)
            {
                sb.Append(hours.ToString("##0")).Append(':');
                sb.Append(minutes.ToString("00")).Append(':');
            }
            else
            {
                sb.Append(minutes.ToString("#0")).Append(':');
            }

            sb.Append(seconds.ToString("00"));

            return sb.ToString();
        }
    }
}
