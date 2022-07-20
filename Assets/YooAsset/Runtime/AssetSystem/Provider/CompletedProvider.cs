
namespace YooAsset
{
	internal sealed class CompletedProvider : ProviderBase
	{
		public override float Progress
		{
			get
			{
				if (IsDone)
					return 1f;
				else
					return 0;
			}
		}

		public CompletedProvider( AssetInfo assetInfo, AssetSystem assetSystem) : base(string.Empty, assetInfo, assetSystem)
		{
		}
		public override void Update()
		{
		}

		public void SetCompleted()
		{
			if (Status == EStatus.None)
			{
				Status = EStatus.Fail;
				LastError = MainAssetInfo.Error;
				InvokeCompletion();
			}
		}
	}
}