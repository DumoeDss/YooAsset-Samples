using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace YooAsset.Editor
{
	public class AssetBundleCollectorSettingData
	{
		private static readonly Dictionary<string, System.Type> _cachePackRuleTypes = new Dictionary<string, System.Type>();
		private static readonly Dictionary<string, IPackRule> _cachePackRuleInstance = new Dictionary<string, IPackRule>();

		private static readonly Dictionary<string, System.Type> _cacheFilterRuleTypes = new Dictionary<string, System.Type>();
		private static readonly Dictionary<string, IFilterRule> _cacheFilterRuleInstance = new Dictionary<string, IFilterRule>();

		/// <summary>
		/// 配置数据是否被修改
		/// </summary>
		public static bool IsDirty { private set; get; } = false;


		private static AssetBundleCollectorSetting _setting = null;
		public static AssetBundleCollectorSetting Setting
		{
			get
			{
				if (_setting == null)
					LoadSettingData();
				return _setting;
			}
		}

		public static List<string> GetPackRuleNames()
		{
			if (_setting == null)
				LoadSettingData();

			List<string> names = new List<string>();
			foreach (var pair in _cachePackRuleTypes)
			{
				names.Add(pair.Key);
			}
			return names;
		}
		public static List<string> GetFilterRuleNames()
		{
			if (_setting == null)
				LoadSettingData();

			List<string> names = new List<string>();
			foreach (var pair in _cacheFilterRuleTypes)
			{
				names.Add(pair.Key);
			}
			return names;
		}

		public static bool HasPackRuleName(string ruleName)
		{
			foreach (var pair in _cachePackRuleTypes)
			{
				if (pair.Key == ruleName)
					return true;
			}
			return false;
		}
		public static bool HasFilterRuleName(string ruleName)
		{
			foreach (var pair in _cacheFilterRuleTypes)
			{
				if (pair.Key == ruleName)
					return true;
			}
			return false;
		}


		/// <summary>
		/// 加载配置文件
		/// </summary>
		private static void LoadSettingData()
		{
			_setting = EditorHelper.LoadSettingData<AssetBundleCollectorSetting>();

			// IPackRule
			{
				// 清空缓存集合
				_cachePackRuleTypes.Clear();
				_cachePackRuleInstance.Clear();

				// 获取所有类型
				List<Type> types = new List<Type>()
				{
					typeof(PackGroup),
					typeof(PackCollector),
					typeof(PackRawFile),
				};

				var customTypes = EditorTools.GetAssignableTypes(typeof(IPackRule));
				types.AddRange(customTypes);
				for (int i = 0; i < types.Count; i++)
				{
					Type type = types[i];
					if (_cachePackRuleTypes.ContainsKey(type.Name) == false)
						_cachePackRuleTypes.Add(type.Name, type);
				}
			}

			// IFilterRule
			{
				// 清空缓存集合
				_cacheFilterRuleTypes.Clear();
				_cacheFilterRuleInstance.Clear();

				// 获取所有类型
				List<Type> types = new List<Type>(100)
				{
					typeof(CollectAll),
					typeof(CollectScene),
					typeof(CollectPrefab),
					typeof(CollectSprite)
				};

				var customTypes = EditorTools.GetAssignableTypes(typeof(IFilterRule));
				types.AddRange(customTypes);
				for (int i = 0; i < types.Count; i++)
				{
					Type type = types[i];
					if (_cacheFilterRuleTypes.ContainsKey(type.Name) == false)
						_cacheFilterRuleTypes.Add(type.Name, type);
				}
			}

		}

		/// <summary>
		/// 存储文件
		/// </summary>
		public static void SaveFile()
		{
			if (Setting != null)
			{
				IsDirty = false;
				EditorUtility.SetDirty(Setting);
				AssetDatabase.SaveAssets();
				Debug.Log($"{nameof(AssetBundleCollectorSetting)}.asset is saved!");
			}
		}

		/// <summary>
		/// 清空所有数据
		/// </summary>
		public static void ClearAll()
		{
			Setting.Packages.Clear();
			SaveFile();
		}

		static IAddressRule addressRuleiInstance;
		public static IAddressRule GetAddressRuleInstance(string ruleName)
		{
			if (addressRuleiInstance == null)
				addressRuleiInstance = new AddressByGroupAndFileName();
			return addressRuleiInstance;
		}
		public static IPackRule GetPackRuleInstance(string ruleName)
		{
			if (_cachePackRuleInstance.TryGetValue(ruleName, out IPackRule instance))
				return instance;

			// 如果不存在创建类的实例
			if (_cachePackRuleTypes.TryGetValue(ruleName, out Type type))
			{
				instance = (IPackRule)Activator.CreateInstance(type);
				_cachePackRuleInstance.Add(ruleName, instance);
				return instance;
			}
			else
			{
				throw new Exception($"{nameof(IPackRule)}类型无效：{ruleName}");
			}
		}
		public static IFilterRule GetFilterRuleInstance(string ruleName)
		{
			if (_cacheFilterRuleInstance.TryGetValue(ruleName, out IFilterRule instance))
				return instance;

			// 如果不存在创建类的实例
			if (_cacheFilterRuleTypes.TryGetValue(ruleName, out Type type))
			{
				instance = (IFilterRule)Activator.CreateInstance(type);
				_cacheFilterRuleInstance.Add(ruleName, instance);
				return instance;
			}
			else
			{
				throw new Exception($"{nameof(IFilterRule)}类型无效：{ruleName}");
			}
		}

		#region 资源分组编辑相关
		public static void CreatePackage(string packageName)
		{
			AssetBundleCollectorPackage package = new AssetBundleCollectorPackage();
			package.PackageName = packageName;
			Setting.Packages.Add(package);
			IsDirty = true;
		}
		public static void RemovePackage(AssetBundleCollectorPackage package)
		{
			if (Setting.Packages.Remove(package))
			{
				IsDirty = true;
			}
			else
			{
				Debug.LogWarning($"Failed remove group : {package.PackageName}");
			}
		}
		public static void ModifyPackage(AssetBundleCollectorPackage package)
		{
			if (package != null)
			{
				IsDirty = true;
			}
		}
		#endregion

		#region 资源分组编辑相关
		public static void CreateGroup(AssetBundleCollectorPackage package, string groupName)
		{
			AssetBundleCollectorGroup group = new AssetBundleCollectorGroup();
			group.GroupName = groupName;
			package.Groups.Add(group);
			IsDirty = true;
		}
		public static void RemoveGroup(AssetBundleCollectorPackage package, AssetBundleCollectorGroup group)
		{
			if (package.Groups.Remove(group))
			{
				IsDirty = true;
			}
			else
			{
				Debug.LogWarning($"Failed remove group : {group.GroupName}");
			}
		}
		public static void ModifyGroup(AssetBundleCollectorPackage package, AssetBundleCollectorGroup group)
		{
			if (package!=null&&group != null)
			{
				IsDirty = true;
			}
		}
		#endregion	

		#region 资源收集器编辑相关
		public static void CreateCollector(AssetBundleCollectorGroup group, string collectPath)
		{
			AssetBundleCollector collector = new AssetBundleCollector();
			collector.CollectPath = collectPath;
			collector.Address = collectPath;
			group.Collectors.Add(collector);
			IsDirty = true;
		}
		public static void RemoveCollector(AssetBundleCollectorGroup group, AssetBundleCollector collector)
		{
			if (group.Collectors.Remove(collector))
			{
				IsDirty = true;
			}
			else
			{
				Debug.LogWarning($"Failed remove collector : {collector.CollectPath}");
			}
		}
		public static void ModifyCollector(AssetBundleCollectorGroup group, AssetBundleCollector collector)
		{
			if (group != null && collector != null)
			{
				IsDirty = true;
			}
		}
		#endregion
	}
}