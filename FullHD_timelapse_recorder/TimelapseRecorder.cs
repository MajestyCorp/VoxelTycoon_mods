using System;
using System.IO;
using UnityEngine;
using VoxelTycoon;
using System.Diagnostics;
using VoxelTycoon.Game;
using VoxelTycoon.UI;
using System.Collections.Generic;
using System.Reflection;

namespace TimelapseMod
{
    public enum EImageFormat { jpeg, png };
    class TimelapseRecorder
    {
        private EImageFormat ImageFormat = EImageFormat.jpeg;
        private Camera _camera = Camera.main;
        private Transform _cameraTransform = Camera.main.transform;

        //cached camera stuff
        private bool _ortohraphicCamera;
        private float _nearClipPlane;
        private float _orthographicSize;
        private float _orthographicSizeRecorded;

        private Vector3 _oldCameraPosition;
        private Quaternion _oldCameraRotation;

        private int _screenshotId;
        private string _folder;

        //cached effects stuff
        private bool _initialized = false;
        private VoxelSelectionRenderer[] _arrayAreaSelections = null;
        private List<VoxelSelectionRenderer> _restoreAreas = new List<VoxelSelectionRenderer>();
        private bool _restoreWhite;
        private bool _restoreGrid;
        public void Start(string folder)
        {
            this._folder = folder;
            Initialize();
            PrepareForStart();
            ClearFolder();
        }

        public void Stop()
        {
            RecordVideo();
            ClearFolder();
        }

        public void MakeScreenshot(Vector3 cameraPosition, Quaternion cameraRotation)
        {
            PrepareCamera();
            DisableEffects();

            _cameraTransform.position = cameraPosition;
            _cameraTransform.rotation = cameraRotation;

            var screenshot = Helper.TakeScreenshot(1920, 1080, false);
            SaveScreenshot(screenshot, "Screenshot_" + _screenshotId.ToString("0000") + GetFormat());
            UnityEngine.Object.Destroy(screenshot);

            RestoreCamera();
            EnableEffects();

            _screenshotId++;
        }

        private void Initialize()
        {

            if (_initialized)
                return;

            _initialized = true;
            _arrayAreaSelections = GameObject.FindObjectsOfType<VoxelSelectionRenderer>(true);
        }

        private void PrepareForStart()
        {
            _screenshotId = 1;

            _ortohraphicCamera = _camera.orthographic;
            _orthographicSizeRecorded = _camera.orthographicSize;
        }

        private void DisableEffects()
        {
            _restoreWhite = WhiteMode.Current.IsActive;
            _restoreGrid = WorldGridManager.Current.IsActive;

            //disable construction effects
            if (WhiteMode.Current.IsActive)
                WhiteMode.Current.SetKeywords(false);
            if (WorldGridManager.Current.IsActive)
            {
                MethodInfo dynMethod = WorldGridManager.Current.GetType().GetMethod("SetKeywords",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                dynMethod.Invoke(WorldGridManager.Current, new object[] { false });
            }

            if (_arrayAreaSelections != null)
            {
                _restoreAreas.Clear();
                foreach (VoxelSelectionRenderer area in _arrayAreaSelections)
                    if (area.gameObject.activeSelf)
                    {
                        area.gameObject.SetActive(false);
                        _restoreAreas.Add(area);
                    }
            }
        }

        private void EnableEffects()
        {
            //restore construction effects
            if (_restoreWhite)
                WhiteMode.Current.SetKeywords(_restoreWhite);
            if (_restoreGrid)
            {
                MethodInfo dynMethod = WorldGridManager.Current.GetType().GetMethod("SetKeywords",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                dynMethod.Invoke(WorldGridManager.Current, new object[] { true });
            }

            foreach (VoxelSelectionRenderer area in _restoreAreas)
                area.gameObject.SetActive(true);
        }

        private void PrepareCamera()
        {
            _camera.orthographic = true;
            _nearClipPlane = _camera.nearClipPlane;
            _orthographicSize = _camera.orthographicSize;

            //keep orthographic size constant during whole record
            _camera.orthographicSize = _orthographicSizeRecorded;
            _camera.nearClipPlane = -10f;

            _oldCameraPosition = _cameraTransform.position;
            _oldCameraRotation = _cameraTransform.rotation;
        }

        private void RestoreCamera()
        {
            _camera.orthographic = _ortohraphicCamera;
            _camera.nearClipPlane = _nearClipPlane;
            _camera.orthographicSize = _orthographicSize;

            _cameraTransform.position = _oldCameraPosition;
            _cameraTransform.rotation = _oldCameraRotation;
        }

        private void SaveScreenshot(Texture2D image, string filename)
        {
            byte[] bytes = GetImageBytes(image);
            File.WriteAllBytes(_folder + @"/" + filename, bytes);
        }

        private void ClearFolder()
        {
            string[] files = Directory.GetFiles(_folder, "*" + GetFormat());

            if (files == null || files.Length == 0)
                return;

            foreach(string file in files)
                File.Delete(file);
        }

        private void RecordVideo()
        {
            string ffmpeg_path = AppDomain.CurrentDomain.BaseDirectory + @"/Content/" + AVoxelMod.ModeFolder + @"/ffmpeg/bin/ffmpeg.exe";
            string video_path = _folder + @"/Video_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".mp4";
            string png_mask = _folder + @"/Screenshot_%04d" + GetFormat();

            Process cmd = new Process();
            cmd.StartInfo.FileName = ffmpeg_path;
            cmd.StartInfo.Arguments = @"-f image2 -r 30 -i """ + png_mask +
                @""" -s 1920x1080 -vcodec mpeg4 -q:v 0 -r 30 """ + video_path + @"""";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.StandardInput.WriteLine("echo done");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();

            if(File.Exists(video_path))
                AVoxelMod.Popup("Timelapse recorded to: " + video_path);
            else
                AVoxelMod.Popup("Something went wrong, cant record the timelapse");
        }

        private byte[] GetImageBytes(Texture2D image)
        {
            switch(ImageFormat)
            {
                case EImageFormat.jpeg:
                    return ImageConversion.EncodeArrayToJPG(image.GetRawTextureData(), image.graphicsFormat, 1920, 1080);
                default:
                    return ImageConversion.EncodeToPNG(image);
            }
        }

        private string GetFormat()
        {
            switch(ImageFormat)
            {
                case EImageFormat.jpeg:
                    return ".jpeg";

                default:
                    return ".png";
            }
        }
    }
}
