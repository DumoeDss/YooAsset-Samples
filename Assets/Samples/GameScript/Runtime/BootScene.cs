using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

public class BootScene : MonoBehaviour
{
	public static BootScene Instance { private set; get; }
	public static EPlayMode GamePlayMode;
	public int version;
	public EPlayMode PlayMode = EPlayMode.HostPlayMode;
	public string artVersion, dependVersion;
	public YooAssets YooAssets;

	void Awake()
	{
		Instance = this;

		Application.targetFrameRate = 60;
		Application.runInBackground = true;
		PatchUpdater.ResourceVersion = version;
		InitBundleList();
	}

	public List<PackageVersion> InitBundleList()
    {
		var bundles = new List<PackageVersion>();

		bundles.Add(new PackageVersion() { 
			_name="Art",
			_version= artVersion
		});

		bundles.Add(new PackageVersion()
		{
			_name = "Dependency",
			_version = dependVersion
		});

		YooAssetsManager.Instance.SetBundleList(bundles);
		return bundles;
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

	void Start()
	{
		GamePlayMode = PlayMode;
		Debug.Log($"资源系统运行模式：{PlayMode}");

		// 单机运行模式
		if (PlayMode == EPlayMode.OfflinePlayMode)
		{
			var createParameters = new OfflinePlayModeParameters();
			createParameters.LocationServices = new AddressLocationServices();
			YooAssetsManager.Instance.InitializeAsync(createParameters);
		}

		// 联机运行模式
		if (PlayMode == EPlayMode.HostPlayMode)
		{
			var createParameters = new HostPlayModeParameters();
			createParameters.LocationServices = new AddressLocationServices();
			createParameters.DecryptionServices = null;
			createParameters.ClearCacheWhenDirty = false;
			createParameters.DefaultHostServer = GetHostServerURL();
			createParameters.FallbackHostServer = GetHostServerURL();
			createParameters.VerifyLevel = EVerifyLevel.High;
			YooAssetsManager.Instance.InitializeAsync(createParameters);
		}

		Init();
	}
	
	async void Init()
    {
		YooAssets = await YooAssetsManager.Instance.GetYooAssetsAsync("Art");
		// 运行补丁流程
		PatchUpdater.Run();
	}


	public string hostServerIP = "http://192.168.53.154:1024";
	private string GetHostServerURL()
	{
		string gameVersion = version+"";

#if UNITY_EDITOR 
		if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
			return $"{hostServerIP}/Android/{gameVersion}";
		else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS)
			return $"{hostServerIP}/iOS/{gameVersion}";
		else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
			return $"{hostServerIP}/WebGL/{gameVersion}";
		else
			return $"{hostServerIP}/StandaloneWindows64/{gameVersion}";
#else
		if (Application.platform == RuntimePlatform.Android)
			return $"{hostServerIP}/Android/{gameVersion}";
		else if (Application.platform == RuntimePlatform.IPhonePlayer)
			return $"{hostServerIP}/iOS/{gameVersion}";
		else if (Application.platform == RuntimePlatform.WebGLPlayer)
			return $"{hostServerIP}/WebGL/{gameVersion}";
		else
			return $"{hostServerIP}/StandaloneWindows64/{gameVersion}";
#endif
	}
}