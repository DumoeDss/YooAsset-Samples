using System.IO;

namespace YooAsset.Editor
{
	/// <summary>
	/// 以组名+文件名为定位地址
	/// </summary>
	public class AddressByGroupAndFileName : IAddressRule
	{
		string IAddressRule.GetAssetAddress(AddressRuleData data)
		{
			if (Path.HasExtension(data.CollectPath))
            {
				return data.Address;
            }
            else
            {
				string path = data.AssetPath.Replace(data.CollectPath, "");
				string fileName = Path.GetFileName(data.AssetPath);
				return $"{data.Address}{path}";
			}
			
		}
	}
}