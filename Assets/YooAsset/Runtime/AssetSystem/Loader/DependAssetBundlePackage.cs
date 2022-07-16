using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
	internal class DependAssetBundlePackage
	{
		/// <summary>
		/// ��������Դ���������б�
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
		/// �Ƿ��Ѿ���ɣ����۳ɹ���ʧ�ܣ�
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
		/// ������Դ���Ƿ�ȫ�����سɹ�
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
		/// ��ȡĳ������ʧ�ܵ���Դ��������Ϣ
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
		/// ���̵߳ȴ��첽�������
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
		/// �������ü���
		/// </summary>
		public void Reference()
		{
			foreach (var loader in _dependBundles)
			{
				loader.Reference();
			}
		}

		/// <summary>
		/// �������ü���
		/// </summary>
		public void Release()
		{
			foreach (var loader in _dependBundles)
			{
				loader.Release();
			}
		}

		/// <summary>
		/// ��ȡ��Դ���ĵ�����Ϣ�б�
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