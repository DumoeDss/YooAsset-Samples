using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace YooAsset
{
    public class YooAssetsManager : UnitySingleton<YooAssetsManager>
    {
        private bool _isInitialize = false;
        private string _initializeError = string.Empty;
        private EOperationStatus _initializeStatus = EOperationStatus.None;
        private EPlayMode _playMode;
        private IBundleServices _bundleServices;
        private ILocationServices _locationServices;
        private EditorSimulateModeImpl _editorSimulateModeImpl;
        private OfflinePlayModeImpl _offlinePlayModeImpl;
        private HostPlayModeImpl _hostPlayModeImpl;

        InitializeParameters parameters;

        /// <summary>
        /// 是否已经初始化
        /// </summary>
        public bool IsInitialized
        {
            get { return _isInitialize; }
        }

        /// <summary>
        /// 异步初始化
        /// </summary>
        public void InitializeAsync(InitializeParameters parameters)
        {
            if (parameters == null)
                throw new Exception($"YooAsset create parameters is null.");

            if (parameters.LocationServices == null)
                throw new Exception($"{nameof(IBundleServices)} is null.");
            else
                _locationServices = parameters.LocationServices;
            this.parameters = parameters;

#if !UNITY_EDITOR
			if (parameters is EditorSimulateModeParameters)
				throw new Exception($"Editor simulate mode only support unity editor.");
#endif

            // 初始化异步操作系统
            OperationSystem.Initialize(parameters.OperationSystemMaxTimeSlice);

#if DEBUG
            gameObject.AddComponent<RemoteDebuggerInRuntime>();
#endif

            // 检测参数范围
            if (parameters.AssetLoadingMaxNumber < 1)
            {
                parameters.AssetLoadingMaxNumber = 1;
                YooLogger.Warning($"{nameof(parameters.AssetLoadingMaxNumber)} minimum value is 1");
            }
            if (parameters.OperationSystemMaxTimeSlice < 30)
            {
                parameters.OperationSystemMaxTimeSlice = 30;
                YooLogger.Warning($"{nameof(parameters.OperationSystemMaxTimeSlice)} minimum value is 30 milliseconds");
            }

            // 鉴定运行模式
            if (parameters is EditorSimulateModeParameters)
                _playMode = EPlayMode.EditorSimulateMode;
            else if (parameters is OfflinePlayModeParameters)
                _playMode = EPlayMode.OfflinePlayMode;
            else if (parameters is HostPlayModeParameters)
                _playMode = EPlayMode.HostPlayMode;
            else
                throw new NotImplementedException();

            // 初始化下载系统
            if (_playMode == EPlayMode.HostPlayMode)
            {
#if UNITY_WEBGL
				throw new Exception($"{EPlayMode.HostPlayMode} not supports WebGL platform !");
#else
                var hostPlayModeParameters = parameters as HostPlayModeParameters;
                DownloadSystem.Initialize(hostPlayModeParameters.BreakpointResumeFileSize, hostPlayModeParameters.VerifyLevel);
#endif
            }


        }

        Dictionary<string, YooAssets> yooAssetsDic;

        public YooAssets GetYooAssets(string name)
        {
            if(yooAssetsDic == null)
                yooAssetsDic= new Dictionary<string, YooAssets>();
            if (yooAssetsDic.ContainsKey(name))
            {
                return yooAssetsDic[name];
            }
            YooAssets yooAssets = new YooAssets();
            yooAssetsDic.Add(name, yooAssets);
            return yooAssets;
        }

        /// <summary>
        /// 开启一个异步操作
        /// </summary>
        /// <param name="operation">异步操作对象</param>
        public void StartOperaiton(GameAsyncOperation operation)
        {
            OperationSystem.StartOperaiton(operation);
        }

        void Update()
        {
            OperationSystem.Update();
            DownloadSystem.Update();
            foreach (var item in yooAssetsDic)
            {
                if(item.Value != null && item.Value.IsInitialized)
                {
                    item.Value.InternalUpdate();
                }
            }
        }

        void OnApplicationQuit()
        {
            _isInitialize = false;
            _initializeError = string.Empty;
            _initializeStatus = EOperationStatus.None;

            _bundleServices = null;
            _locationServices = null;
            _editorSimulateModeImpl = null;
            _offlinePlayModeImpl = null;
            _hostPlayModeImpl = null;

            OperationSystem.DestroyAll();
            DownloadSystem.DestroyAll();
            foreach (var item in yooAssetsDic)
            {
                if (item.Value != null && item.Value.IsInitialized)
                {
                    item.Value.InternalDestroy();
                }
            }
        }
    }
}