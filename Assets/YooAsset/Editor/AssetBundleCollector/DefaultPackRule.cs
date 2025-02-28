﻿using System;
using System.IO;
using UnityEditor;

namespace YooAsset.Editor
{
	///// <summary>
	///// 以文件路径作为资源包名
	///// 注意：每个文件独自打资源包
	///// 例如：收集器路径为 "Assets/UIPanel"
	///// 例如："Assets/UIPanel/Shop/Image/backgroud.png" --> "assets/uipanel/shop/image/backgroud.bundle"
	///// 例如："Assets/UIPanel/Shop/View/main.prefab" --> "assets/uipanel/shop/view/main.bundle"
	///// </summary>
	//public class PackSeparately : IPackRule
	//{
	//	string IPackRule.GetBundleName(PackRuleData data)
	//	{
	//		return StringUtility.RemoveExtension(data.BundleName);
	//	}
	//}

	///// <summary>
	///// 以父类文件夹路径作为资源包名
	///// 注意：文件夹下所有文件打进一个资源包
	///// 例如：收集器路径为 "Assets/UIPanel"
	///// 例如："Assets/UIPanel/Shop/Image/backgroud.png" --> "assets/uipanel/shop/image.bundle"
	///// 例如："Assets/UIPanel/Shop/View/main.prefab" --> "assets/uipanel/shop/view.bundle"
	///// </summary>
	//public class PackDirectory : IPackRule
	//{
	//	public static PackDirectory StaticPackRule = new PackDirectory();

	//	string IPackRule.GetBundleName(PackRuleData data)
	//	{
	//		//return data.BundleName+"_"+ Path.GetDirectoryName(data.AssetPath);
	//		string assetPath = data.AssetPath;//.Replace(data.CollectPath, string.Empty);
	//		UnityEngine.Debug.Log(assetPath);
	//		assetPath = assetPath.TrimStart('/');
	//		string[] splits = assetPath.Split('/');
	//		if (splits.Length > 1)
	//		{
	//			if (Path.HasExtension(splits[splits.Length-2]))
	//				throw new Exception($"Not found root directory : {assetPath}");
	//			string bundleName = $"{data.BundleName}/{splits[splits.Length - 2]}";
	//			return bundleName;
 //           }
 //           else
 //           {

	//			throw new Exception($"Can't set assets to root directory : {assetPath}");

	//		}
	//	}
	//}

	/// <summary>
	/// 以收集器路径作为资源包名
	/// 注意：收集的所有文件打进一个资源包
	/// </summary>
	public class PackGroup : IPackRule
	{
		public static PackGroup StaticPackRule = new PackGroup();

		string IPackRule.GetBundleName(PackRuleData data)
		{
			return StringUtility.RemoveExtension(data.GroupName);// StringUtility.RemoveExtension(data.CollectPath);
		}
	}

	/// <summary>
	/// 以收集器路径作为资源包名
	/// 注意：收集的所有文件打进一个资源包
	/// </summary>
	public class PackCollector : IPackRule
	{
		string IPackRule.GetBundleName(PackRuleData data)
		{
			//return StringUtility.RemoveExtension(data.BundleName.Replace("/","_"));
			return StringUtility.RemoveExtension(data.BundleName);
		}
	}

	/// <summary>
	/// 原生文件打包模式
	/// 注意：原生文件打包支持：图片，音频，视频，文本
	/// </summary>
	public class PackRawFile : IPackRule
	{
		string IPackRule.GetBundleName(PackRuleData data)
		{
			string extension = StringUtility.RemoveFirstChar(Path.GetExtension(data.AssetPath));
			if (extension == EAssetFileExtension.unity.ToString() || extension == EAssetFileExtension.prefab.ToString() ||
				extension == EAssetFileExtension.mat.ToString() || extension == EAssetFileExtension.controller.ToString() ||
				extension == EAssetFileExtension.fbx.ToString() || extension == EAssetFileExtension.anim.ToString() ||
				extension == EAssetFileExtension.shader.ToString())
			{
				throw new Exception($"{nameof(PackRawFile)} is not support file estension : {extension}");
			}

			// 注意：原生文件只支持无依赖关系的资源
			string[] depends = AssetDatabase.GetDependencies(data.AssetPath, true);
			if (depends.Length != 1)
				throw new Exception($"{nameof(PackRawFile)} is not support estension : {extension}");

			return StringUtility.RemoveExtension(data.AssetPath);
		}
	}
}