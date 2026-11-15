using UnityEditor;
using UnityEditor.SceneManagement;

// 에디터에서 Play를 누르면 열려 있는 씬과 상관없이 항상 곡 선택 화면(SongSelectScene)부터 시작한다.
// (빌드된 게임은 Build Settings 0번인 SongSelectScene에서 시작하므로 에디터도 같은 흐름으로 맞춘다)
[InitializeOnLoad]
public static class PlayModeStartScene
{
    private const string StartScenePath = "Assets/Scenes/SongSelectScene.unity";

    static PlayModeStartScene()
    {
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);
    }
}
