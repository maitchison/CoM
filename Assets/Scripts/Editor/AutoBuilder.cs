using UnityEditor;

class AutoBuilder
{

	private static string[] singleScene = { "Assets/Scenes/Game.unity" };
	private static string[] preloaderScenes = { "Assets/Scenes/Loading.unity", "Assets/Scenes/Game.unity" };

	private static string basePath = "D:/Google Drive/Endurance Builds/Endurance_" + VersionInfo.code + "/";

	/** Enabaling preloader will create a loading screen on WebGL builds. */
	private static bool preloaderEnabled = false;

	private static void BuildToTarget(string path, BuildTarget target, bool isDevelopment = false)
	{		
		bool needsPreloader = (target == BuildTarget.WebGL && preloaderEnabled);

		var scenes = needsPreloader ? preloaderScenes : singleScene;

		DungeonBuilder.ClearAll();

		BuildPipeline.BuildPlayer(scenes, path, target, isDevelopment ? BuildOptions.Development : BuildOptions.None);
	}

	[MenuItem("Build/Build AssetBundles")]
	static void BuildAllAssetBundles()
	{
		System.IO.Directory.CreateDirectory(basePath + "/AssetBundles");        
		BuildPipeline.BuildAssetBundles(basePath + "/AssetBundles", BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget );
	}

	/** Builds only the current target. */
	[MenuItem("Build/Quick Build")]
	static void QuickBuild()
	{
		BuildToTarget(basePath + "Quickbuild", EditorUserBuildSettings.activeBuildTarget);
	}

	/** Builds only the current target. */
	[MenuItem("Build/Test Build")]
	static void TestBuild()
	{
		BuildToTarget(basePath + "TestBuildFat", EditorUserBuildSettings.activeBuildTarget);
		BuildToTarget(basePath + "TestBuildThin", EditorUserBuildSettings.activeBuildTarget);
	}

	private static void setup()
	{
		CoM.LoadGameData();	
	}

	[MenuItem("Build/Build")]
	static void Build()
	{		
		setup();
		BuildToTarget(basePath + "EnduranceWebGL", BuildTarget.WebGL);
		BuildToTarget(basePath + "EnduranceWin.exe", BuildTarget.StandaloneWindows, true);
	}

	[MenuItem("Build/Build All")]
	static void BuildAll()
	{		
		setup();
		BuildToTarget(basePath + "EnduranceWebStreamed", BuildTarget.WebPlayerStreamed);
		BuildToTarget(basePath + "EnduranceWeb", BuildTarget.WebPlayer);
		BuildToTarget(basePath + "EnduranceWebGL", BuildTarget.WebGL);
		BuildToTarget(basePath + "EnduranceiOS", BuildTarget.iOS);
		BuildToTarget(basePath + "EnduranceLinux", BuildTarget.StandaloneLinuxUniversal);
		BuildToTarget(basePath + "EnduranceOSX", BuildTarget.StandaloneOSXUniversal);
		BuildToTarget(basePath + "EnduranceWin.exe", BuildTarget.StandaloneWindows, true);		
	}
}