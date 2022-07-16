using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    internal abstract class BundledProvider : ProviderBase
    {
		protected AssetBundleLoaderBase OwnerBundle { private set; get; }
		protected DependAssetBundleGroup DependBundleGroup { private set; get; }
		protected DependAssetBundlePackage DependAssetBundlePackage { private set; get; }

		protected string[] OtherPackageDependBundles { get; private set; }

		public BundledProvider(AssetInfo assetInfo, AssetSystem assetSystem) : base(assetInfo, assetSystem)
		{
			OwnerBundle = assetSystem.CreateOwnerAssetBundleLoader(assetInfo);
			OwnerBundle.Reference();
			OwnerBundle.AddProvider(this);

			OtherPackageDependBundles = assetSystem.CreateOtherPackageDependAssetBundleLoaders(assetInfo);
			if(OtherPackageDependBundles != null&& OtherPackageDependBundles.Length > 0)
            {
				DependAssetBundlePackage = new DependAssetBundlePackage(OtherPackageDependBundles);
			}

			var dependBundles = assetSystem.CreateDependAssetBundleLoaders(assetInfo);
			DependBundleGroup = new DependAssetBundleGroup(dependBundles);
			DependBundleGroup.Reference();
		}

		public override void Destroy()
		{
			base.Destroy();

			// 释放资源包
			if (OwnerBundle != null)
			{
				OwnerBundle.Release();
				OwnerBundle = null;
			}

            if (DependAssetBundlePackage != null)
            {
				DependAssetBundlePackage.Release();
				DependAssetBundlePackage = null;
			}

			if (DependBundleGroup != null)
			{
				DependBundleGroup.Release();
				DependBundleGroup = null;
			}
		}

		/// <summary>
		/// 获取资源包的调试信息列表
		/// </summary>
		internal void GetBundleDebugInfos(List<DebugBundleInfo> output)
		{
			var bundleInfo = new DebugBundleInfo();
			bundleInfo.BundleName = OwnerBundle.MainBundleInfo.BundleName;
			bundleInfo.RefCount = OwnerBundle.RefCount;
			bundleInfo.Status = (int)OwnerBundle.Status;
			output.Add(bundleInfo);

			DependBundleGroup.GetBundleDebugInfos(output);
		}
	}
}