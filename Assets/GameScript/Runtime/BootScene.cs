using System;
using System.Collections;
using UnityEngine;
using YooAsset;

public class BootScene : MonoBehaviour
{
	public static BootScene Instance { private set; get; }
	public static EPlayMode GamePlayMode;
	public int version;
	public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

	void Awake()
	{
		Instance = this;

		Application.targetFrameRate = 60;
		Application.runInBackground = true;
		PatchUpdater.ResourceVersion = version;
	}
	void OnGUI()
	{
		GUIConsole.OnGUI();
	}
	void OnDestroy()
	{
		Instance = null;
	}
	void Update()
	{
		EventManager.Update();
		FsmManager.Update();
	}

	IEnumerator Start()
	{
		GamePlayMode = PlayMode;
		Debug.Log($"资源系统运行模式：{PlayMode}");
		var yooAssets = YooAssetsManager.Instance.GetYooAssets("Test");
		// 编辑器下的模拟模式
		if (PlayMode == EPlayMode.EditorSimulateMode)
		{
			var createParameters = new EditorSimulateModeParameters();
			createParameters.LocationServices = new DefaultLocationServices( "Assets/GameRes");
			//createParameters.SimulatePatchManifestPath = GetPatchManifestPath();
			YooAssetsManager.Instance.InitializeAsync(createParameters);
			if (!yooAssets.IsInitialized)
				yield return yooAssets.InitializeAsync(createParameters, PlayMode);
		}

		// 单机运行模式
		if (PlayMode == EPlayMode.OfflinePlayMode)
		{
			var createParameters = new OfflinePlayModeParameters();
			createParameters.LocationServices = new DefaultLocationServices( "Assets/GameRes");
			YooAssetsManager.Instance.InitializeAsync(createParameters);
			if (!yooAssets.IsInitialized)
				yield return yooAssets.InitializeAsync(createParameters, PlayMode);
		}

		// 联机运行模式
		if (PlayMode == EPlayMode.HostPlayMode)
		{
			var createParameters = new HostPlayModeParameters();
			createParameters.LocationServices = new DefaultLocationServices( "Assets/GameRes");
			createParameters.DecryptionServices = null;
			createParameters.ClearCacheWhenDirty = false;
			createParameters.DefaultHostServer = GetHostServerURL();
			createParameters.FallbackHostServer = GetHostServerURL();
			createParameters.VerifyLevel = EVerifyLevel.High;
			YooAssetsManager.Instance.InitializeAsync(createParameters);
			if (!yooAssets.IsInitialized)
				yield return yooAssets.InitializeAsync(createParameters, PlayMode);
		}

		// 运行补丁流程
		PatchUpdater.Run();
	}

	private string GetPatchManifestPath()
	{
		string directory = System.IO.Path.GetDirectoryName(Application.dataPath);
		return $"{directory}/Bundles/StandaloneWindows64/UnityManifest_SimulateBuild/PatchManifest_100.bytes";
	}
	private string GetHostServerURL()
	{
		//string hostServerIP = "http://10.0.2.2"; //安卓模拟器地址
		string hostServerIP = "http://192.168.53.59:1024";
		string gameVersion = "100";

#if UNITY_EDITOR
		if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
			return $"{hostServerIP}/CDN/Android/{gameVersion}";
		else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
			return $"{hostServerIP}/CDN/IPhone/{gameVersion}";
		else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
			return $"{hostServerIP}/CDN/WebGL/{gameVersion}";
		else
			return $"{hostServerIP}/StandaloneWindows64/{gameVersion}";
#else
		if (Application.platform == RuntimePlatform.Android)
			return $"{hostServerIP}/CDN/Android/{gameVersion}";
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
			return $"{hostServerIP}/CDN/IPhone/{gameVersion}";
		else if (Application.platform == RuntimePlatform.WebGLPlayer)
			return $"{hostServerIP}/CDN/WebGL/{gameVersion}";
		else
			return $"{hostServerIP}/CDN/PC/{gameVersion}";
#endif
	}
}