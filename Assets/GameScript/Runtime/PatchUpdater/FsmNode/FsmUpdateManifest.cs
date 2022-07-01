using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class FsmUpdateManifest : IFsmNode
{
	public string Name { private set; get; } = nameof(FsmUpdateManifest);
	public YooAssets YooAssets { get { return YooAssetsManager.Instance.GetYooAssets("Test"); } }

	void IFsmNode.OnEnter()
	{
		PatchEventDispatcher.SendPatchStepsChangeMsg(EPatchStates.UpdateManifest);
		BootScene.Instance.StartCoroutine(UpdateManifest());
	}
	void IFsmNode.OnUpdate()
	{
	}
	void IFsmNode.OnExit()
	{
	}

	private IEnumerator UpdateManifest()
	{
		yield return new WaitForSecondsRealtime(0.5f);

		// 更新补丁清单
		var operation = YooAssets.UpdateManifestAsync("PatchManifest_" + PatchUpdater.ResourceVersion, 30);
		yield return operation;

		if(operation.Status == EOperationStatus.Succeed)
		{
			FsmManager.Transition(nameof(FsmCreateDownloader));
		}
		else
		{
			Debug.LogWarning(operation.Error);
			PatchEventDispatcher.SendPatchManifestUpdateFailedMsg();
		}
	}
}