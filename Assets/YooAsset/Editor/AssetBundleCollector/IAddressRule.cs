
namespace YooAsset.Editor
{
	public struct AddressRuleData
	{
		public string AssetPath;
		public string CollectPath;
		public string GroupName;
		public string Address;


		public AddressRuleData(string assetPath, string address, string collectPath, string groupName)
		{
			AssetPath = assetPath;
			CollectPath = collectPath;
			GroupName = groupName;
			Address = address;
		}
	}

	/// <summary>
	/// 寻址规则接口
	/// </summary>
	public interface IAddressRule
	{
		string GetAssetAddress(AddressRuleData data);
	}
}