
using System;
using System.IO;

namespace YooAsset.Editor
{
	public struct PackRuleData
	{
		public string AssetPath;		
		public string CollectPath;
		public string BundleName;
		public string GroupName;

		//public PackRuleData(string assetPath, string groupName)
		//{
		//	AssetPath = assetPath;
		//	assetPath = assetPath.TrimStart('/');
		//	string[] splits = assetPath.Split('/');
		//	if (splits.Length > 1)
		//	{
		//		if (Path.HasExtension(splits[splits.Length - 2]))
		//			throw new Exception($"Not found root directory : {assetPath}");
		//		BundleName = splits[splits.Length - 2];
		//	}else if (splits.Length > 0)
		//	{
		//		BundleName = Path.GetFileNameWithoutExtension(splits[0]);
  //          }
  //          else
  //          {
		//		throw new Exception($"PackRuleData Error : {assetPath}");
		//	}

		//	CollectPath = string.Empty;
		//	GroupName = groupName;
		//}
		public PackRuleData(string assetPath, string groupName, string collectPath, string bundleName)
		{
			AssetPath = assetPath;
			GroupName = groupName;
			CollectPath = collectPath;
			BundleName = bundleName;
		}
	}

	/// <summary>
	/// 资源打包规则接口
	/// </summary>
	public interface IPackRule
	{
		/// <summary>
		/// 获取资源打包所属的资源包名称
		/// </summary>
		string GetBundleName(PackRuleData data);
	}
}