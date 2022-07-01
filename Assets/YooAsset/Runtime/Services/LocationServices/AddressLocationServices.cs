
namespace YooAsset
{
	public class AddressLocationServices : ILocationServices
	{
		string ILocationServices.ConvertLocationToAssetPath(YooAssets yooAssets, string location)
		{
			return yooAssets.MappingToAssetPath(location);
		}
	}
}