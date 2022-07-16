using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using static YooAsset.Editor.AssetBundleBuilder;
using System.Linq;
using UnityEngine;

namespace YooAsset.Editor
{
	public class CreatePatchManifestContext : IContextObject
	{
		public List<string> PatchManifestPaths { get; set; }
	}

		[TaskAttribute("创建补丁清单文件")]
	public class TaskCreatePatchManifest : IBuildTask
	{
		void IBuildTask.Run(BuildContext context)
		{
			var buildParameters = context.GetContextObject<BuildParametersContext>();
			var buildMapContext = context.GetContextObject<BuildMapContext>();
			var encryptionContext = context.GetContextObject<TaskEncryption.EncryptionContext>();	
			var createPatchManifestContext =  CreatePatchManifestFile(buildParameters, buildMapContext, encryptionContext);
			context.SetContextObject(createPatchManifestContext);
		}

		/// <summary>
		/// 创建补丁清单文件到输出目录
		/// </summary>
		private CreatePatchManifestContext CreatePatchManifestFile(BuildParametersContext buildParameters, BuildMapContext buildMapContext,
			TaskEncryption.EncryptionContext encryptionContext)
		{
			CreatePatchManifestContext createPatchManifestContext = new CreatePatchManifestContext();
			createPatchManifestContext.PatchManifestPaths = new List<string>();
			int resourceVersion = buildParameters.Parameters.BuildVersion;
			var bundles= GetAllPatchBundle(buildParameters, buildMapContext, encryptionContext);
			// 创建新补丁清单
			foreach (var bundle in bundles)
            {
				var patchManifest = new PatchManifest();
				patchManifest.ResourceVersion = buildParameters.Parameters.BuildVersion;
				patchManifest.BuildinTags = buildParameters.Parameters.BuildinTags;
				patchManifest.PackageName = bundle.Key;
				patchManifest.BundleList = bundle.Value;
				patchManifest.DependPackages = new List<string>();
				patchManifest.DependBundles = new List<string>();

				var patchAssets = GetAllPatchAsset(buildParameters, buildMapContext, patchManifest.PackageName);
				patchManifest.AssetList=patchAssets.Select(_=>_).Where(_=>!string.IsNullOrEmpty(_.Address)).ToList();
				foreach (var item in patchManifest.AssetList)
				{
					if (item.DependIDs != null)
					{
						foreach (var dependID in item.DependIDs)
						{
							if (dependID.Contains("@"))
							{
								var depend = dependID.Split('@');
								if (!patchManifest.DependPackages.Contains(depend[0]))
								{
									patchManifest.DependPackages.Add(depend[0]);
								}

							}
						}
					}
				}

				var dependAssets = patchAssets.Select(_ => _).Where(_ => string.IsNullOrEmpty(_.Address)).ToList();
				if(dependAssets!=null&& dependAssets.Count() > 0)
                {
                    foreach (var item in dependAssets)
                    {
						if (item.DependIDs != null)
						{
							foreach (var dependID in item.DependIDs)
							{
								if (dependID.Contains("@"))
								{
									var depend = dependID.Split('@');
									if (!patchManifest.DependPackages.Contains(depend[0]))
									{
										patchManifest.DependPackages.Add(depend[0]);
									}
								}
								if (!patchManifest.DependBundles.Contains(dependID))
								{
									patchManifest.DependBundles.Add(dependID);
								}
							}
						}
					}
                }
		
				// 创建补丁清单文件
				string manifestFilePath = $"{buildParameters.PipelineOutputDirectory}/{"Manifest_" + patchManifest.PackageName}";
				BuildRunner.Log($"创建补丁清单文件：{manifestFilePath}");
				PatchManifest.Serialize(manifestFilePath, patchManifest);
				var crc = GetFileCRC(manifestFilePath);
				var path = $"{manifestFilePath}_{crc}";
				if(File.Exists(path))
					File.Delete(path);
				File.Move(manifestFilePath, path);
				createPatchManifestContext.PatchManifestPaths.Add($"{"Manifest_" + patchManifest.PackageName}_{crc}");
				// 创建补丁清单哈希文件
				string manifestHashFilePath = $"{buildParameters.PipelineOutputDirectory}/{YooAssetSettingsData.GetPatchManifestHashFileName($"{"Manifest_" + patchManifest.PackageName}_{crc}")}";
				BuildRunner.Log($"创建补丁清单哈希文件：{manifestHashFilePath}");
				var info = new FileInfo(path);

				YooAssetVersion yooAssetVersion = new YooAssetVersion()
				{
					crc = crc,
					version = 1,
					size = info.Length
				};

				var json = JsonUtility.ToJson(yooAssetVersion);
				FileUtility.CreateFile(manifestHashFilePath, json);

			}

			return createPatchManifestContext;
		}

