using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

internal class FsmPatchDone : IFsmNode
{
	public string Name { private set; get; } = nameof(FsmPatchDone);

    void IFsmNode.OnEnter()
	{
		PatchEventDispatcher.SendPatchStepsChangeMsg(EPatchStates.PatchDone);
		Debug.Log("补丁流程更新完毕！");

		BootScene.Instance.YooAssets.LoadSceneAsync("scene/Game1.unity");
	}
	void IFsmNode.OnUpdate()
	{
	}
	void IFsmNode.OnExit()
	{
	}
}