using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace YooAsset
{
	/// <summary>
	/// 运行模式
	/// </summary>
	public enum EPlayMode
	{
		/// <summary>
		/// 编辑器下的模拟模式
		/// 注意：在初始化的时候自动构建真机模拟环境。
		/// </summary>
		EditorSimulateMode,

		/// <summary>
		/// 离线运行模式
		/// </summary>
		OfflinePlayMode,

		/// <summary>
		/// 网络运行模式
		/// </summary>
		HostPlayMode,
	}

	/// <summary>
	/// 初始化参数
	/// </summary>
	public abstract class InitializeParameters
	{
		/// <summary>
		/// 资源定位地址大小写不敏感
		/// </summary>
		public bool LocationToLower = false;

		/// <summary>
		/// 资源定位服务接口
		/// </summary>
		public ILocationServices LocationServices = null;

		/// <summary>
		/// 文件解密服务接口
		/// </summary>
		public IDecryptionServices DecryptionServices = null;

		/// <summary>
		/// 资源加载的最大数量
		/// </summary>
		public int AssetLoadingMaxNumber = int.MaxValue;

		/// <summary>
		/// 异步操作系统每帧允许运行的最大时间切片（单位：毫秒）
		/// </summary>
		public long OperationSystemMaxTimeSlice = long.MaxValue;
	}

	/// <summary>
	/// 编辑器下模拟运行模式的初始化参数
	/// </summary>
	public class EditorSimulateModeParameters : InitializeParameters
	{
		/// <summary>
		/// 用于模拟运行的资源清单路径
		/// 注意：如果路径为空，会自动重新构建补丁清单。
		/// </summary>
		public string SimulatePatchManifestPath;
	}

	/// <summary>
	/// 离线运行模式的初始化参数
	/// </summary>
	public class OfflinePlayModeParameters : InitializeParameters
	{
	}

	/// <summary>
	/// 网络运行模式的初始化参数
	/// </summary>
	public class HostPlayModeParameters : InitializeParameters
	{
		/// <summary>
		/// 默认的资源服务器下载地址
		/// </summary>
		public string DefaultHostServer;

		/// <summary>
		/// 备用的资源服务器下载地址
		/// </summary>
		public string FallbackHostServer;

		/// <summary>
		/// 当缓存池被污染的时候清理缓存池
		/// </summary>
		public bool ClearCacheWhenDirty = false;

		/// <summary>
		/// 启用断点续传功能的文件大小
		/// </summary>
		public int BreakpointResumeFileSize = int.MaxValue;

		/// <summary>
		/// 下载文件校验等级
		/// </summary>
		public EVerifyLevel VerifyLevel = EVerifyLevel.High;
	}

	public class YooAssets
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
		internal AssetSystem AssetSystem;

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
		public InitializationOperation InitializeAsync(InitializeParameters parameters, EPlayMode _playMode)
		{
			_locationServices = parameters.LocationServices;

			// 创建驱动器
			if (!_isInitialize)
			{
				_isInitialize = true;
			}
			else
			{
				throw new Exception("YooAsset is initialized yet.");
			}

			// 鉴定运行模式
			this._playMode = _playMode;

			AssetSystem = new AssetSystem();

			// 初始化资源系统
			InitializationOperation initializeOperation;
			if (_playMode == EPlayMode.EditorSimulateMode)
			{
				_editorSimulateModeImpl = new EditorSimulateModeImpl();
				_bundleServices = _editorSimulateModeImpl;
				AssetSystem.Initialize(true, parameters.AssetLoadingMaxNumber, parameters.DecryptionServices, _bundleServices);
				var editorSimulateModeParameters = parameters as EditorSimulateModeParameters;
				initializeOperation = _editorSimulateModeImpl.InitializeAsync(
					editorSimulateModeParameters.LocationToLower,
					editorSimulateModeParameters.SimulatePatchManifestPath);
			}
			else if (_playMode == EPlayMode.OfflinePlayMode)
			{
				_offlinePlayModeImpl = new OfflinePlayModeImpl();
				_bundleServices = _offlinePlayModeImpl;
				AssetSystem.Initialize(false, parameters.AssetLoadingMaxNumber, parameters.DecryptionServices, _bundleServices);
				initializeOperation = _offlinePlayModeImpl.InitializeAsync(parameters.LocationToLower);
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				_hostPlayModeImpl = new HostPlayModeImpl();
				_bundleServices = _hostPlayModeImpl;
				AssetSystem.Initialize(false, parameters.AssetLoadingMaxNumber, parameters.DecryptionServices, _bundleServices);
				var hostPlayModeParameters = parameters as HostPlayModeParameters;
				initializeOperation = _hostPlayModeImpl.InitializeAsync(
					hostPlayModeParameters.LocationToLower,
					hostPlayModeParameters.ClearCacheWhenDirty,
					hostPlayModeParameters.DefaultHostServer,
					hostPlayModeParameters.FallbackHostServer);
			}
			else
			{
				throw new NotImplementedException();
			}

			// 监听初始化结果
			initializeOperation.Completed += InitializeOperation_Completed;
			return initializeOperation;
		}
		private void InitializeOperation_Completed(AsyncOperationBase op)
		{
			_initializeStatus = op.Status;
			_initializeError = op.Error;
		}

		/// <summary>
		/// 向网络端请求并更新补丁清单
		/// </summary>
		/// <param name="resourceVersion">更新的资源版本</param>
		/// <param name="timeout">超时时间（默认值：60秒）</param>
		public UpdateManifestOperation UpdateManifestAsync(string manifestName, int timeout = 60)
		{
			DebugCheckInitialize();
			if (_playMode == EPlayMode.EditorSimulateMode)
			{
				var operation = new EditorPlayModeUpdateManifestOperation();
				OperationSystem.StartOperaiton(operation);
				return operation;
			}
			else if (_playMode == EPlayMode.OfflinePlayMode)
			{
				var operation = new OfflinePlayModeUpdateManifestOperation();
				OperationSystem.StartOperaiton(operation);
				return operation;
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				return _hostPlayModeImpl.UpdatePatchManifestAsync(manifestName, timeout);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// 获取资源版本号
		/// </summary>
		public int GetResourceVersion()
		{
			DebugCheckInitialize();
			if (_playMode == EPlayMode.EditorSimulateMode)
			{
				return _editorSimulateModeImpl.GetResourceVersion();
			}
			else if (_playMode == EPlayMode.OfflinePlayMode)
			{
				return _offlinePlayModeImpl.GetResourceVersion();
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				return _hostPlayModeImpl.GetResourceVersion();
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// 资源回收（卸载引用计数为零的资源）
		/// </summary>
		public void UnloadUnusedAssets()
		{
			if (_isInitialize)
			{
				AssetSystem.Update();
				AssetSystem.UnloadUnusedAssets();
			}
		}

		/// <summary>
		/// 强制回收所有资源
		/// </summary>
		public void ForceUnloadAllAssets()
		{
			if (_isInitialize)
			{
				AssetSystem.ForceUnloadAllAssets();
			}
		}


		#region 资源信息
		/// <summary>
		/// 是否需要从远端更新下载
		/// </summary>
		/// <param name="location">资源的定位地址</param>
		public bool IsNeedDownloadFromRemote(string location)
		{
			DebugCheckInitialize();
			AssetInfo assetInfo = ConvertLocationToAssetInfo(location, null);
			if (assetInfo.IsInvalid)
			{
				YooLogger.Warning(assetInfo.Error);
				return false;
			}

			BundleInfo bundleInfo = _bundleServices.GetBundleInfo(assetInfo);
			if (bundleInfo.LoadMode == BundleInfo.ELoadMode.LoadFromRemote)
				return true;
			else
				return false;
		}

		/// <summary>
		/// 是否需要从远端更新下载
		/// </summary>
		/// <param name="location">资源的定位地址</param>
		public bool IsNeedDownloadFromRemote(AssetInfo assetInfo)
		{
			DebugCheckInitialize();
			if (assetInfo.IsInvalid)
			{
				YooLogger.Warning(assetInfo.Error);
				return false;
			}

			BundleInfo bundleInfo = _bundleServices.GetBundleInfo(assetInfo);
			if (bundleInfo.LoadMode == BundleInfo.ELoadMode.LoadFromRemote)
				return true;
			else
				return false;
		}

		/// <summary>
		/// 获取资源信息列表
		/// </summary>
		/// <param name="tag">资源标签</param>
		public AssetInfo[] GetAssetInfos(string tag)
		{
			DebugCheckInitialize();
			string[] tags = new string[] { tag };
			return _bundleServices.GetAssetInfos(tags);
		}

		/// <summary>
		/// 获取资源信息列表
		/// </summary>
		/// <param name="tags">资源标签列表</param>
		public AssetInfo[] GetAssetInfos(string[] tags)
		{
			DebugCheckInitialize();
			return _bundleServices.GetAssetInfos(tags);
		}

		/// <summary>
		/// 获取资源路径
		/// </summary>
		/// <param name="location">资源的定位地址</param>
		/// <returns>如果location地址无效，则返回空字符串</returns>
		public string GetAssetPath(string location)
		{
			DebugCheckInitialize();
			return _locationServices.ConvertLocationToAssetPath(this,location);
		}
		#endregion

		#region 场景加载
		/// <summary>
		/// 异步加载场景
		/// </summary>
		/// <param name="location">场景的定位地址</param>
		/// <param name="sceneMode">场景加载模式</param>
		/// <param name="activateOnLoad">加载完毕时是否主动激活</param>
		/// <param name="priority">优先级</param>
		public SceneOperationHandle LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
		{
			DebugCheckInitialize();
			AssetInfo assetInfo = ConvertLocationToAssetInfo(location, null);
			var handle = AssetSystem.LoadSceneAsync(assetInfo, sceneMode, activateOnLoad, priority);
			return handle;
		}

		/// <summary>
		/// 异步加载场景
		/// </summary>
		/// <param name="assetInfo">场景的资源信息</param>
		/// <param name="sceneMode">场景加载模式</param>
		/// <param name="activateOnLoad">加载完毕时是否主动激活</param>
		/// <param name="priority">优先级</param>
		public SceneOperationHandle LoadSceneAsync(AssetInfo assetInfo, LoadSceneMode sceneMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
		{
			DebugCheckInitialize();
			var handle = AssetSystem.LoadSceneAsync(assetInfo, sceneMode, activateOnLoad, priority);
			return handle;
		}
		#endregion

		#region 资源加载
		/// <summary>
		/// 异步获取原生文件
		/// </summary>
		/// <param name="location">资源的定位地址</param>
		/// <param name="copyPath">拷贝路径</param>
		public RawFileOperation GetRawFileAsync(string location, string copyPath = null)
		{
			DebugCheckInitialize();
			AssetInfo assetInfo = ConvertLocationToAssetInfo(location, null);
			return GetRawFileInternal(assetInfo, copyPath);
		}

		/// <summary>
		/// 异步获取原生文件
		/// </summary>
		/// <param name="assetInfo">资源信息</param>
		/// <param name="copyPath">拷贝路径</param>
		public RawFileOperation GetRawFileAsync(AssetInfo assetInfo, string copyPath = null)
		{
			DebugCheckInitialize();
			return GetRawFileInternal(assetInfo, copyPath);
		}


		private RawFileOperation GetRawFileInternal(AssetInfo assetInfo, string copyPath)
		{
			if (assetInfo.IsInvalid)
			{
				YooLogger.Warning(assetInfo.Error);
				RawFileOperation operation = new CompletedRawFileOperation(assetInfo.Error, copyPath);
				OperationSystem.StartOperaiton(operation);
				return operation;
			}

			BundleInfo bundleInfo = _bundleServices.GetBundleInfo(assetInfo);
			if (_playMode == EPlayMode.EditorSimulateMode)
			{
				RawFileOperation operation = new EditorPlayModeRawFileOperation(bundleInfo, copyPath);
				OperationSystem.StartOperaiton(operation);
				return operation;
			}
			else if (_playMode == EPlayMode.OfflinePlayMode)
			{
				RawFileOperation operation = new OfflinePlayModeRawFileOperation(bundleInfo, copyPath);
				OperationSystem.StartOperaiton(operation);
				return operation;
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				RawFileOperation operation = new HostPlayModeRawFileOperation(bundleInfo, copyPath);
				OperationSystem.StartOperaiton(operation);
				return operation;
			}
			else
			{
				throw new NotImplementedException();
			}
		}
		#endregion

		#region 资源加载
		/// <summary>
		/// 同步加载资源对象
		/// </summary>
		/// <param name="assetInfo">资源信息</param>
		public AssetOperationHandle LoadAssetSync(AssetInfo assetInfo)
		{
			DebugCheckInitialize();
			return LoadAssetInternal(assetInfo, true);
		}

		/// <summary>
		/// 同步加载资源对象
		/// </summary>
		/// <typeparam name="TObject">资源类型</typeparam>
		/// <param name="location">资源的定位地址</param>
		public AssetOperationHandle LoadAssetSync<TObject>(string location) where TObject : UnityEngine.Object
		{
			DebugCheckInitialize();
			AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(TObject));
			return LoadAssetInternal(assetInfo, true);
		}

		/// <summary>
		/// 同步加载资源对象
		/// </summary>
		/// <param name="location">资源的定位地址</param>
		/// <param name="type">资源类型</param>
		public AssetOperationHandle LoadAssetSync(string location, System.Type type)
		{
			DebugCheckInitialize();
			AssetInfo assetInfo = ConvertLocationToAssetInfo(location, type);
			return LoadAssetInternal(assetInfo, true);
		}


		/// <summary>
		/// 异步加载资源对象
		/// </summary>
		/// <param name="assetInfo">资源信息</param>
		public AssetOperationHandle LoadAssetAsync(AssetInfo assetInfo)
		{
			DebugCheckInitialize();
			return LoadAssetInternal(assetInfo, false);
		}

		/// <summary>
		/// 异步加载资源对象
		/// </summary>
		/// <typeparam name="TObject">资源类型</typeparam>
		/// <param name="location">资源的定位地址</param>
		public AssetOperationHandle LoadAssetAsync<TObject>(string location) where TObject : UnityEngine.Object
		{
			DebugCheckInitialize();
			AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(TObject));
			return LoadAssetInternal(assetInfo, false);
		}

		/// <summary>
		/// 异步加载资源对象
		/// </summary>
		/// <param name="location">资源的定位地址</param>
		/// <param name="type">资源类型</param>
		public AssetOperationHandle LoadAssetAsync(string location, System.Type type)
		{
			DebugCheckInitialize();
			AssetInfo assetInfo = ConvertLocationToAssetInfo(location, type);
			return LoadAssetInternal(assetInfo, false);
		}


		private AssetOperationHandle LoadAssetInternal(AssetInfo assetInfo, bool waitForAsyncComplete)
		{
			var handle = AssetSystem.LoadAssetAsync(assetInfo);
			if (waitForAsyncComplete)
				handle.WaitForAsyncComplete();
			return handle;
		}
		#endregion

		#region 资源加载
		/// <summary>
		/// 同步加载子资源对象
		/// </summary>
		/// <param name="assetInfo">资源信息</param>
		public SubAssetsOperationHandle LoadSubAssetsSync(AssetInfo assetInfo)
		{
			DebugCheckInitialize();
			return LoadSubAssetsInternal(assetInfo, true);
		}

		/// <summary>
		/// 同步加载子资源对象
		/// </summary>
		/// <typeparam name="TObject">资源类型</typeparam>
		/// <param name="location">资源的定位地址</param>
		public SubAssetsOperationHandle LoadSubAssetsSync<TObject>(string location) where TObject : UnityEngine.Object
		{
			DebugCheckInitialize();
			AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(TObject));
			return LoadSubAssetsInternal(assetInfo, true);
		}

		/// <summary>
		/// 同步加载子资源对象
		/// </summary>
		/// <param name="location">资源的定位地址</param>
		/// <param name="type">子对象类型</param>
		public SubAssetsOperationHandle LoadSubAssetsSync(string location, System.Type type)
		{
			DebugCheckInitialize();
			AssetInfo assetInfo = ConvertLocationToAssetInfo(location, type);
			return LoadSubAssetsInternal(assetInfo, true);
		}


		/// <summary>
		/// 异步加载子资源对象
		/// </summary>
		/// <param name="assetInfo">资源信息</param>
		public SubAssetsOperationHandle LoadSubAssetsAsync(AssetInfo assetInfo)
		{
			DebugCheckInitialize();
			return LoadSubAssetsInternal(assetInfo, false);
		}

		/// <summary>
		/// 异步加载子资源对象
		/// </summary>
		/// <typeparam name="TObject">资源类型</typeparam>
		/// <param name="location">资源的定位地址</param>
		public SubAssetsOperationHandle LoadSubAssetsAsync<TObject>(string location) where TObject : UnityEngine.Object
		{
			DebugCheckInitialize();
			AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(TObject));
			return LoadSubAssetsInternal(assetInfo, false);
		}

		/// <summary>
		/// 异步加载子资源对象
		/// </summary>
		/// <param name="location">资源的定位地址</param>
		/// <param name="type">子对象类型</param>
		public SubAssetsOperationHandle LoadSubAssetsAsync(string location, System.Type type)
		{
			DebugCheckInitialize();
			AssetInfo assetInfo = ConvertLocationToAssetInfo(location, type);
			return LoadSubAssetsInternal(assetInfo, false);
		}


		private SubAssetsOperationHandle LoadSubAssetsInternal(AssetInfo assetInfo, bool waitForAsyncComplete)
		{
			var handle = AssetSystem.LoadSubAssetsAsync(assetInfo);
			if (waitForAsyncComplete)
				handle.WaitForAsyncComplete();
			return handle;
		}
		#endregion

		#region 资源下载
		/// <summary>
		/// 创建补丁下载器，用于下载更新资源标签指定的资源包文件
		/// </summary>
		/// <param name="tag">资源标签</param>
		/// <param name="downloadingMaxNumber">同时下载的最大文件数</param>
		/// <param name="failedTryAgain">下载失败的重试次数</param>
		public PatchDownloaderOperation CreatePatchDownloader(string tag, int downloadingMaxNumber, int failedTryAgain)
		{
			DebugCheckInitialize();
			return CreatePatchDownloader(new string[] { tag }, downloadingMaxNumber, failedTryAgain);
		}

		/// <summary>
		/// 创建补丁下载器，用于下载更新资源标签指定的资源包文件
		/// </summary>
		/// <param name="tags">资源标签列表</param>
		/// <param name="downloadingMaxNumber">同时下载的最大文件数</param>
		/// <param name="failedTryAgain">下载失败的重试次数</param>
		public PatchDownloaderOperation CreatePatchDownloader(string[] tags, int downloadingMaxNumber, int failedTryAgain)
		{
			DebugCheckInitialize();
			if (_playMode == EPlayMode.EditorSimulateMode || _playMode == EPlayMode.OfflinePlayMode)
			{
				List<BundleInfo> downloadList = new List<BundleInfo>();
				var operation = new PatchDownloaderOperation(downloadList, downloadingMaxNumber, failedTryAgain);
				return operation;
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				return _hostPlayModeImpl.CreatePatchDownloaderByTags(tags, downloadingMaxNumber, failedTryAgain);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// 创建补丁下载器，用于下载更新当前资源版本所有的资源包文件
		/// </summary>
		/// <param name="downloadingMaxNumber">同时下载的最大文件数</param>
		/// <param name="failedTryAgain">下载失败的重试次数</param>
		public PatchDownloaderOperation CreatePatchDownloader(int downloadingMaxNumber, int failedTryAgain)
		{
			DebugCheckInitialize();
			if (_playMode == EPlayMode.EditorSimulateMode || _playMode == EPlayMode.OfflinePlayMode)
			{
				List<BundleInfo> downloadList = new List<BundleInfo>();
				var operation = new PatchDownloaderOperation(downloadList, downloadingMaxNumber, failedTryAgain);
				return operation;
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				return _hostPlayModeImpl.CreatePatchDownloaderByAll(downloadingMaxNumber, failedTryAgain);
			}
			else
			{
				throw new NotImplementedException();
			}
		}


		/// <summary>
		/// 创建补丁下载器，用于下载更新指定的资源列表依赖的资源包文件
		/// </summary>
		/// <param name="locations">资源定位列表</param>
		/// <param name="downloadingMaxNumber">同时下载的最大文件数</param>
		/// <param name="failedTryAgain">下载失败的重试次数</param>
		public PatchDownloaderOperation CreateBundleDownloader(string[] locations, int downloadingMaxNumber, int failedTryAgain)
		{
			DebugCheckInitialize();
			if (_playMode == EPlayMode.EditorSimulateMode || _playMode == EPlayMode.OfflinePlayMode)
			{
				List<BundleInfo> downloadList = new List<BundleInfo>();
				var operation = new PatchDownloaderOperation(downloadList, downloadingMaxNumber, failedTryAgain);
				return operation;
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				List<AssetInfo> assetInfos = new List<AssetInfo>(locations.Length);
				foreach (var location in locations)
				{
					AssetInfo assetInfo = ConvertLocationToAssetInfo(location, null);
					assetInfos.Add(assetInfo);
				}
				return _hostPlayModeImpl.CreatePatchDownloaderByPaths(assetInfos.ToArray(), downloadingMaxNumber, failedTryAgain);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// 创建补丁下载器，用于下载更新指定的资源列表依赖的资源包文件
		/// </summary>
		/// <param name="assetInfos">资源信息列表</param>
		/// <param name="downloadingMaxNumber">同时下载的最大文件数</param>
		/// <param name="failedTryAgain">下载失败的重试次数</param>
		public PatchDownloaderOperation CreateBundleDownloader(AssetInfo[] assetInfos, int downloadingMaxNumber, int failedTryAgain)
		{
			DebugCheckInitialize();
			if (_playMode == EPlayMode.EditorSimulateMode || _playMode == EPlayMode.OfflinePlayMode)
			{
				List<BundleInfo> downloadList = new List<BundleInfo>();
				var operation = new PatchDownloaderOperation(downloadList, downloadingMaxNumber, failedTryAgain);
				return operation;
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				return _hostPlayModeImpl.CreatePatchDownloaderByPaths(assetInfos, downloadingMaxNumber, failedTryAgain);
			}
			else
			{
				throw new NotImplementedException();
			}
		}
		#endregion

		#region 资源解压
		/// <summary>
		/// 创建补丁解压器
		/// </summary>
		/// <param name="tag">资源标签</param>
		/// <param name="unpackingMaxNumber">同时解压的最大文件数</param>
		/// <param name="failedTryAgain">解压失败的重试次数</param>
		public PatchUnpackerOperation CreatePatchUnpacker(string tag, int unpackingMaxNumber, int failedTryAgain)
		{
			DebugCheckInitialize();
			return CreatePatchUnpacker(new string[] { tag }, unpackingMaxNumber, failedTryAgain);
		}

		/// <summary>
		/// 创建补丁解压器
		/// </summary>
		/// <param name="tags">资源标签列表</param>
		/// <param name="unpackingMaxNumber">同时解压的最大文件数</param>
		/// <param name="failedTryAgain">解压失败的重试次数</param>
		public PatchUnpackerOperation CreatePatchUnpacker(string[] tags, int unpackingMaxNumber, int failedTryAgain)
		{
			DebugCheckInitialize();
			if (_playMode == EPlayMode.EditorSimulateMode)
			{
				List<BundleInfo> downloadList = new List<BundleInfo>();
				var operation = new PatchUnpackerOperation(downloadList, unpackingMaxNumber, failedTryAgain);
				return operation;
			}
			else if (_playMode == EPlayMode.OfflinePlayMode)
			{
				List<BundleInfo> downloadList = new List<BundleInfo>();
				var operation = new PatchUnpackerOperation(downloadList, unpackingMaxNumber, failedTryAgain);
				return operation;
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				return _hostPlayModeImpl.CreatePatchUnpackerByTags(tags, unpackingMaxNumber, failedTryAgain);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		/// <summary>
		/// 创建补丁解压器
		/// </summary>
		/// <param name="unpackingMaxNumber">同时解压的最大文件数</param>
		/// <param name="failedTryAgain">解压失败的重试次数</param>
		public PatchUnpackerOperation CreatePatchUnpacker(int unpackingMaxNumber, int failedTryAgain)
		{
			DebugCheckInitialize();
			if (_playMode == EPlayMode.EditorSimulateMode)
			{
				List<BundleInfo> downloadList = new List<BundleInfo>();
				var operation = new PatchUnpackerOperation(downloadList, unpackingMaxNumber, failedTryAgain);
				return operation;
			}
			else if (_playMode == EPlayMode.OfflinePlayMode)
			{
				List<BundleInfo> downloadList = new List<BundleInfo>();
				var operation = new PatchUnpackerOperation(downloadList, unpackingMaxNumber, failedTryAgain);
				return operation;
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				return _hostPlayModeImpl.CreatePatchUnpackerByAll(unpackingMaxNumber, failedTryAgain);
			}
			else
			{
				throw new NotImplementedException();
			}
		}
		#endregion

		#region 包裹更新
		/// <summary>
		/// 创建资源包裹下载器，用于下载更新指定资源版本所有的资源包文件
		/// </summary>
		/// <param name="resourceVersion">指定更新的资源版本</param>
		/// <param name="timeout">超时时间</param>
		public UpdatePackageOperation UpdatePackageAsync(string manifestName, int timeout = 60)
		{
			DebugCheckInitialize();
			if (_playMode == EPlayMode.EditorSimulateMode)
			{
				var operation = new EditorPlayModeUpdatePackageOperation();
				OperationSystem.StartOperaiton(operation);
				return operation;
			}
			else if (_playMode == EPlayMode.OfflinePlayMode)
			{
				var operation = new OfflinePlayModeUpdatePackageOperation();
				OperationSystem.StartOperaiton(operation);
				return operation;
			}
			else if (_playMode == EPlayMode.HostPlayMode)
			{
				return _hostPlayModeImpl.UpdatePackageAsync(manifestName, timeout);
			}
			else
			{
				throw new NotImplementedException();
			}
		}
		#endregion

		#region 沙盒相关
		/// <summary>
		/// 获取沙盒的根路径
		/// </summary>
		public string GetSandboxRoot()
		{
			return PathHelper.MakePersistentRootPath();
		}

		/// <summary>
		/// 清空沙盒目录
		/// </summary>
		public void ClearSandbox()
		{
			SandboxHelper.DeleteSandbox();
		}

		/// <summary>
		/// 清空所有的缓存文件
		/// </summary>
		public void ClearAllCacheFiles()
		{
			SandboxHelper.DeleteCacheFolder();
		}

		/// <summary>
		/// 清空未被使用的缓存文件
		/// </summary>
		public void ClearUnusedCacheFiles()
		{
			if (_playMode == EPlayMode.HostPlayMode)
				_hostPlayModeImpl.ClearUnusedCacheFiles();
		}
		#endregion

		#region 内部方法
		internal void InternalDestroy()
		{
			_isInitialize = false;
			_initializeError = string.Empty;
			_initializeStatus = EOperationStatus.None;

			_bundleServices = null;
			_locationServices = null;
			_editorSimulateModeImpl = null;
			_offlinePlayModeImpl = null;
			_hostPlayModeImpl = null;

			AssetSystem.DestroyAll();
			YooLogger.Log("YooAssets destroy all !");
		}
		internal void InternalUpdate()
		{
			AssetSystem.Update();
		}

		/// <summary>
		/// 资源定位地址转换为资源完整路径
		/// </summary>
		internal string MappingToAssetPath(string location)
		{
			return _bundleServices.MappingToAssetPath(location);
		}
		#endregion

		#region 调试方法
		[Conditional("DEBUG")]
		private void DebugCheckInitialize()
		{
			if (_initializeStatus == EOperationStatus.None)
				throw new Exception("YooAssets initialize not completed !");
			else if (_initializeStatus == EOperationStatus.Failed)
				throw new Exception($"YooAssets initialize failed : {_initializeError}");
		}

		[Conditional("DEBUG")]
		private void DebugCheckLocation(string location)
		{
			if (string.IsNullOrEmpty(location) == false)
			{
				// 检查路径末尾是否有空格
				int index = location.LastIndexOf(" ");
				if (index != -1)
				{
					if (location.Length == index + 1)
						YooLogger.Warning($"Found blank character in location : \"{location}\"");
				}

				if (location.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
					YooLogger.Warning($"Found illegal character in location : \"{location}\"");
			}
		}
		#endregion

		#region 私有方法
		private AssetInfo ConvertLocationToAssetInfo(string location, System.Type assetType)
		{
			DebugCheckLocation(location);
			string assetPath = _locationServices.ConvertLocationToAssetPath(this,location);
			PatchAsset patchAsset = _bundleServices.TryGetPatchAsset(assetPath);
			if (patchAsset != null)
			{
				AssetInfo assetInfo = new AssetInfo(patchAsset, assetType);
				return assetInfo;
			}
			else
			{
				string error;
				if (string.IsNullOrEmpty(location))
					error = $"The location is null or empty !";
				else
					error = $"The location is invalid : {location}";
				YooLogger.Error(error);
				AssetInfo assetInfo = new AssetInfo(error);
				return assetInfo;
			}
		}
		#endregion
	}
}