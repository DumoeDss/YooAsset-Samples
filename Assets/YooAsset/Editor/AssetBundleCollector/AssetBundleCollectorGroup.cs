﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace YooAsset.Editor
{	
	[Serializable]
	public class AssetBundleCollectorGroup
	{
		/// <summary>
		/// 分组名称
		/// </summary>
		public string GroupName = string.Empty;

		/// <summary>
		/// 分组描述
		/// </summary>
		public string GroupDesc = string.Empty;

		/// <summary>
		/// 资源分类标签
		/// </summary>
		public string AssetTags = string.Empty;

		/// <summary>
		/// 打包规则类名
		/// </summary>
		public string PackRuleName = nameof(PackGroup);

		/// <summary>
		/// 分组的收集器列表
		/// </summary>
		public List<AssetBundleCollector> Collectors = new List<AssetBundleCollector>();


		/// <summary>
		/// 检测配置错误
		/// </summary>
		public void CheckConfigError()
		{

			if (AssetBundleCollectorSettingData.HasPackRuleName(PackRuleName) == false)
				throw new Exception($"Invalid {nameof(IPackRule)} class type : {PackRuleName} in collector : {GroupName}");

			foreach (var collector in Collectors)
			{
				collector.CheckConfigError();
			}
		}

		/// <summary>
		/// 获取打包收集的资源文件
		/// </summary>
		public List<CollectAssetInfo> GetAllCollectAssets(EBuildMode buildMode,AssetBundleCollectorPackage package)
		{
			Dictionary<string, CollectAssetInfo> result = new Dictionary<string, CollectAssetInfo>(10000);

			// 收集打包资源
			foreach (var collector in Collectors)
			{
				var temper = collector.GetAllCollectAssets(buildMode, package, this);
				foreach (var assetInfo in temper)
				{
					if (!result.ContainsKey(assetInfo.AssetPath))
						result.Add(assetInfo.AssetPath, assetInfo);
					else
						throw new Exception($"The collecting asset file is existed : {assetInfo.AssetPath} in group : {GroupName}");
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
						throw new Exception($"The address is existed : {address} in group : {GroupName}");
				}
			}

			// 返回列表
			return result.Values.ToList();
		}
	}
}