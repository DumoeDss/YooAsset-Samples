﻿using UnityEngine;

namespace YooAsset
{
	public class YooAssetVersion
    {
		public string crc;
		public long size;
		public int version;
    }

	internal static class YooAssetSettingsData
	{
		private static YooAssetSettings _setting = null;
		public static YooAssetSettings Setting
		{
			get
			{
				if (_setting == null)
					LoadSettingData();
				return _setting;
			}
		}

		/// <summary>
		/// 加载配置文件
		/// </summary>
		private static void LoadSettingData()
		{
			_setting = Resources.Load<YooAssetSettings>("YooAssetSettings");
			if (_setting == null)
			{
				YooLogger.Log("YooAsset use default settings.");
				_setting = ScriptableObject.CreateInstance<YooAssetSettings>();
			}
			else
			{
				YooLogger.Log("YooAsset use user settings.");
			}
		}

		/// <summary>
		/// 获取构建报告文件名
		/// </summary>
		public static string GetReportFileName(int resourceVersion)
		{
			return $"{YooAssetSettings.ReportFileName}_{resourceVersion}.json";
		}

		/// <summary>
		/// 获取补丁清单文件完整名称
		/// </summary>
		public static string GetPatchManifestFileName(string _manifestName)
		{
			return $"{_manifestName}";
		}

		/// <summary>
		/// 获取补丁清单哈希文件完整名称
		/// </summary>
		public static string GetPatchManifestHashFileName(string _manifestName)
		{
			return $"{_manifestName}.version";
		}
	}
}