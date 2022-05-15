using System;
using VoxelTycoon;
using VoxelTycoon.Buildings;
using VoxelTycoon.Modding;
using VoxelTycoon.Notifications;
using VoxelTycoon.UI;
using VoxelTycoon.Game.UI.ModernUI;
using UnityEngine;
using UnityEngine.UI;
using VoxelTycoon.Tracks;

namespace TimelapseMod
{
    public class AVoxelMod : Mod
    {
        public const string ModeFolder = "timelapse_mod";
        public const string ModeName = "FullHD Timelapse Recorder";
        private const string c_template_button = "Camera";
        private const string c_panel_name = "Fill";
        private const float c_button_step = 40f;

        public static Building TrackedBuilding { get; private set; }

        private RectTransform _rectButton = null;

        private Color _defaultPanelColor;
        private Color _defaultFillColor;

        private Panel _buttonPanel;
        private Panel _fillPanel;

        protected override void Initialize()
        {
            BuildingManager.Current.BuildingBuilt += OnBuildingBuilt;
        }

        protected override void Deinitialize()
        {
            BuildingManager.Current.BuildingBuilt -= OnBuildingBuilt;
            if (_rectButton != null)
            {
                GameObject.Destroy(_rectButton);
                _rectButton = null;
            }
        }

        protected override void OnGameStarted()
        {
            TimelapseManager.Initialize();
            InitButton();

        }

        private void OnBuildingBuilt(Building building)
        {
            if (building is VehicleStation || TrackedBuilding == null || !(TrackedBuilding is VehicleStation))
                TrackedBuilding = building;
        }

        private void InitButton()
        {
            Transform modernGameUI = Toolbar.Current.transform.parent;
            RectTransform camRect = modernGameUI.Find<RectTransform>(c_template_button);
            Button buttonTimelapse;
            TooltipTarget tooltip = null;

            if (camRect == null)
            {
                Popup("Incompatible mod version (no template button)");
                return;
            }

            _rectButton = GameObject.Instantiate(camRect, camRect.parent);
            _rectButton.anchoredPosition = camRect.anchoredPosition + Vector2.right * c_button_step;

            buttonTimelapse = _rectButton.GetComponentInChildren<Button>();
            if (buttonTimelapse == null)
            {
                Popup("Incompatible mod version (no Button script)");
                GameObject.Destroy(_rectButton.gameObject);
                _rectButton = null;
                return;
            }

            //set cached panels
            _buttonPanel = buttonTimelapse.GetComponent<Panel>();
            _fillPanel = _rectButton.Find<Panel>(c_panel_name);

            //set tooltip
            tooltip = buttonTimelapse.GetComponent<TooltipTarget>();
            tooltip.Text = ModeName;


            //save default colors
            _defaultFillColor = _fillPanel.color;
            _defaultPanelColor = _buttonPanel.color;

            //set event
            buttonTimelapse.onClick.RemoveAllListeners();
            buttonTimelapse.onClick.AddListener(ButtonToggle);
        }

        private void ButtonToggle()
        {
            TimelapseManager.Current.Enabled ^= true;
            UpdateButtonState(TimelapseManager.Current.Enabled);
        }

        private void UpdateButtonState(bool enabled)
        {
            _buttonPanel.color = enabled ? Color.red : _defaultPanelColor;
            _fillPanel.color = enabled ? Color.red : _defaultFillColor;
        }

        public static void Popup(string message)
        {
            var priority = NotificationPriority.Critical;
            var color = Company.Current.Color;
            var title = ModeName + ":";
            var action = default(INotificationAction);
            var icon = FontIcon.FaSolid("\uf7e4");
            NotificationManager.Current.Push(priority, color, title, message, action, icon);
        }

        public static void ClearTrackedObject()
        {
            TrackedBuilding = null;
        }

    }
}
