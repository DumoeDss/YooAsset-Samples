﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace YooAsset.Editor
{
	[Serializable]
	public class AssetBundleCollector
	{
		/// <summary>
		/// 收集路径
		/// 注意：支持文件夹或单个资源文件
		/// </summary>
		public string CollectPath = string.Empty;

		public string Address = string.Empty;

		public bool IsAssembly = false;

		/// <summary>
		/// 收集器类型
		/// </summary>
		public ECollectorType CollectorType = ECollectorType.MainAssetCollector;

		/// <summary>
		/// 打包规则类名
		/// </summary>
		public string PackRuleName = nameof(PackGroup);

		/// <summary>
		/// 寻址规则类名
		/// </summary>
		public string AddressRuleName = nameof(AddressByGroupAndFileName);

		/// <summary>
		/// 过滤规则类名
		/// </summary>
		public string FilterRuleName = nameof(CollectAll);

		/// <summary>
		/// 资源分类标签
		/// </summary>
		public string AssetTags = string.Empty;


		/// <summary>
		/// 收集器是否有效
		/// </summary>
		public bool IsValid()
		{
			if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(CollectPath) == null)
				return false;

            if (CollectorType== ECollectorType.MainAssetCollector&&string.IsNullOrEmpty(Address))
            {
				UnityEngine.Debug.LogError(CollectPath + " 的Address 为空，请检查! ");
				return false;
			}

			//if (CollectorType == ECollectorType.None)
			//	return false;

			//if (AssetBundleCollectorSettingData.HasPackRuleName(PackRuleName) == false)
			//	return false;

			if (AssetBundleCollectorSettingData.HasFilterRuleName(FilterRuleName) == false)
				return false;

			return true;
		}

		/// <summary>
		/// 检测配置错误
		/// </summary>
		public void CheckConfigError()
		{
			if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(CollectPath) == null)
				throw new Exception($"Invalid collect path : {CollectPath}");

			if (AssetBundleCollectorSettingData.HasFilterRuleName(FilterRuleName) == false)
				throw new Exception($"Invalid {nameof(IFilterRule)} class type : {FilterRuleName} in collector : {CollectPath}");
		}

		/// <summary>
		/// 获取打包收集的资源文件
		/// </summary>
		public List<CollectAssetInfo> GetAllCollectAssets(EBuildMode buildMode, AssetBundleCollectorPackage package, AssetBundleCollectorGroup group)
		{
			// 注意：模拟构建模式下只收集主资源
			if (buildMode == EBuildMode.SimulateBuild)
			{
				if (CollectorType != ECollectorType.MainAssetCollector)
					return new List<CollectAssetInfo>();
			}

			Dictionary<string, CollectAssetInfo> result = new Dictionary<string, CollectAssetInfo>(1000);
			bool isRawAsset = PackRuleName == nameof(PackRawFile);

			// 检测原生资源包的收集器类型
			if (isRawAsset && CollectorType != ECollectorType.MainAssetCollector)
				throw new Exception($"The raw file must be set to {nameof(ECollectorType)}.{ECollectorType.MainAssetCollector} : {CollectPath}");

			if (string.IsNullOrEmpty(CollectPath))
				throw new Exception($"The collect path is null or empty in group : {group.GroupName}");

			// 收集打包资源
			if (AssetDatabase.IsValidFolder(CollectPath))
			{
				string collectDirectory = CollectPath;
				string[] findAssets = EditorTools.FindAssets(EAssetSearchType.All, collectDirectory);
				foreach (string assetPath in findAssets)
				{
					if (IsValidateAsset(assetPath) && IsCollectAsset(assetPath))
					{
						if (result.ContainsKey(assetPath) == false)
						{
							var collectAssetInfo = CreateCollectAssetInfo(buildMode, package, group, assetPath, isRawAsset, IsAssembly);
							result.Add(assetPath, collectAssetInfo);
						}
						else
						{
							throw new Exception($"The collecting asset file is existed : {assetPath} in collector : {CollectPath}");
						}
					}
				}
			}
			else
			{
				string assetPath = CollectPath;
				if (IsValidateAsset(assetPath) && IsCollectAsset(assetPath))
				{
					var collectAssetInfo = CreateCollectAssetInfo(buildMode, package, group, assetPath, isRawAsset, IsAssembly);
					result.Add(assetPath, collectAssetInfo);
				}
				else
				{
					throw new Exception($"The collecting single asset file is invalid : {assetPath} in collector : {CollectPath}");
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
						throw new Exception($"The address is existed : {address} in collector : {CollectPath}");
				}
			}

			// 返回列表
			return result.Values.ToList();
		}

		private CollectAssetInfo CreateCollectAssetInfo(EBuildMode buildMode, AssetBundleCollectorPackage package, AssetBundleCollectorGroup group, string assetPath, bool isRawAsset, bool isAssembly)
		{
			string address = GetAddress(group, assetPath);
			string bundleName = GetBundleName(group, assetPath);
			List<string> assetTags = GetAssetTags(group);
			CollectAssetInfo collectAssetInfo = new CollectAssetInfo(CollectorType, 
				package.PackageName,
				package.IncludeInBuild, 
				bundleName, 
				address, 
				assetPath, 
				assetTags, 
				isRawAsset,
				isAssembly);

			// 注意：模拟构建模式下不需要收集依赖资源
			if (buildMode == EBuildMode.SimulateBuild)
				collectAssetInfo.DependAssets = new List<string>();
			else
				collectAssetInfo.DependAssets = GetAllDependencies(assetPath);
			return collectAssetInfo;
		}
		private bool IsValidateAsset(string assetPath)
		{
			if (assetPath.StartsWith("Assets/") == false && assetPath.StartsWith("Packages/") == false)
				return false;

			if (AssetDatabase.IsValidFolder(assetPath))
				return false;

			// 注意：忽略编辑器下的类型资源
			Type type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
			if (type == typeof(LightingDataAsset))
				return false;

			string ext = System.IO.Path.GetExtension(assetPath);
			if (ext == "" || ext == ".dll" || ext == ".cs" || ext == ".js" || ext == ".boo" || ext == ".meta" || ext == ".cginc")
				return false;

			return true;
		}
		private bool IsCollectAsset(string assetPath)
		{
			// 根据规则设置过滤资源文件
			IFilterRule filterRuleInstance = AssetBundleCollectorSettingData.GetFilterRuleInstance(FilterRuleName);
			return filterRuleInstance.IsCollectAsset(new FilterRuleData(assetPath));
		}
		private string GetAddress(AssetBundleCollectorGroup group, string assetPath)
		{
			if (CollectorType != ECollectorType.MainAssetCollector)
				return string.Empty;

			IAddressRule addressRuleInstance = AssetBundleCollectorSettingData.GetAddressRuleInstance(AddressRuleName);
			string adressValue = addressRuleInstance.GetAssetAddress(new AddressRuleData(assetPath, Address, CollectPath, group.GroupName));
			return adressValue;
		}
		private string GetBundleName(AssetBundleCollectorGroup group, string assetPath)
		{
			// 根据规则设置获取资源包名称
			{
				IPackRule packRuleInstance = AssetBundleCollectorSettingData.GetPackRuleInstance(PackRuleName);
				string bundleName = packRuleInstance.GetBundleName(new PackRuleData(assetPath, group.GroupName, CollectPath, Address));
				return EditorTools.GetRegularPath(bundleName).ToLower();
			}
		}
		private List<string> GetAssetTags(AssetBundleCollectorGroup group)
		{
			List<string> tags = StringUtility.StringToStringList(group.AssetTags, ';');
			List<string> temper = StringUtility.StringToStringList(AssetTags, ';');
			tags.AddRange(temper);
			return tags;
		}
		private List<string> GetAllDependencies(string mainAssetPath)
		{
			List<string> result = new List<string>();
			string[] depends = AssetDatabase.GetDependencies(mainAssetPath, true);
			foreach (string assetPath in depends)
			{
				if (IsValidateAsset(assetPath))
				{
					// 注意：排除主资源对象
					if (assetPath != mainAssetPath)
						result.Add(assetPath);
				}
			}
			return result;
		}
	}
}