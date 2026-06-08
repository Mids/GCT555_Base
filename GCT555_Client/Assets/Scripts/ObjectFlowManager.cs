using UnityEngine;

public class ObjectFlowManager : MonoBehaviour
{
    public enum NavigationMode
    {
        MoveDistance,
        ScreenTouch
    }

    private enum FlowMode
    {
        Overview,
        Browsing,
        TouchConfirmed,
        Detail
    }

    private enum TouchSection
    {
        Left,
        Center,
        Right
    }

    private const int ObjectCount = 7;
    private const int LeftShoulderIndex = 11;
    private const int RightShoulderIndex = 12;
    private const int LeftWristIndex = 15;
    private const int RightWristIndex = 16;
    private const int LeftHipIndex = 23;
    private const int RightHipIndex = 24;
    private const float MinSpacing = 0.45f;
    private const float MaxSpacing = 1.05f;
    private const float CenterScale = 1f;
    private const float SideScale = 0.52f;
    private const float BrowsingCenterScale = 1.05f;
    private static readonly int LeonardMoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int LeonardIsPointingHash = Animator.StringToHash("IsPointing");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [Header("Flow Controls")]
    [Range(0f, 1f)]
    public float depth = 0.5f;

    [Range(0f, 1f)]
    public float position = 0.5f;

    public float gapMultiplier = 2f;
    public float overviewSpacing = 0.85f;
    public float overviewScale = 0.52f;
    public float browseSpacing = 1.05f;
    public float browseScale = 0.62f;
    public float touchSelectionFlowSpeed = 8f;

    [Header("Artifact Prefabs")]
    public GameObject[] finalArtifactPrefabs;
    public TextAsset museumArtifactData;
    public float artifactNormalizedSize = CenterScale;
    public float artifactYawOffsetDegrees = 180f;

    [Header("Artifact Stands")]
    public bool showArtifactStands = true;
    public float artifactStandFloorY = -0.5f;
    public Vector2 artifactStandFootprint = new Vector2(1.1f, 1.1f);
    public float artifactStandHeight = 0.65f;
    public float artifactStandTopThickness = 0.08f;
    public float artifactStandPanelDepth = 0.025f;
    public Color artifactStandBodyColor = new Color(0.72f, 0.71f, 0.66f, 1f);
    public Color artifactStandTopColor = new Color(0.94f, 0.93f, 0.9f, 1f);
    public Color artifactStandPanelColor = new Color(0.82f, 0.81f, 0.76f, 1f);

    [Header("Pose Source")]
    public StreamManager streamManager;
    public bool driveFromPose = true;
    public bool mirrorPoseX = false;
    public bool topIsDepthOne = true;
    public float poseFollowSpeed = 8f;
    public float poseLostTimeout = 1f;

    [Header("Leonard Avatar")]
    public GameObject leonardAvatarPrefab;
    public RuntimeAnimatorController leonardAnimatorController;
    public float leonardAnimatorSpeed = 1f;
    public float leonardWalkSpeedScale = 1f;
    public float leonardMoveSpeedDampTime = 0.12f;
    public bool spawnLeonardAvatar = true;
    public Vector2 leonardLocalXRange = new Vector2(-3f, 3f);
    public Vector2 leonardLocalZRange = new Vector2(-3f, 0.5f);
    public float leonardLocalY = -1.25f;
    public Vector3 leonardLocalEulerAngles = Vector3.zero;
    public float leonardPositionFollowSpeed = 10f;

    [Header("Stage Controls")]
    public NavigationMode navigationMode = NavigationMode.MoveDistance;

    [Range(0f, 1f)]
    public float browsingLine = 0.5f;

    [Range(0f, 1f)]
    public float screenTouchLine = 0.92f;

    public bool useBrowsingMode = true;
    public float browsingYawRange = 55f;
    public float browsingPitchRange = 35f;

    [Header("Wrist Touch Detection")]
    public bool useWristTouchDetection = true;
    public float wristVisibilityThreshold = 0.35f;
    public bool logWristTouchDebug = true;
    public float wristSampleLogInterval = 0.75f;

    [Header("Two-Hand Manipulation")]
    public bool useTwoHandManipulation = true;
    public float minManipulationScale = 0.6f;
    public float maxManipulationScale = 1.8f;
    public float manipulationYawRange = 90f;
    public float manipulationPitchRange = 70f;
    public float manipulationRollMultiplier = 1f;
    public float manipulationSmoothing = 12f;
    public bool logTwoHandManipulationDebug = true;

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

    [Header("Mode Switching")]
    public Color candidateColor = new Color(1f, 0.65f, 0.15f);
    public float candidateScaleBoost = 1.12f;
    public float candidateLift = 0.08f;
    public float touchConfirmationDuration = 0.45f;
    public float touchConfirmedScaleBoost = 1.22f;
    public float detailScale = 1.85f;
    public float detailSideSpacing = 1.75f;
    public float detailBackgroundScale = 0.42f;
    public float detailReturnDepthDelta = 0.12f;
    [Range(0f, 1f)]
    public float detailBackgroundDim = 0.72f;
    public bool showModeLabels = true;
    public Vector3 modeBannerLocalPosition = new Vector3(0f, 1.72f, -0.45f);
    public Vector3 modeHintLocalPosition = new Vector3(0f, -1.72f, -0.45f);
    public bool anchorModeLabelsToCamera = true;
    public Camera modeLabelCamera;
    public Vector2 modeBannerViewportPosition = new Vector2(0.06f, 0.92f);
    public Vector2 modeHintViewportPosition = new Vector2(0.06f, 0.86f);
    public float modeLabelCameraDistance = 6.6f;
    [Range(0.1f, 0.95f)]
    public float modeLabelMaxViewportWidth = 0.5f;
    public float modeBannerCharacterSize = 0.075f;
    public float modeHintCharacterSize = 0.058f;
    public float modeLabelMinCharacterSize = 0.035f;

    [Header("Detail Modal")]
    public bool showDetailModal = true;
    public Vector2 detailModalViewportPosition = new Vector2(0.78f, 0.52f);
    public Vector2 detailModalViewportSize = new Vector2(0.28f, 0.58f);
    public float detailModalCameraDistance = 6.6f;
    public Color detailModalBackgroundColor = new Color(0f, 0f, 0f, 0.58f);
    public Color detailModalBorderColor = new Color(1f, 0.65f, 0.15f, 0.55f);
    public Color detailModalTextColor = Color.white;
    public Color detailModalMutedTextColor = new Color(0.82f, 0.82f, 0.82f, 1f);
    public Color detailModalAccentColor = new Color(1f, 0.65f, 0.15f, 1f);
    public float detailModalTitleCharacterSize = 0.07f;
    public float detailModalBodyCharacterSize = 0.046f;
    public float detailModalFooterCharacterSize = 0.048f;
    public int detailModalMaxDescriptionCharacters = 260;

    private readonly GameObject[] flowObjects = new GameObject[ObjectCount];
    private readonly Renderer[][] flowRenderers = new Renderer[ObjectCount][];
    private readonly Vector3[] flowBaseScales = new Vector3[ObjectCount];
    private readonly bool[] flowUsesGeneratedMaterial = new bool[ObjectCount];
    private readonly GameObject[] artifactStandObjects = new GameObject[ObjectCount];
    private readonly Bounds[] artifactLocalBounds = new Bounds[ObjectCount];
    private readonly bool[] hasArtifactLocalBounds = new bool[ObjectCount];
    private readonly MuseumArtifactInfo[] flowArtifactInfos = new MuseumArtifactInfo[ObjectCount];
    private readonly Color[] baseColors = { Color.red, Color.green, Color.blue };
    private static readonly string[] DetailTitles =
    {
        "Lunar Compass",
        "Glass Archive",
        "Ceramic Signal",
        "Bronze Dial",
        "Field Relic",
        "Obsidian Lens",
        "Ivory Token",
        "Slate Vessel",
        "Amber Figure"
    };

    private static readonly string[] DetailOrigins =
    {
        "Aegean coast, 1240 BCE",
        "North observatory, 540 CE",
        "River delta workshop, 920 BCE",
        "Mountain foundry, 310 BCE",
        "Desert survey camp, 1884",
        "Island shrine, 760 CE",
        "Harbor market, 150 BCE",
        "Highland kiln, 610 CE",
        "Forest archive, 1320"
    };

    private static readonly string[] DetailMaterials =
    {
        "Brass, shell, mineral ink",
        "Smoked glass and cedar",
        "Fired clay, ash glaze",
        "Bronze and black enamel",
        "Limestone and cotton cord",
        "Obsidian, copper, resin",
        "Ivory and lapis pigment",
        "Slate, tin, charcoal",
        "Amber and carved walnut"
    };

    private static readonly string[] DetailPeriods =
    {
        "Late Maritime Kingdom",
        "Early Astronomer Guild",
        "Delta Settlement Period",
        "Hellenistic Trade Route",
        "Industrial Expedition Era",
        "Middle Temple Period",
        "Coastal Exchange Age",
        "Northern Kiln Dynasty",
        "Royal Scriptorium Phase"
    };

