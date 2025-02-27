﻿using UnityEngine.SceneManagement;

namespace YooAsset
{
	public class SceneOperationHandle : OperationHandleBase
	{
		private System.Action<SceneOperationHandle> _callback;

		internal SceneOperationHandle(ProviderBase provider) : base(provider)
		{
		}
		internal override void InvokeCallback()
		{
			_callback?.Invoke(this);
		}

		/// <summary>
		/// 完成委托
		/// </summary>
		public event System.Action<SceneOperationHandle> Completed
		{
			add
			{
				if (IsValid == false)
					throw new System.Exception($"{nameof(SceneOperationHandle)} is invalid");
				if (Provider.IsDone)
					value.Invoke(this);
				else
					_callback += value;
			}
			remove
			{
				if (IsValid == false)
					throw new System.Exception($"{nameof(SceneOperationHandle)} is invalid");
				_callback -= value;
			}
		}

		/// <summary>
		/// 场景对象
		/// </summary>
		public Scene SceneObject
		{
			get
			{
				if (IsValid == false)
					return new Scene();
				return Provider.SceneObject;
			}
		}

		/// <summary>
		/// 激活场景
		/// </summary>
		public bool ActivateScene()
		{
			if (IsValid == false)
				return false;

			if (SceneObject.IsValid() && SceneObject.isLoaded)
			{
				return SceneManager.SetActiveScene(SceneObject);
			}
			else
			{
				YooLogger.Warning($"Scene is invalid or not loaded : {SceneObject.name}");
				return false;
			}
		}

		/// <summary>
		/// 是否为主场景
		/// </summary>
		public bool IsMainScene()
		{
			if (IsValid == false)
				return false;

			if (Provider is DatabaseSceneProvider)
			{
				var temp = Provider as DatabaseSceneProvider;
				return temp.SceneMode == LoadSceneMode.Single;
			}
			else if (Provider is BundledSceneProvider)
			{
				var temp = Provider as BundledSceneProvider;
				return temp.SceneMode == LoadSceneMode.Single;
			}
			else
			{
				throw new System.NotImplementedException();
			}
		}

		/// <summary>
		/// 异步卸载子场景
		/// </summary>
		public UnloadSceneOperation UnloadAsync(YooAssets yooAssets)
		{
			// 如果句柄无效
			if (IsValid == false)
			{
				string error = $"{nameof(SceneOperationHandle)} is invalid.";
				var operation = new UnloadSceneOperation(error);
				OperationSystem.StartOperaiton(operation);
				return operation;
			}

			// 如果是主场景
			if (IsMainScene())
			{
				string error = $"Cannot unload main scene. Use {nameof(YooAssets.LoadSceneAsync)} method to change the main scene !";
				YooLogger.Error(error);
				var operation = new UnloadSceneOperation(error);
				OperationSystem.StartOperaiton(operation);
				return operation;
			}

			// 卸载子场景
			Scene sceneObject = SceneObject;
			yooAssets.AssetSystem.UnloadSubScene(Provider);
			{
				var operation = new UnloadSceneOperation(sceneObject);
				OperationSystem.StartOperaiton(operation);
				return operation;
			}
		}
	}
}