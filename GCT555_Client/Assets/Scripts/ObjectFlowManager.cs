using UnityEngine;

public class ObjectFlowManager : MonoBehaviour
{
    private enum FlowMode
    {
        Overview,
        Browsing
    }

    private const int ObjectCount = 9;
    private const int LeftShoulderIndex = 11;
    private const int RightShoulderIndex = 12;
    private const int LeftHipIndex = 23;
    private const int RightHipIndex = 24;
    private const float MinSpacing = 0.45f;
    private const float MaxSpacing = 1.05f;
    private const float MinSideDepth = 0.08f;
    private const float MaxSideDepth = 0.55f;
    private const float MaxSideAngle = 62f;
    private const float CenterScale = 0.75f;
    private const float SideScale = 0.52f;
    private const float BrowsingCenterScale = 1.05f;
    private const float FlowCurve = 0.82f;

    [Header("Flow Controls")]
    [Range(0f, 1f)]
    public float depth = 0.5f;

    [Range(0f, 1f)]
    public float position = 0.5f;

    public float gapMultiplier = 2f;

    [Header("Pose Source")]
    public StreamManager streamManager;
    public bool driveFromPose = true;
    public bool mirrorPoseX = false;
    public bool topIsDepthOne = true;
    public float poseFollowSpeed = 8f;

    [Header("Stage Controls")]
    [Range(0f, 1f)]
    public float browsingLine = 0.5f;

    [Range(0f, 1f)]
    public float screenTouchLine = 0.92f;

    public bool useBrowsingMode = true;
    public float browsingYawRange = 55f;
    public float browsingPitchRange = 35f;

    [Header("Debug Lines")]
    public bool showDebugLines = true;
    public float debugLineLength = 8f;
    public float debugLineZ = -0.65f;
    public Color browsingLineColor = Color.cyan;
    public Color touchLineColor = Color.yellow;

    [Header("Selection Effect")]
    public Color selectionColor = Color.yellow;
    public float selectionPulseSpeed = 8f;
    public float selectionPulseAmount = 0.16f;
    public float selectionLift = 0.18f;

    private readonly GameObject[] flowObjects = new GameObject[ObjectCount];
    private readonly Color[] baseColors = { Color.red, Color.green, Color.blue };
    private Material[] generatedMaterials;
    private StreamClient poseClient;
    private FlowMode currentMode = FlowMode.Overview;
    private int selectedIndex = -1;

    private void Awake()
    {
        EnsureObjects();
        UpdateLayout();
    }

    private void Update()
    {
        EnsureObjects();
        ApplyPoseInput();
        UpdateTrackingState();
        UpdateLayout();
        UpdateDebugLines();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < flowObjects.Length; i++)
        {
            if (flowObjects[i] != null)
            {
                Destroy(flowObjects[i]);
            }
        }

