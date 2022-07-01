using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using static YooAsset.Editor.AssetBundleBuilder;

namespace YooAsset.Editor
{
	[TaskAttribute("创建补丁清单文件")]
	public class TaskCreatePatchManifest : IBuildTask
	{
		void IBuildTask.Run(BuildContext context)
		{
			var buildParameters = context.GetContextObject<BuildParametersContext>();
			var buildMapContext = context.GetContextObject<BuildMapContext>();
			var encryptionContext = context.GetContextObject<TaskEncryption.EncryptionContext>();	
			CreatePatchManifestFile(buildParameters, buildMapContext, encryptionContext);
		}

		/// <summary>
		/// 创建补丁清单文件到输出目录
		/// </summary>
		private void CreatePatchManifestFile(BuildParametersContext buildParameters, BuildMapContext buildMapContext,
			TaskEncryption.EncryptionContext encryptionContext)
		{
			int resourceVersion = buildParameters.Parameters.BuildVersion;

			// 创建新补丁清单
			PatchManifest patchManifest = new PatchManifest();
			patchManifest.EnableAddressable = buildParameters.Parameters.EnableAddressable;
			patchManifest.ResourceVersion = buildParameters.Parameters.BuildVersion;
			patchManifest.BuildinTags = buildParameters.Parameters.BuildinTags;
			patchManifest.BundleList = GetAllPatchBundle(buildParameters, buildMapContext, encryptionContext);
			patchManifest.AssetList = GetAllPatchAsset(buildParameters, buildMapContext, patchManifest);

			// 创建补丁清单文件
			string manifestFilePath = $"{buildParameters.PipelineOutputDirectory}/{YooAssetSettingsData.GetPatchManifestFileName("PatchManifest_"+resourceVersion)}";
			BuildRunner.Log($"创建补丁清单文件：{manifestFilePath}");
			PatchManifest.Serialize(manifestFilePath, patchManifest);

			// 创建补丁清单哈希文件
			string manifestHashFilePath = $"{buildParameters.PipelineOutputDirectory}/{YooAssetSettingsData.GetPatchManifestHashFileName("PatchManifest_" + resourceVersion)}";
			string manifestHash = HashUtility.FileMD5(manifestFilePath);
			BuildRunner.Log($"创建补丁清单哈希文件：{manifestHashFilePath}");
			FileUtility.CreateFile(manifestHashFilePath, manifestHash);
		}

		/// <summary>
		/// 获取资源包列表
		/// </summary>
		private List<PatchBundle> GetAllPatchBundle(BuildParametersContext buildParameters, BuildMapContext buildMapContext,
			TaskEncryption.EncryptionContext encryptionContext)
		{
			List<PatchBundle> result = new List<PatchBundle>(1000);

			List<string> buildinTags = buildParameters.Parameters.GetBuildinTags();
			var buildMode = buildParameters.Parameters.BuildMode;
			bool standardBuild = buildMode == EBuildMode.ForceRebuild || buildMode == EBuildMode.IncrementalBuild;
			int count = buildMapContext.BundleInfos.Count;
			for (int i = 0; i < count; i++)
            {
				var bundleInfo = buildMapContext.BundleInfos[i];
				var bundleName = bundleInfo.BundleName;
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

				PatchBundle patchBundle = new PatchBundle(bundleName, bundleName, hash, crc32, size, tags);
				patchBundle.SetFlagsValue(isEncrypted, isBuildin, isRawFile);
				result.Add(patchBundle);
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
		private string GetFileHash(string filePath, bool standardBuild)
		{
			if (standardBuild)
				return HashUtility.FileMD5(filePath);
			else
				return "00000000000000000000000000000000"; //32位
		}
		private string GetFileCRC(string filePath, bool standardBuild)
		{
			if (standardBuild)
				return HashUtility.FileCRC32(filePath);
			else
				return "00000000"; //8位
		}
		private long GetFileSize(string filePath, bool standardBuild)
		{
			if (standardBuild)
				return FileUtility.GetFileSize(filePath);
			else
				return 0;
		}

		/// <summary>
		/// 获取资源列表
		/// </summary>
		private List<PatchAsset> GetAllPatchAsset(BuildParametersContext buildParameters, BuildMapContext buildMapContext, PatchManifest patchManifest)
		{
			List<PatchAsset> result = new List<PatchAsset>(1000);
			foreach (var bundleInfo in buildMapContext.BundleInfos)
			{
				var assetInfos = bundleInfo.GetAllPatchAssetInfos();
				foreach (var assetInfo in assetInfos)
				{
					PatchAsset patchAsset = new PatchAsset();
					if (buildParameters.Parameters.EnableAddressable)
						patchAsset.Address = assetInfo.Address;
					else
						patchAsset.Address = string.Empty;
					patchAsset.AssetPath = assetInfo.AssetPath;
					patchAsset.AssetTags = assetInfo.AssetTags.ToArray();
					patchAsset.BundleID = GetAssetBundleID(assetInfo.GetBundleName(), patchManifest);
					patchAsset.DependIDs = GetAssetBundleDependIDs(patchAsset.BundleID, assetInfo, patchManifest);
					result.Add(patchAsset);
				}
			}
			return result;
		}
		private string[] GetAssetBundleDependIDs(string mainBundleID, BuildAssetInfo assetInfo, PatchManifest patchManifest)
		{
			List<string> result = new List<string>();
			foreach (var dependAssetInfo in assetInfo.AllDependAssetInfos)
			{
				if (dependAssetInfo.HasBundleName())
				{
					string bundleID = GetAssetBundleID(dependAssetInfo.GetBundleName(), patchManifest);
					if (mainBundleID != bundleID)
					{
						if (result.Contains(bundleID) == false)
							result.Add(bundleID);
					}
				}
			}
			return result.ToArray();
		}
		private string GetAssetBundleID(string bundleName, PatchManifest patchManifest)
		{
			var patchBundle = patchManifest.BundleList.Find(_=>_.BundleName== bundleName);
			if (patchBundle != null)
				return patchBundle.Id;
			throw new Exception($"Not found bundle name : {bundleName}");
		}
	}
}