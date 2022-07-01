using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

internal class FsmPatchDone : IFsmNode
{
	public string Name { private set; get; } = nameof(FsmPatchDone);
    public YooAssets YooAssets { get { return YooAssetsManager.Instance.GetYooAssets("Test"); } }

    void IFsmNode.OnEnter()
	{
		PatchEventDispatcher.SendPatchStepsChangeMsg(EPatchStates.PatchDone);
		Debug.Log("补丁流程更新完毕！");

		YooAssets.LoadSceneAsync("Scene/Game1");
	}
	void IFsmNode.OnUpdate()
	{
	}
	void IFsmNode.OnExit()
	{
	}
}