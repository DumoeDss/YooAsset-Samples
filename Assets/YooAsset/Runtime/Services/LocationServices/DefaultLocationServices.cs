﻿
namespace YooAsset
{
	public class DefaultLocationServices : ILocationServices
	{
		private readonly string _resourceRoot;

		public DefaultLocationServices(string resourceRoot)
		{
			if (string.IsNullOrEmpty(resourceRoot) == false)
				_resourceRoot = PathHelper.GetRegularPath(resourceRoot);
		}

		string ILocationServices.ConvertLocationToAssetPath(YooAssets yooAssets, string location)
		{
			if (string.IsNullOrEmpty(_resourceRoot))
			{
				return yooAssets.MappingToAssetPath(location);
			}
			else
			{
				string tempLocation = $"{_resourceRoot}/{location}";
				return yooAssets.MappingToAssetPath(tempLocation);
			}
		}
	}
}