		/// <summary>
		/// 获取资源包列表
		/// </summary>
		private Dictionary<string, List<PatchBundle>> GetAllPatchBundle(BuildParametersContext buildParameters, BuildMapContext buildMapContext,
			TaskEncryption.EncryptionContext encryptionContext)
		{
			Dictionary<string, List<PatchBundle>> result = new Dictionary<string, List<PatchBundle>>(1000);

			List<string> buildinTags = buildParameters.Parameters.GetBuildinTags();
			var buildMode = buildParameters.Parameters.BuildMode;
			bool standardBuild = buildMode == EBuildMode.ForceRebuild || buildMode == EBuildMode.IncrementalBuild;
			int count = buildMapContext.BundleInfos.Count;

			for (int i = 0; i < count; i++)
            {
				var bundleInfo = buildMapContext.BundleInfos[i];
				var bundleName = bundleInfo.BundleName;
				var package = bundleInfo.Package;
				var includeInBuild = bundleInfo.IncludeInBuild;
                if (!includeInBuild)
                {
					continue;
                }
				if (!result.ContainsKey(package)){
					result[package] = new List<PatchBundle>();
				}

				string filePath = $"{buildParameters.PipelineOutputDirectory}/{bundleName}";
				string hash = GetFileHash(filePath, standardBuild);
				string crc32 = GetFileCRC(filePath, standardBuild);
				long size = GetFileSize(filePath, standardBuild);
				string[] tags = buildMapContext.GetBundleTags(bundleName);
				bool isEncrypted = encryptionContext.IsEncryptFile(bundleName);
				bool isBuildin = IsBuildinBundle(tags, buildinTags);
				bool isRawFile = bundleInfo.IsRawFile;

				// 附加文件扩展名
				if (buildParameters.Parameters.AppendFileExtension)
				{
					hash += bundleInfo.GetAppendExtension();
				}

				PatchBundle patchBundle = new PatchBundle(package, bundleName, hash, crc32, size, tags);
				patchBundle.SetFlagsValue(isEncrypted, isBuildin, isRawFile);
				result[package].Add(patchBundle);
			}
			return result;
		}
		private bool IsBuildinBundle(string[] bundleTags, List<string> buildinTags)
		{
			// 注意：没有任何分类标签的Bundle文件默认为内置文件
			if (bundleTags.Length == 0)
				return true;

			foreach (var tag in bundleTags)
			{
				if (buildinTags.Contains(tag))
					return true;
			}
			return false;
		}
		private string GetFileHash(string filePath, bool standardBuild = true)
		{
			if (standardBuild)
				return HashUtility.FileMD5(filePath);
			else
				return "00000000000000000000000000000000"; //32位
		}
		private string GetFileCRC(string filePath, bool standardBuild=true)
		{
			if (standardBuild)
				return HashUtility.FileCRC32(filePath);
			else
				return "00000000"; //8位
		}
		private long GetFileSize(string filePath, bool standardBuild = true)
		{
			if (standardBuild)
				return FileUtility.GetFileSize(filePath);
			else
				return 0;
		}

		/// <summary>
		/// 获取资源列表
		/// </summary>
		private List<PatchAsset> GetAllPatchAsset(BuildParametersContext buildParameters, 
			BuildMapContext buildMapContext, string package)
		{
			List<PatchAsset> result = new List<PatchAsset>(1000);

			foreach (var bundleInfo in buildMapContext.BundleInfos)
			{
				if (bundleInfo.Package == package)
                {
					var assetInfos = bundleInfo.GetAllPatchAssetInfos();
                    if (assetInfos == null||assetInfos.Length==0)
                    {
						assetInfos = bundleInfo.GetAllDependPatchAssetInfos();
						foreach (var assetInfo in assetInfos)
						{
							PatchAsset patchAsset = new PatchAsset();
							patchAsset.Address = string.Empty;
							patchAsset.DependIDs = GetAssetBundleDependIDs(assetInfo.GetBundleName(), assetInfo);
							result.Add(patchAsset);
						}
					}
                    else
                    {
						foreach (var assetInfo in assetInfos)
						{
							PatchAsset patchAsset = new PatchAsset();
							patchAsset.Address = assetInfo.Address;
							patchAsset.AssetPath = assetInfo.AssetPath;
							patchAsset.AssetTags = assetInfo.AssetTags.ToArray();
							patchAsset.BundleID = assetInfo.GetBundleName();
							patchAsset.DependIDs = GetAssetBundleDependIDs(patchAsset.BundleID, assetInfo);
							result.Add(patchAsset);
						}
					}
					
				}
					
			}
			return result;
		}
		private string[] GetAssetBundleDependIDs(string mainBundleID, BuildAssetInfo assetInfo)
		{
			List<string> result = new List<string>();
			foreach (var dependAssetInfo in assetInfo.AllDependAssetInfos)
			{
				if (dependAssetInfo.HasBundleName())
				{
					string bundleID = dependAssetInfo.GetBundleName();
					if (mainBundleID != bundleID)
					{
						if (!result.Contains(bundleID))
							result.Add(bundleID);
					}
				}
			}
			return result.ToArray();
		}
		private string GetAssetBundleID(string bundleName, string package, List<PatchManifest> patchManifests)
		{
			var patchManifest = patchManifests.Find(_ => _.PackageName == package);
			var patchBundle = patchManifest.BundleList.Find(_=>_.BundleName== bundleName);
			if (patchBundle != null)
				return patchBundle.BundleName;
			throw new Exception($"Not found bundle name : {bundleName}");
		}
	}
}