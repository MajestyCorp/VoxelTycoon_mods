using System.Collections.Generic;
using UnityEngine;
using VoxelTycoon;
using VoxelTycoon.Cities;
using System.IO;
using VoxelTycoon.Serialization;

namespace TimelapseMod
{
    class TimelapseManager : Manager<TimelapseManager>
    {
        private const float c_camera_distance = 125f;
        private const float c_video_framerate = 30f;
        private const float c_screenshot_delay = 10f;
        private const float c_video_session = 10f;
        private const float c_video_angle_speed = 5f;
        private const float c_vertical_camera_angle = 45f;
        private const float c_game_time_for_1_video_sec = c_video_framerate * c_screenshot_delay;
        private const float c_game_session = c_game_time_for_1_video_sec * c_video_session;
        private const float c_angle_speed = c_video_angle_speed / c_game_time_for_1_video_sec;

        public bool Enabled { get; set; } = false;

        private bool _notified = false;
        private TimelapseRecorder _recorder = new TimelapseRecorder();

        private Timer _screenshotTimer = new Timer();
        private Timer _sessionTimer = new Timer();

        private Vector3 _targetPosition = Vector3.zero;

        private Vector3 _camPosition = Vector3.zero;
        private Vector3 _camAngles = Vector3.zero;

        protected override void OnUpdate()
        {
            if(Enabled != _notified)
            {
                _notified = Enabled;

                if (Enabled)
                    _recorder.Start(GetFolder());
                else
                    _recorder.Stop();
            }

            if (!Enabled)
                return;

            if(_sessionTimer.IsFinished)
            {
                _targetPosition = GetNewPosition();
                _sessionTimer.Activate(c_game_session);
            }

            if(_screenshotTimer.IsFinished)
            {
                UpdateCameraPosition();
                _recorder.MakeScreenshot(_camPosition, Quaternion.Euler(_camAngles));
                _screenshotTimer.Activate(c_screenshot_delay);
            }
        }

        private void UpdateCameraPosition()
        {
            _camAngles.x = c_vertical_camera_angle;
            _camAngles.y = Time.time * c_angle_speed;

            _camPosition = _targetPosition - Quaternion.Euler(_camAngles) * Vector3.forward * c_camera_distance;
        }

        private Vector3 GetNewPosition()
        {
            Vector3 result;
            if (AVoxelMod.TrackedBuilding != null)
            {
                result = AVoxelMod.TrackedBuilding.transform.position;
                AVoxelMod.ClearTrackedObject();
            } else
            {
                result = (Vector3)GetRandomCity().Position;
            }

            return result;
        }

        private City GetRandomCity()
        {
            var cities = CityManager.Current.Cities;
            List<City> randomCities = new List<City>();

            for (var i = 0; i < cities.Count; i++)
            {
                var city = cities[i];
                if (city.Region.State != RegionState.Unlocked) continue;

                randomCities.Add(city);
            }

            if (randomCities.Count == 0)
            {
                //if no unlocked cities - select random locked cities
                return cities[Random.Range(0, cities.Count - 1)];
            } else
            {
                //select random unlocked city
                return randomCities[Random.Range(0, randomCities.Count)];
            }
        }

        private string GetFolder()
        {
            string result = Application.dataPath + @"\Timelapse";

            //check Timelapse directory
            if (!Directory.Exists(result))
                Directory.CreateDirectory(result);

            //add world seed
            result += @"/" + WorldSettings.Current.SeedString;
            if (!Directory.Exists(result))
                Directory.CreateDirectory(result);

            return result;
        }

    }
}
