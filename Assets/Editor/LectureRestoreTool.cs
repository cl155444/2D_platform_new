// 고박사 어드벤처 강의 복구 도구
// 메뉴: Tools > 강의 복구 > 01강 + 02강 45쪽(이동 발판)까지 복구
//
// 강의 PDF(01강 전체, 02강 타일 ~ 45쪽 이동 발판)에서 에디터로 하던 작업을 자동으로 다시 만들어 줍니다.
//  - 레이어(Ground, Tile, Platform), Input Manager "Sprint" 축, Active Input Handling = Both
//  - PlatformSide(Physics Material 2D), 머티리얼, 플레이어 애니메이션/애니메이터
//  - Player 컴포넌트, 이동/착지 이펙트, StageData(Level01), 카메라 추적, 배경 스크롤
//  - 타일 프리팹(Tile_00 ~ Tile_03) 컴포넌트, Tile_02_BrokeEffect 프리팹
//  - 이동 발판 프리팹(Platform_00_Moving)
// 여러 번 실행해도 같은 결과가 되도록 작성되어 있습니다. 복구가 끝나면 이 파일은 지워도 됩니다.

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class LectureRestoreTool
{
	private const string ScenePath = "Assets/Scenes/SampleScene.unity";

	[MenuItem("Tools/강의 복구/01강 + 02강 45쪽(이동 발판)까지 복구")]
	public static void RestoreAll()
	{
		if ( !EditorUtility.DisplayDialog("강의 복구",
			"SampleScene과 프리팹에 01강 ~ 02강 45쪽(이동 발판)까지의 설정을 적용합니다.\n계속할까요?", "복구", "취소") )
			return;

		if ( !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() ) return;

		var log = new List<string>();

		try
		{
			// ---------- 프로젝트 설정 ----------
			bool needRestart = SetupProjectSettings(log);
			int groundLayer  = EnsureLayer("Ground", 6, log);
			int tileLayer    = EnsureLayer("Tile", 7, log);
			int platformLayer = EnsureLayer("Platform", 8, log);

			// 작업할 씬을 먼저 연다 (임시 오브젝트가 다른 씬에 생기지 않도록)
			var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

			// ---------- 에셋 ----------
			EnsureFolder("Assets/PhysicsMaterials");
			EnsureFolder("Assets/Animations");
			EnsureFolder("Assets/Materials");

			PhysicsMaterial2D platformSide = CreatePlatformSide();

			Material footStepMat   = MakeMaterial("FootStepEffect", "Universal Render Pipeline/Particles/Unlit", null);
			Material landingMat    = MakeMaterial("LandingEffect", "Universal Render Pipeline/Particles/Unlit",
										LoadTexture("Assets/Textures/Player/LandingEffect.png"));
			Material tileBrokeMat  = MakeMaterial("TileBrokeEffect", "Universal Render Pipeline/Particles/Unlit",
										LoadTexture("Assets/Textures/Ground/Tile_01_Broken.png"));

			string[] bgNames = { "Background_00", "Background_01", "Background_02", "Background_Cloud", "Background_Sky" };
			var bgMats = new Dictionary<string, Material>();
			foreach ( var n in bgNames )
			{
				string texPath = $"Assets/Textures/Background/{n}.png";
				SetWrapRepeat(texPath);
				bgMats[n] = MakeMaterial(n, "Universal Render Pipeline/Unlit", LoadTexture(texPath));
			}

			AnimatorController playerController = CreatePlayerAnimator();
			StageData stageData = CreateStageData();
			GameObject brokeEffectPrefab = CreateTileBrokeEffectPrefab(tileBrokeMat);

			SetupTilePrefabs(tileLayer, platformSide, brokeEffectPrefab);
			log.Add("타일 프리팹(Tile_00 ~ Tile_03) 설정");

			SetupMovingPlatformPrefab(platformLayer, platformSide);
			log.Add("이동 발판 프리팹(Platform_00_Moving) 설정");

			// ---------- 씬 ----------
			SetupGround(groundLayer, platformSide);
			GameObject player = SetupPlayer(groundLayer, tileLayer, platformLayer, playerController, stageData, footStepMat, landingMat);
			Camera mainCamera = SetupMainCamera(player, stageData);
			SetupParallaxBackground(mainCamera, bgMats);
			log.Add("SampleScene: Ground / Player / Main Camera / ParallaxBackground 설정");

			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			string msg = "복구 완료!\n\n- " + string.Join("\n- ", log);
			if ( needRestart )
				msg += "\n\n※ Active Input Handling을 'Both'로 바꿨습니다. 에디터를 재시작해야 플레이 시 키 입력이 동작합니다.";
			Debug.Log("[강의 복구] " + msg);
			EditorUtility.DisplayDialog("강의 복구", msg, "확인");

			if ( needRestart && EditorUtility.DisplayDialog("강의 복구", "지금 에디터를 재시작할까요?", "재시작", "나중에") )
				EditorApplication.OpenProject(System.IO.Directory.GetCurrentDirectory());
		}
		catch ( System.Exception e )
		{
			Debug.LogException(e);
			EditorUtility.DisplayDialog("강의 복구", "복구 중 오류가 발생했습니다. Console 창을 확인해 주세요.\n\n" + e.Message, "확인");
		}
	}

	// =====================================================================
	// 프로젝트 설정
	// =====================================================================
	private static bool SetupProjectSettings(List<string> log)
	{
		bool needRestart = false;

		// Input Manager : Sprint 축 (01강 - x 키를 단축키로 등록)
		var inputManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
		var axes = inputManager.FindProperty("m_Axes");
		bool hasSprint = false;
		for ( int i = 0; i < axes.arraySize; ++ i )
		{
			if ( axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue == "Sprint" ) hasSprint = true;
		}
		if ( !hasSprint )
		{
			axes.arraySize ++;
			var a = axes.GetArrayElementAtIndex(axes.arraySize - 1);
			a.FindPropertyRelative("m_Name").stringValue					= "Sprint";
			a.FindPropertyRelative("descriptiveName").stringValue			= "";
			a.FindPropertyRelative("descriptiveNegativeName").stringValue	= "";
			a.FindPropertyRelative("negativeButton").stringValue			= "";
			a.FindPropertyRelative("positiveButton").stringValue			= "x";
			a.FindPropertyRelative("altNegativeButton").stringValue			= "";
			a.FindPropertyRelative("altPositiveButton").stringValue			= "";
			a.FindPropertyRelative("gravity").floatValue					= 3;
			a.FindPropertyRelative("dead").floatValue						= 0.001f;
			a.FindPropertyRelative("sensitivity").floatValue				= 3;
			a.FindPropertyRelative("snap").boolValue						= false;
			a.FindPropertyRelative("invert").boolValue						= false;
			a.FindPropertyRelative("type").intValue							= 0;	// Key or Mouse Button
			a.FindPropertyRelative("axis").intValue							= 0;	// X axis
			a.FindPropertyRelative("joyNum").intValue						= 0;
			inputManager.ApplyModifiedPropertiesWithoutUndo();
			log.Add("Input Manager에 Sprint 축(x 키) 추가");
		}

		// 강의 코드는 Input.GetAxisRaw()를 사용하므로 Active Input Handling = Both(2) 필요
		var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
		var handler = playerSettings.FindProperty("activeInputHandler");
		if ( handler != null && handler.intValue != 2 )
		{
			handler.intValue = 2;
			playerSettings.ApplyModifiedPropertiesWithoutUndo();
			needRestart = true;
			log.Add("Active Input Handling = Both");
		}

		return needRestart;
	}

	private static int EnsureLayer(string layerName, int preferredIndex, List<string> log)
	{
		var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
		var layers = tagManager.FindProperty("layers");

		for ( int i = 0; i < layers.arraySize; ++ i )
		{
			if ( layers.GetArrayElementAtIndex(i).stringValue == layerName ) return i;
		}

		int index = preferredIndex;
		if ( !string.IsNullOrEmpty(layers.GetArrayElementAtIndex(index).stringValue) )
		{
			index = -1;
			for ( int i = 6; i < layers.arraySize; ++ i )
			{
				if ( string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue) ) { index = i; break; }
			}
			if ( index == -1 ) throw new System.Exception($"{layerName} 레이어를 추가할 빈 칸이 없습니다.");
		}

		layers.GetArrayElementAtIndex(index).stringValue = layerName;
		tagManager.ApplyModifiedPropertiesWithoutUndo();
		log.Add($"{layerName} 레이어 추가 (User Layer {index})");
		return index;
	}

	// =====================================================================
	// 에셋
	// =====================================================================
	private static void EnsureFolder(string path)
	{
		if ( AssetDatabase.IsValidFolder(path) ) return;
		int slash = path.LastIndexOf('/');
		AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
	}

	private static Texture2D LoadTexture(string path)
	{
		var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		if ( tex == null ) Debug.LogWarning($"[강의 복구] 텍스처를 찾을 수 없습니다: {path}");
		return tex;
	}

	private static Sprite LoadSprite(string path)
	{
		return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
	}

	private static void SetWrapRepeat(string texturePath)
	{
		var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
		if ( importer == null || importer.wrapMode == TextureWrapMode.Repeat ) return;
		importer.wrapMode = TextureWrapMode.Repeat;
		importer.SaveAndReimport();
	}

	// 01강 - 벽 마찰력 설정
	private static PhysicsMaterial2D CreatePlatformSide()
	{
		const string path = "Assets/PhysicsMaterials/PlatformSide.physicsMaterial2D";
		var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
		if ( material == null )
		{
			material = new PhysicsMaterial2D("PlatformSide");
			AssetDatabase.CreateAsset(material, path);
		}
		material.friction	= 0;
		material.bounciness	= 0;
		EditorUtility.SetDirty(material);
		return material;
	}

	// Surface Type : Transparent, Base Map : texture
	private static Material MakeMaterial(string name, string shaderName, Texture texture)
	{
		string path = $"Assets/Materials/{name}.mat";
		var shader = Shader.Find(shaderName);
		if ( shader == null ) throw new System.Exception($"셰이더를 찾을 수 없습니다: {shaderName}");

		var material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if ( material == null )
		{
			material = new Material(shader);
			AssetDatabase.CreateAsset(material, path);
		}
		material.shader = shader;

		if ( material.HasProperty("_BaseMap") )		material.SetTexture("_BaseMap", texture);
		if ( material.HasProperty("_BaseColor") )	material.SetColor("_BaseColor", Color.white);

		// Surface Type = Transparent, Blending Mode = Alpha
		material.SetFloat("_Surface", 1);
		material.SetFloat("_Blend", 0);
		material.SetFloat("_AlphaClip", 0);
		material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
		material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
		material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
		material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
		material.SetFloat("_ZWrite", 0);
		material.SetOverrideTag("RenderType", "Transparent");
		material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
		material.DisableKeyword("_ALPHATEST_ON");
		material.renderQueue = (int)RenderQueue.Transparent;
		material.SetShaderPassEnabled("DepthOnly", false);
		material.SetShaderPassEnabled("ShadowCaster", false);

		EditorUtility.SetDirty(material);
		return material;
	}

	// 01강 - 플레이어 캐릭터 애니메이션
	private static AnimatorController CreatePlayerAnimator()
	{
		var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Textures/Player/PlayerBase.png")
						.OfType<Sprite>().ToDictionary(s => s.name);

		AnimationClip idle		= MakeSpriteClip("Player_Idle",		Frames(sprites, 0, 6),   6, true);
		AnimationClip walk		= MakeSpriteClip("Player_Walk",		Frames(sprites, 7, 13),  12, true);
		AnimationClip run		= MakeSpriteClip("Player_Run",		Frames(sprites, 14, 20), 12, true);
		AnimationClip jumpUp	= MakeSpriteClip("Player_JumpUp",	Frames(sprites, 21, 23), 6, false);
		AnimationClip jumpDown	= MakeSpriteClip("Player_JumpDown",	Frames(sprites, 24, 27), 6, false);

		const string path = "Assets/Animations/Player.controller";
		if ( AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null ) AssetDatabase.DeleteAsset(path);
		var controller = AnimatorController.CreateAnimatorControllerAtPath(path);

		controller.AddParameter("velocityX", AnimatorControllerParameterType.Float);
		controller.AddParameter("velocityY", AnimatorControllerParameterType.Float);
		controller.AddParameter("isJump",    AnimatorControllerParameterType.Bool);

		var stateMachine = controller.layers[0].stateMachine;

		// 대기, 걷기, 뛰기 블렌드 트리 (Movement)
		var tree = new BlendTree
		{
			name					= "Movement",
			blendType				= BlendTreeType.Simple1D,
			blendParameter			= "velocityX",
			useAutomaticThresholds	= false,
			hideFlags				= HideFlags.HideInHierarchy
		};
		AssetDatabase.AddObjectToAsset(tree, controller);
		tree.AddChild(idle, 0f);
		tree.AddChild(walk, 0.5f);
		tree.AddChild(run,  1f);

		var movementState	= stateMachine.AddState("Movement");
		movementState.motion = tree;
		var jumpUpState		= stateMachine.AddState("Player_JumpUp");
		jumpUpState.motion	= jumpUp;
		var jumpDownState	= stateMachine.AddState("Player_JumpDown");
		jumpDownState.motion = jumpDown;
		stateMachine.defaultState = movementState;

		// Movement -> JumpUp : isJump true, velocityY > 0
		var t = AddTransition(movementState, jumpUpState);
		t.AddCondition(AnimatorConditionMode.If, 0, "isJump");
		t.AddCondition(AnimatorConditionMode.Greater, 0, "velocityY");
		// JumpUp -> Movement : isJump false
		t = AddTransition(jumpUpState, movementState);
		t.AddCondition(AnimatorConditionMode.IfNot, 0, "isJump");
		// Movement -> JumpDown : isJump true, velocityY < 0
		t = AddTransition(movementState, jumpDownState);
		t.AddCondition(AnimatorConditionMode.If, 0, "isJump");
		t.AddCondition(AnimatorConditionMode.Less, 0, "velocityY");
		// JumpDown -> Movement : isJump false
		t = AddTransition(jumpDownState, movementState);
		t.AddCondition(AnimatorConditionMode.IfNot, 0, "isJump");
		// JumpUp -> JumpDown : velocityY < 0
		t = AddTransition(jumpUpState, jumpDownState);
		t.AddCondition(AnimatorConditionMode.Less, 0, "velocityY");
		// JumpDown -> JumpUp : velocityY > 0
		t = AddTransition(jumpDownState, jumpUpState);
		t.AddCondition(AnimatorConditionMode.Greater, 0, "velocityY");

		EditorUtility.SetDirty(controller);
		return controller;
	}

	private static Sprite[] Frames(Dictionary<string, Sprite> sprites, int first, int last)
	{
		var list = new List<Sprite>();
		for ( int i = first; i <= last; ++ i )
		{
			Sprite s;
			if ( !sprites.TryGetValue("PlayerBase_" + i, out s) )
				throw new System.Exception("PlayerBase_" + i + " 스프라이트가 없습니다. PlayerBase 이미지의 Sprite Mode(Multiple) 분할을 확인해 주세요.");
			list.Add(s);
		}
		return list.ToArray();
	}

	private static AnimatorStateTransition AddTransition(AnimatorState source, AnimatorState destination)
	{
		var transition = source.AddTransition(destination);
		transition.hasExitTime = false;		// 조건을 만족하면 바로 전환
		return transition;
	}

	private static AnimationClip MakeSpriteClip(string name, Sprite[] frames, int sampleRate, bool loop)
	{
		string path = $"Assets/Animations/{name}.anim";
		var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
		if ( clip == null )
		{
			clip = new AnimationClip();
			AssetDatabase.CreateAsset(clip, path);
		}
		clip.ClearCurves();
		clip.frameRate = sampleRate;

		var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
		var keys = new ObjectReferenceKeyframe[frames.Length];
		for ( int i = 0; i < frames.Length; ++ i )
		{
			keys[i] = new ObjectReferenceKeyframe { time = i / (float)sampleRate, value = frames[i] };
		}
		AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

		var settings = AnimationUtility.GetAnimationClipSettings(clip);
		settings.loopTime = loop;
		AnimationUtility.SetAnimationClipSettings(clip, settings);

		EditorUtility.SetDirty(clip);
		return clip;
	}

	// 01강 - 스테이지 데이터 (Project View - "+" - Stage Data)
	private static StageData CreateStageData()
	{
		const string path = "Assets/Level01.asset";
		var data = AssetDatabase.LoadAssetAtPath<StageData>(path);
		if ( data == null )
		{
			data = ScriptableObject.CreateInstance<StageData>();
			AssetDatabase.CreateAsset(data, path);
		}
		var so = new SerializedObject(data);
		so.FindProperty("cameraLimitMinX").floatValue	= 0;
		so.FindProperty("cameraLimitMaxX").floatValue	= 112;
		so.FindProperty("playerLimitMinX").floatValue	= -10;
		so.FindProperty("playerLimitMaxX").floatValue	= 122;
		so.FindProperty("mapLimitMinY").floatValue		= -8;
		so.ApplyModifiedPropertiesWithoutUndo();
		EditorUtility.SetDirty(data);
		return data;
	}

	// 02강 - 부서지는 타일 파티클 (Tile_02_BrokeEffect)
	private static GameObject CreateTileBrokeEffectPrefab(Material material)
	{
		const string path = "Assets/Prefabs/Tile_02_BrokeEffect.prefab";

		var go = new GameObject("Tile_02_BrokeEffect");
		var ps = go.AddComponent<ParticleSystem>();
		ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

		var main = ps.main;
		main.duration				= 1;
		main.loop					= false;
		main.startLifetime			= 0.8f;
		main.startSpeed				= 5f;
		main.startSize				= new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
		main.startRotation			= new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
		main.gravityModifier		= 2f;
		main.simulationSpace		= ParticleSystemSimulationSpace.Local;
		main.emitterVelocityMode	= ParticleSystemEmitterVelocityMode.Transform;
		main.playOnAwake			= true;

		var emission = ps.emission;
		emission.rateOverTime = 0;
		emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)10, (short)10, 1, 0.01f) });

		var shape = ps.shape;
		shape.enabled			= true;
		shape.shapeType			= ParticleSystemShapeType.Circle;
		shape.radius			= 1f;
		shape.radiusThickness	= 1f;
		shape.arc				= 90f;
		shape.rotation			= new Vector3(0, 0, 45);

		var rotation = ps.rotationOverLifetime;
		rotation.enabled	= true;
		rotation.z			= new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

		var renderer = go.GetComponent<ParticleSystemRenderer>();
		renderer.sharedMaterial	= material;
		renderer.sortingOrder	= 2;

		go.AddComponent<ParticleAutoDestroyer>();

		var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
		Object.DestroyImmediate(go);
		return prefab;
	}

	// 02강 - 타일 프리팹 설정
	private static void SetupTilePrefabs(int tileLayer, PhysicsMaterial2D platformSide, GameObject brokeEffect)
	{
		EditTilePrefab<TileBase>("Assets/Prefabs/Tile_00_NonBroke.prefab", tileLayer, platformSide, so =>
		{
			so.FindProperty("canBounce").boolValue = false;
		});

		EditTilePrefab<TileBase>("Assets/Prefabs/Tile_01_Bounce.prefab", tileLayer, platformSide, so =>
		{
			so.FindProperty("canBounce").boolValue = true;
		});

		EditTilePrefab<TileBroke>("Assets/Prefabs/Tile_02_Broke.prefab", tileLayer, platformSide, so =>
		{
			so.FindProperty("canBounce").boolValue = false;
			so.FindProperty("tileBrokeEffect").objectReferenceValue = brokeEffect;
		});

		string[] itemPaths =
		{
			"Assets/Prefabs/Item_00_Coin.prefab",
			"Assets/Prefabs/Item_01_Invincibility.prefab",
			"Assets/Prefabs/Item_02_HPPotion.prefab",
			"Assets/Prefabs/Item_03_Projectile.prefab",
		};
		Sprite emptyImage = LoadSprite("Assets/Textures/Ground/Tile_02_Item_01.png");

		EditTilePrefab<TileItem>("Assets/Prefabs/Tile_03_Item.prefab", tileLayer, platformSide, so =>
		{
			so.FindProperty("canBounce").boolValue	= true;
			so.FindProperty("itemType").intValue	= (int)ItemType.Random;
			so.FindProperty("coinCount").intValue	= 5;
			var prefabs = so.FindProperty("itemPrefabs");
			prefabs.arraySize = itemPaths.Length;
			for ( int i = 0; i < itemPaths.Length; ++ i )
			{
				prefabs.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(itemPaths[i]);
			}
			so.FindProperty("nonBrokeImage").objectReferenceValue = emptyImage;
		});
	}

	// 02강 - 이동 발판 (Platform_00_Moving)
	private static void SetupMovingPlatformPrefab(int platformLayer, PhysicsMaterial2D platformSide)
	{
		const string path = "Assets/Prefabs/Platform_00_Moving.prefab";
		var root = PrefabUtility.LoadPrefabContents(path);
		try
		{
			// 자식 오브젝트까지 모두 Platform 레이어 (Yes, change children)
			foreach ( var t in root.GetComponentsInChildren<Transform>(true) ) t.gameObject.layer = platformLayer;

			var target = root.transform.Find("Platform");
			var way01  = root.transform.Find("WayPoint01");
			var way02  = root.transform.Find("WayPoint02");
			if ( target == null || way01 == null || way02 == null )
				throw new System.Exception("Platform_00_Moving 프리팹에서 Platform / WayPoint01 / WayPoint02 오브젝트를 찾을 수 없습니다.");

			// Platform : Box Collider 2D (Material PlatformSide, Size 3 x 1)
			var box = target.GetComponent<BoxCollider2D>();
			if ( box == null ) box = target.gameObject.AddComponent<BoxCollider2D>();
			box.sharedMaterial	= platformSide;
			box.offset			= Vector2.zero;
			box.size			= new Vector2(3, 1);

			// Platform_00_Moving : Platform Moving (Script)
			var moving = root.GetComponent<PlatformMoving>();
			if ( moving == null ) moving = root.AddComponent<PlatformMoving>();
			var so = new SerializedObject(moving);
			so.FindProperty("target").objectReferenceValue = target;
			var points = so.FindProperty("wayPoints");
			points.arraySize = 2;
			points.GetArrayElementAtIndex(0).objectReferenceValue = way01;
			points.GetArrayElementAtIndex(1).objectReferenceValue = way02;
			so.FindProperty("waitTime").floatValue		= 1;
			so.FindProperty("timeOffset").floatValue	= 0.5f;
			so.ApplyModifiedPropertiesWithoutUndo();

			// Platform_00_Moving : Rigidbody 2D (Kinematic)
			var body = root.GetComponent<Rigidbody2D>();
			if ( body == null ) body = root.AddComponent<Rigidbody2D>();
			body.bodyType = RigidbodyType2D.Kinematic;

			PrefabUtility.SaveAsPrefabAsset(root, path);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	private static void EditTilePrefab<T>(string path, int tileLayer, PhysicsMaterial2D platformSide,
		System.Action<SerializedObject> setup) where T : TileBase
	{
		var root = PrefabUtility.LoadPrefabContents(path);
		try
		{
			root.layer = tileLayer;

			var box = root.GetComponent<BoxCollider2D>();
			if ( box == null ) box = root.AddComponent<BoxCollider2D>();
			box.sharedMaterial	= platformSide;
			box.offset			= Vector2.zero;
			box.size			= Vector2.one;

			// 다른 종류의 TileBase 컴포넌트가 붙어 있으면 제거 후 올바른 컴포넌트 추가
			foreach ( var old in root.GetComponents<TileBase>() )
			{
				if ( old.GetType() != typeof(T) ) Object.DestroyImmediate(old);
			}
			var tile = root.GetComponent<T>();
			if ( tile == null ) tile = root.AddComponent<T>();

			var so = new SerializedObject(tile);
			setup(so);
			so.ApplyModifiedPropertiesWithoutUndo();

			PrefabUtility.SaveAsPrefabAsset(root, path);
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(root);
		}
	}

	// =====================================================================
	// 씬
	// =====================================================================
	private static T GetOrAdd<T>(GameObject go) where T : Component
	{
		var c = go.GetComponent<T>();
		return c != null ? c : go.AddComponent<T>();
	}

	private static void MarkModified(Object obj)
	{
		EditorUtility.SetDirty(obj);
		if ( PrefabUtility.IsPartOfPrefabInstance(obj) ) PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
	}

	private static GameObject FindRequired(string path)
	{
		var go = GameObject.Find(path);
		if ( go == null ) throw new System.Exception($"씬에서 '{path}' 오브젝트를 찾을 수 없습니다.");
		return go;
	}

	private static Transform GetOrCreateChild(Transform parent, string name)
	{
		var child = parent.Find(name);
		if ( child != null ) return child;
		var go = new GameObject(name);
		go.transform.SetParent(parent, false);
		return go.transform;
	}

	// 01강 - 바닥 충돌 / Ground 레이어 / 벽 마찰력
	private static void SetupGround(int groundLayer, PhysicsMaterial2D platformSide)
	{
		var ground = FindRequired("Level01/Ground");
		ground.layer = groundLayer;
		MarkModified(ground);

		var body = GetOrAdd<Rigidbody2D>(ground);
		body.bodyType = RigidbodyType2D.Static;
		MarkModified(body);

		var tilemapCollider = GetOrAdd<UnityEngine.Tilemaps.TilemapCollider2D>(ground);
		tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
		MarkModified(tilemapCollider);

		var composite = GetOrAdd<CompositeCollider2D>(ground);
		composite.sharedMaterial = platformSide;
		MarkModified(composite);
	}

	// 01강 - 플레이어 캐릭터 / 02강 - 머리 충돌 레이어
	private static GameObject SetupPlayer(int groundLayer, int tileLayer, int platformLayer, AnimatorController controller,
		StageData stageData, Material footStepMat, Material landingMat)
	{
		var player = FindRequired("Player");

		// 강의에는 없는 CompositeCollider2D가 Player에 붙어 있으면 제거
		var strayComposite = player.GetComponent<CompositeCollider2D>();
		if ( strayComposite != null ) Object.DestroyImmediate(strayComposite);

		var body = GetOrAdd<Rigidbody2D>(player);
		body.bodyType		= RigidbodyType2D.Dynamic;
		body.constraints	= RigidbodyConstraints2D.FreezeRotation;

		var capsule = GetOrAdd<CapsuleCollider2D>(player);
		capsule.offset		= new Vector2(0, 0.9f);
		capsule.size		= new Vector2(1, 1.8f);
		capsule.direction	= CapsuleDirection2D.Vertical;

		var controllerComp = GetOrAdd<PlayerController>(player);
		var so = new SerializedObject(controllerComp);
		so.FindProperty("stageData").objectReferenceValue = stageData;
		so.ApplyModifiedPropertiesWithoutUndo();

		var movement = GetOrAdd<MovementRigidbody2D>(player);
		so = new SerializedObject(movement);
		so.FindProperty("groundCheckLayer").intValue	= (1 << groundLayer) | (1 << tileLayer) | (1 << platformLayer);
		so.FindProperty("aboveCollisionLayer").intValue	= 1 << tileLayer;
		so.FindProperty("walkSpeed").floatValue			= 5;
		so.FindProperty("runSpeed").floatValue			= 8;
		so.FindProperty("jumpForce").floatValue			= 13;
		so.FindProperty("lowGravityScale").floatValue	= 2;
		so.FindProperty("highGravityScale").floatValue	= 3.5f;
		so.ApplyModifiedPropertiesWithoutUndo();

		// Renderer : Sprite Renderer(Order 22) + Animator + PlayerAnimator
		var rendererTr = GetOrCreateChild(player.transform, "Renderer");
		var sprite = GetOrAdd<SpriteRenderer>(rendererTr.gameObject);
		sprite.sortingOrder = 22;
		var animator = GetOrAdd<Animator>(rendererTr.gameObject);
		animator.runtimeAnimatorController = controller;
		GetOrAdd<PlayerAnimator>(rendererTr.gameObject);

		// MovementEffects : FootStepEffect, LandingEffect
		var effectsTr = GetOrCreateChild(player.transform, "MovementEffects");
		effectsTr.localPosition = Vector3.zero;

		var footStep = SetupFootStepEffect(GetOrCreateChild(effectsTr, "FootStepEffect").gameObject, footStepMat);
		var landing  = SetupLandingEffect(GetOrCreateChild(effectsTr, "LandingEffect").gameObject, landingMat);

		var effects = GetOrAdd<MovementEffects>(effectsTr.gameObject);
		so = new SerializedObject(effects);
		so.FindProperty("footStepEffect").objectReferenceValue	= footStep;
		so.FindProperty("landingEffect").objectReferenceValue	= landing;
		so.ApplyModifiedPropertiesWithoutUndo();

		EditorUtility.SetDirty(player);
		return player;
	}

	private static ParticleSystem SetupFootStepEffect(GameObject go, Material material)
	{
		go.transform.localPosition = Vector3.zero;
		go.transform.localRotation = Quaternion.identity;
		var ps = GetOrAdd<ParticleSystem>(go);
		ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

		var main = ps.main;
		main.duration			= 5;
		main.loop				= true;
		main.startLifetime		= 1f;
		main.startSpeed			= 0.2f;
		main.startSize			= new ParticleSystem.MinMaxCurve(0.01f, 0.1f);
		main.gravityModifier	= -0.01f;
		main.simulationSpace	= ParticleSystemSimulationSpace.World;
		main.playOnAwake		= true;

		var emission = ps.emission;
		emission.rateOverTime = 0;

		var shape = ps.shape;
		shape.enabled	= true;
		shape.shapeType	= ParticleSystemShapeType.Circle;
		shape.radius	= 0.1f;

		var renderer = go.GetComponent<ParticleSystemRenderer>();
		renderer.sharedMaterial	= material;
		renderer.sortingOrder	= 20;
		return ps;
	}

	private static ParticleSystem SetupLandingEffect(GameObject go, Material material)
	{
		go.transform.localPosition = Vector3.zero;
		go.transform.localRotation = Quaternion.identity;
		var ps = GetOrAdd<ParticleSystem>(go);
		ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

		var main = ps.main;
		main.duration			= 2;
		main.loop				= false;
		main.startLifetime		= 1f;
		main.startSpeed			= 0.01f;
		main.startSize			= new ParticleSystem.MinMaxCurve(0.01f, 0.1f);
		main.startRotation		= new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
		main.gravityModifier	= -0.01f;
		main.simulationSpace	= ParticleSystemSimulationSpace.World;
		main.playOnAwake		= true;

		var emission = ps.emission;
		emission.rateOverTime = 0;
		emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)50, (short)50, 1, 0.01f) });

		var shape = ps.shape;
		shape.enabled	= true;
		shape.shapeType	= ParticleSystemShapeType.Box;
		shape.position	= Vector3.zero;
		shape.rotation	= Vector3.zero;
		shape.scale		= new Vector3(0.6f, 0.1f, 1f);

		var size = ps.sizeOverLifetime;
		size.enabled	= true;
		size.size		= new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

		var renderer = go.GetComponent<ParticleSystemRenderer>();
		renderer.sharedMaterial	= material;
		renderer.sortingOrder	= 20;
		return ps;
	}

	// 01강 - 플레이어 캐릭터를 추적하는 카메라
	private static Camera SetupMainCamera(GameObject player, StageData stageData)
	{
		var camGo = FindRequired("Main Camera");
		var camera = camGo.GetComponent<Camera>();
		camera.orthographic		= true;
		camera.orthographicSize	= 6;

		var follow = GetOrAdd<CameraFollowTarget>(camGo);
		var so = new SerializedObject(follow);
		so.FindProperty("target").objectReferenceValue		= player.transform;
		so.FindProperty("smoothTime").floatValue			= 0.25f;
		so.FindProperty("stageData").objectReferenceValue	= stageData;
		so.ApplyModifiedPropertiesWithoutUndo();

		// 배경 카메라(ParallaxCamera) 위에 겹쳐 그리도록 Overlay로 설정
		var data = camera.GetUniversalAdditionalCameraData();
		data.renderType = CameraRenderType.Overlay;
		EditorUtility.SetDirty(data);
		EditorUtility.SetDirty(camera);
		return camera;
	}

	// 01강 - 배경화면 스크롤 (Parallax Background)
	private static void SetupParallaxBackground(Camera mainCamera, Dictionary<string, Material> bgMats)
	{
		var rootGo = GameObject.Find("ParallaxBackground");
		if ( rootGo == null ) rootGo = new GameObject("ParallaxBackground");
		rootGo.transform.position = new Vector3(-30, 0, 0);

		// 배경을 촬영하는 카메라 (Base) + Stack에 Main Camera 등록
		var camTr = GetOrCreateChild(rootGo.transform, "ParallaxCamera");
		camTr.localPosition = new Vector3(0, 0, -10);
		var parallaxCamera = GetOrAdd<Camera>(camTr.gameObject);
		parallaxCamera.orthographic		= true;
		parallaxCamera.orthographicSize	= 6;
		parallaxCamera.nearClipPlane	= 0.3f;
		parallaxCamera.farClipPlane		= 1000;
		parallaxCamera.clearFlags		= CameraClearFlags.SolidColor;
		parallaxCamera.backgroundColor	= mainCamera.backgroundColor;
		var listener = camTr.GetComponent<AudioListener>();
		if ( listener != null ) Object.DestroyImmediate(listener);

		var parallaxData = parallaxCamera.GetUniversalAdditionalCameraData();
		parallaxData.renderType = CameraRenderType.Base;
		if ( !parallaxData.cameraStack.Contains(mainCamera) ) parallaxData.cameraStack.Add(mainCamera);
		EditorUtility.SetDirty(parallaxData);

		// 하늘(z 4), 구름(3), 원경(2), 중경(1), 근경(0)
		var renderers = new Dictionary<string, MeshRenderer>();
		string[] orderNames = { "Background_Sky", "Background_Cloud", "Background_02", "Background_01", "Background_00" };
		float[]  orderZ     = { 4, 3, 2, 1, 0 };
		for ( int k = 0; k < orderNames.Length; ++ k )
		{
			string name = orderNames[k];
			float  z    = orderZ[k];
			var quadTr = rootGo.transform.Find(name);
			if ( quadTr == null )
			{
				var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
				quad.name = name;
				quad.transform.SetParent(rootGo.transform, false);
				quadTr = quad.transform;
			}
			var collider = quadTr.GetComponent<MeshCollider>();
			if ( collider != null ) Object.DestroyImmediate(collider);

			quadTr.localPosition = new Vector3(0, 0, z);
			quadTr.localScale	 = new Vector3(22, 12, 1);
			var mr = quadTr.GetComponent<MeshRenderer>();
			mr.sharedMaterial = bgMats[name];
			renderers[name] = mr;
		}
		// Hierarchy 순서: ParallaxCamera, Sky, Cloud, 02, 01, 00
		camTr.SetSiblingIndex(0);
		for ( int i = 0; i < orderNames.Length; ++ i ) renderers[orderNames[i]].transform.SetSiblingIndex(i + 1);

		var parallax = GetOrAdd<ParallaxBackground>(rootGo);
		var so = new SerializedObject(parallax);
		so.FindProperty("targetCamera").objectReferenceValue = mainCamera.transform;
		var backgrounds = so.FindProperty("backgrounds");
		backgrounds.arraySize = 3;
		string[] names  = { "Background_00", "Background_01", "Background_02" };
		float[]  speeds = { 0.03f, 0.02f, 0.01f };
		for ( int i = 0; i < 3; ++ i )
		{
			var element = backgrounds.GetArrayElementAtIndex(i);
			element.FindPropertyRelative("background").objectReferenceValue	= renderers[names[i]];
			element.FindPropertyRelative("speed").floatValue				= speeds[i];
		}
		var cloud = so.FindProperty("backgroundCloud");
		cloud.FindPropertyRelative("background").objectReferenceValue	= renderers["Background_Cloud"];
		cloud.FindPropertyRelative("speed").floatValue					= 0.02f;
		so.ApplyModifiedPropertiesWithoutUndo();
	}
}
