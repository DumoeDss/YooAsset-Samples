using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace YooAsset.Editor
{
	[Serializable]
	public class AssetBundleCollectorPackage
	{
		/// <summary>
		/// Package名称
		/// </summary>
		public string PackageName = string.Empty;

		/// <summary>
		/// Package描述
		/// </summary>
		public string PackageDesc = string.Empty;

		/// <summary>
		/// 是否打包
		/// 无论是否打包都会生成Manifest文件
		/// </summary>
		public bool IncludeInBuild;

		/// <summary>
		/// Package的收集器列表
		/// </summary>
		public List<AssetBundleCollectorGroup> Groups = new List<AssetBundleCollectorGroup>();


		/// <summary>
		/// 检测配置错误
		/// </summary>
		public void CheckConfigError()
		{
			foreach (var group in Groups)
			{
				group.CheckConfigError();
			}
		}

		/// <summary>
		/// 获取打包收集的资源文件
		/// </summary>
		public List<CollectAssetInfo> GetAllGroupAssets(EBuildMode buildMode)
		{
			Dictionary<string, CollectAssetInfo> result = new Dictionary<string, CollectAssetInfo>(10000);

			// 收集打包资源
			foreach (var group in Groups)
			{
				var temper = group.GetAllCollectAssets(buildMode, this);
				foreach (var assetInfo in temper)
				{
					if (result.ContainsKey(assetInfo.AssetPath) == false)
						result.Add(assetInfo.AssetPath, assetInfo);
					else
						throw new Exception($"The collecting asset file is existed : {assetInfo.AssetPath} in group : {group.GroupName}");
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
						throw new Exception($"The address is existed : {address} in package : {PackageName}");
				}
			}

			// 返回列表
			return result.Values.ToList();
		}
	}
}