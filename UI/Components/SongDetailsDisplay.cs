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
using SongCore;
using SongCore.Data;
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
        private TextMeshProUGUI _songAuthorText;
        private TextMeshProUGUI _songMapperText;
        private TextMeshProUGUI _timeText;
        private TextMeshProUGUI _bpmText;
        private List<GameObject> _characteristicsIconContainers = new List<GameObject>();
        private List<Image> _characteristicsIcons = new List<Image>(7);
        private TextMeshProUGUI _detailsText;
        private GameObject _loadingSpinner;
        private Dictionary<string, Image> _difficultyElements = new Dictionary<string, Image>(5);

        private Button _selectButton;
        private Button _keyboardButton;

        private BeatmapCharacteristicCollectionSO _characteristics;

        private string _settingCoverImageForLevelId;
        private CancellationTokenSource _cancellationTokenSource;
        private Texture2D _blurredCoverImageTexture;
        private static KawaseBlurRendererSO _blurRenderer;

        private const float CharacteristicIconSize = 3.3f;
        private const float CharacteristicIconSpacing = 0.8f;
        private const float AuthorTextMapperTextXHalfPoint = 0.5f;
        private static readonly Color CheckmarkColour = new Color(0.8f, 1f, 0.8f);
        private static readonly Color CrossColour = new Color(1f, 0.8f, 0.8f);
        private static readonly Color TitleColour = new Color(0.75f, 0.75f, 0.75f);

        private void Awake()
        {
            rectTransform = this.GetComponent<RectTransform>();

            // cover image background
            var mask = new GameObject("SearchCoverImageMask").AddComponent<Mask>();
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

            _coverImage = new GameObject("SearchCoverImage").AddComponent<RawImage>();
            _coverImage.transform.SetParent(mask.transform, false);
            _coverImage.rectTransform.anchorMin = Vector2.zero;
            _coverImage.rectTransform.anchorMax = Vector2.one;
            _coverImage.rectTransform.anchoredPosition = Vector2.zero;
            _coverImage.rectTransform.sizeDelta = new Vector2(2f, 2f);

            _coverImage.enabled = false;
            _coverImage.texture = null;
            _coverImage.color = new Color(1f, 1f, 1f, 0.16f);

            // song text
            var nameContainer = CreateContainer(
                mask.transform,
                "SongNameContainer",
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(0f, -1f),
                new Vector2(-4f, 12f),
                new Vector2(0.5f, 1f));

            _songNameText = BeatSaberUI.CreateText(nameContainer, "", Vector2.zero, Vector2.zero);
            _songNameText.name = "SongNameText";
            _songNameText.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _songNameText.rectTransform.anchorMax = Vector2.one;
            _songNameText.rectTransform.sizeDelta = new Vector2(-6f, 0f);

            _songNameText.alignment = TextAlignmentOptions.Center;
            _songNameText.overflowMode = TextOverflowModes.Ellipsis;
            _songNameText.enableWordWrapping = false;
            _songNameText.fontSize = 4f;

            _songAuthorText = BeatSaberUI.CreateText(nameContainer, "", Vector2.zero, Vector2.zero);
            _songAuthorText.name = "SongAuthorText";
            _songAuthorText.rectTransform.anchorMin = Vector2.zero;
            _songAuthorText.rectTransform.anchorMax = new Vector2(AuthorTextMapperTextXHalfPoint, 0.5f);
            _songAuthorText.rectTransform.sizeDelta = new Vector2(-3f, 0f);

            _songAuthorText.alignment = TextAlignmentOptions.Center;
            _songAuthorText.overflowMode = TextOverflowModes.Ellipsis;
            _songAuthorText.enableWordWrapping = false;
            _songAuthorText.fontSize = 3.4f;

            _songMapperText = BeatSaberUI.CreateText(nameContainer, "", Vector2.zero, Vector2.zero);
            _songMapperText.name = "SongMapperText";
            _songMapperText.rectTransform.anchorMin = new Vector2(AuthorTextMapperTextXHalfPoint, 0f);
            _songMapperText.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            _songMapperText.rectTransform.sizeDelta = new Vector2(-3f, 0f);

            _songMapperText.alignment = TextAlignmentOptions.Center;
            _songMapperText.overflowMode = TextOverflowModes.Ellipsis;
            _songMapperText.enableWordWrapping = false;
            _songMapperText.fontSize = 3.4f;

            // characteristics panel
            const float HalfPoint = 0.34f;
            const float BackgroundScale = 0.65f;

            const float CharacteristicsTitleYAnchor = 0.8f;
            var characteristicsBG = CreateBackground(
                mask.transform,
                "Characteristics",
                new Vector2(0f, 1f),
                new Vector2(HalfPoint, 1f),
                new Vector2(0.5f, -14f),
                new Vector2(-3f, 13f),
                new Vector2(0.5f, 1f),
                BackgroundScale);

            var characteristicsTitleText = BeatSaberUI.CreateText(characteristicsBG, "CHARACTERISTICS", new Vector2(0f, -0.25f), new Vector2(0f, -0.5f));
            characteristicsTitleText.name = "DifficultiesTitle";
            characteristicsTitleText.rectTransform.anchorMin = new Vector2(0f, CharacteristicsTitleYAnchor);
            characteristicsTitleText.rectTransform.anchorMax = Vector2.one;

            characteristicsTitleText.alignment = TextAlignmentOptions.Top;
            characteristicsTitleText.fontSize = 2.1f;
            characteristicsTitleText.color = TitleColour;

            var characteristicsContainerParent = CreateContainer(characteristicsBG, "Container", Vector2.zero, new Vector2(1f, CharacteristicsTitleYAnchor), Vector2.zero, Vector2.zero).gameObject;
            var vlg = characteristicsContainerParent.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 0.5f;
            vlg.padding = new RectOffset(1, 1, 0, 0);

            var iconRow = CreateContainer(characteristicsContainerParent.transform, "Row", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var layoutElement = iconRow.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = CharacteristicIconSize;

            var hlg = iconRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = CharacteristicIconSpacing;
            _characteristicsIconContainers.Add(iconRow);

            _characteristics = Resources.FindObjectsOfTypeAll<CustomLevelLoader>().First().GetField<BeatmapCharacteristicCollectionSO, CustomLevelLoader>("_beatmapCharacteristicCollection");

            // song info panel
            var songInfoBG = CreateBackground(
                mask.transform,
                "SongInfo",
                new Vector2(HalfPoint, 1f),
                Vector2.one,
                new Vector2(-0.5f, -14f),
                new Vector2(-3f, 6f),
                new Vector2(0.5f, 1f),
                BackgroundScale);

            var container = new GameObject("Time").AddComponent<RectTransform>();
            container.SetParent(songInfoBG.transform, false);
            container.anchorMin = new Vector2(0.15f, 0f);
            container.anchorMax = new Vector2(0.45f, 1f);
            container.anchoredPosition = Vector2.zero;
            container.sizeDelta = Vector2.zero;

            var timeIcon = new GameObject("TimeIcon").AddComponent<Image>();
            timeIcon.rectTransform.SetParent(container, false);
            timeIcon.rectTransform.anchorMin = new Vector2(0.25f, 0.5f);
            timeIcon.rectTransform.anchorMax = new Vector2(0.25f, 0.5f);
            timeIcon.rectTransform.anchoredPosition = Vector2.zero;
            timeIcon.rectTransform.sizeDelta = new Vector2(3.2f, 3.2f);

            timeIcon.sprite = Resources.FindObjectsOfTypeAll<Image>().First(x => x.name == "Icon" && x.sprite?.name == "ClockIcon").sprite;

            _timeText = BeatSaberUI.CreateText(container, "", Vector2.zero, Vector2.zero);
            _timeText.name = "TimeText";
            _timeText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            _timeText.rectTransform.anchorMax = new Vector2(1f, 1f);

            _timeText.alignment = TextAlignmentOptions.Center;
            _timeText.overflowMode = TextOverflowModes.Truncate;
            _timeText.enableWordWrapping = false;
            _timeText.fontSize = 3.2f;

            container = new GameObject("BPM").AddComponent<RectTransform>();
            container.SetParent(songInfoBG.transform, false);
            container.anchorMin = new Vector2(0.55f, 0f);
            container.anchorMax = new Vector2(0.85f, 1f);
            container.anchoredPosition = Vector2.zero;
            container.sizeDelta = Vector2.zero;

            var bpmIcon = new GameObject("BPMIcon").AddComponent<Image>();
            bpmIcon.rectTransform.SetParent(container, false);
            bpmIcon.rectTransform.anchorMin = new Vector2(0.25f, 0.5f);
            bpmIcon.rectTransform.anchorMax = new Vector2(0.25f, 0.5f);
            bpmIcon.rectTransform.anchoredPosition = Vector2.zero;
            bpmIcon.rectTransform.sizeDelta = new Vector2(3.2f, 3.2f);

            bpmIcon.sprite = Resources.FindObjectsOfTypeAll<Image>().First(x => x.name == "Icon" && x.sprite?.name == "MetronomeIcon").sprite;

            _bpmText = BeatSaberUI.CreateText(container, "", Vector2.zero, Vector2.zero);
            _bpmText.name = "BPMText";
            _bpmText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            _bpmText.rectTransform.anchorMax = new Vector2(1f, 1f);

            _bpmText.alignment = TextAlignmentOptions.Center;
            _bpmText.overflowMode = TextOverflowModes.Truncate;
            _bpmText.enableWordWrapping = false;
            _bpmText.fontSize = 3.2f;

            // difficulties panel
            var difficultiesBG = CreateBackground(
                mask.transform,
                "Difficulties",
                new Vector2(0f, 1f),
                new Vector2(HalfPoint, 1f),
                new Vector2(0.5f, -28f),
                new Vector2(-3f, 28f),
                new Vector2(0.5f, 1f),
                BackgroundScale);
            string[] DifficultyStrings = { "Easy", "Normal", "Hard", "Expert", "ExpertPlus" };
            string[] DifficultyTexts =
            {
                "<color=#55CC55>Easy</color>",
                "<color=#2299EE>Normal</color>",
                "<color=#FF9900>Hard</color>",
                "<color=#FF4433>Expert</color>",
                "<color=#AA22BB>Expert+</color>"
            };
            const float IconSize = 2.5f;
            const float DifficultiesTitleYAnchor = 0.88f;
            float divisionSize = DifficultiesTitleYAnchor / DifficultyStrings.Length;
            for (int i = 0; i < DifficultyStrings.Length; ++i)
            {
                float minYAnchor = DifficultiesTitleYAnchor - divisionSize * (i + 1);
                float maxYAnchor = DifficultiesTitleYAnchor - divisionSize * i;
                float halfYAnchor = (minYAnchor + maxYAnchor) / 2f;

                // offset 1u to the right
                var text = BeatSaberUI.CreateText(difficultiesBG, DifficultyTexts[i], new Vector2(2f, 0f), new Vector2(-2f, 0f));
                text.name = DifficultyStrings[i] + "Text";
                text.rectTransform.anchorMin = new Vector2(0f, minYAnchor);
                text.rectTransform.anchorMax = new Vector2(0.5f, maxYAnchor);

                text.alignment = TextAlignmentOptions.Center;
                text.overflowMode = TextOverflowModes.Truncate;
                text.enableWordWrapping = false;
                text.fontSize = 2.9f;

                var image = new GameObject("Icon").AddComponent<Image>();
                image.rectTransform.SetParent(difficultiesBG.transform, false);
                image.rectTransform.anchorMin = new Vector2(0.75f, halfYAnchor);
                image.rectTransform.anchorMax = new Vector2(0.75f, halfYAnchor);
                image.rectTransform.anchoredPosition = Vector2.zero;
                image.rectTransform.sizeDelta = new Vector2(IconSize, IconSize);

                image.sprite = UIUtilities.BlankSprite;
                image.color = Color.black;
                image.material = UIUtilities.NoGlowMaterial;

                _difficultyElements.Add(DifficultyStrings[i], image);
            }

            var difficultiesTitleText = BeatSaberUI.CreateText(difficultiesBG, "DIFFICULTIES", new Vector2(0f, -0.25f), new Vector2(0f, -0.5f));
            difficultiesTitleText.name = "DifficultiesTitle";
            difficultiesTitleText.rectTransform.anchorMin = new Vector2(0f, DifficultiesTitleYAnchor);
            difficultiesTitleText.rectTransform.anchorMax = Vector2.one;

            difficultiesTitleText.alignment = TextAlignmentOptions.Top;
            difficultiesTitleText.fontSize = 2.1f;
            difficultiesTitleText.color = TitleColour;

            // details text
            var detailsTextBG = CreateBackground(
                mask.transform,
                "DetailsText",
                new Vector2(HalfPoint, 1f),
                Vector2.one,
                new Vector2(-0.5f, -21f),
                new Vector2(-3f, 35f),
                new Vector2(0.5f, 1f),
                BackgroundScale);

            var detailsTitleText = BeatSaberUI.CreateText(detailsTextBG, "ADDITIONAL INFORMATION", new Vector2(0f, -1.5f), new Vector2(0f, 1.5f));
            detailsTitleText.name = "DetailsTitle";
            detailsTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
            detailsTitleText.rectTransform.anchorMax = Vector2.one;

            detailsTitleText.alignment = TextAlignmentOptions.Top;
            detailsTitleText.fontSize = 2.1f;
            detailsTitleText.color = TitleColour;

            _detailsText = BeatSaberUI.CreateText(detailsTextBG, "", new Vector2(0f, -2f), new Vector2(-6f, -3f));
            _detailsText.name = "DetailsText";
            _detailsText.rectTransform.anchorMin = Vector2.zero;
            _detailsText.rectTransform.anchorMax = Vector2.one;

            _detailsText.alignment = TextAlignmentOptions.TopLeft;
            _detailsText.overflowMode = TextOverflowModes.Truncate;
            _detailsText.enableWordWrapping = true;
            _detailsText.fontSize = 3f;

            _loadingSpinner = UIUtilities.CreateLoadingSpinner(detailsTextBG);
            _loadingSpinner.SetActive(false);

            // buttons
            var buttonsContainer = CreateContainer(
                mask.transform,
                "ButtonContainer",
                Vector2.zero,
                new Vector2(1f, 0f),
                new Vector2(0f, 3f),
                new Vector2(-4f, 11f),
                new Vector2(0.5f, 0f));

            hlg = buttonsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 3f;

            var buttonPrefab = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "PlayButton");

            _selectButton = Instantiate(buttonPrefab, buttonsContainer, false);
            _selectButton.name = "SelectSongButton";
            _selectButton.SetButtonText("SELECT SONG");
            _selectButton.SetButtonTextSize(4.7f);
            _selectButton.interactable = true;
            _selectButton.GetComponentInChildren<Image>().color = new Color(0f, 0.706f, 1f, 0.784f);
            _selectButton.ToggleWordWrapping(false);
            _selectButton.onClick.RemoveAllListeners();
            _selectButton.onClick.AddListener(() => SelectButtonPressed?.Invoke());

            _keyboardButton = Instantiate(buttonPrefab, buttonsContainer, false);
            _keyboardButton.name = "DisplayKeyboardButton";
            _keyboardButton.SetButtonText("DISPLAY\nKEYBOARD");
            _keyboardButton.SetButtonTextSize(3.3f);
            _keyboardButton.interactable = true;
            _keyboardButton.GetComponentInChildren<Image>().color = new Color(0f, 0.706f, 1f, 0.784f);
            _keyboardButton.onClick.RemoveAllListeners();
            _keyboardButton.onClick.AddListener(() => KeyboardButtonPressed?.Invoke());
            _keyboardButton.gameObject.SetActive(PluginConfig.CompactSearchMode);

            var detailView = Resources.FindObjectsOfTypeAll<StandardLevelDetailView>().First();
            _blurRenderer = detailView.GetField<KawaseBlurRendererSO, StandardLevelDetailView>("_kawaseBlurRenderer");
        }

        private RectTransform CreateContainer(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2? pivot = null)
        {
            var container = new GameObject(name).AddComponent<RectTransform>();
            container.SetParent(parent, false);
            container.anchorMin = anchorMin;
            container.anchorMax = anchorMax;
            container.anchoredPosition = anchoredPosition;
            container.sizeDelta = sizeDelta;

            if (pivot.HasValue)
                container.pivot = pivot.Value;

            return container;
        }

        private RectTransform CreateBackground(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2? pivot = null, float scale = -1f)
        {
            var container = CreateContainer(parent, name, anchorMin, anchorMax, anchoredPosition, sizeDelta, pivot);

            Image bg;
            if (scale > 0f && scale < 2f)
            {
                float halfInverseScale = ((1f / scale) - 1f) / 2f;
                bg = new GameObject("Background").AddComponent<Image>();
                bg.rectTransform.SetParent(container, false);
                bg.rectTransform.anchorMin = new Vector2(-halfInverseScale, -halfInverseScale);
                bg.rectTransform.anchorMax = new Vector2(1 + halfInverseScale, 1 + halfInverseScale);
                bg.rectTransform.anchoredPosition = Vector2.zero;
                bg.rectTransform.localScale = new Vector3(scale, scale, scale);
                bg.rectTransform.sizeDelta = Vector2.zero;
            }
            else
            {
                bg = container.gameObject.AddComponent<Image>();
            }

            bg.sprite = UIUtilities.RoundRectPanelSprite;
            bg.material = UIUtilities.NoGlowMaterial;
            bg.color = UIUtilities.RoundRectDarkColour;
            bg.type = Image.Type.Sliced;

            return container;
        }

        private Image CreateCharacteristicIcon(Sprite icon)
        {
            RectTransform parent = null;
            float maxRowSize = (_characteristicsIconContainers[0].transform.parent as RectTransform).rect.width;
            for (int i = 0; i < _characteristicsIconContainers.Count; ++i)
            {
                // check if this row can fit another icon
                var row = _characteristicsIconContainers[i];

                if ((row.GetComponentsInChildren<Image>().Length + 1) * (CharacteristicIconSize + CharacteristicIconSpacing) + 2f <= maxRowSize)
                {
                    row.gameObject.SetActive(true);
                    parent = row.transform as RectTransform;
                    break;
                }
            }
            if (parent == null)
            {
                parent = CreateContainer(_characteristicsIconContainers[0].transform.parent, "Row", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                var newRow = parent.gameObject;
                var newLayoutElement = newRow.AddComponent<LayoutElement>();
                newLayoutElement.preferredHeight = CharacteristicIconSize;

                var hlg = newRow.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = CharacteristicIconSpacing;
                _characteristicsIconContainers.Add(newRow);

            }

            var container = CreateContainer(parent, "CharacteristicIconContainer", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var layoutElement = container.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = CharacteristicIconSize;
            layoutElement.preferredHeight = CharacteristicIconSize;

            var img = new GameObject("Icon").AddComponent<Image>();
            img.rectTransform.SetParent(container, false);
            img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.rectTransform.anchoredPosition = Vector2.zero;
            img.rectTransform.sizeDelta = new Vector2(CharacteristicIconSize, CharacteristicIconSize);

            img.sprite = icon;
            img.material = UIUtilities.NoGlowMaterial;

            _characteristicsIcons.Add(img);

            return img;
        }

        public void SetContent(IPreviewBeatmapLevel level)
        {
            _songNameText.text = level.songName.EscapeTextMeshProTags().ToUpper();
            _songAuthorText.text = string.IsNullOrEmpty(level.songAuthorName) ? "" : "<color=#BBBBBB><size=60%>BY:</size></color> " + level.songAuthorName.EscapeTextMeshProTags();
            _songMapperText.text = string.IsNullOrEmpty(level.levelAuthorName) ? "" : "<color=#BBBBBB><size=60%>MAPPED BY:</size></color> " + level.levelAuthorName.EscapeTextMeshProTags();
            _bpmText.text = level.beatsPerMinute.ToString();
            SetCoverImageAsync(level);
            _timeText.text = FloatToTimeString(level.songDuration);

            if (string.IsNullOrEmpty(level.songAuthorName))
                _songMapperText.rectTransform.anchorMin = new Vector2(0f, _songMapperText.rectTransform.anchorMin.y);
            else
                _songMapperText.rectTransform.anchorMin = new Vector2(AuthorTextMapperTextXHalfPoint, _songMapperText.rectTransform.anchorMin.y);

            if (string.IsNullOrEmpty(level.levelAuthorName))
                _songAuthorText.rectTransform.anchorMax = new Vector2(1f, _songAuthorText.rectTransform.anchorMax.y);
            else
                _songAuthorText.rectTransform.anchorMax = new Vector2(AuthorTextMapperTextXHalfPoint, _songAuthorText.rectTransform.anchorMax.y);

            // clear info that will be populated later
            _detailsText.text = "";
            foreach (var image in _difficultyElements.Values)
            {
                image.sprite = UIUtilities.BlankSprite;
                image.color = new Color(0f, 0f, 0f, 0f);
            }

            HideAllCharacteristicsIcons();
            _loadingSpinner.SetActive(true);

            if (level is CustomPreviewBeatmapLevel customLevel)
            {
#pragma warning disable CS4014
                BeatmapDetailsLoader.instance.LoadSingleBeatmapAsync(customLevel,
                    delegate (IBeatmapLevel beatmapLevel)
                    {
                        if (beatmapLevel != null)
                            SetLoadedSongDetails(beatmapLevel);
                    });
#pragma warning restore CS4014
            }
            else if (level is IBeatmapLevel ostLevel)
            {
                SetLoadedSongDetails(ostLevel, false);
            }
            else
            {
                foreach (var image in _difficultyElements.Values)
                {
                    image.sprite = UIUtilities.CrossSprite;
                    image.color = CrossColour;
                }

                _detailsText.text = ": DLC song not yet purchased";
                _loadingSpinner.SetActive(false);
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

            ShowCharacteristicIcons(beatmapLevel.previewDifficultyBeatmapSets.Select(x => x.beatmapCharacteristic));

            int playCount = PlayerDataHelper.Instance?.GetPlayCountForLevel(beatmapLevel.levelID) ?? 0;
            _detailsText.text += $": Has been played {playCount} time{(playCount == 1 ? "" : "s")}\n";

            // if we can't find the PlayerData object for whatever reason, assume the level has been completed before
            if (!(PlayerDataHelper.Instance?.HasCompletedLevel(beatmapLevel.levelID) ?? true))
                _detailsText.text += ": Has never been completed\n";

            foreach (var dbs in beatmapLevelData.difficultyBeatmapSets)
            {
                // set difficulty icons and lightshow text (if not already present)
                foreach (var db in dbs.difficultyBeatmaps)
                {
                    if (!_difficultyElements.TryGetValue(db.difficulty.ToString(), out var image))
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

            // requires other mods
            if (beatmapLevel is CustomBeatmapLevel customBeatmapLevel)
            {
                ExtraSongData songData = Collections.RetrieveExtraSongData(BeatmapDetailsLoader.GetCustomLevelHash(customBeatmapLevel), customBeatmapLevel.customLevelPath);
                const string HasRequirementColour = "#CCFFCC";
                const string MissingRequirementColour = "#FFCCCC";

                if (songData._difficulties?.Any(x => x.additionalDifficultyData?._requirements.Any(y => y == "Mapping Extensions") ?? false) ?? false)
                {
                    string colour = Collections.capabilities.Contains("Mapping Extensions") ? HasRequirementColour : MissingRequirementColour;
                    _detailsText.text += $": Requires '<color={colour}>Mapping Extensions</color>'\n";
                }

                if (songData._difficulties?.Any(x => x.additionalDifficultyData?._requirements.Any(y => y == "Noodle Extensions") ?? false) ?? false)
                {
                    string colour = Collections.capabilities.Contains("Noodle Extensions") ? HasRequirementColour : MissingRequirementColour;
                    _detailsText.text += $": Requires '<color={colour}>Noodle Extensions</color>'\n";
                }

                if (songData._difficulties?.Any(x => x.additionalDifficultyData?._requirements.Any(y => y == "Chroma") ?? false) ?? false)
                {
                    string colour = Collections.capabilities.Contains("Chroma") ? HasRequirementColour : MissingRequirementColour;
                    _detailsText.text += $": Requires '<color={colour}>Chroma</color>'\n";
                }
            }

            if (Tweaks.SongDataCoreTweaks.IsRanked(beatmapLevel.levelID, out var ppList))
            {
                if (ppList.Length == 1)
                    _detailsText.text += $": Is Ranked ({ppList[0].ToString("0")}pp)\n";
                else
                    _detailsText.text += $": Is Ranked ({ppList.Min().ToString("0")}pp to {ppList.Max().ToString("0")}pp)\n";
            }

            _loadingSpinner.SetActive(false);
        }

        private void HideAllCharacteristicsIcons()
        {
            foreach (var container in _characteristicsIconContainers)
                container.SetActive(false);
            foreach (var icon in _characteristicsIcons)
                icon.transform.parent.gameObject.SetActive(false);
        }

        private void ShowCharacteristicIcons(IEnumerable<BeatmapCharacteristicSO> characteristics)
        {
            int iconIndex = 0;
            foreach (var c in characteristics)
            {
                if (iconIndex < _characteristicsIcons.Count)
                {
                    var icon = _characteristicsIcons[iconIndex++];
                    icon.sprite = c.icon;

                    icon.transform.parent.gameObject.SetActive(true);
                    icon.transform.parent.parent.gameObject.SetActive(true);
                }
                else
                {
                    CreateCharacteristicIcon(c.icon);
                    ++iconIndex;
                }
            }
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
