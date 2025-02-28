﻿using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace YooAsset.Editor
{
	[TaskAttribute("制作补丁包")]
	public class TaskCreatePatchPackage : IBuildTask
	{
		void IBuildTask.Run(BuildContext context)
		{
			var buildParameters = context.GetContextObject<AssetBundleBuilder.BuildParametersContext>();
			var createPatchManifestContext = context.GetContextObject<CreatePatchManifestContext>();

			var buildMode = buildParameters.Parameters.BuildMode;
			if (buildMode == EBuildMode.ForceRebuild || buildMode == EBuildMode.IncrementalBuild)
			{
				CopyPatchFiles(buildParameters, createPatchManifestContext);
			}
		}

		/// <summary>
		/// 拷贝补丁文件到补丁包目录
		/// </summary>
		private void CopyPatchFiles(AssetBundleBuilder.BuildParametersContext buildParameters, CreatePatchManifestContext createPatchManifestContext)
		{
			int resourceVersion = buildParameters.Parameters.BuildVersion;
			string packageDirectory = buildParameters.GetPackageDirectory();
			BuildRunner.Log($"开始拷贝补丁文件到补丁包目录：{packageDirectory}");

			// 拷贝Report文件
			{
				string reportFileName = YooAssetSettingsData.GetReportFileName(buildParameters.Parameters.BuildVersion);
				string sourcePath = $"{buildParameters.PipelineOutputDirectory}/{reportFileName}";
				string destPath = $"{packageDirectory}/{reportFileName}";
				EditorTools.CopyFile(sourcePath, destPath, true);
			}

			// 拷贝UnityManifest序列化文件
			{
				string sourcePath = $"{buildParameters.PipelineOutputDirectory}/{YooAssetSettingsData.Setting.UnityManifestFileName}";
				string destPath = $"{packageDirectory}/{YooAssetSettingsData.Setting.UnityManifestFileName}";
				EditorTools.CopyFile(sourcePath, destPath, true);
			}

			// 拷贝UnityManifest文本文件
			{
				string sourcePath = $"{buildParameters.PipelineOutputDirectory}/{YooAssetSettingsData.Setting.UnityManifestFileName}.manifest";
				string destPath = $"{packageDirectory}/{YooAssetSettingsData.Setting.UnityManifestFileName}.manifest";
				EditorTools.CopyFile(sourcePath, destPath, true);
			}

            foreach (var PatchManifestPath in createPatchManifestContext.PatchManifestPaths)
            {
				var package = PatchManifestPath.Split('_')[1];
				string dir = $"{packageDirectory}/{package}";

				// 拷贝补丁清单文件
				{
					string sourcePath = $"{buildParameters.PipelineOutputDirectory}/{PatchManifestPath}";
                    if (!Directory.Exists(dir))
                    {
						Directory.CreateDirectory(dir);
					}
					string destPath = $"{dir}/{PatchManifestPath}";
					EditorTools.CopyFile(sourcePath, destPath, true);
				}

                // 拷贝补丁清单哈希文件
                {
                    string sourcePath = $"{buildParameters.PipelineOutputDirectory}/{PatchManifestPath}.version";
                    string destPath = $"{dir}/{PatchManifestPath}.version";
                    EditorTools.CopyFile(sourcePath, destPath, true);
                }


                // 拷贝所有补丁文件
                int progressValue = 0;
				PatchManifest patchManifest = AssetBundleBuilderHelper.LoadPatchManifestFile($"{ dir}/{ PatchManifestPath}");
				int patchFileTotalCount = patchManifest.BundleList.Count;
				foreach (var patchBundle in patchManifest.BundleList)
				{
					string sourcePath = $"{buildParameters.PipelineOutputDirectory}/{patchBundle.BundleName}";
					string destPath = $"{dir}/{patchBundle.BundleName}_{patchBundle.Hash}";
					EditorTools.CopyFile(sourcePath, destPath, true);
					EditorTools.DisplayProgressBar("拷贝补丁文件", ++progressValue, patchFileTotalCount);
				}
			}

			
			EditorTools.ClearProgressBar();
		}
	}
}