        if (generatedMaterials != null)
        {
            for (int i = 0; i < generatedMaterials.Length; i++)
            {
                if (generatedMaterials[i] != null)
                {
                    Destroy(generatedMaterials[i]);
                }
            }
        }

    }

    private void EnsureObjects()
    {
        EnsureMaterials();

        for (int i = 0; i < ObjectCount; i++)
        {
            if (flowObjects[i] != null)
                continue;

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"FlowCube_{i + 1}";
            cube.transform.SetParent(transform, false);
            cube.transform.localScale = Vector3.one * CenterScale;

            Collider cubeCollider = cube.GetComponent<Collider>();
            if (cubeCollider != null)
            {
                Destroy(cubeCollider);
            }

            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null && generatedMaterials != null)
            {
                cubeRenderer.sharedMaterial = generatedMaterials[i % generatedMaterials.Length];
            }

            flowObjects[i] = cube;
        }
    }

    private void ApplyPoseInput()
    {
        if (!driveFromPose)
            return;

        StreamClient client = GetPoseClient();
        if (client == null || client.latestPoseData == null || client.latestPoseData.landmarks == null)
            return;

        if (!TryGetPoseCenter(client.latestPoseData, out Vector2 poseCenter))
            return;

        float targetPosition = mirrorPoseX ? 1f - poseCenter.x : poseCenter.x;
        float targetDepth = topIsDepthOne ? 1f - poseCenter.y : poseCenter.y;
        float followT = 1f - Mathf.Exp(-poseFollowSpeed * Time.deltaTime);

        position = Mathf.Lerp(position, Mathf.Clamp01(targetPosition), followT);
        depth = Mathf.Lerp(depth, Mathf.Clamp01(targetDepth), followT);
    }

    private StreamClient GetPoseClient()
    {
        if (poseClient != null && poseClient.clientType == StreamClient.ClientType.Pose)
            return poseClient;

        if (streamManager == null)
        {
            streamManager = FindObjectOfType<StreamManager>();
        }

        if (streamManager == null || streamManager.activeClients == null)
            return null;

        for (int i = 0; i < streamManager.activeClients.Count; i++)
        {
            StreamClient client = streamManager.activeClients[i];
            if (client != null && client.clientType == StreamClient.ClientType.Pose)
            {
                poseClient = client;
                return poseClient;
            }
        }

        return null;
    }

    private bool TryGetPoseCenter(PoseData poseData, out Vector2 center)
    {
        center = new Vector2(0.5f, 0.5f);

        if (TryAverageLandmark(poseData, LeftShoulderIndex, RightShoulderIndex, LeftHipIndex, RightHipIndex, out center))
            return true;

        return TryAverageLandmark(poseData, 0, 1, 2, 3, out center);
    }

    private bool TryAverageLandmark(PoseData poseData, int a, int b, int c, int d, out Vector2 center)
    {
        center = new Vector2(0.5f, 0.5f);

        if (poseData.landmarks.Count <= Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d)))
            return false;

        center.x = (poseData.landmarks[a].x + poseData.landmarks[b].x + poseData.landmarks[c].x + poseData.landmarks[d].x) * 0.25f;
        center.y = (poseData.landmarks[a].y + poseData.landmarks[b].y + poseData.landmarks[c].y + poseData.landmarks[d].y) * 0.25f;
        return true;
    }

    private void EnsureMaterials()
    {
        if (generatedMaterials != null)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader == null)
        {
            Debug.LogError("[ObjectFlowManager] Could not find a compatible cube shader.");
            return;
        }

        generatedMaterials = new Material[ObjectCount];
        for (int i = 0; i < generatedMaterials.Length; i++)
        {
            generatedMaterials[i] = CreateMaterial(shader, baseColors[i % baseColors.Length]);
        }
    }

    private Material CreateMaterial(Shader shader, Color color)
    {
        Material material = new Material(shader)
        {
            name = $"ObjectFlow_{color}"
        };

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        return material;
    }

    private void UpdateTrackingState()
    {
        if (!useBrowsingMode)
        {
            currentMode = FlowMode.Overview;
            selectedIndex = -1;
            return;
        }

        currentMode = depth >= browsingLine ? FlowMode.Browsing : FlowMode.Overview;

        if (currentMode == FlowMode.Browsing && IsScreenTouched())
        {
            int newSelectedIndex = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(position) * (ObjectCount - 1)), 0, ObjectCount - 1);
            if (newSelectedIndex != selectedIndex)
            {
                Debug.Log($"[ObjectFlowManager] Selected FlowCube_{newSelectedIndex + 1} in Browsing mode. position={position:F3}, depth={depth:F3}");
            }

            selectedIndex = newSelectedIndex;
        }
        else if (currentMode == FlowMode.Overview)
        {
            selectedIndex = -1;
        }
    }

    private bool IsScreenTouched()
    {
        return depth >= screenTouchLine;
    }

    private void UpdateLayout()
    {
        float clampedDepth = Mathf.Clamp01(depth);
        float focusedIndex = Mathf.Clamp01(position) * (ObjectCount - 1);
        float spacing = Mathf.Lerp(MinSpacing, MaxSpacing, clampedDepth) * gapMultiplier;
        float sideDepth = Mathf.Lerp(MinSideDepth, MaxSideDepth, clampedDepth) * gapMultiplier;

        for (int i = 0; i < ObjectCount; i++)
        {
            GameObject flowObject = flowObjects[i];
            if (flowObject == null)
                continue;

            float offset = i - focusedIndex;
            float absOffset = Mathf.Abs(offset);
            float sideAmount = Mathf.Clamp01(absOffset);
            float x = Mathf.Sign(offset) * Mathf.Pow(absOffset, FlowCurve) * spacing;
            float z = Mathf.Min(absOffset, 4f) * sideDepth;
            float xRotation = 0f;
            float yRotation = -Mathf.Sign(offset) * MaxSideAngle * sideAmount;
            float scale = Mathf.Lerp(CenterScale, SideScale, sideAmount);
            float y = 0f;

            bool isSelected = currentMode == FlowMode.Browsing && i == selectedIndex;
            if (currentMode == FlowMode.Browsing && absOffset < 0.5f)
            {
                scale = BrowsingCenterScale;
                Vector2 detailRotation = GetBodyDetailRotation();
                xRotation = detailRotation.y;
                yRotation += detailRotation.x;
            }

            if (isSelected)
            {
                float pulse = 1f + Mathf.Sin(Time.time * selectionPulseSpeed) * selectionPulseAmount;
                scale *= pulse;
                y += selectionLift;
            }

            flowObject.transform.localPosition = new Vector3(x, y, z);
            flowObject.transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
            flowObject.transform.localScale = Vector3.one * scale;
            UpdateSelectionVisual(i, isSelected);
        }
    }

    private Vector2 GetBodyDetailRotation()
    {
        float yaw = (Mathf.Clamp01(position) - 0.5f) * browsingYawRange;
        float browsingSpan = Mathf.Max(0.001f, 1f - browsingLine);
        float browsingProgress = Mathf.Clamp01((Mathf.Clamp01(depth) - browsingLine) / browsingSpan);
        float pitch = (browsingProgress - 0.5f) * browsingPitchRange;
        return new Vector2(yaw, pitch);
    }

    private void UpdateSelectionVisual(int index, bool isSelected)
    {
        Renderer flowRenderer = flowObjects[index].GetComponent<Renderer>();
        if (flowRenderer == null || generatedMaterials == null)
            return;

        Material material = generatedMaterials[index % generatedMaterials.Length];
        Color color = isSelected ? Color.Lerp(baseColors[index % baseColors.Length], selectionColor, 0.75f) : baseColors[index % baseColors.Length];
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            if (isSelected)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", selectionColor * 1.2f);
            }
            else
            {
                material.SetColor("_EmissionColor", Color.black);
            }
        }
    }

    private void UpdateDebugLines()
    {
        if (!showDebugLines)
            return;

        DrawDebugLine(browsingLine, browsingLineColor);
        DrawDebugLine(screenTouchLine, touchLineColor);
    }

    private void DrawDebugLine(float normalizedY, Color color)
    {
        float localY = NormalizedFloorToLocalY(normalizedY);
        float halfLength = debugLineLength * 0.5f;
        Vector3 start = transform.TransformPoint(new Vector3(-halfLength, localY, debugLineZ));
        Vector3 end = transform.TransformPoint(new Vector3(halfLength, localY, debugLineZ));
        Debug.DrawLine(start, end, color);
    }

    private float NormalizedFloorToLocalY(float normalizedY)
    {
        return Mathf.Lerp(2f, -2f, Mathf.Clamp01(normalizedY));
    }
}
