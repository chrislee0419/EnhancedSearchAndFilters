using System;
using System.Linq;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SemVerVersion = SemVer.Version;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Notify;

namespace EnhancedSearchAndFilters.UI.Components.ButtonPanelModules
{
    [RequireComponent(typeof(RectTransform))]
    internal class MiscModule : MonoBehaviour, INotifiableHost
    {
        public event Action ReportButtonPressed;
        public event PropertyChangedEventHandler PropertyChanged;

        [UIValue("version-text")]
        public string VersionText
        {
            get
            {
                if (LatestVersion == null || Plugin.Version >= LatestVersion)
                    return $"Mod Version : {Plugin.Version.Clean()}";
                else
                    return $"<color=#BBBBFF>Latest Version : {_latestVersion.Clean()}</color>";
            }
        }
        private SemVerVersion _latestVersion;
        private SemVerVersion LatestVersion
        {
            get => _latestVersion;
            set
            {
                if (_latestVersion == value)
                    return;

                _latestVersion = value;
                
                try
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(VersionText)));
                }
                catch (Exception e)
                {
                    Logger.log.Error($"Error Invoking PropertyChanged: {e.Message}");
                    Logger.log.Error(e);
                }
            }
        }

#pragma warning disable CS0649
        [UIComponent("report-button")]
        private Button _reportButton;
        [UIComponent("update-button")]
        private Button _updateButton;
#pragma warning restore CS0649

        public RectTransform RectTransform { get; private set; }

        private const string ModRepositoryLatestReleaseURL = "https://github.com/chrislee0419/EnhancedSearchAndFilters/releases/latest";

        private void Awake()
        {
            RectTransform = this.GetComponent<RectTransform>();
        }

        private void Start()
        {
            Utilities.ParseBSML("EnhancedSearchAndFilters.UI.Views.ButtonPanelModules.MiscModuleView.bsml", this.gameObject, this);

            Utilities.ScaleButton(_reportButton);
            Utilities.ScaleButton(_updateButton);

            OnEnable();
        }

        private void OnEnable()
        {
            if (_updateButton == null)
                return;

            GitHubAPIHelper.instance.GetLatestReleaseVersion(delegate (bool success, SemVerVersion latestVersion)
            {
                if (success && Plugin.Version < latestVersion)
                {
                    _updateButton.GetComponentInChildren<TextMeshProUGUI>().text = "Update Available";
                    _updateButton.interactable = true;
                    _updateButton.GetComponentsInChildren<Image>().First(x => x.name == "Stroke").color = new Color(0.7f, 0.6f, 1f);

                    LatestVersion = latestVersion;
                }
            });
        }

        [UIAction("report-button-clicked")]
        private void OnReportButtonClicked() => ReportButtonPressed?.Invoke();

        [UIAction("update-button-clicked")]
        private void OnInfoButtonClicked() => Process.Start(ModRepositoryLatestReleaseURL);
    }
}
