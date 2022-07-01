using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace YooAsset.Editor
{
	[TaskAttribute("设置资源包名")]
	public class TaskSetBundleName : IBuildTask
	{
		public class UnityManifestContext : IContextObject
		{
			public AssetBundleManifest UnityManifest;
		}

		void IBuildTask.Run(BuildContext context)
		{
			var buildParametersContext = context.GetContextObject<AssetBundleBuilder.BuildParametersContext>();
			var buildMapContext = context.GetContextObject<BuildMapContext>();

			// 模拟构建模式下跳过引擎构建
			var buildMode = buildParametersContext.Parameters.BuildMode;
			if (buildMode == EBuildMode.SimulateBuild)
				return;

			BuildAssetBundleOptions opt = buildParametersContext.GetPipelineBuildOptions();
			buildMapContext.SetBundleName();
		}

		
	}
}