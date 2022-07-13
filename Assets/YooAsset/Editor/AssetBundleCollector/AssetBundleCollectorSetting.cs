using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YooAsset.Editor
{
	public class AssetBundleCollectorSetting : ScriptableObject
	{

		public List<AssetBundleCollectorPackage> Packages = new List<AssetBundleCollectorPackage>();

		/// <summary>
		/// 检测配置错误
		/// </summary>
		public void CheckConfigError()
		{
			foreach (var package in Packages)
			{
				package.CheckConfigError();
			}
		}

		/// <summary>
		/// 获取打包收集的资源文件
		/// </summary>
		public List<CollectAssetInfo> GetAllCollectAssets(EBuildMode buildMode)
		{
			Dictionary<string, CollectAssetInfo> result = new Dictionary<string, CollectAssetInfo>(10000);

            foreach (var package in Packages)
            {
				var temper = package.GetAllGroupAssets(buildMode);
				foreach (var assetInfo in temper)
				{
					if (!result.ContainsKey(assetInfo.AssetPath))
						result.Add(assetInfo.AssetPath, assetInfo);
					else
						throw new Exception($"The collecting asset file is existed : {assetInfo.AssetPath} in group setting.");
				}
			}

			// 检测可寻址地址是否重复
			HashSet<string> adressTemper = new HashSet<string>();
			foreach (var collectInfoPair in result)
			{
				if (collectInfoPair.Value.CollectorType == ECollectorType.MainAssetCollector)
				{
					string address = collectInfoPair.Value.Address;
					if (adressTemper.Contains(address) == false)
						adressTemper.Add(address);
					else
						throw new Exception($"The address is existed : {address} in group setting.");
				}
			}

			// 返回列表
			return result.Values.ToList();
		}
	}
}