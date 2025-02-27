﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace YooAsset.Editor
{
	public class BuildAssetInfo
	{
		private string _mainBundleName;
		private string _shareBundleName;
		private bool _isAddAssetTags = false;
		private readonly HashSet<string> _referenceBundleNames = new HashSet<string>();

		/// <summary>
		/// 收集器类型
		/// </summary>
		public ECollectorType CollectorType { private set; get; }

		/// <summary>
		/// 包名
		/// </summary>
		public string Package { private set; get; }

		public bool IncludeInBuild { private set; get; }

		/// <summary>
		/// 可寻址地址
		/// </summary>
		public string Address { private set; get; }

		/// <summary>
		/// 资源路径
		/// </summary>
		public string AssetPath { private set; get; }

		/// <summary>
		/// 是否为原生资源
		/// </summary>
		public bool IsRawAsset { private set; get; }

		public bool IsAssemblyAsset { private set; get; }

		/// <summary>
		/// 是否为着色器资源
		/// </summary>
		public bool IsShaderAsset { private set; get; }

		/// <summary>
		/// 资源的分类标签
		/// </summary>
		public readonly List<string> AssetTags = new List<string>();

		/// <summary>
		/// 资源包的分类标签
		/// </summary>
		public readonly List<string> BundleTags = new List<string>();

		/// <summary>
		/// 依赖的所有资源
		/// 注意：包括零依赖资源和冗余资源（资源包名无效）
		/// </summary>
		public List<BuildAssetInfo> AllDependAssetInfos { private set; get; }

		public BuildAssetInfo(BuildAssetInfo clone)
        {
			Package = clone.Package;
			_mainBundleName = $"{Package}@{clone._mainBundleName}";
			_shareBundleName= $"{Package}@{clone._shareBundleName}";
			CollectorType = clone.CollectorType;
			Address = clone.Address;
			IncludeInBuild = clone.IncludeInBuild;
			AssetPath = clone.AssetPath;
			IsRawAsset = clone.IsRawAsset;
			IsShaderAsset = clone.IsShaderAsset;
			IsAssemblyAsset=clone.IsAssemblyAsset;
			if (clone.AllDependAssetInfos!=null)
				AllDependAssetInfos = new List<BuildAssetInfo>(clone.AllDependAssetInfos);
			AssetTags = new List<string>(clone.AssetTags);
			BundleTags = new List<string>(clone.BundleTags);
			_referenceBundleNames = new HashSet<string>(clone._referenceBundleNames);
		}

		public BuildAssetInfo(ECollectorType collectorType,string package, bool includeInBuild,
			string mainBundleName, string address, string assetPath, bool isRawAsset,
			bool isAssemblyAsset)
		{

			_mainBundleName = mainBundleName;
			CollectorType = collectorType;
			Package = package;
			Address = address;
			IncludeInBuild=includeInBuild;
			AssetPath = assetPath;
			IsRawAsset = isRawAsset;
			IsAssemblyAsset = isAssemblyAsset;

			System.Type assetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(assetPath);
			if (assetType == typeof(UnityEngine.Shader))
				IsShaderAsset = true;
			else
				IsShaderAsset = false;
		}
		public BuildAssetInfo(string assetPath, string package, bool includeInBuild)
		{
			CollectorType = ECollectorType.None;
			Address = "ShaderAsset";
			Package = package;
			IncludeInBuild = includeInBuild;
			AssetPath = assetPath;
			IsRawAsset = false;
			IsAssemblyAsset = false;
			System.Type assetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(assetPath);
			if (assetType == typeof(UnityEngine.Shader))
				IsShaderAsset = true;
			else
				IsShaderAsset = false;
		}


		/// <summary>
		/// 设置所有依赖的资源
		/// </summary>
		public void SetAllDependAssetInfos(List<BuildAssetInfo> dependAssetInfos)
		{
			if (AllDependAssetInfos != null)
				throw new System.Exception("Should never get here !");

			AllDependAssetInfos = dependAssetInfos;
		}

		/// <summary>
		/// 添加资源的分类标签
		/// 说明：原始定义的资源分类标签
		/// </summary>
		public void AddAssetTags(List<string> tags)
		{
			if (_isAddAssetTags)
				throw new Exception("Should never get here !");
			_isAddAssetTags = true;

			foreach (var tag in tags)
			{
				if (AssetTags.Contains(tag) == false)
				{
					AssetTags.Add(tag);
				}
			}
		}
		
		/// <summary>
		/// 添加资源包的分类标签
		/// 说明：传染算法统计到的分类标签
		/// </summary>
		public void AddBundleTags(List<string> tags)
		{
			foreach (var tag in tags)
			{
				if (BundleTags.Contains(tag) == false)
				{
					BundleTags.Add(tag);
				}
			}
		}

		/// <summary>
		/// 资源包名是否存在
		/// </summary>
		public bool HasBundleName()
		{
			string bundleName = GetBundleName();
			if (string.IsNullOrEmpty(bundleName))
				return false;
			else
				return true;
		}

		/// <summary>
		/// 获取资源包名称
		/// </summary>
		public string GetBundleName()
		{
			if (CollectorType == ECollectorType.None)
				return _shareBundleName;
			else
				return _mainBundleName;
		}

		/// <summary>
		/// 添加关联的资源包名称
		/// </summary>
		public void AddReferenceBundleName(string bundleName)
		{
			if (string.IsNullOrEmpty(bundleName))
				throw new Exception("Should never get here !");

			if (_referenceBundleNames.Contains(bundleName) == false)
				_referenceBundleNames.Add(bundleName);
		}

		/// <summary>
		/// 计算主资源或共享资源的完整包名
		/// </summary>
		public void CalculateFullBundleName()
		{
			if (CollectorType == ECollectorType.None)
			{
				if (IsRawAsset)
					throw new Exception("Should never get here !");

				if(_referenceBundleNames.Count > 0){
					IPackRule packRule = PackGroup.StaticPackRule;
					string groupName, bundleName;
					if(_referenceBundleNames.Count == 1)
                    {
						bundleName = groupName = "AutoDependencies";
                    }
                    else
                    {
						bundleName = groupName = "SharedAutoDependencies";
					}
					var shareBundleName = $"share_{bundleName}";
					_shareBundleName = EditorTools.GetRegularPath(shareBundleName).ToLower();
					var package = AssetBundleCollectorSettingData.Setting.Packages.Find(_ => _.PackageName == Package);
                    if (package != null)
                    {
						var group = package.Groups.Find(_ => _.GroupName == groupName);
						if(group == null)
                        {
							group = new AssetBundleCollectorGroup()
							{
								GroupName = groupName,
								GroupDesc = "自动依赖",
								PackRuleName = "PackGroup",
							};
							
							package.Groups.Add(group);

                        }
						group.Collectors.Add(new AssetBundleCollector()
						{
							CollectPath = AssetPath,
							CollectorType = ECollectorType.DependAssetCollector,
						});
					}

				}

			}
			else
			{
				if (IsRawAsset)
				{
					string mainBundleName = $"{_mainBundleName}.{YooAssetSettingsData.Setting.RawFileVariant}";
					_mainBundleName = EditorTools.GetRegularPath(mainBundleName).ToLower();
				}
				else
				{
					string mainBundleName = $"{_mainBundleName}";//.{YooAssetSettingsData.Setting.AssetBundleFileVariant}
					_mainBundleName = EditorTools.GetRegularPath(mainBundleName).ToLower(); ;
				}
			}
		}
	}
}