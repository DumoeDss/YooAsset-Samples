using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
	internal class DependAssetBundlePackage
	{
		/// <summary>
		/// 依赖的资源包加载器列表
		/// </summary>
		private readonly List<AssetBundleLoaderBase> _dependBundles;

		Dictionary<string, List<string>> _dependencies;

		bool isInit;

		public DependAssetBundlePackage(string[] dpendBundles)
		{
			_dependencies=new Dictionary<string, List<string>>();
			foreach (var d in dpendBundles)
            {
				var data = d.Split('@');
                if (!_dependencies.ContainsKey(data[0]))
                {
					_dependencies[data[0]] = new List<string>();
                }
				_dependencies[data[0]].Add(data[1]);
			}
			isInit = false;
			_dependBundles = new List<AssetBundleLoaderBase>();
			GetBundleLoader().Forget();
		}

		async UniTask GetBundleLoader()
        {
			foreach (var item in _dependencies)
			{
				_dependBundles.AddRange( await YooAssetsManager.Instance.GetBundleLoader(item.Key, item.Value));
			}
			isInit = true;
			Reference();
		}

		/// <summary>
		/// 是否已经完成（无论成功或失败）
		/// </summary>
		public bool IsDone()
		{
			if(!isInit)
				return false;
			foreach (var loader in _dependBundles)
			{
				if (loader.IsDone() == false)
					return false;
			}
			return true;
		}

		/// <summary>
		/// 依赖资源包是否全部加载成功
		/// </summary>
		public bool IsSucceed()
		{
			if (!isInit)
				return false;
			foreach (var loader in _dependBundles)
			{
				if (loader.Status != AssetBundleLoaderBase.EStatus.Succeed)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// 获取某个加载失败的资源包错误信息
		/// </summary>
		public string GetLastError()
		{
			foreach (var loader in _dependBundles)
			{
				if (loader.Status != AssetBundleLoaderBase.EStatus.Succeed)
				{
					return loader.LastError;
				}
			}
			return string.Empty;
		}

		/// <summary>
		/// 主线程等待异步操作完毕
		/// </summary>
		public void WaitForAsyncComplete()
		{
			if(_dependBundles!=null)
			foreach (var loader in _dependBundles)
			{
				if (loader.IsDone() == false)
					loader.WaitForAsyncComplete();
			}
		}

		/// <summary>
		/// 增加引用计数
		/// </summary>
		public void Reference()
		{
			foreach (var loader in _dependBundles)
			{
				loader.Reference();
			}
		}

		/// <summary>
		/// 减少引用计数
		/// </summary>
		public void Release()
		{
			foreach (var loader in _dependBundles)
			{
				loader.Release();
			}
		}

		/// <summary>
		/// 获取资源包的调试信息列表
		/// </summary>
		internal void GetBundleDebugInfos(List<DebugBundleInfo> output)
		{
			foreach (var loader in _dependBundles)
			{
				var bundleInfo = new DebugBundleInfo();
				bundleInfo.BundleName = loader.MainBundleInfo.BundleName;
				bundleInfo.RefCount = loader.RefCount;
				bundleInfo.Status = (int)loader.Status;
				output.Add(bundleInfo);
			}
		}
	}
}