    private static readonly string[] DetailDescriptions =
    {
        "A portable navigation object used to align night travel with seasonal star paths.",
        "A preserved record fragment believed to store ritual measurements and tax marks.",
        "A hand-sized signal vessel whose glaze pattern marked ownership and destination.",
        "A calibrated dial used by merchants to compare weights across regional systems.",
        "A field marker carried by survey teams to register boundary and route decisions.",
        "A polished lens mounted in resin for ceremonial inspection of small inscriptions.",
        "A counting token exchanged during harbor audits and sealed cargo transfers.",
        "A storage vessel associated with pigment preparation and workshop apprentices.",
        "A carved figure used as a teaching object in courtly archive demonstrations."
    };
    private Material[] generatedMaterials;
    private Material artifactStandBodyMaterial;
    private Material artifactStandTopMaterial;
    private Material artifactStandPanelMaterial;
    private MuseumArtifactInfo[] museumArtifacts;
    private bool didLoadMuseumArtifacts;
    private StreamClient poseClient;
    private FlowMode currentMode = FlowMode.Overview;
    private int candidateIndex = -1;
    private int selectedIndex = -1;
    private bool wasWristTouching;
    private float nextWristSampleLogTime;
    private float touchConfirmationEndTime;
    private float detailEntryDepth;
    private TextMesh modeBannerLabel;
    private TextMesh modeHintLabel;
    private GameObject detailModalRoot;
    private GameObject detailModalPanel;
    private GameObject[] detailModalBorders;
    private Material detailModalPanelMaterial;
    private Material detailModalBorderMaterial;
    private TextMesh detailModalTitleLabel;
    private TextMesh detailModalSubtitleLabel;
    private TextMesh detailModalBodyLabel;
    private TextMesh detailModalFooterLabel;
    private GameObject leonardAvatarInstance;
    private Animator leonardAnimator;
    private bool hasPoseTracking = true;
    private bool poseTrackingJustRestored;
    private bool hasLeonardMoveSpeedParameter;
    private bool hasLeonardPointingParameter;
    private bool hasPreviousLeonardLocalPosition;
    private bool hasTouchLayoutFocusedIndex;
    private float touchLayoutFocusedIndex;
    private Vector3 previousLeonardLocalPosition;
    private bool hasTwoHandBaseline;
    private int twoHandManipulationIndex = -1;
    private float baselineWristDistance;
    private float baselineWristAngle;
    private Vector2 baselineWristMidpoint;
    private float baselineSelectedScaleMultiplier = 1f;
    private float baselineSelectedYawOffset;
    private float baselineSelectedPitchOffset;
    private float baselineSelectedRollOffset;
    private float selectedScaleMultiplier = 1f;
    private float selectedYawOffset;
    private float selectedPitchOffset;
    private float selectedRollOffset;
    private MaterialPropertyBlock selectionPropertyBlock;

    private void Awake()
    {
        EnsureObjects();
        EnsureModeLabels();
        EnsureDetailModal();
        UpdateLayout();
        UpdateModeLabels();
        UpdateDetailModal();
    }

