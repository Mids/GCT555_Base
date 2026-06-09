using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroSceneController : MonoBehaviour
{
    public string moveNavSceneName = "MoveNavScene";
    public string touchNavSceneName = "TouchNavScene";

    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;
    private GUIStyle indexLabelStyle;
    private GUIStyle indexNumberStyle;
    private int[] selectedArtifactIds;

    private void Awake()
    {
        selectedArtifactIds = ArtifactSelectionPlan.GenerateAndSaveArtifactIds();
    }

    private void OnGUI()
    {
        EnsureStyles();

        DrawSolidRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.055f, 0.06f, 0.07f, 1f));

        float panelWidth = Mathf.Min(760f, Screen.width - 80f);
        float panelHeight = Mathf.Min(580f, Screen.height - 80f);
        Rect panelRect = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight);

        DrawSolidRect(panelRect, new Color(0.12f, 0.125f, 0.135f, 0.94f));

        GUILayout.BeginArea(new Rect(panelRect.x + 48f, panelRect.y + 44f, panelRect.width - 96f, panelRect.height - 88f));
        GUILayout.Label("Select Navigation Scene", titleStyle);
        GUILayout.Space(8f);
        GUILayout.Label("Choose the interaction mode to launch.", subtitleStyle);
        GUILayout.Space(40f);

        if (GUILayout.Button("Move Navigation\nUser position controls browse and detail modes", buttonStyle, GUILayout.Height(112f)))
        {
            LoadMoveNavScene();
        }

        GUILayout.Space(18f);

        if (GUILayout.Button("Touch Navigation\nScreen touches control mode switching and selection", buttonStyle, GUILayout.Height(112f)))
        {
            LoadTouchNavScene();
        }

        GUILayout.Space(26f);
        GUILayout.Label("Selected Artifact Numbers (1-18)", indexLabelStyle);
        GUILayout.Space(8f);
        DrawArtifactIndexRow();

        GUILayout.EndArea();
    }

    public void LoadMoveNavScene()
    {
        LoadScene(moveNavSceneName);
    }

    public void LoadTouchNavScene()
    {
        LoadScene(touchNavSceneName);
    }

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[IntroSceneController] Scene name is empty.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 42,
            fontStyle = FontStyle.Bold
        };
        titleStyle.normal.textColor = new Color(0.95f, 0.95f, 0.93f);

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22
        };
        subtitleStyle.normal.textColor = new Color(0.72f, 0.75f, 0.78f);

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            padding = new RectOffset(24, 24, 18, 18)
        };

        indexLabelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        indexLabelStyle.normal.textColor = new Color(0.74f, 0.78f, 0.82f);

        indexNumberStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            margin = new RectOffset(6, 6, 0, 0),
            padding = new RectOffset(0, 0, 7, 7)
        };
        indexNumberStyle.normal.textColor = new Color(0.98f, 0.94f, 0.72f);
    }

    private void DrawArtifactIndexRow()
    {
        if (selectedArtifactIds == null || selectedArtifactIds.Length == 0)
        {
            selectedArtifactIds = ArtifactSelectionPlan.GetOrCreateArtifactIds();
        }

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        for (int i = 0; i < selectedArtifactIds.Length; i++)
        {
            GUILayout.Label(selectedArtifactIds[i].ToString(), indexNumberStyle, GUILayout.Width(44f), GUILayout.Height(44f));
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private static void DrawSolidRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}
