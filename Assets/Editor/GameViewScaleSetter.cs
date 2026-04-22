using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GameViewScaleSetter
{
    private const float TargetScale = 0.4f;

    static GameViewScaleSetter()
    {
        EditorApplication.delayCall += ApplyTargetScale;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode ||
            state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += ApplyTargetScale;
        }
    }

    private static void ApplyTargetScale()
    {
        System.Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType == null)
        {
            return;
        }

        EditorWindow gameView = EditorWindow.GetWindow(gameViewType);
        if (gameView == null)
        {
            return;
        }

        FieldInfo zoomAreaField = gameViewType.GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic);
        object zoomArea = zoomAreaField?.GetValue(gameView);
        if (zoomArea == null)
        {
            return;
        }

        FieldInfo scaleField = zoomArea.GetType().GetField("m_Scale", BindingFlags.Instance | BindingFlags.NonPublic);
        if (scaleField == null)
        {
            return;
        }

        scaleField.SetValue(zoomArea, new Vector2(TargetScale, TargetScale));
        gameView.Repaint();
    }
}
