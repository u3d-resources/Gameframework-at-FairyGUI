using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YooAsset
{
	public static partial class YooAssets
	{
		private static bool _isInitialize = false;
		private static GameObject _driver = null;
		private static readonly List<ResourcePackage> _packages = new List<ResourcePackage>();

		/// <summary>
		/// 初始化资源系统
		/// </summary>
		/// <param name="logger">自定义日志处理</param>
		public static void Initialize(ILogger logger = null)
		{
			if (_isInitialize)
			{
				UnityEngine.Debug.LogWarning($"{nameof(YooAssets)} is initialized !");
				return;
			}

			if (_isInitialize == false)
			{
				YooLogger.Logger = logger;

				// 创建驱动器
				_isInitialize = true;
				_driver = new UnityEngine.GameObject($"[{nameof(YooAssets)}]");
				_driver.AddComponent<YooAssetsDriver>();
				UnityEngine.Object.DontDestroyOnLoad(_driver);
				YooLogger.Log($"{nameof(YooAssets)} initialize !");

#if DEBUG
				// 添加远程调试脚本
				_driver.AddComponent<RemoteDebuggerInRuntime>();
#endif

				OperationSystem.Initialize();
				DownloadSystem.Initialize();
			}
		}
		
		/// <summary>
		/// 初始化资源系统
		/// </summary>
		/// <param name="logger">自定义日志处理</param>
		/// <param name="instanceRoot">实例化根节点</param>
		public static void Initialize(ILogger logger,Transform instanceRoot)
		{
			if (_isInitialize)
			{
				UnityEngine.Debug.LogWarning($"{nameof(YooAssets)} is initialized !");
				return;
			}

			if (_isInitialize == false)
			{
				YooLogger.Logger = logger;

				// 创建驱动器
				_isInitialize = true;
				_driver = new UnityEngine.GameObject($"[{nameof(YooAssets)}]");
				_driver.transform.SetParent(instanceRoot);
				_driver.AddComponent<YooAssetsDriver>();
				YooLogger.Log($"{nameof(YooAssets)} initialize !");

#if DEBUG
				// 添加远程调试脚本
				_driver.AddComponent<RemoteDebuggerInRuntime>();
#endif

				OperationSystem.Initialize();
				DownloadSystem.Initialize();
			}
		}

		/// <summary>
		/// 销毁资源系统
		/// </summary>
		public static void Destroy()
		{
			if (_isInitialize)
			{
				OperationSystem.DestroyAll();
				DownloadSystem.DestroyAll();
				CacheSystem.ClearAll();

				foreach (var package in _packages)
				{
					package.DestroyPackage();
				}
				_packages.Clear();

				_isInitialize = false;
				if (_driver != null)
					GameObject.Destroy(_driver);
				YooLogger.Log($"{nameof(YooAssets)} destroy all !");
			}
		}

		/// <summary>
		/// 更新资源系统
		/// </summary>
		internal static void Update()
		{
			if (_isInitialize)
			{
				OperationSystem.Update();
				DownloadSystem.Update();

				for (int i = 0; i < _packages.Count; i++)
				{
					_packages[i].UpdatePackage();
				}
			}
		}


		/// <summary>
		/// 创建资源包
		/// </summary>
		/// <param name="packageName">资源包名称</param>
		public static ResourcePackage CreatePackage(string packageName)
		{
			if (_isInitialize == false)
				throw new Exception($"{nameof(YooAssets)} not initialize !");

			if (string.IsNullOrEmpty(packageName))
				throw new Exception("Package name is null or empty !");

			if (HasPackage(packageName))
				throw new Exception($"Package {packageName} already existed !");

			YooLogger.Log($"Create resource package : {packageName}");
			ResourcePackage package = new ResourcePackage(packageName);
			_packages.Add(package);
			return package;
		}

		/// <summary>
		/// 获取资源包
		/// </summary>
		/// <param name="packageName">资源包名称</param>
		public static ResourcePackage GetPackage(string packageName)
		{
			var package = TryGetPackage(packageName);
			if (package == null)
				YooLogger.Error($"Not found resource package : {packageName}");
			return package;
		}

		/// <summary>
		/// 尝试获取资源包
		/// </summary>
		/// <param name="packageName">资源包名称</param>
		public static ResourcePackage TryGetPackage(string packageName)
		{
			if (_isInitialize == false)
				throw new Exception($"{nameof(YooAssets)} not initialize !");

			if (string.IsNullOrEmpty(packageName))
				throw new Exception("Package name is null or empty !");

			foreach (var package in _packages)
			{
				if (package.PackageName == packageName)
					return package;
			}
			return null;
		}

		/// <summary>
		/// 销毁资源包
		/// </summary>
		/// <param name="packageName">资源包名称</param>
		public static void DestroyPackage(string packageName)
		{
			ResourcePackage package = GetPackage(packageName);
			if (package == null)
				return;

			YooLogger.Log($"Destroy resource package : {packageName}");
			_packages.Remove(package);
			package.DestroyPackage();

			// 清空缓存
			CacheSystem.ClearPackage(packageName);
		}

		/// <summary>
		/// 检测资源包是否存在
		/// </summary>
		/// <param name="packageName">资源包名称</param>
		public static bool HasPackage(string packageName)
		{
			if (_isInitialize == false)
				throw new Exception($"{nameof(YooAssets)} not initialize !");

			foreach (var package in _packages)
			{
				if (package.PackageName == packageName)
					return true;
			}
			return false;
		}

		/// <summary>
		/// 开启一个异步操作
		/// </summary>
		/// <param name="operation">异步操作对象</param>
		public static void StartOperation(GameAsyncOperation operation)
		{
			OperationSystem.StartOperation(operation);
		}

		#region 系统参数
		/// <summary>
		/// 设置下载系统参数，启用断点续传功能文件的最小字节数
		/// </summary>
		public static void SetDownloadSystemBreakpointResumeFileSize(int fileBytes)
		{
			DownloadSystem.BreakpointResumeFileSize = fileBytes;
		}

		/// <summary>
		/// 设置下载系统参数，下载失败后清理文件的HTTP错误码
		/// </summary>
		public static void SetDownloadSystemClearFileResponseCode(List<long> codes)
		{
			DownloadSystem.ClearFileResponseCodes = codes;
		}

		/// <summary>
		/// 设置下载系统参数，自定义的证书认证实例
		/// </summary>
		public static void SetDownloadSystemCertificateHandler(UnityEngine.Networking.CertificateHandler instance)
		{
			DownloadSystem.CertificateHandlerInstance = instance;
		}

		/// <summary>
		/// 设置下载系统参数，自定义下载请求
		/// </summary>
		public static void SetDownloadSystemUnityWebRequest(DownloadRequestDelegate requestDelegate)
		{
			DownloadSystem.RequestDelegate = requestDelegate;
		}

		/// <summary>
		/// 设置下载系统参数，网络重定向次数（Unity引擎默认值32）
		/// 注意：不支持设置为负值
		/// </summary>
		public static void SetDownloadSystemRedirectLimit(int redirectLimit)
		{
			if (redirectLimit < 0)
			{
				YooLogger.Warning($"Invalid param value : {redirectLimit}");
				return;
			}

			DownloadSystem.RedirectLimit = redirectLimit;
		}

		/// <summary>
		/// 设置异步系统参数，每帧执行消耗的最大时间切片（单位：毫秒）
		/// </summary>
		public static void SetOperationSystemMaxTimeSlice(long milliseconds)
		{
			if (milliseconds < 10)
			{
				milliseconds = 10;
				YooLogger.Warning($"MaxTimeSlice minimum value is 10 milliseconds.");
			}
			OperationSystem.MaxTimeSlice = milliseconds;
		}

		/// <summary>
		/// 设置缓存系统参数，已经缓存文件的校验等级
		/// </summary>
		public static void SetCacheSystemCachedFileVerifyLevel(EVerifyLevel verifyLevel)
		{
			CacheSystem.InitVerifyLevel = verifyLevel;
		}

		/// <summary>
		/// 设置缓存系统参数，禁用缓存在WebGL平台
		/// </summary>
		public static void SetCacheSystemDisableCacheOnWebGL()
		{
			CacheSystem.DisableUnityCacheOnWebGL = true;
		}
		#endregion

		#region 调试信息
		internal static DebugReport GetDebugReport()
		{
			DebugReport report = new DebugReport();
			report.FrameCount = Time.frameCount;

			foreach (var package in _packages)
			{
				var packageData = package.GetDebugPackageData();
				report.PackageDatas.Add(packageData);
			}
			return report;
		}
		#endregion
	}
}