using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public sealed class GameManagerEditor : Editor
{
    private SerializedProperty floorGeneratorProperty = null!;
    private SerializedProperty uiManagerProperty = null!;
    private SerializedProperty currentDifficultyProperty = null!;
    private SerializedProperty easyProfileProperty = null!;
    private SerializedProperty normalProfileProperty = null!;
    private SerializedProperty hardProfileProperty = null!;
    private SerializedProperty commonFloorPreviewFlipDurationProperty = null!;
    private SerializedProperty commonRowAdvanceScrollDurationProperty = null!;

    private void OnEnable()
    {
        floorGeneratorProperty = serializedObject.FindProperty("floorGenerator");
        uiManagerProperty = serializedObject.FindProperty("uiManager");
        currentDifficultyProperty = serializedObject.FindProperty("currentDifficulty");
        easyProfileProperty = serializedObject.FindProperty("easyProfile");
        normalProfileProperty = serializedObject.FindProperty("normalProfile");
        hardProfileProperty = serializedObject.FindProperty("hardProfile");
        commonFloorPreviewFlipDurationProperty = serializedObject.FindProperty("commonFloorPreviewFlipDuration");
        commonRowAdvanceScrollDurationProperty = serializedObject.FindProperty("commonRowAdvanceScrollDuration");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawReferencesSection();

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("난이도 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(currentDifficultyProperty, new GUIContent("현재 난이도"));
        EditorGUILayout.HelpBox(
            "난이도별로 카드 공개 속도와 랜덤 카드 확률을 따로 조절합니다. Easy는 느리고 좋은 아이템이 많고, Hard는 빠르고 좋은 아이템이 적습니다.",
            MessageType.Info);

        DrawPresetButtons();
        EditorGUILayout.Space(8f);

        DrawCommonFastTiming();
        EditorGUILayout.Space(8f);

        DrawDifficultyProfile("Easy", easyProfileProperty);
        DrawDifficultyProfile("Normal", normalProfileProperty);
        DrawDifficultyProfile("Hard", hardProfileProperty);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawReferencesSection()
    {
        EditorGUILayout.LabelField("참조", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(floorGeneratorProperty, new GUIContent("Floor Generator"));
        EditorGUILayout.PropertyField(uiManagerProperty, new GUIContent("UI Manager"));
    }

    private void DrawPresetButtons()
    {
        EditorGUILayout.LabelField("빠른 기본값", EditorStyles.miniBoldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Easy 기본값"))
            {
                ApplyProfile(easyProfileProperty, GameDifficultyProfile.CreateEasy());
            }

            if (GUILayout.Button("Normal 기본값"))
            {
                ApplyProfile(normalProfileProperty, GameDifficultyProfile.CreateNormal());
            }

            if (GUILayout.Button("Hard 기본값"))
            {
                ApplyProfile(hardProfileProperty, GameDifficultyProfile.CreateHard());
            }
        }
    }

    private void DrawDifficultyProfile(string title, SerializedProperty profileProperty)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        DrawTimingSection(profileProperty);
        EditorGUILayout.Space(6f);
        DrawWeightSection(profileProperty);
        DrawProfileSummary(profileProperty);

        EditorGUILayout.EndVertical();
    }

    private void DrawCommonFastTiming()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("공통 빠른 시간", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("세 가지 난이도 모두 같은 값을 사용합니다.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Slider(commonFloorPreviewFlipDurationProperty, 0f, 1.5f, new GUIContent("카드 뒤집기 시간", "초 단위"));
        EditorGUILayout.Slider(commonRowAdvanceScrollDurationProperty, 0f, 2f, new GUIContent("다음 행 자동 스크롤 시간", "초 단위"));
        EditorGUILayout.EndVertical();
    }

    private static void DrawTimingSection(SerializedProperty profileProperty)
    {
        EditorGUILayout.LabelField("카드 표시/연출 시간", EditorStyles.miniBoldLabel);
        DrawSlider(profileProperty, "boardPanDurationPerRow", "카메라 내려오는 시간(행당)", 0f, 2f);
        DrawSlider(profileProperty, "floorPreviewDuration", "카드 공개 유지 시간", 0f, 3f);
        DrawSlider(profileProperty, "revealAnimationDuration", "선택 카드 공개 시간", 0f, 1.5f);
        DrawSlider(profileProperty, "postRevealDelay", "선택 후 대기 시간", 0f, 1.5f);
        DrawSlider(profileProperty, "floorTransitionDelay", "새 층 전환 대기 시간", 0f, 2f);
    }

    private static void DrawWeightSection(SerializedProperty profileProperty)
    {
        EditorGUILayout.LabelField("랜덤 카드 등장 확률", EditorStyles.miniBoldLabel);
        DrawIntSlider(profileProperty, "emptyWeight", "빈 카드 가중치", 0, 100);
        DrawIntSlider(profileProperty, "curseWeight", "저주 카드 가중치", 0, 100);
        DrawIntSlider(profileProperty, "goodItemWeight", "좋은 아이템 가중치", 0, 100);
    }

    private static void DrawProfileSummary(SerializedProperty profileProperty)
    {
        SerializedProperty emptyWeight = profileProperty.FindPropertyRelative("emptyWeight");
        SerializedProperty curseWeight = profileProperty.FindPropertyRelative("curseWeight");
        SerializedProperty goodItemWeight = profileProperty.FindPropertyRelative("goodItemWeight");
        int totalWeight = Mathf.Max(0, emptyWeight.intValue) +
            Mathf.Max(0, curseWeight.intValue) +
            Mathf.Max(0, goodItemWeight.intValue);

        if (totalWeight <= 0)
        {
            EditorGUILayout.HelpBox("모든 가중치가 0이면 빈 카드가 기본으로 선택됩니다.", MessageType.Warning);
            return;
        }

        float emptyPercent = emptyWeight.intValue / (float)totalWeight * 100f;
        float cursePercent = curseWeight.intValue / (float)totalWeight * 100f;
        float goodItemPercent = goodItemWeight.intValue / (float)totalWeight * 100f;

        EditorGUILayout.HelpBox(
            $"현재 확률: 빈 카드 {emptyPercent:0.#}% / 저주 {cursePercent:0.#}% / 좋은 아이템 {goodItemPercent:0.#}%",
            MessageType.None);
    }

    private static void DrawSlider(SerializedProperty profileProperty, string propertyName, string label, float min, float max)
    {
        SerializedProperty property = profileProperty.FindPropertyRelative(propertyName);
        EditorGUILayout.Slider(property, min, max, new GUIContent(label, "초 단위"));
    }

    private static void DrawIntSlider(SerializedProperty profileProperty, string propertyName, string label, int min, int max)
    {
        SerializedProperty property = profileProperty.FindPropertyRelative(propertyName);
        EditorGUILayout.IntSlider(property, min, max, new GUIContent(label));
    }

    private static void ApplyProfile(SerializedProperty targetProperty, GameDifficultyProfile source)
    {
        targetProperty.FindPropertyRelative("boardPanDurationPerRow").floatValue = source.boardPanDurationPerRow;
        targetProperty.FindPropertyRelative("floorPreviewDuration").floatValue = source.floorPreviewDuration;
        targetProperty.FindPropertyRelative("revealAnimationDuration").floatValue = source.revealAnimationDuration;
        targetProperty.FindPropertyRelative("postRevealDelay").floatValue = source.postRevealDelay;
        targetProperty.FindPropertyRelative("floorTransitionDelay").floatValue = source.floorTransitionDelay;
        targetProperty.FindPropertyRelative("emptyWeight").intValue = source.emptyWeight;
        targetProperty.FindPropertyRelative("curseWeight").intValue = source.curseWeight;
        targetProperty.FindPropertyRelative("goodItemWeight").intValue = source.goodItemWeight;
    }
}