    private void Update()
    {
        EnsureObjects();
        EnsureModeLabels();
        EnsureDetailModal();
        ApplyPoseInput();
        UpdateLeonardAvatar();
        UpdateTrackingState();
        UpdateLeonardAnimation();
        UpdateTwoHandManipulation();
        UpdateLayout();
        UpdateModeLabels();
        UpdateDetailModal();
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

        for (int i = 0; i < artifactStandObjects.Length; i++)
        {
            if (artifactStandObjects[i] != null)
            {
                Destroy(artifactStandObjects[i]);
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

        if (artifactStandBodyMaterial != null)
        {
            Destroy(artifactStandBodyMaterial);
        }

        if (artifactStandTopMaterial != null)
        {
            Destroy(artifactStandTopMaterial);
        }

        if (artifactStandPanelMaterial != null)
        {
            Destroy(artifactStandPanelMaterial);
        }

        if (leonardAvatarInstance != null)
        {
            Destroy(leonardAvatarInstance);
        }

        if (modeBannerLabel != null)
        {
            Destroy(modeBannerLabel.gameObject);
        }

        if (modeHintLabel != null)
        {
            Destroy(modeHintLabel.gameObject);
        }

        if (detailModalRoot != null)
        {
            Destroy(detailModalRoot);
        }

        if (detailModalPanelMaterial != null)
        {
            Destroy(detailModalPanelMaterial);
        }

        if (detailModalBorderMaterial != null)
        {
            Destroy(detailModalBorderMaterial);
        }
    }

    private void EnsureObjects()
    {
        SelectedArtifactPrefab[] selectedPrefabs = PickRandomFinalArtifactPrefabs();
        if (selectedPrefabs == null || selectedPrefabs.Length < ObjectCount)
        {
            EnsureMaterials();
        }

        for (int i = 0; i < ObjectCount; i++)
        {
            if (flowObjects[i] != null)
                continue;

            GameObject flowObject = CreateFlowObject(i, selectedPrefabs);
            flowObjects[i] = flowObject;
            flowBaseScales[i] = flowObject.transform.localScale;
            CacheFlowRenderers(i);
        }
    }

    private SelectedArtifactPrefab[] PickRandomFinalArtifactPrefabs()
    {
        int availableCount = CountValidFinalArtifactPrefabs();
        if (availableCount == 0)
            return null;

        SelectedArtifactPrefab[] pool = new SelectedArtifactPrefab[availableCount];
        int poolIndex = 0;
        for (int i = 0; i < finalArtifactPrefabs.Length; i++)
        {
            if (finalArtifactPrefabs[i] != null)
            {
                pool[poolIndex] = new SelectedArtifactPrefab
                {
                    prefab = finalArtifactPrefabs[i],
                    artifactId = i + 1
                };
                poolIndex++;
            }
        }

        int selectedCount = Mathf.Min(ObjectCount, pool.Length);
        SelectedArtifactPrefab[] selectedPrefabs = new SelectedArtifactPrefab[selectedCount];
        for (int i = 0; i < selectedCount; i++)
        {
            int selectedIndexInPool = Random.Range(i, pool.Length);
            selectedPrefabs[i] = pool[selectedIndexInPool];
            pool[selectedIndexInPool] = pool[i];
            pool[i] = selectedPrefabs[i];
        }

        return selectedPrefabs;
    }

    private int CountValidFinalArtifactPrefabs()
    {
        if (finalArtifactPrefabs == null)
            return 0;

        int count = 0;
        for (int i = 0; i < finalArtifactPrefabs.Length; i++)
        {
            if (finalArtifactPrefabs[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private GameObject CreateFlowObject(int index, SelectedArtifactPrefab[] selectedPrefabs)
    {
        if (selectedPrefabs != null && index < selectedPrefabs.Length && selectedPrefabs[index].prefab != null)
        {
            return CreateArtifactFlowObject(index, selectedPrefabs[index].prefab, selectedPrefabs[index].artifactId);
        }

        return CreateFallbackCube(index);
    }

    private GameObject CreateArtifactFlowObject(int index, GameObject prefab, int artifactId)
    {
        GameObject artifactRoot = new GameObject($"FlowArtifact_{index + 1}_{prefab.name}");
        artifactRoot.transform.SetParent(transform, false);
        flowArtifactInfos[index] = FindMuseumArtifactInfo(artifactId);

        GameObject artifact = Instantiate(prefab, artifactRoot.transform);
        artifact.name = prefab.name;
        artifact.transform.localPosition = Vector3.zero;
        RemoveColliders(artifact);
        NormalizeArtifactChild(artifactRoot.transform, artifact.transform);
        Bounds artifactBounds = GetDefaultArtifactBounds();
        if (TryGetLocalRendererBounds(artifactRoot.transform, artifactRoot.GetComponentsInChildren<Renderer>(true), out Bounds normalizedBounds))
        {
            artifactBounds = normalizedBounds;
        }

        artifactLocalBounds[index] = artifactBounds;
        hasArtifactLocalBounds[index] = true;
        CreateArtifactStand(index);
        flowUsesGeneratedMaterial[index] = false;
        return artifactRoot;
    }

    private GameObject CreateFallbackCube(int index)
    {
        flowArtifactInfos[index] = null;

        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = $"FlowCube_{index + 1}";
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
            cubeRenderer.sharedMaterial = generatedMaterials[index % generatedMaterials.Length];
        }

        flowUsesGeneratedMaterial[index] = true;
        return cube;
    }

    private void CreateArtifactStand(int index)
    {
        if (!showArtifactStands)
            return;

        EnsureArtifactStandMaterials();

        float width = Mathf.Max(0.1f, artifactStandFootprint.x);
        float depth = Mathf.Max(0.1f, artifactStandFootprint.y);
        float bodyHeight = Mathf.Max(0.01f, artifactStandHeight);
        float topThickness = Mathf.Max(0.01f, artifactStandTopThickness);
        float panelDepth = Mathf.Max(0.001f, artifactStandPanelDepth);

        GameObject standRoot = new GameObject($"ArtifactStand_{index + 1}");
        standRoot.transform.SetParent(transform, false);
        artifactStandObjects[index] = standRoot;

        CreateStandCube(
            "StandBody",
            standRoot.transform,
            new Vector3(width, bodyHeight, depth),
            new Vector3(0f, bodyHeight * 0.5f, 0f),
            artifactStandBodyMaterial);

        CreateStandCube(
            "StandTop",
            standRoot.transform,
            new Vector3(width * 1.08f, topThickness, depth * 1.08f),
            new Vector3(0f, bodyHeight + topThickness * 0.5f, 0f),
            artifactStandTopMaterial);

        Vector3 panelScale = new Vector3(width * 0.66f, bodyHeight * 0.58f, panelDepth);
        Vector3 frontPanelPosition = new Vector3(0f, bodyHeight * 0.5f, -depth * 0.5f - panelDepth * 0.5f - 0.001f);
        Vector3 backPanelPosition = new Vector3(0f, bodyHeight * 0.5f, depth * 0.5f + panelDepth * 0.5f + 0.001f);
        CreateStandCube("StandFrontInset", standRoot.transform, panelScale, frontPanelPosition, artifactStandPanelMaterial);
        CreateStandCube("StandBackInset", standRoot.transform, panelScale, backPanelPosition, artifactStandPanelMaterial);
    }

    private GameObject CreateStandCube(string name, Transform parent, Vector3 localScale, Vector3 localPosition, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = localScale;

        Collider collider = cube.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null && material != null)
        {
            renderer.sharedMaterial = material;
        }

        return cube;
    }

    private Bounds GetDefaultArtifactBounds()
    {
        float size = Mathf.Max(0.0001f, artifactNormalizedSize);
        return new Bounds(Vector3.zero, Vector3.one * size);
    }

    private void CacheFlowRenderers(int index)
    {
        if (flowObjects[index] == null)
        {
            flowRenderers[index] = null;
            return;
        }

        flowRenderers[index] = flowObjects[index].GetComponentsInChildren<Renderer>(true);
    }

    private void RemoveColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Destroy(colliders[i]);
        }
    }

    private void NormalizeArtifactChild(Transform artifactRoot, Transform artifactChild)
    {
        Renderer[] renderers = artifactRoot.GetComponentsInChildren<Renderer>(true);
        if (!TryGetLocalRendererBounds(artifactRoot, renderers, out Bounds bounds))
            return;

        float largestSize = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (largestSize <= 0.0001f)
            return;

        float targetSize = Mathf.Max(0.0001f, artifactNormalizedSize);
        artifactChild.localScale *= targetSize / largestSize;

        renderers = artifactRoot.GetComponentsInChildren<Renderer>(true);
        if (TryGetLocalRendererBounds(artifactRoot, renderers, out bounds))
        {
            artifactChild.localPosition -= bounds.center;
        }
    }

    private bool TryGetLocalRendererBounds(Transform root, Renderer[] renderers, out Bounds bounds)
    {
        bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Bounds rendererBounds = renderer.bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };

            for (int cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
            {
                Vector3 localCorner = root.InverseTransformPoint(corners[cornerIndex]);
                if (!hasBounds)
                {
                    bounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(localCorner);
                }
            }
        }

        return hasBounds;
    }

    private void ApplyPoseInput()
    {
        poseTrackingJustRestored = false;

        if (!driveFromPose)
        {
            SetPoseTracking(true);
            return;
        }

        StreamClient client = GetPoseClient();
        if (client == null || client.latestPoseData == null || client.latestPoseData.landmarks == null)
        {
            SetPoseTracking(false);
            return;
        }

        if (poseLostTimeout > 0f && client.lastDataTime >= 0f && Time.time - client.lastDataTime > poseLostTimeout)
        {
            SetPoseTracking(false);
            return;
        }

        if (!TryGetPoseCenter(client.latestPoseData, out Vector2 poseCenter))
        {
            SetPoseTracking(false);
            return;
        }

        SetPoseTracking(true);

        float targetPosition = mirrorPoseX ? 1f - poseCenter.x : poseCenter.x;
        float targetDepth = topIsDepthOne ? 1f - poseCenter.y : poseCenter.y;
        float followT = 1f - Mathf.Exp(-poseFollowSpeed * Time.deltaTime);

        position = Mathf.Lerp(position, Mathf.Clamp01(targetPosition), followT);
        depth = Mathf.Lerp(depth, Mathf.Clamp01(targetDepth), followT);
    }

    private void SetPoseTracking(bool isTracking)
    {
        if (!hasPoseTracking && isTracking)
        {
            poseTrackingJustRestored = true;
        }

        hasPoseTracking = isTracking;
    }

    private void UpdateLeonardAvatar()
    {
        if (!spawnLeonardAvatar || leonardAvatarPrefab == null)
            return;

        Vector3 targetLocalPosition = GetLeonardTargetLocalPosition();

        if (leonardAvatarInstance == null)
        {
            leonardAvatarInstance = Instantiate(leonardAvatarPrefab, transform);
            leonardAvatarInstance.name = "LeonardAvatar";
            leonardAvatarInstance.transform.localPosition = targetLocalPosition;
            leonardAvatarInstance.transform.localRotation = Quaternion.Euler(leonardLocalEulerAngles);
            ConfigureLeonardAnimator();
        }

        float followT = 1f - Mathf.Exp(-leonardPositionFollowSpeed * Time.deltaTime);

        leonardAvatarInstance.transform.localPosition = Vector3.Lerp(leonardAvatarInstance.transform.localPosition, targetLocalPosition, followT);
        leonardAvatarInstance.transform.localRotation = Quaternion.Euler(leonardLocalEulerAngles);
    }

    private void ConfigureLeonardAnimator()
    {
        leonardAnimator = null;
        hasLeonardMoveSpeedParameter = false;
        hasLeonardPointingParameter = false;
        hasPreviousLeonardLocalPosition = false;

        if (leonardAvatarInstance == null)
            return;

        leonardAnimator = leonardAvatarInstance.GetComponent<Animator>();
        if (leonardAnimator == null)
        {
            leonardAnimator = leonardAvatarInstance.GetComponentInChildren<Animator>();
        }

        if (leonardAnimator == null)
            return;

        if (leonardAnimatorController != null)
        {
            leonardAnimator.runtimeAnimatorController = leonardAnimatorController;
        }

        leonardAnimator.applyRootMotion = false;
        leonardAnimator.speed = leonardAnimatorSpeed;
        leonardAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        CacheLeonardAnimatorParameters();
    }

    private void CacheLeonardAnimatorParameters()
    {
        if (leonardAnimator == null)
            return;

        AnimatorControllerParameter[] parameters = leonardAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Float && parameters[i].nameHash == LeonardMoveSpeedHash)
            {
                hasLeonardMoveSpeedParameter = true;
            }
            else if (parameters[i].type == AnimatorControllerParameterType.Bool && parameters[i].nameHash == LeonardIsPointingHash)
            {
                hasLeonardPointingParameter = true;
            }
        }
    }

    private void UpdateLeonardAnimation()
    {
        if (leonardAvatarInstance == null || leonardAnimator == null)
            return;

        Vector3 currentLocalPosition = leonardAvatarInstance.transform.localPosition;
        float moveSpeed = 0f;
        if (hasPreviousLeonardLocalPosition && Time.deltaTime > 0f)
        {
            moveSpeed = (currentLocalPosition - previousLeonardLocalPosition).magnitude / Time.deltaTime;
        }

        previousLeonardLocalPosition = currentLocalPosition;
        hasPreviousLeonardLocalPosition = true;

        if (hasLeonardMoveSpeedParameter)
        {
            float normalizedMoveSpeed = Mathf.Clamp01(moveSpeed * leonardWalkSpeedScale);
            leonardAnimator.SetFloat(LeonardMoveSpeedHash, normalizedMoveSpeed, leonardMoveSpeedDampTime, Time.deltaTime);
        }

        if (hasLeonardPointingParameter)
        {
            bool shouldPoint = IsSelectionActive();
            leonardAnimator.SetBool(LeonardIsPointingHash, shouldPoint);
        }
    }

    private Vector3 GetLeonardTargetLocalPosition()
    {
        float localX = Mathf.Lerp(leonardLocalXRange.x, leonardLocalXRange.y, Mathf.Clamp01(position));
        float localZ = Mathf.Lerp(leonardLocalZRange.x, leonardLocalZRange.y, Mathf.Clamp01(depth));
        return new Vector3(localX, leonardLocalY, localZ);
    }

    private StreamClient GetPoseClient()
    {
        if (poseClient != null && poseClient.clientType == StreamClient.ClientType.Pose)
            return poseClient;

        if (streamManager == null)
        {
            streamManager = FindFirstObjectByType<StreamManager>();
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

        if (!TryGetVisibleLandmark(poseData, a, out Landmark landmarkA)
            || !TryGetVisibleLandmark(poseData, b, out Landmark landmarkB)
            || !TryGetVisibleLandmark(poseData, c, out Landmark landmarkC)
            || !TryGetVisibleLandmark(poseData, d, out Landmark landmarkD))
        {
            return false;
        }

        center.x = (landmarkA.x + landmarkB.x + landmarkC.x + landmarkD.x) * 0.25f;
        center.y = (landmarkA.y + landmarkB.y + landmarkC.y + landmarkD.y) * 0.25f;
        return true;
    }

    private void EnsureMaterials()
    {
        if (generatedMaterials != null)
            return;

        Shader shader = FindCompatibleLitShader();
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

    private void EnsureArtifactStandMaterials()
    {
        if (artifactStandBodyMaterial != null && artifactStandTopMaterial != null && artifactStandPanelMaterial != null)
            return;

        Shader shader = FindCompatibleLitShader();
        if (shader == null)
        {
            Debug.LogError("[ObjectFlowManager] Could not find a compatible stand shader.");
            return;
        }

        artifactStandBodyMaterial = CreateMaterial(shader, artifactStandBodyColor);
        artifactStandBodyMaterial.name = "ArtifactStand_Body";
        artifactStandTopMaterial = CreateMaterial(shader, artifactStandTopColor);
        artifactStandTopMaterial.name = "ArtifactStand_Top";
        artifactStandPanelMaterial = CreateMaterial(shader, artifactStandPanelColor);
        artifactStandPanelMaterial.name = "ArtifactStand_Panel";
    }

    private Shader FindCompatibleLitShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        return shader;
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
        if (driveFromPose && !hasPoseTracking)
        {
            ReturnToOverviewAfterPoseLoss();
            return;
        }

        if (!useBrowsingMode)
        {
            currentMode = FlowMode.Overview;
            ClearModeSelection();
            ResetTwoHandManipulation();
            return;
        }

        if (poseTrackingJustRestored)
        {
            EnterBrowseAfterPoseRestore();
            return;
        }

        if (navigationMode == NavigationMode.ScreenTouch)
        {
            UpdateTouchNavigationState();
            return;
        }

        UpdateMoveNavigationState();
    }

    private void ReturnToOverviewAfterPoseLoss()
    {
        if (currentMode != FlowMode.Overview || selectedIndex >= 0 || candidateIndex >= 0)
        {
            Debug.Log("[ObjectFlowManager] Pose tracking lost. Returning to Overview mode.");
        }

        currentMode = FlowMode.Overview;
        ClearModeSelection();
        ResetTwoHandManipulation();
        wasWristTouching = false;
    }

    private void EnterBrowseAfterPoseRestore()
    {
        currentMode = FlowMode.Browsing;
        selectedIndex = -1;
        candidateIndex = GetObjectIndexAtPosition();
        ResetTwoHandManipulation();
        wasWristTouching = IsScreenTouched();
        Debug.Log($"[ObjectFlowManager] Pose tracking restored. Switching to Browsing mode. navigation={navigationMode}, candidate=FlowCube_{candidateIndex + 1}");
    }

    private void UpdateMoveNavigationState()
    {
        candidateIndex = GetObjectIndexAtPosition();

        bool wasTouchingBefore = wasWristTouching;
        bool isTouching = IsScreenTouched();
        bool touchStarted = isTouching && !wasTouchingBefore;

        if (currentMode == FlowMode.TouchConfirmed)
        {
            if (Time.time >= touchConfirmationEndTime)
            {
                currentMode = FlowMode.Detail;
                detailEntryDepth = depth;
                Debug.Log($"[ObjectFlowManager] Detail mode entered for FlowCube_{selectedIndex + 1}.");
            }

            return;
        }

        if (currentMode == FlowMode.Detail)
        {
            if (touchStarted)
            {
                Debug.Log($"[ObjectFlowManager] Returned to Browsing mode from Detail by touch. position={position:F3}, depth={depth:F3}");
                currentMode = FlowMode.Browsing;
                selectedIndex = -1;
                ResetTwoHandManipulation();
            }

            return;
        }

        currentMode = FlowMode.Browsing;
        selectedIndex = -1;

        if (touchStarted)
        {
            selectedIndex = candidateIndex;
            currentMode = FlowMode.TouchConfirmed;
            touchConfirmationEndTime = Time.time + touchConfirmationDuration;
            ResetTwoHandManipulation();
            Debug.Log($"[ObjectFlowManager] Touch confirmed FlowCube_{selectedIndex + 1}. position={position:F3}, depth={depth:F3}");
        }
    }

    private void UpdateTouchNavigationState()
    {
        EnsureTouchNavigationCandidate();

        bool wasTouchingBefore = wasWristTouching;
        bool isTouching = TryGetScreenTouch(out TouchSection touchSection, out float touchX, out string touchReason);
        if (logWristTouchDebug && isTouching != wasTouchingBefore)
        {
            Debug.Log(isTouching ? $"[ObjectFlowManager] Screen touch detected: {touchReason}" : "[ObjectFlowManager] Screen touch ended.");
        }

        wasWristTouching = isTouching;
        bool touchStarted = isTouching && !wasTouchingBefore;

        if (currentMode == FlowMode.TouchConfirmed)
        {
            if (Time.time >= touchConfirmationEndTime)
            {
                currentMode = FlowMode.Detail;
                detailEntryDepth = depth;
                Debug.Log($"[ObjectFlowManager] Detail mode entered for FlowCube_{selectedIndex + 1}.");
            }

            return;
        }

        if (!touchStarted)
            return;

        switch (currentMode)
        {
            case FlowMode.Overview:
                currentMode = FlowMode.Browsing;
                candidateIndex = GetObjectIndexFromTouchX(touchX);
                selectedIndex = -1;
                ResetTwoHandManipulation();
                Debug.Log($"[ObjectFlowManager] Touch navigation entered Browsing mode. section={touchSection}, candidate=FlowCube_{candidateIndex + 1}, touchX={touchX:F3}");
                break;
            case FlowMode.Browsing:
                HandleTouchNavigationBrowseTouch(touchSection, touchX);
                break;
            case FlowMode.Detail:
                int returnCandidate = selectedIndex >= 0 ? selectedIndex : candidateIndex;
                currentMode = FlowMode.Browsing;
                selectedIndex = -1;
                candidateIndex = Mathf.Clamp(returnCandidate, 0, ObjectCount - 1);
                ResetTwoHandManipulation();
                Debug.Log($"[ObjectFlowManager] Touch navigation returned to Browsing mode from Detail. candidate=FlowCube_{candidateIndex + 1}, section={touchSection}, touchX={touchX:F3}");
                break;
            default:
                currentMode = FlowMode.Overview;
                ClearModeSelection();
                ResetTwoHandManipulation();
                break;
        }
    }

    private void EnsureTouchNavigationCandidate()
    {
        if (currentMode == FlowMode.Overview)
        {
            candidateIndex = -1;
            return;
        }

        if (candidateIndex < 0)
        {
            candidateIndex = GetObjectIndexAtPosition();
        }

        candidateIndex = Mathf.Clamp(candidateIndex, 0, ObjectCount - 1);
    }

    private void HandleTouchNavigationBrowseTouch(TouchSection touchSection, float touchX)
    {
        switch (touchSection)
        {
            case TouchSection.Left:
                candidateIndex = Mathf.Max(0, candidateIndex - 1);
                Debug.Log($"[ObjectFlowManager] Touch navigation moved selection left to FlowCube_{candidateIndex + 1}. touchX={touchX:F3}");
                break;
            case TouchSection.Right:
                candidateIndex = Mathf.Min(ObjectCount - 1, candidateIndex + 1);
                Debug.Log($"[ObjectFlowManager] Touch navigation moved selection right to FlowCube_{candidateIndex + 1}. touchX={touchX:F3}");
                break;
            default:
                selectedIndex = candidateIndex;
                currentMode = FlowMode.Detail;
                detailEntryDepth = depth;
                ResetTwoHandManipulation();
                Debug.Log($"[ObjectFlowManager] Touch navigation entered Detail mode for FlowCube_{selectedIndex + 1}. touchX={touchX:F3}");
                break;
        }
    }

    private int GetObjectIndexAtPosition()
    {
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(position) * (ObjectCount - 1)), 0, ObjectCount - 1);
    }

    private int GetObjectIndexFromTouchX(float touchX)
    {
        return Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(touchX) * (ObjectCount - 1)), 0, ObjectCount - 1);
    }

    private bool IsSelectionActive()
    {
        return selectedIndex >= 0 && (currentMode == FlowMode.TouchConfirmed || currentMode == FlowMode.Detail);
    }

    private void ClearModeSelection()
    {
        candidateIndex = -1;
        selectedIndex = -1;
        touchConfirmationEndTime = 0f;
        detailEntryDepth = 0f;
    }

    private bool IsScreenTouched()
    {
        bool isTouching = TryGetScreenTouch(out _, out _, out string reason);
        if (logWristTouchDebug && isTouching != wasWristTouching)
        {
            Debug.Log(isTouching ? $"[ObjectFlowManager] Wrist touch detected: {reason}" : "[ObjectFlowManager] Wrist touch ended.");
        }

        wasWristTouching = isTouching;
        return isTouching;
    }

    private bool TryGetScreenTouch(out TouchSection touchSection, out float touchX, out string reason)
    {
        touchX = Mathf.Clamp01(position);
        touchSection = GetTouchSection(touchX);
        reason = "";

        if (!useWristTouchDetection)
        {
            bool isTouching = depth >= screenTouchLine;
            reason = $"depth={depth:F3}, touchX={touchX:F3}, section={touchSection}, touchLine={screenTouchLine:F3}";
            return isTouching;
        }

        StreamClient client = GetPoseClient();
        PoseData poseData = client != null ? client.latestPoseData : null;
        if (poseData == null || poseData.landmarks == null)
            return false;

        bool hasLeftWrist = TryGetVisibleLandmark(poseData, LeftWristIndex, out Landmark leftWrist);
        bool hasRightWrist = TryGetVisibleLandmark(poseData, RightWristIndex, out Landmark rightWrist);
        bool leftTouch = hasLeftWrist && leftWrist.y >= screenTouchLine;
        bool rightTouch = hasRightWrist && rightWrist.y >= screenTouchLine;

        if (leftTouch || rightTouch)
        {
            touchX = GetTouchX(leftWrist, leftTouch, rightWrist, rightTouch);
            touchSection = GetTouchSection(touchX);
            reason = $"{FormatWrist("left", leftWrist, leftTouch)} {FormatWrist("right", rightWrist, rightTouch)} touchX={touchX:F3}, section={touchSection}, touchLine={screenTouchLine:F3}";
            return true;
        }

        if (logWristTouchDebug && Time.unscaledTime >= nextWristSampleLogTime)
        {
            Debug.Log($"[ObjectFlowManager] Wrist sample {FormatWrist("left", leftWrist, false)} {FormatWrist("right", rightWrist, false)} touchLine={screenTouchLine:F3}");
            nextWristSampleLogTime = Time.unscaledTime + Mathf.Max(0.1f, wristSampleLogInterval);
        }

        return false;
    }

    private bool TryGetVisibleLandmark(PoseData poseData, int index, out Landmark landmark)
    {
        landmark = null;
        if (poseData.landmarks.Count <= index)
            return false;

        landmark = poseData.landmarks[index];
        return landmark != null && (landmark.visibility <= 0f || landmark.visibility >= wristVisibilityThreshold);
    }

    private string FormatWrist(string label, Landmark wrist, bool touching)
    {
        if (wrist == null)
            return $"{label}=missing";

        return $"{label}(x={GetPoseScreenX(wrist):F3}, y={wrist.y:F3}, visibility={wrist.visibility:F3}, touching={touching})";
    }

    private float GetTouchX(Landmark leftWrist, bool leftTouch, Landmark rightWrist, bool rightTouch)
    {
        float touchXSum = 0f;
        int touchCount = 0;

        if (leftTouch)
        {
            touchXSum += GetPoseScreenX(leftWrist);
            touchCount++;
        }

        if (rightTouch)
        {
            touchXSum += GetPoseScreenX(rightWrist);
            touchCount++;
        }

        return touchCount > 0 ? Mathf.Clamp01(touchXSum / touchCount) : Mathf.Clamp01(position);
    }

    private float GetPoseScreenX(Landmark landmark)
    {
        if (landmark == null)
            return Mathf.Clamp01(position);

        return Mathf.Clamp01(mirrorPoseX ? 1f - landmark.x : landmark.x);
    }

    private TouchSection GetTouchSection(float normalizedX)
    {
        float clampedX = Mathf.Clamp01(normalizedX);
        if (clampedX < 1f / 3f)
            return TouchSection.Left;

        if (clampedX > 2f / 3f)
            return TouchSection.Right;

        return TouchSection.Center;
    }

    private void UpdateTwoHandManipulation()
    {
        if (!useTwoHandManipulation || currentMode != FlowMode.Detail || selectedIndex < 0)
        {
            ResetTwoHandManipulation();
            return;
        }

        if (!TryGetBothWristControls(out Vector2 leftWrist, out Vector2 rightWrist))
        {
            hasTwoHandBaseline = false;
            return;
        }

        float wristDistance = Vector2.Distance(leftWrist, rightWrist);
        if (wristDistance <= 0.001f)
            return;

        Vector2 wristMidpoint = (leftWrist + rightWrist) * 0.5f;
        float wristAngle = Mathf.Atan2(rightWrist.y - leftWrist.y, rightWrist.x - leftWrist.x) * Mathf.Rad2Deg;

        if (!hasTwoHandBaseline || twoHandManipulationIndex != selectedIndex)
        {
            bool selectedObjectChanged = twoHandManipulationIndex != selectedIndex;
            if (selectedObjectChanged)
            {
                selectedScaleMultiplier = 1f;
                selectedYawOffset = 0f;
                selectedPitchOffset = 0f;
                selectedRollOffset = 0f;
            }

            hasTwoHandBaseline = true;
            twoHandManipulationIndex = selectedIndex;
            baselineWristDistance = wristDistance;
            baselineWristAngle = wristAngle;
            baselineWristMidpoint = wristMidpoint;
            baselineSelectedScaleMultiplier = selectedScaleMultiplier;
            baselineSelectedYawOffset = selectedYawOffset;
            baselineSelectedPitchOffset = selectedPitchOffset;
            baselineSelectedRollOffset = selectedRollOffset;

            if (logTwoHandManipulationDebug)
            {
                Debug.Log($"[ObjectFlowManager] Two-hand manipulation started for FlowCube_{selectedIndex + 1}. wristDistance={wristDistance:F3}, wristMidpoint={wristMidpoint}");
            }

            return;
        }

        float targetScale = Mathf.Clamp(baselineSelectedScaleMultiplier * wristDistance / Mathf.Max(0.001f, baselineWristDistance), minManipulationScale, maxManipulationScale);
        Vector2 midpointOffset = wristMidpoint - baselineWristMidpoint;
        float targetYaw = baselineSelectedYawOffset + midpointOffset.x * manipulationYawRange;
        float targetPitch = baselineSelectedPitchOffset - midpointOffset.y * manipulationPitchRange;
        float targetRoll = baselineSelectedRollOffset + Mathf.DeltaAngle(baselineWristAngle, wristAngle) * manipulationRollMultiplier;
        float follow = 1f - Mathf.Exp(-manipulationSmoothing * Time.deltaTime);

        selectedScaleMultiplier = Mathf.Lerp(selectedScaleMultiplier, targetScale, follow);
        selectedYawOffset = Mathf.Lerp(selectedYawOffset, targetYaw, follow);
        selectedPitchOffset = Mathf.Lerp(selectedPitchOffset, targetPitch, follow);
        selectedRollOffset = Mathf.LerpAngle(selectedRollOffset, targetRoll, follow);
    }

    private bool TryGetBothWristControls(out Vector2 leftWristControl, out Vector2 rightWristControl)
    {
        leftWristControl = Vector2.zero;
        rightWristControl = Vector2.zero;

        StreamClient client = GetPoseClient();
        PoseData poseData = client != null ? client.latestPoseData : null;
        if (poseData == null || poseData.landmarks == null)
            return false;

        if (!TryGetVisibleLandmark(poseData, LeftWristIndex, out Landmark leftWrist))
            return false;

        if (!TryGetVisibleLandmark(poseData, RightWristIndex, out Landmark rightWrist))
            return false;

        leftWristControl = GetManipulationControlPoint(leftWrist);
        rightWristControl = GetManipulationControlPoint(rightWrist);
        return true;
    }

    private Vector2 GetManipulationControlPoint(Landmark landmark)
    {
        float x = mirrorPoseX ? 1f - landmark.x : landmark.x;
        return new Vector2(x, landmark.y);
    }

    private void ResetTwoHandManipulation()
    {
        hasTwoHandBaseline = false;
        twoHandManipulationIndex = -1;
        baselineSelectedScaleMultiplier = 1f;
        baselineSelectedYawOffset = 0f;
        baselineSelectedPitchOffset = 0f;
        baselineSelectedRollOffset = 0f;
        selectedScaleMultiplier = 1f;
        selectedYawOffset = 0f;
        selectedPitchOffset = 0f;
        selectedRollOffset = 0f;
    }

    private void UpdateLayout()
    {
        float clampedDepth = Mathf.Clamp01(depth);
        float focusedIndex = Mathf.Clamp01(position) * (ObjectCount - 1);
        float spacing = Mathf.Lerp(MinSpacing, MaxSpacing, clampedDepth) * gapMultiplier;
        bool useTouchSelectionFlow = navigationMode == NavigationMode.ScreenTouch && currentMode == FlowMode.Browsing && candidateIndex >= 0;
        if (useTouchSelectionFlow)
        {
            float targetFocusedIndex = Mathf.Clamp(candidateIndex, 0, ObjectCount - 1);
            if (!hasTouchLayoutFocusedIndex)
            {
                touchLayoutFocusedIndex = (ObjectCount - 1) * 0.5f;
                hasTouchLayoutFocusedIndex = true;
            }

            float followT = 1f - Mathf.Exp(-Mathf.Max(0.01f, touchSelectionFlowSpeed) * Time.deltaTime);
            touchLayoutFocusedIndex = Mathf.Lerp(touchLayoutFocusedIndex, targetFocusedIndex, followT);
            focusedIndex = touchLayoutFocusedIndex;
            spacing = browseSpacing;
        }
        else
        {
            hasTouchLayoutFocusedIndex = false;
        }

        for (int i = 0; i < ObjectCount; i++)
        {
            GameObject flowObject = flowObjects[i];
            if (flowObject == null)
                continue;

            float offset = i - focusedIndex;
            float absOffset = Mathf.Abs(offset);
            float sideAmount = Mathf.Clamp01(absOffset);
            float x = offset * spacing;
            float z = 0f;
            float xRotation = 0f;
            float yRotation = 0f;
            float zRotation = 0f;
            float scale = Mathf.Lerp(CenterScale, SideScale, sideAmount);
            float y = 0f;

            bool isSelected = IsSelectionActive() && i == selectedIndex;
            bool isCandidate = currentMode == FlowMode.Browsing && selectedIndex < 0 && i == candidateIndex;
            bool isDetailBackground = currentMode == FlowMode.Detail && selectedIndex >= 0 && i != selectedIndex;
            if (UsesStaticLineupLayout())
            {
                float overviewOffset = i - (ObjectCount - 1) * 0.5f;
                x = overviewOffset * overviewSpacing;
                scale = overviewScale;
                sideAmount = 1f;
            }
            else if (useTouchSelectionFlow)
            {
                x = offset * browseSpacing;
                scale = browseScale;
                sideAmount = 1f;
            }
            else if (currentMode == FlowMode.Detail && selectedIndex >= 0)
            {
                int detailOffset = i - selectedIndex;
                x = detailOffset * detailSideSpacing;
                scale = isSelected ? detailScale : detailBackgroundScale;
                yRotation = 0f;
                sideAmount = isSelected ? 0f : 1f;
            }
            else if (currentMode == FlowMode.Browsing && absOffset < 0.5f)
            {
                scale = BrowsingCenterScale;
                Vector2 detailRotation = GetBodyDetailRotation();
                xRotation = detailRotation.y;
                yRotation += detailRotation.x;
            }

            if (isCandidate)
            {
                float pulse = 1f + Mathf.Sin(Time.time * selectionPulseSpeed) * (selectionPulseAmount * 0.45f);
                scale *= candidateScaleBoost * pulse;
                y += candidateLift;
            }

            if (isSelected)
            {
                float pulse = 1f + Mathf.Sin(Time.time * selectionPulseSpeed) * selectionPulseAmount;
                xRotation += selectedPitchOffset;
                yRotation += selectedYawOffset;
                zRotation += selectedRollOffset;
                scale *= selectedScaleMultiplier;
                scale *= currentMode == FlowMode.TouchConfirmed ? touchConfirmedScaleBoost * pulse : pulse;
                y += selectionLift;
            }

            float artifactY = y + GetArtifactYOnStand(i, scale);
            flowObject.transform.localPosition = new Vector3(x, artifactY, z);
            float artifactYawOffset = flowUsesGeneratedMaterial[i] ? 0f : artifactYawOffsetDegrees;
            flowObject.transform.localRotation = Quaternion.Euler(xRotation, yRotation + artifactYawOffset, zRotation);
            Vector3 baseScale = flowBaseScales[i] == Vector3.zero ? Vector3.one : flowBaseScales[i];
            flowObject.transform.localScale = baseScale * scale;
            UpdateArtifactStandLayout(i, x, z);
            UpdateSelectionVisual(i, isSelected, isCandidate, isDetailBackground);
        }
    }

    private void UpdateArtifactStandLayout(int index, float artifactX, float artifactZ)
    {
        GameObject stand = artifactStandObjects[index];
        if (stand == null)
            return;

        if (!showArtifactStands)
        {
            stand.SetActive(false);
            return;
        }

        if (!stand.activeSelf)
        {
            stand.SetActive(true);
        }

        stand.transform.localPosition = new Vector3(artifactX, artifactStandFloorY, artifactZ);
        stand.transform.localRotation = Quaternion.identity;
        stand.transform.localScale = Vector3.one;
    }

    private float GetArtifactYOnStand(int index, float artifactScale)
    {
        if (!showArtifactStands || artifactStandObjects[index] == null)
            return 0f;

        Bounds bounds = hasArtifactLocalBounds[index] ? artifactLocalBounds[index] : GetDefaultArtifactBounds();
        float standTopY = artifactStandFloorY + GetArtifactStandTotalHeight();
        return standTopY - bounds.min.y * artifactScale;
    }

    private float GetArtifactStandTotalHeight()
    {
        return Mathf.Max(0.01f, artifactStandHeight) + Mathf.Max(0.01f, artifactStandTopThickness);
    }

    private bool UsesStaticLineupLayout()
    {
        return currentMode == FlowMode.Overview;
    }

    private Vector2 GetBodyDetailRotation()
    {
        float yaw = (Mathf.Clamp01(position) - 0.5f) * browsingYawRange;
        float browsingSpan = Mathf.Max(0.001f, 1f - browsingLine);
        float browsingProgress = Mathf.Clamp01((Mathf.Clamp01(depth) - browsingLine) / browsingSpan);
        float pitch = (browsingProgress - 0.5f) * browsingPitchRange;
        return new Vector2(yaw, pitch);
    }

    private void UpdateSelectionVisual(int index, bool isSelected, bool isCandidate, bool isDetailBackground)
    {
        Color baseColor = baseColors[index % baseColors.Length];
        Color color = baseColor;
        if (isDetailBackground)
        {
            color = Color.Lerp(baseColor, Color.black, detailBackgroundDim);
        }
        else if (isSelected)
        {
            color = Color.Lerp(baseColor, selectionColor, 0.75f);
        }
        else if (isCandidate)
        {
            color = Color.Lerp(baseColor, candidateColor, 0.55f);
        }

        if (flowUsesGeneratedMaterial[index] && generatedMaterials != null)
        {
            Material material = generatedMaterials[index % generatedMaterials.Length];
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
                else if (isCandidate)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", candidateColor * 0.55f);
                }
                else
                {
                    material.SetColor("_EmissionColor", Color.black);
                }
            }

            return;
        }

        Renderer[] renderers = flowRenderers[index];
        if ((renderers == null || renderers.Length == 0) && flowObjects[index] != null)
        {
            CacheFlowRenderers(index);
            renderers = flowRenderers[index];
        }

        if (renderers == null)
            return;

        if (selectionPropertyBlock == null)
        {
            selectionPropertyBlock = new MaterialPropertyBlock();
        }

        bool hasOverride = isSelected || isCandidate || isDetailBackground;
        Color tint = Color.white;
        Color emission = Color.black;
        if (isDetailBackground)
        {
            tint = Color.Lerp(Color.white, Color.black, detailBackgroundDim);
        }
        else if (isSelected)
        {
            tint = Color.Lerp(Color.white, selectionColor, 0.35f);
            emission = selectionColor * 0.85f;
        }
        else if (isCandidate)
        {
            tint = Color.Lerp(Color.white, candidateColor, 0.28f);
            emission = candidateColor * 0.35f;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (!hasOverride)
            {
                renderer.SetPropertyBlock(null);
                continue;
            }

            selectionPropertyBlock.Clear();
            selectionPropertyBlock.SetColor(BaseColorId, tint);
            selectionPropertyBlock.SetColor(ColorId, tint);
            selectionPropertyBlock.SetColor(EmissionColorId, emission);
            renderer.SetPropertyBlock(selectionPropertyBlock);
        }
    }

    private void EnsureModeLabels()
    {
        if (!showModeLabels)
        {
            SetLabelActive(modeBannerLabel, false);
            SetLabelActive(modeHintLabel, false);
            return;
        }

        if (modeBannerLabel == null)
        {
            modeBannerLabel = CreateModeLabel("ModeBanner", modeBannerLocalPosition, modeBannerCharacterSize);
        }

        if (modeHintLabel == null)
        {
            modeHintLabel = CreateModeLabel("ModeHint", modeHintLocalPosition, modeHintCharacterSize);
        }
    }

    private TextMesh CreateModeLabel(string labelName, Vector3 localPosition, float characterSize)
    {
        GameObject labelObject = new GameObject(labelName);
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = localPosition;
        labelObject.transform.localRotation = Quaternion.identity;

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.MiddleLeft;
        label.alignment = TextAlignment.Left;
        label.fontSize = 64;
        label.characterSize = characterSize;
        label.color = Color.white;
        return label;
    }

    private void UpdateModeLabels()
    {
        if (!showModeLabels || modeBannerLabel == null || modeHintLabel == null)
            return;

        modeBannerLabel.transform.localPosition = modeBannerLocalPosition;
        modeHintLabel.transform.localPosition = modeHintLocalPosition;

        switch (currentMode)
        {
            case FlowMode.Browsing:
                SetLabel(modeBannerLabel, navigationMode == NavigationMode.ScreenTouch ? "Browse: left / center / right" : "Browse: touch to select", candidateColor);
                SetLabel(modeHintLabel, candidateIndex >= 0 ? $"Candidate: Cube {candidateIndex + 1}" : string.Empty, candidateColor);
                break;
            case FlowMode.TouchConfirmed:
                SetLabel(modeBannerLabel, "Touch detected", selectionColor);
                SetLabel(modeHintLabel, selectedIndex >= 0 ? $"Selected: Cube {selectedIndex + 1}" : string.Empty, selectionColor);
                break;
            case FlowMode.Detail:
                SetLabel(modeBannerLabel, selectedIndex >= 0 ? $"Detail: Cube {selectedIndex + 1}" : "Detail", selectionColor);
                SetLabel(modeHintLabel, navigationMode == NavigationMode.ScreenTouch ? "Touch to browse" : "Step back to browse", candidateColor);
                break;
            default:
                SetLabel(modeBannerLabel, "Overview", Color.white);
                SetLabel(modeHintLabel, navigationMode == NavigationMode.ScreenTouch ? "Touch to browse" : "Move closer", Color.white);
                break;
        }

        UpdateModeLabelTransform(modeBannerLabel, modeBannerLocalPosition, modeBannerViewportPosition, modeBannerCharacterSize);
        UpdateModeLabelTransform(modeHintLabel, modeHintLocalPosition, modeHintViewportPosition, modeHintCharacterSize);
    }

    private void SetLabel(TextMesh label, string text, Color color)
    {
        if (label == null)
            return;

        label.gameObject.SetActive(!string.IsNullOrEmpty(text));
        label.text = text;
        label.color = color;
    }

    private void UpdateModeLabelTransform(TextMesh label, Vector3 fallbackLocalPosition, Vector2 viewportPosition, float baseCharacterSize)
    {
        if (label == null || !label.gameObject.activeSelf)
            return;

        if (!anchorModeLabelsToCamera || !TryGetModeLabelCamera(out Camera labelCamera))
        {
            label.transform.localPosition = fallbackLocalPosition;
            label.transform.localRotation = Quaternion.identity;
            label.characterSize = baseCharacterSize;
            return;
        }

        Vector2 clampedViewportPosition = new Vector2(Mathf.Clamp(viewportPosition.x, 0.05f, 0.95f), Mathf.Clamp(viewportPosition.y, 0.05f, 0.95f));
        float labelDistance = Mathf.Max(0.1f, modeLabelCameraDistance);
        label.transform.position = labelCamera.ViewportToWorldPoint(new Vector3(clampedViewportPosition.x, clampedViewportPosition.y, labelDistance));
        label.transform.rotation = labelCamera.transform.rotation;
        FitModeLabelToCamera(label, labelCamera, labelDistance, baseCharacterSize);
    }

    private bool TryGetModeLabelCamera(out Camera labelCamera)
    {
        labelCamera = modeLabelCamera != null ? modeLabelCamera : Camera.main;
        if (labelCamera == null)
        {
            labelCamera = FindFirstObjectByType<Camera>();
        }

        return labelCamera != null;
    }

    private void FitModeLabelToCamera(TextMesh label, Camera labelCamera, float labelDistance, float baseCharacterSize)
    {
        label.characterSize = baseCharacterSize;

        Renderer labelRenderer = label.GetComponent<Renderer>();
        if (labelRenderer == null)
            return;

        float cameraWorldWidth = GetCameraWorldWidth(labelCamera, labelDistance);
        float allowedWorldWidth = cameraWorldWidth * Mathf.Clamp(modeLabelMaxViewportWidth, 0.1f, 0.95f);
        float labelWorldWidth = labelRenderer.bounds.size.x;
        if (allowedWorldWidth <= 0f || labelWorldWidth <= 0f || labelWorldWidth <= allowedWorldWidth)
            return;

        float fittedCharacterSize = baseCharacterSize * allowedWorldWidth / labelWorldWidth;
        label.characterSize = Mathf.Max(modeLabelMinCharacterSize, fittedCharacterSize);
    }

    private float GetCameraWorldWidth(Camera labelCamera, float labelDistance)
    {
        return GetCameraWorldSize(labelCamera, labelDistance).x;
    }

    private Vector2 GetCameraWorldSize(Camera labelCamera, float labelDistance)
    {
        if (labelCamera.orthographic)
        {
            float orthographicHeight = labelCamera.orthographicSize * 2f;
            return new Vector2(orthographicHeight * labelCamera.aspect, orthographicHeight);
        }

        float perspectiveHeight = 2f * labelDistance * Mathf.Tan(labelCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        return new Vector2(perspectiveHeight * labelCamera.aspect, perspectiveHeight);
    }

    private void EnsureDetailModal()
    {
        if (!showDetailModal)
        {
            SetDetailModalActive(false);
            return;
        }

        if (detailModalRoot != null)
            return;

        detailModalRoot = new GameObject("DetailModal");
        detailModalRoot.transform.SetParent(transform, false);

        detailModalPanelMaterial = CreateDetailModalMaterial("DetailModalPanelMaterial", detailModalBackgroundColor);
        detailModalBorderMaterial = CreateDetailModalMaterial("DetailModalBorderMaterial", detailModalBorderColor);
        detailModalPanel = CreateDetailModalQuad("DetailModalPanel", detailModalPanelMaterial);
        detailModalBorders = new[]
        {
            CreateDetailModalQuad("DetailModalBorderTop", detailModalBorderMaterial),
            CreateDetailModalQuad("DetailModalBorderBottom", detailModalBorderMaterial),
            CreateDetailModalQuad("DetailModalBorderLeft", detailModalBorderMaterial),
            CreateDetailModalQuad("DetailModalBorderRight", detailModalBorderMaterial)
        };

        detailModalTitleLabel = CreateDetailModalLabel("DetailModalTitle", detailModalTitleCharacterSize, detailModalTextColor);
        detailModalSubtitleLabel = CreateDetailModalLabel("DetailModalSubtitle", detailModalBodyCharacterSize, detailModalMutedTextColor);
        detailModalBodyLabel = CreateDetailModalLabel("DetailModalBody", detailModalBodyCharacterSize, detailModalTextColor);
        detailModalFooterLabel = CreateDetailModalLabel("DetailModalFooter", detailModalFooterCharacterSize, detailModalAccentColor);
        SetDetailModalActive(false);
    }

    private Material CreateDetailModalMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetInt("_ZWrite", 0);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", 0);
        }

        material.renderQueue = 3000;
        return material;
    }

    private GameObject CreateDetailModalQuad(string objectName, Material material)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = objectName;
        quad.transform.SetParent(detailModalRoot.transform, false);

        Collider quadCollider = quad.GetComponent<Collider>();
        if (quadCollider != null)
        {
            Destroy(quadCollider);
        }

        Renderer quadRenderer = quad.GetComponent<Renderer>();
        if (quadRenderer != null)
        {
            quadRenderer.sharedMaterial = material;
        }

        return quad;
    }

    private TextMesh CreateDetailModalLabel(string labelName, float characterSize, Color color)
    {
        GameObject labelObject = new GameObject(labelName);
        labelObject.transform.SetParent(detailModalRoot.transform, false);

        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.UpperLeft;
        label.alignment = TextAlignment.Left;
        label.fontSize = 64;
        label.characterSize = characterSize;
        label.lineSpacing = 0.92f;
        label.color = color;
        return label;
    }

    private void UpdateDetailModal()
    {
        if (detailModalRoot == null)
            return;

        Camera labelCamera = null;
        bool shouldShow = showDetailModal && currentMode == FlowMode.Detail && selectedIndex >= 0 && TryGetModeLabelCamera(out labelCamera);
        SetDetailModalActive(shouldShow);
        if (!shouldShow)
            return;

        float modalDistance = Mathf.Max(0.1f, detailModalCameraDistance);
        Vector2 clampedPosition = new Vector2(Mathf.Clamp(detailModalViewportPosition.x, 0.05f, 0.95f), Mathf.Clamp(detailModalViewportPosition.y, 0.05f, 0.95f));
        Vector2 clampedSize = new Vector2(Mathf.Clamp(detailModalViewportSize.x, 0.1f, 0.8f), Mathf.Clamp(detailModalViewportSize.y, 0.1f, 0.85f));
        Vector2 cameraWorldSize = GetCameraWorldSize(labelCamera, modalDistance);
        Vector2 modalWorldSize = new Vector2(cameraWorldSize.x * clampedSize.x, cameraWorldSize.y * clampedSize.y);

        detailModalRoot.transform.position = labelCamera.ViewportToWorldPoint(new Vector3(clampedPosition.x, clampedPosition.y, modalDistance));
        detailModalRoot.transform.rotation = labelCamera.transform.rotation;
        UpdateDetailModalPanel(modalWorldSize);
        UpdateDetailModalText(modalWorldSize);
    }

    private void UpdateDetailModalPanel(Vector2 modalWorldSize)
    {
        if (detailModalPanel == null)
            return;

        detailModalPanel.transform.localPosition = Vector3.zero;
        detailModalPanel.transform.localRotation = Quaternion.identity;
        detailModalPanel.transform.localScale = new Vector3(modalWorldSize.x, modalWorldSize.y, 1f);

        if (detailModalBorders == null || detailModalBorders.Length < 4)
            return;

        float borderSize = Mathf.Max(0.01f, Mathf.Min(modalWorldSize.x, modalWorldSize.y) * 0.012f);
        float halfWidth = modalWorldSize.x * 0.5f;
        float halfHeight = modalWorldSize.y * 0.5f;
        SetModalBorder(detailModalBorders[0], new Vector3(0f, halfHeight - borderSize * 0.5f, -0.01f), new Vector3(modalWorldSize.x, borderSize, 1f));
        SetModalBorder(detailModalBorders[1], new Vector3(0f, -halfHeight + borderSize * 0.5f, -0.01f), new Vector3(modalWorldSize.x, borderSize, 1f));
        SetModalBorder(detailModalBorders[2], new Vector3(-halfWidth + borderSize * 0.5f, 0f, -0.01f), new Vector3(borderSize, modalWorldSize.y, 1f));
        SetModalBorder(detailModalBorders[3], new Vector3(halfWidth - borderSize * 0.5f, 0f, -0.01f), new Vector3(borderSize, modalWorldSize.y, 1f));
    }

    private void SetModalBorder(GameObject border, Vector3 localPosition, Vector3 localScale)
    {
        if (border == null)
            return;

        border.transform.localPosition = localPosition;
        border.transform.localRotation = Quaternion.identity;
        border.transform.localScale = localScale;
    }

    private void UpdateDetailModalText(Vector2 modalWorldSize)
    {
        int infoIndex = Mathf.Abs(selectedIndex) % DetailTitles.Length;
        MuseumArtifactInfo artifactInfo = GetSelectedMuseumArtifactInfo();
        float padding = Mathf.Min(modalWorldSize.x, modalWorldSize.y) * 0.08f;
        float left = -modalWorldSize.x * 0.5f + padding;
        float top = modalWorldSize.y * 0.5f - padding;
        float bottom = -modalWorldSize.y * 0.5f + padding;
        float labelZ = -0.03f;
        int titleLineLength = Mathf.Clamp(Mathf.FloorToInt((modalWorldSize.x - padding * 2f) / Mathf.Max(0.001f, detailModalTitleCharacterSize * 0.55f)), 8, 18);
        int descriptionLineLength = Mathf.Clamp(Mathf.FloorToInt((modalWorldSize.x - padding * 2f) / Mathf.Max(0.001f, detailModalBodyCharacterSize * 0.55f)), 18, 38);

        SetLabel(detailModalTitleLabel, WrapText(GetDetailTitle(artifactInfo, infoIndex), titleLineLength), detailModalTextColor);
        SetLabel(detailModalSubtitleLabel, GetDetailSubtitle(artifactInfo, infoIndex), detailModalMutedTextColor);
        SetLabel(detailModalBodyLabel, BuildDetailModalBody(artifactInfo, infoIndex, descriptionLineLength), detailModalTextColor);
        SetLabel(detailModalFooterLabel, "More Info", detailModalAccentColor);

        detailModalTitleLabel.transform.localPosition = new Vector3(left, top, labelZ);
        detailModalSubtitleLabel.transform.localPosition = new Vector3(left, top - modalWorldSize.y * 0.11f, labelZ);
        detailModalBodyLabel.transform.localPosition = new Vector3(left, top - modalWorldSize.y * 0.25f, labelZ);
        detailModalFooterLabel.transform.localPosition = new Vector3(left, bottom, labelZ);
    }

    private string BuildDetailModalBody(MuseumArtifactInfo artifactInfo, int infoIndex, int descriptionLineLength)
    {
        if (artifactInfo != null && HasText(artifactInfo.description))
        {
            return "Description\n"
                + WrapText(LimitText(artifactInfo.description, detailModalMaxDescriptionCharacters), descriptionLineLength);
        }

        return "Material\n"
            + DetailMaterials[infoIndex]
            + "\n\nPeriod\n"
            + DetailPeriods[infoIndex]
            + "\n\nDescription\n"
            + WrapText(DetailDescriptions[infoIndex], descriptionLineLength);
    }

    private MuseumArtifactInfo GetSelectedMuseumArtifactInfo()
    {
        if (selectedIndex < 0 || selectedIndex >= flowArtifactInfos.Length)
            return null;

        return flowArtifactInfos[selectedIndex];
    }

    private string GetDetailTitle(MuseumArtifactInfo artifactInfo, int fallbackIndex)
    {
        if (artifactInfo != null && HasText(artifactInfo.title))
            return artifactInfo.title;

        return DetailTitles[fallbackIndex];
    }

    private string GetDetailSubtitle(MuseumArtifactInfo artifactInfo, int fallbackIndex)
    {
        if (artifactInfo != null && artifactInfo.id > 0)
            return $"Artifact {artifactInfo.id}";

        return DetailOrigins[fallbackIndex];
    }

    private MuseumArtifactInfo FindMuseumArtifactInfo(int artifactId)
    {
        EnsureMuseumArtifactsLoaded();

        if (artifactId <= 0 || museumArtifacts == null)
            return null;

        for (int i = 0; i < museumArtifacts.Length; i++)
        {
            MuseumArtifactInfo artifactInfo = museumArtifacts[i];
            if (artifactInfo != null && artifactInfo.id == artifactId)
                return artifactInfo;
        }

        return null;
    }

    private void EnsureMuseumArtifactsLoaded()
    {
        if (didLoadMuseumArtifacts)
            return;

        didLoadMuseumArtifacts = true;
        museumArtifacts = null;

        if (museumArtifactData == null || string.IsNullOrEmpty(museumArtifactData.text))
            return;

        MuseumArtifactCollection collection;
        try
        {
            collection = JsonUtility.FromJson<MuseumArtifactCollection>(museumArtifactData.text);
        }
        catch (System.ArgumentException exception)
        {
            Debug.LogWarning($"[ObjectFlowManager] Could not parse museum artifact JSON: {exception.Message}");
            return;
        }

        if (collection == null || collection.artifacts == null || collection.artifacts.Length == 0)
        {
            Debug.LogWarning("[ObjectFlowManager] Museum artifact JSON did not contain any artifacts.");
            return;
        }

        museumArtifacts = collection.artifacts;
    }

    private static bool HasText(string text)
    {
        return !string.IsNullOrEmpty(text);
    }

    private static string LimitText(string text, int maxCharacters)
    {
        if (string.IsNullOrEmpty(text) || maxCharacters <= 0 || text.Length <= maxCharacters)
            return text;

        return text.Substring(0, maxCharacters).TrimEnd() + "...";
    }

    [System.Serializable]
    private class MuseumArtifactCollection
    {
        public MuseumArtifactInfo[] artifacts;
    }

    [System.Serializable]
    private class MuseumArtifactInfo
    {
        public int id;
        public string title;
        public string description;
    }

    private struct SelectedArtifactPrefab
    {
        public GameObject prefab;
        public int artifactId;
    }

    private string WrapText(string text, int maxLineLength)
    {
        if (string.IsNullOrEmpty(text) || maxLineLength <= 0)
            return text;

        string[] words = text.Split(' ');
        string wrappedText = string.Empty;
        string currentLine = string.Empty;
        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            if (currentLine.Length > 0 && currentLine.Length + word.Length + 1 > maxLineLength)
            {
                wrappedText += currentLine + "\n";
                currentLine = word;
            }
            else
            {
                currentLine = currentLine.Length == 0 ? word : currentLine + " " + word;
            }
        }

        return wrappedText + currentLine;
    }

    private void SetDetailModalActive(bool isActive)
    {
        if (detailModalRoot != null && detailModalRoot.activeSelf != isActive)
        {
            detailModalRoot.SetActive(isActive);
        }
    }

    private void SetLabelActive(TextMesh label, bool isActive)
    {
        if (label != null)
        {
            label.gameObject.SetActive(isActive);
        }
    }

    private void UpdateDebugLines()
    {
        if (!showDebugLines)
            return;

        if (navigationMode == NavigationMode.MoveDistance)
        {
            DrawDebugLine(browsingLine, browsingLineColor);
        }
        else
        {
            DrawDebugSectionLine(1f / 3f, touchLineColor);
            DrawDebugSectionLine(2f / 3f, touchLineColor);
        }

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

    private void DrawDebugSectionLine(float normalizedX, Color color)
    {
        float halfLength = debugLineLength * 0.5f;
        float localX = Mathf.Lerp(-halfLength, halfLength, Mathf.Clamp01(normalizedX));
        Vector3 start = transform.TransformPoint(new Vector3(localX, 2f, debugLineZ));
        Vector3 end = transform.TransformPoint(new Vector3(localX, -2f, debugLineZ));
        Debug.DrawLine(start, end, color);
    }

    private float NormalizedFloorToLocalY(float normalizedY)
    {
        return Mathf.Lerp(2f, -2f, Mathf.Clamp01(normalizedY));
    }
}
