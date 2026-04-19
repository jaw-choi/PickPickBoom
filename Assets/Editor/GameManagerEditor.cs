using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameManager))]
public sealed class GameManagerEditor : Editor
{
    private SerializedProperty floorGeneratorProperty = null!;
    private SerializedProperty uiManagerProperty = null!;
    private SerializedProperty boardPanDurationPerRowProperty = null!;
    private SerializedProperty floorPreviewDurationProperty = null!;
    private SerializedProperty floorPreviewFlipDurationProperty = null!;
    private SerializedProperty rowAdvanceScrollDurationProperty = null!;
    private SerializedProperty revealAnimationDurationProperty = null!;
    private SerializedProperty postRevealDelayProperty = null!;
    private SerializedProperty floorTransitionDelayProperty = null!;

    private void OnEnable()
    {
        floorGeneratorProperty = serializedObject.FindProperty("floorGenerator");
        uiManagerProperty = serializedObject.FindProperty("uiManager");
        boardPanDurationPerRowProperty = serializedObject.FindProperty("boardPanDurationPerRow");
        floorPreviewDurationProperty = serializedObject.FindProperty("floorPreviewDuration");
        floorPreviewFlipDurationProperty = serializedObject.FindProperty("floorPreviewFlipDuration");
        rowAdvanceScrollDurationProperty = serializedObject.FindProperty("rowAdvanceScrollDuration");
        revealAnimationDurationProperty = serializedObject.FindProperty("revealAnimationDuration");
        postRevealDelayProperty = serializedObject.FindProperty("postRevealDelay");
        floorTransitionDelayProperty = serializedObject.FindProperty("floorTransitionDelay");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawReferencesSection();

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("디자이너 타이밍 패널", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("카메라 이동, 카드 공개, 뒤집기, 층 전환 시간을 여기서 바로 조절할 수 있습니다.", MessageType.Info);

        DrawPresetButtons();
        EditorGUILayout.Space(8f);

        DrawTimingSection("보드 이동", "값이 클수록 느려집니다.", () =>
        {
            DrawSlider(boardPanDurationPerRowProperty, "카메라 내려오는 시간(행당)", 0f, 2f);
            DrawSlider(rowAdvanceScrollDurationProperty, "다음 행 자동 스크롤 시간", 0f, 2f);
        });

        DrawTimingSection("카드 프리뷰", "라운드 시작 시 카드들을 보여주는 연출입니다.", () =>
        {
            DrawSlider(floorPreviewDurationProperty, "카드 공개 유지 시간", 0f, 3f);
            DrawSlider(floorPreviewFlipDurationProperty, "카드 뒤집기 시간", 0f, 1.5f);
        });

        DrawTimingSection("선택 연출", "카드 선택 직후 공개되고 다음 단계로 넘어가기 전까지의 시간입니다.", () =>
        {
            DrawSlider(revealAnimationDurationProperty, "선택 카드 공개 시간", 0f, 1.5f);
            DrawSlider(postRevealDelayProperty, "선택 후 대기 시간", 0f, 1.5f);
        });

        DrawTimingSection("층 전환", "한 층을 끝내고 다음 층을 만들기 전 대기 시간입니다.", () =>
        {
            DrawSlider(floorTransitionDelayProperty, "새 층 전환 대기 시간", 0f, 2f);
        });

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            $"시작 프리뷰 체감 시간: {floorPreviewDurationProperty.floatValue + floorPreviewFlipDurationProperty.floatValue:0.00}초\n" +
            $"카드 선택 후 다음 행 이동 전 체감 시간: {revealAnimationDurationProperty.floatValue + postRevealDelayProperty.floatValue:0.00}초",
            MessageType.None);

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
        EditorGUILayout.LabelField("빠른 프리셋", EditorStyles.miniBoldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("빠름"))
            {
                ApplyPreset(0.22f, 0.35f, 0.12f, 0.18f, 0.12f, 0.08f, 0.2f);
            }

            if (GUILayout.Button("기본"))
            {
                ApplyPreset(0.4f, 0.8f, 0.22f, 0.35f, 0.18f, 0.18f, 0.45f);
            }

            if (GUILayout.Button("느림"))
            {
                ApplyPreset(0.7f, 1.2f, 0.35f, 0.55f, 0.3f, 0.3f, 0.65f);
            }
        }
    }

    private void DrawTimingSection(string title, string description, System.Action drawFields)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(4f);
        drawFields.Invoke();
        EditorGUILayout.EndVertical();
    }

    private static void DrawSlider(SerializedProperty property, string label, float min, float max)
    {
        EditorGUILayout.Slider(property, min, max, new GUIContent(label, "초 단위"));
    }

    private void ApplyPreset(
        float boardPanDurationPerRow,
        float floorPreviewDuration,
        float floorPreviewFlipDuration,
        float rowAdvanceScrollDuration,
        float revealAnimationDuration,
        float postRevealDelay,
        float floorTransitionDelay)
    {
        boardPanDurationPerRowProperty.floatValue = boardPanDurationPerRow;
        floorPreviewDurationProperty.floatValue = floorPreviewDuration;
        floorPreviewFlipDurationProperty.floatValue = floorPreviewFlipDuration;
        rowAdvanceScrollDurationProperty.floatValue = rowAdvanceScrollDuration;
        revealAnimationDurationProperty.floatValue = revealAnimationDuration;
        postRevealDelayProperty.floatValue = postRevealDelay;
        floorTransitionDelayProperty.floatValue = floorTransitionDelay;
    }
}
