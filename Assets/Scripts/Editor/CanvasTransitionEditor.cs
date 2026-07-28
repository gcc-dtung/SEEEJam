using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CanvasTransition))]
public class CanvasTransitionEditor : Editor
{
    private const string PreviewTimeKey = "CanvasTransitionEditor.PreviewTime";
    private const string IsPreviewingKey = "CanvasTransitionEditor.IsPreviewing";

    private double previewStartTime;
    private float previewStartValue;
    private bool isPreviewing;

    private void OnEnable()
    {
        isPreviewing = SessionState.GetBool(IsPreviewingKey, false);
        previewStartTime = EditorApplication.timeSinceStartup;
        previewStartValue = SessionState.GetFloat(PreviewTimeKey, 0f);

        if (isPreviewing)
            EditorApplication.update += UpdatePreview;
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
        SessionState.SetBool(IsPreviewingKey, isPreviewing);
        SessionState.SetFloat(PreviewTimeKey, PreviewTime);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12f);
        EditorGUILayout.LabelField("Editor Preview", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        float previewTime = EditorGUILayout.Slider("Preview Time", PreviewTime, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            StopAutoPreview();
            PreviewTime = previewTime;
            ApplyPreview(previewTime);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(isPreviewing ? "Stop Preview" : "Play Preview"))
            {
                if (isPreviewing)
                    StopAutoPreview();
                else
                    StartAutoPreview();
            }

            if (GUILayout.Button("Reset"))
            {
                StopAutoPreview();
                PreviewTime = 0f;
                ((CanvasTransition)target).ResetPreview();
                MarkDirty();
            }
        }

        EditorGUILayout.HelpBox(
            "Dung trong Edit Mode: keo Preview Time de xem tung khoanh khac, hoac bam Play Preview de chay thu transition ma khong can Play Scene.",
            MessageType.Info);
    }

    private float PreviewTime
    {
        get => SessionState.GetFloat(PreviewTimeKey, 0f);
        set => SessionState.SetFloat(PreviewTimeKey, Mathf.Clamp01(value));
    }

    private void StartAutoPreview()
    {
        isPreviewing = true;
        previewStartTime = EditorApplication.timeSinceStartup;
        previewStartValue = PreviewTime;
        SessionState.SetBool(IsPreviewingKey, true);
        EditorApplication.update -= UpdatePreview;
        EditorApplication.update += UpdatePreview;
    }

    private void StopAutoPreview()
    {
        isPreviewing = false;
        SessionState.SetBool(IsPreviewingKey, false);
        EditorApplication.update -= UpdatePreview;
    }

    private void UpdatePreview()
    {
        if (target == null)
        {
            StopAutoPreview();
            return;
        }

        CanvasTransition transition = (CanvasTransition)target;
        float elapsed = (float)(EditorApplication.timeSinceStartup - previewStartTime);
        float normalizedElapsed = elapsed / transition.TotalDuration;
        float previewTime = Mathf.Clamp01(previewStartValue + normalizedElapsed);

        PreviewTime = previewTime;
        ApplyPreview(previewTime);
        Repaint();

        if (previewTime >= 1f)
            StopAutoPreview();
    }

    private void ApplyPreview(float previewTime)
    {
        ((CanvasTransition)target).Preview(previewTime);
        MarkDirty();
    }

    private void MarkDirty()
    {
        EditorUtility.SetDirty(target);
        SceneView.RepaintAll();
    }
}
