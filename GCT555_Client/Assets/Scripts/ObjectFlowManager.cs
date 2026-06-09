using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    private const int LandmarkFilterCapacity = 33;
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

    [Header("Pose Filtering")]
    public bool usePoseCenterSmoothing = true;
    public float poseCenterFilterSpeed = 14f;
    public bool useWristSmoothing = true;
    public float wristFilterSpeed = 18f;
    [Range(0f, 0.1f)]
    public float touchLineHysteresis = 0.02f;

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
    public bool hideModeLabelsInDetail = true;

    [Header("Detail Modal")]
    public bool showDetailModal = true;
    public Vector2 detailModalViewportPosition = new Vector2(0.79f, 0.52f);
    public Vector2 detailModalViewportSize = new Vector2(0.3f, 0.72f);
    public float detailModalCameraDistance = 6.6f;
    public Color detailModalBackgroundColor = new Color(0.015f, 0.04f, 0.075f, 0.86f);
    public Color detailModalBorderColor = new Color(0.64f, 0.82f, 1f, 0.22f);
    public Color detailModalTextColor = new Color(0.86f, 0.9f, 0.95f, 1f);
    public Color detailModalMutedTextColor = new Color(0.68f, 0.76f, 0.84f, 1f);
    public Color detailModalAccentColor = new Color(0.74f, 0.86f, 1f, 1f);
    public float detailModalTitleCharacterSize = 0.052f;
    public float detailModalBodyCharacterSize = 0.04f;
    public float detailModalFooterCharacterSize = 0.044f;
    public float detailModalBodyLineSpacing = 1.12f;
    [Range(0.04f, 0.2f)]
    public float detailModalPaddingRatio = 0.11f;
    public int detailModalMaxDescriptionCharacters = 220;
    public bool detailModalMinimalMuseumLayout = true;

    [Header("User Event Logging")]
    public bool logUserEventsToFile = true;
    public string userEventLogDirectoryName = "UserEventLogs";
    public string userEventLogFileName = "user_events.tsv";
    public float userEventManipulationLogInterval = 0.25f;

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
    private RectTransform detailModalRect;
    private RectTransform detailModalAccentRect;
    private Image detailModalPanelImage;
    private Image detailModalAccentImage;
    private Text detailModalTitleLabel;
    private Text detailModalSubtitleLabel;
    private Text detailModalBodyLabel;
    private Text detailModalFooterLabel;
    private Font detailModalFont;
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
    private bool hasFilteredPoseCenter;
    private Vector2 filteredPoseCenter;
    private readonly bool[] hasFilteredLandmarkScreenPoints = new bool[LandmarkFilterCapacity];
    private readonly Vector2[] filteredLandmarkScreenPoints = new Vector2[LandmarkFilterCapacity];
    private readonly int[] filteredLandmarkScreenPointFrames = new int[LandmarkFilterCapacity];
    private string userEventLogPath;
    private bool didInitializeUserEventLog;
    private float nextManipulationUserEventLogTime;
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
        LogUserEvent("session_start", $"navigation={navigationMode}; log_path={GetUserEventLogPath()}");
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
        LogUserEvent("session_end", $"navigation={navigationMode}");

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
    }

    private void LogUserEvent(string eventName, string details = "")
    {
        if (!logUserEventsToFile)
            return;

        try
        {
            string logPath = GetUserEventLogPath();
            string line = string.Join("\t", new[]
            {
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                DateTime.Now.ToString("O", CultureInfo.InvariantCulture),
                SanitizeLogValue(SceneManager.GetActiveScene().name),
                SanitizeLogValue(eventName),
                SanitizeLogValue(currentMode.ToString()),
                FormatLogIndex(candidateIndex),
                FormatLogIndex(selectedIndex),
                position.ToString("F4", CultureInfo.InvariantCulture),
                depth.ToString("F4", CultureInfo.InvariantCulture),
                SanitizeLogValue(details)
            });

            File.AppendAllText(logPath, line + Environment.NewLine);
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[ObjectFlowManager] Could not write user event log: {exception.Message}");
        }
    }

    private string GetUserEventLogPath()
    {
        if (didInitializeUserEventLog && !string.IsNullOrEmpty(userEventLogPath))
            return userEventLogPath;

        string safeDirectoryName = string.IsNullOrEmpty(userEventLogDirectoryName) ? "UserEventLogs" : userEventLogDirectoryName;
        string safeFileName = BuildTimestampedUserEventLogFileName(userEventLogFileName, DateTime.Now);
        string logDirectory = Path.Combine(Application.persistentDataPath, safeDirectoryName);
        Directory.CreateDirectory(logDirectory);
        userEventLogPath = Path.Combine(logDirectory, safeFileName);

        if (!File.Exists(userEventLogPath) || new FileInfo(userEventLogPath).Length == 0)
        {
            File.AppendAllText(userEventLogPath, "timestamp_utc\ttimestamp_local\tscene\tevent\tmode\tcandidate_index\tselected_index\tposition\tdepth\tdetails" + Environment.NewLine);
        }

        didInitializeUserEventLog = true;
        Debug.Log($"[ObjectFlowManager] User event log file: {userEventLogPath}");
        return userEventLogPath;
    }

    private static string BuildTimestampedUserEventLogFileName(string baseFileName, DateTime startTime)
    {
        string safeBaseFileName = string.IsNullOrEmpty(baseFileName) ? "user_events.tsv" : baseFileName;
        string directory = Path.GetDirectoryName(safeBaseFileName);
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(safeBaseFileName);
        string extension = Path.GetExtension(safeBaseFileName);

        if (string.IsNullOrEmpty(fileNameWithoutExtension))
            fileNameWithoutExtension = "user_events";

        if (string.IsNullOrEmpty(extension))
            extension = ".tsv";

        string timestamp = startTime.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        string timestampedFileName = $"{fileNameWithoutExtension}_{timestamp}{extension}";
        return string.IsNullOrEmpty(directory) ? timestampedFileName : Path.Combine(directory, timestampedFileName);
    }

    private static string SanitizeLogValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
    }

    private static string FormatLogIndex(int index)
    {
        return index >= 0 ? (index + 1).ToString(CultureInfo.InvariantCulture) : "";
    }

    private void SetFlowMode(FlowMode nextMode, string reason, string details = "")
    {
        if (currentMode == nextMode)
            return;

        FlowMode previousMode = currentMode;
        currentMode = nextMode;
        LogUserEvent("mode_changed", $"from={previousMode}; to={nextMode}; reason={reason}; {details}");
    }

    private void LogCandidateChanged(int previousIndex, int nextIndex, string reason, string details = "")
    {
        if (previousIndex == nextIndex)
            return;

        LogUserEvent("candidate_changed", $"from={FormatLogIndex(previousIndex)}; to={FormatLogIndex(nextIndex)}; reason={reason}; {GetArtifactLogDetails(nextIndex)}; {details}");
    }

    private void LogSelectionEvent(string eventName, int index, string details = "")
    {
        LogUserEvent(eventName, $"{GetArtifactLogDetails(index)}; {details}");
    }

    private string GetArtifactLogDetails(int index)
    {
        if (index < 0 || index >= flowArtifactInfos.Length)
            return "artifact_index=";

        MuseumArtifactInfo artifactInfo = flowArtifactInfos[index];
        if (artifactInfo == null)
            return $"artifact_index={index + 1}";

        return $"artifact_index={index + 1}; artifact_id={artifactInfo.id}; title={artifactInfo.title}";
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

        if (TryPickPlannedFinalArtifactPrefabs(out SelectedArtifactPrefab[] plannedPrefabs))
            return plannedPrefabs;

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
            int selectedIndexInPool = UnityEngine.Random.Range(i, pool.Length);
            selectedPrefabs[i] = pool[selectedIndexInPool];
            pool[selectedIndexInPool] = pool[i];
            pool[i] = selectedPrefabs[i];
        }

        return selectedPrefabs;
    }

    private bool TryPickPlannedFinalArtifactPrefabs(out SelectedArtifactPrefab[] selectedPrefabs)
    {
        selectedPrefabs = null;
        if (!ArtifactSelectionPlan.TryLoadArtifactIds(out int[] artifactIds))
            return false;

        if (finalArtifactPrefabs == null || artifactIds.Length < ObjectCount)
            return false;

        SelectedArtifactPrefab[] plannedPrefabs = new SelectedArtifactPrefab[ObjectCount];
        for (int i = 0; i < ObjectCount; i++)
        {
            int artifactId = artifactIds[i];
            int prefabIndex = artifactId - 1;
            if (prefabIndex < 0 || prefabIndex >= finalArtifactPrefabs.Length || finalArtifactPrefabs[prefabIndex] == null)
                return false;

            plannedPrefabs[i] = new SelectedArtifactPrefab
            {
                prefab = finalArtifactPrefabs[prefabIndex],
                artifactId = artifactId
            };
        }

        selectedPrefabs = plannedPrefabs;
        return true;
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

        poseCenter = FilterPoseCenter(poseCenter);
        float targetPosition = mirrorPoseX ? 1f - poseCenter.x : poseCenter.x;
        float targetDepth = topIsDepthOne ? 1f - poseCenter.y : poseCenter.y;
        float followT = 1f - Mathf.Exp(-poseFollowSpeed * Time.deltaTime);

        position = Mathf.Lerp(position, Mathf.Clamp01(targetPosition), followT);
        depth = Mathf.Lerp(depth, Mathf.Clamp01(targetDepth), followT);
    }

    private void SetPoseTracking(bool isTracking)
    {
        bool previousTracking = hasPoseTracking;
        if (!hasPoseTracking && isTracking)
        {
            poseTrackingJustRestored = true;
        }

        hasPoseTracking = isTracking;
        if (previousTracking && !isTracking)
        {
            ResetPoseFilters();
        }

        if (previousTracking != isTracking)
        {
            LogUserEvent(isTracking ? "pose_tracking_restored" : "pose_tracking_lost", $"position={position:F4}; depth={depth:F4}");
        }
    }

    private Vector2 FilterPoseCenter(Vector2 rawPoseCenter)
    {
        Vector2 clampedPoseCenter = Clamp01(rawPoseCenter);
        if (!usePoseCenterSmoothing || poseCenterFilterSpeed <= 0f)
        {
            filteredPoseCenter = clampedPoseCenter;
            hasFilteredPoseCenter = true;
            return clampedPoseCenter;
        }

        if (!hasFilteredPoseCenter)
        {
            filteredPoseCenter = clampedPoseCenter;
            hasFilteredPoseCenter = true;
            return clampedPoseCenter;
        }

        float follow = GetFilterFollow(poseCenterFilterSpeed);
        filteredPoseCenter = Vector2.Lerp(filteredPoseCenter, clampedPoseCenter, follow);
        return filteredPoseCenter;
    }

    private Vector2 FilterLandmarkScreenPoint(int landmarkIndex, Vector2 rawScreenPoint)
    {
        Vector2 clampedScreenPoint = Clamp01(rawScreenPoint);
        if (!useWristSmoothing || wristFilterSpeed <= 0f || landmarkIndex < 0 || landmarkIndex >= LandmarkFilterCapacity)
            return clampedScreenPoint;

        if (hasFilteredLandmarkScreenPoints[landmarkIndex] && filteredLandmarkScreenPointFrames[landmarkIndex] == Time.frameCount)
            return filteredLandmarkScreenPoints[landmarkIndex];

        if (!hasFilteredLandmarkScreenPoints[landmarkIndex])
        {
            filteredLandmarkScreenPoints[landmarkIndex] = clampedScreenPoint;
            hasFilteredLandmarkScreenPoints[landmarkIndex] = true;
            filteredLandmarkScreenPointFrames[landmarkIndex] = Time.frameCount;
            return clampedScreenPoint;
        }

        float follow = GetFilterFollow(wristFilterSpeed);
        filteredLandmarkScreenPoints[landmarkIndex] = Vector2.Lerp(filteredLandmarkScreenPoints[landmarkIndex], clampedScreenPoint, follow);
        filteredLandmarkScreenPointFrames[landmarkIndex] = Time.frameCount;
        return filteredLandmarkScreenPoints[landmarkIndex];
    }

    private static float GetFilterFollow(float filterSpeed)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0.01f, filterSpeed) * Time.deltaTime);
    }

    private static Vector2 Clamp01(Vector2 value)
    {
        return new Vector2(Mathf.Clamp01(value.x), Mathf.Clamp01(value.y));
    }

    private void ResetPoseFilters()
    {
        hasFilteredPoseCenter = false;
        for (int i = 0; i < hasFilteredLandmarkScreenPoints.Length; i++)
        {
            ResetLandmarkFilter(i);
        }
    }

    private void ResetLandmarkFilter(int landmarkIndex)
    {
        if (landmarkIndex < 0 || landmarkIndex >= LandmarkFilterCapacity)
            return;

        hasFilteredLandmarkScreenPoints[landmarkIndex] = false;
        filteredLandmarkScreenPointFrames[landmarkIndex] = -1;
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
            SetFlowMode(FlowMode.Overview, "browsing_disabled");
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

        SetFlowMode(FlowMode.Overview, "pose_lost");
        ClearModeSelection();
        ResetTwoHandManipulation();
        wasWristTouching = false;
    }

    private void EnterBrowseAfterPoseRestore()
    {
        SetFlowMode(FlowMode.Browsing, "pose_restored");
        selectedIndex = -1;
        int previousCandidate = candidateIndex;
        candidateIndex = GetObjectIndexAtPosition();
        LogCandidateChanged(previousCandidate, candidateIndex, "pose_restored");
        ResetTwoHandManipulation();
        wasWristTouching = IsScreenTouched();
        Debug.Log($"[ObjectFlowManager] Pose tracking restored. Switching to Browsing mode. navigation={navigationMode}, candidate=FlowCube_{candidateIndex + 1}");
    }

    private void UpdateMoveNavigationState()
    {
        int previousCandidate = candidateIndex;
        candidateIndex = GetObjectIndexAtPosition();
        LogCandidateChanged(previousCandidate, candidateIndex, "move_position");

        bool wasTouchingBefore = wasWristTouching;
        bool isTouching = IsScreenTouched();
        bool touchStarted = isTouching && !wasTouchingBefore;

        if (currentMode == FlowMode.TouchConfirmed)
        {
            if (Time.time >= touchConfirmationEndTime)
            {
                SetFlowMode(FlowMode.Detail, "touch_confirmation_complete", GetArtifactLogDetails(selectedIndex));
                detailEntryDepth = depth;
                LogSelectionEvent("detail_entered", selectedIndex, "navigation=move");
                Debug.Log($"[ObjectFlowManager] Detail mode entered for FlowCube_{selectedIndex + 1}.");
            }

            return;
        }

        if (currentMode == FlowMode.Detail)
        {
            if (touchStarted)
            {
                Debug.Log($"[ObjectFlowManager] Returned to Browsing mode from Detail by touch. position={position:F3}, depth={depth:F3}");
                SetFlowMode(FlowMode.Browsing, "detail_touch_exit", $"navigation=move; {GetArtifactLogDetails(selectedIndex)}");
                LogSelectionEvent("detail_exited", selectedIndex, "navigation=move; reason=touch");
                selectedIndex = -1;
                ResetTwoHandManipulation();
            }

            return;
        }

        SetFlowMode(FlowMode.Browsing, "move_navigation_active");
        selectedIndex = -1;

        if (touchStarted)
        {
            selectedIndex = candidateIndex;
            LogSelectionEvent("selection_confirmed", selectedIndex, "navigation=move");
            SetFlowMode(FlowMode.TouchConfirmed, "move_touch_confirmed", GetArtifactLogDetails(selectedIndex));
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

        if (isTouching != wasTouchingBefore)
        {
            LogUserEvent(isTouching ? "touch_started" : "touch_ended", isTouching ? touchReason : "navigation=touch");
        }

        wasWristTouching = isTouching;
        bool touchStarted = isTouching && !wasTouchingBefore;

        if (currentMode == FlowMode.TouchConfirmed)
        {
            if (Time.time >= touchConfirmationEndTime)
            {
                SetFlowMode(FlowMode.Detail, "touch_confirmation_complete", GetArtifactLogDetails(selectedIndex));
                detailEntryDepth = depth;
                LogSelectionEvent("detail_entered", selectedIndex, "navigation=touch");
                Debug.Log($"[ObjectFlowManager] Detail mode entered for FlowCube_{selectedIndex + 1}.");
            }

            return;
        }

        if (!touchStarted)
            return;

        switch (currentMode)
        {
            case FlowMode.Overview:
                SetFlowMode(FlowMode.Browsing, "touch_navigation_overview_touch", $"section={touchSection}; touchX={touchX:F3}");
                int previousCandidate = candidateIndex;
                candidateIndex = GetObjectIndexFromTouchX(touchX);
                LogCandidateChanged(previousCandidate, candidateIndex, "touch_navigation_enter_browse", $"section={touchSection}; touchX={touchX:F3}");
                selectedIndex = -1;
                ResetTwoHandManipulation();
                Debug.Log($"[ObjectFlowManager] Touch navigation entered Browsing mode. section={touchSection}, candidate=FlowCube_{candidateIndex + 1}, touchX={touchX:F3}");
                break;
            case FlowMode.Browsing:
                HandleTouchNavigationBrowseTouch(touchSection, touchX);
                break;
            case FlowMode.Detail:
                int returnCandidate = selectedIndex >= 0 ? selectedIndex : candidateIndex;
                SetFlowMode(FlowMode.Browsing, "touch_navigation_detail_exit", $"section={touchSection}; touchX={touchX:F3}; {GetArtifactLogDetails(selectedIndex)}");
                LogSelectionEvent("detail_exited", selectedIndex, $"navigation=touch; section={touchSection}; touchX={touchX:F3}");
                selectedIndex = -1;
                int previousReturnCandidate = candidateIndex;
                candidateIndex = Mathf.Clamp(returnCandidate, 0, ObjectCount - 1);
                LogCandidateChanged(previousReturnCandidate, candidateIndex, "touch_navigation_detail_exit");
                ResetTwoHandManipulation();
                Debug.Log($"[ObjectFlowManager] Touch navigation returned to Browsing mode from Detail. candidate=FlowCube_{candidateIndex + 1}, section={touchSection}, touchX={touchX:F3}");
                break;
            default:
                SetFlowMode(FlowMode.Overview, "touch_navigation_unexpected_state");
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
            int previousCandidate = candidateIndex;
            candidateIndex = GetObjectIndexAtPosition();
            LogCandidateChanged(previousCandidate, candidateIndex, "touch_navigation_candidate_init");
        }

        int unclampedCandidate = candidateIndex;
        candidateIndex = Mathf.Clamp(candidateIndex, 0, ObjectCount - 1);
        LogCandidateChanged(unclampedCandidate, candidateIndex, "touch_navigation_candidate_clamp");
    }

    private void HandleTouchNavigationBrowseTouch(TouchSection touchSection, float touchX)
    {
        switch (touchSection)
        {
            case TouchSection.Left:
                int previousLeftCandidate = candidateIndex;
                candidateIndex = Mathf.Max(0, candidateIndex - 1);
                LogCandidateChanged(previousLeftCandidate, candidateIndex, "touch_left", $"touchX={touchX:F3}");
                Debug.Log($"[ObjectFlowManager] Touch navigation moved selection left to FlowCube_{candidateIndex + 1}. touchX={touchX:F3}");
                break;
            case TouchSection.Right:
                int previousRightCandidate = candidateIndex;
                candidateIndex = Mathf.Min(ObjectCount - 1, candidateIndex + 1);
                LogCandidateChanged(previousRightCandidate, candidateIndex, "touch_right", $"touchX={touchX:F3}");
                Debug.Log($"[ObjectFlowManager] Touch navigation moved selection right to FlowCube_{candidateIndex + 1}. touchX={touchX:F3}");
                break;
            default:
                selectedIndex = candidateIndex;
                LogSelectionEvent("selection_confirmed", selectedIndex, $"navigation=touch; section={touchSection}; touchX={touchX:F3}");
                SetFlowMode(FlowMode.Detail, "touch_center_detail", $"touchX={touchX:F3}; {GetArtifactLogDetails(selectedIndex)}");
                detailEntryDepth = depth;
                ResetTwoHandManipulation();
                LogSelectionEvent("detail_entered", selectedIndex, $"navigation=touch; section={touchSection}; touchX={touchX:F3}");
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

        if (isTouching != wasWristTouching)
        {
            LogUserEvent(isTouching ? "touch_started" : "touch_ended", isTouching ? reason : "navigation=move");
        }

        wasWristTouching = isTouching;
        return isTouching;
    }

    private bool TryGetScreenTouch(out TouchSection touchSection, out float touchX, out string reason)
    {
        touchX = Mathf.Clamp01(position);
        touchSection = GetTouchSection(touchX);
        reason = "";
        float touchThreshold = GetTouchThreshold();

        if (!useWristTouchDetection)
        {
            bool isTouching = depth >= touchThreshold;
            reason = $"depth={depth:F3}, touchX={touchX:F3}, section={touchSection}, touchLine={screenTouchLine:F3}, threshold={touchThreshold:F3}";
            return isTouching;
        }

        StreamClient client = GetPoseClient();
        PoseData poseData = client != null ? client.latestPoseData : null;
        if (poseData == null || poseData.landmarks == null)
            return false;

        bool hasLeftWrist = TryGetFilteredLandmarkScreenPoint(poseData, LeftWristIndex, out Vector2 leftWristPoint, out Landmark leftWrist);
        bool hasRightWrist = TryGetFilteredLandmarkScreenPoint(poseData, RightWristIndex, out Vector2 rightWristPoint, out Landmark rightWrist);
        bool leftTouch = hasLeftWrist && leftWristPoint.y >= touchThreshold;
        bool rightTouch = hasRightWrist && rightWristPoint.y >= touchThreshold;

        if (leftTouch || rightTouch)
        {
            touchX = GetTouchX(leftWristPoint, leftTouch, rightWristPoint, rightTouch);
            touchSection = GetTouchSection(touchX);
            reason = $"{FormatWrist("left", leftWrist, leftWristPoint, leftTouch)} {FormatWrist("right", rightWrist, rightWristPoint, rightTouch)} touchX={touchX:F3}, section={touchSection}, touchLine={screenTouchLine:F3}, threshold={touchThreshold:F3}";
            return true;
        }

        if (logWristTouchDebug && Time.unscaledTime >= nextWristSampleLogTime)
        {
            Debug.Log($"[ObjectFlowManager] Wrist sample {FormatWrist("left", leftWrist, leftWristPoint, false)} {FormatWrist("right", rightWrist, rightWristPoint, false)} touchLine={screenTouchLine:F3}, threshold={touchThreshold:F3}");
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

    private bool TryGetFilteredLandmarkScreenPoint(PoseData poseData, int index, out Vector2 screenPoint, out Landmark landmark)
    {
        screenPoint = Vector2.zero;
        if (!TryGetVisibleLandmark(poseData, index, out landmark))
        {
            ResetLandmarkFilter(index);
            return false;
        }

        screenPoint = FilterLandmarkScreenPoint(index, GetRawPoseScreenPoint(landmark));
        return true;
    }

    private float GetTouchThreshold()
    {
        float hysteresis = Mathf.Clamp(touchLineHysteresis, 0f, 0.1f);
        return wasWristTouching ? screenTouchLine - hysteresis : screenTouchLine + hysteresis;
    }

    private string FormatWrist(string label, Landmark wrist, Vector2 screenPoint, bool touching)
    {
        if (wrist == null)
            return $"{label}=missing";

        return $"{label}(x={screenPoint.x:F3}, y={screenPoint.y:F3}, rawX={GetRawPoseScreenX(wrist):F3}, rawY={wrist.y:F3}, visibility={wrist.visibility:F3}, touching={touching})";
    }

    private float GetTouchX(Vector2 leftWristPoint, bool leftTouch, Vector2 rightWristPoint, bool rightTouch)
    {
        float touchXSum = 0f;
        int touchCount = 0;

        if (leftTouch)
        {
            touchXSum += leftWristPoint.x;
            touchCount++;
        }

        if (rightTouch)
        {
            touchXSum += rightWristPoint.x;
            touchCount++;
        }

        return touchCount > 0 ? Mathf.Clamp01(touchXSum / touchCount) : Mathf.Clamp01(position);
    }

    private Vector2 GetRawPoseScreenPoint(Landmark landmark)
    {
        if (landmark == null)
            return new Vector2(Mathf.Clamp01(position), Mathf.Clamp01(depth));

        return new Vector2(GetRawPoseScreenX(landmark), Mathf.Clamp01(landmark.y));
    }

    private float GetRawPoseScreenX(Landmark landmark)
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
            if (hasTwoHandBaseline)
            {
                LogUserEvent("two_hand_manipulation_lost", GetArtifactLogDetails(twoHandManipulationIndex));
            }

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
            nextManipulationUserEventLogTime = Time.unscaledTime + Mathf.Max(0.05f, userEventManipulationLogInterval);
            LogUserEvent("two_hand_manipulation_started", $"{GetArtifactLogDetails(selectedIndex)}; wristDistance={wristDistance:F4}; wristMidpoint={wristMidpoint}");

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

        if (Time.unscaledTime >= nextManipulationUserEventLogTime)
        {
            LogUserEvent(
                "two_hand_manipulation_updated",
                $"{GetArtifactLogDetails(selectedIndex)}; scale={selectedScaleMultiplier:F4}; yaw={selectedYawOffset:F2}; pitch={selectedPitchOffset:F2}; roll={selectedRollOffset:F2}; wristDistance={wristDistance:F4}");
            nextManipulationUserEventLogTime = Time.unscaledTime + Mathf.Max(0.05f, userEventManipulationLogInterval);
        }
    }

    private bool TryGetBothWristControls(out Vector2 leftWristControl, out Vector2 rightWristControl)
    {
        leftWristControl = Vector2.zero;
        rightWristControl = Vector2.zero;

        StreamClient client = GetPoseClient();
        PoseData poseData = client != null ? client.latestPoseData : null;
        if (poseData == null || poseData.landmarks == null)
            return false;

        if (!TryGetFilteredLandmarkScreenPoint(poseData, LeftWristIndex, out Vector2 leftWristPoint, out _))
            return false;

        if (!TryGetFilteredLandmarkScreenPoint(poseData, RightWristIndex, out Vector2 rightWristPoint, out _))
            return false;

        leftWristControl = leftWristPoint;
        rightWristControl = rightWristPoint;
        return true;
    }

    private void ResetTwoHandManipulation()
    {
        if (hasTwoHandBaseline)
        {
            LogUserEvent("two_hand_manipulation_ended", GetArtifactLogDetails(twoHandManipulationIndex));
        }

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
                if (hideModeLabelsInDetail)
                {
                    SetLabelActive(modeBannerLabel, false);
                    SetLabelActive(modeHintLabel, false);
                    return;
                }

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

        detailModalRoot = new GameObject("DetailModalCanvas");
        detailModalRoot.transform.SetParent(transform, false);

        Canvas canvas = detailModalRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = detailModalRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1024f, 768f);
        scaler.matchWidthOrHeight = 0.5f;

        detailModalRoot.AddComponent<GraphicRaycaster>();

        detailModalRect = CreateUiRect("DetailModalPanel", detailModalRoot.transform);
        detailModalPanelImage = detailModalRect.gameObject.AddComponent<Image>();
        detailModalPanelImage.raycastTarget = false;

        detailModalAccentRect = CreateUiRect("DetailModalAccent", detailModalRect);
        detailModalAccentImage = detailModalAccentRect.gameObject.AddComponent<Image>();
        detailModalAccentImage.raycastTarget = false;

        detailModalTitleLabel = CreateDetailModalUiLabel("DetailModalTitle", detailModalRect, detailModalTitleCharacterSize, FontStyle.Bold, detailModalTextColor);
        detailModalSubtitleLabel = CreateDetailModalUiLabel("DetailModalSubtitle", detailModalRect, detailModalBodyCharacterSize, FontStyle.Normal, detailModalMutedTextColor);
        detailModalBodyLabel = CreateDetailModalUiLabel("DetailModalBody", detailModalRect, detailModalBodyCharacterSize, FontStyle.Normal, detailModalTextColor);
        detailModalFooterLabel = CreateDetailModalUiLabel("DetailModalFooter", detailModalRect, detailModalFooterCharacterSize, FontStyle.Normal, detailModalAccentColor);
        SetDetailModalActive(false);
    }

    private RectTransform CreateUiRect(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private Text CreateDetailModalUiLabel(string labelName, RectTransform parent, float characterSize, FontStyle fontStyle, Color color)
    {
        RectTransform labelRect = CreateUiRect(labelName, parent);
        Text label = labelRect.gameObject.AddComponent<Text>();
        int fontSize = GetDetailModalFontSize(characterSize);
        label.font = GetDetailModalFont();
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = TextAnchor.UpperLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = Mathf.Max(11, Mathf.RoundToInt(fontSize * 0.62f));
        label.resizeTextMaxSize = fontSize;
        label.supportRichText = false;
        label.raycastTarget = false;
        label.color = color;
        return label;
    }

    private Font GetDetailModalFont()
    {
        if (detailModalFont != null)
            return detailModalFont;

        detailModalFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 64);
        if (detailModalFont == null)
        {
            detailModalFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return detailModalFont;
    }

    private static int GetDetailModalFontSize(float characterSize)
    {
        return Mathf.Clamp(Mathf.RoundToInt(characterSize * 520f), 12, 40);
    }

    private void UpdateDetailModal()
    {
        if (detailModalRoot == null)
            return;

        bool shouldShow = showDetailModal && currentMode == FlowMode.Detail && selectedIndex >= 0;
        SetDetailModalActive(shouldShow);
        if (!shouldShow)
            return;

        UpdateDetailModalPanel();
        UpdateDetailModalText();
    }

    private void UpdateDetailModalPanel()
    {
        if (detailModalRect == null)
            return;

        Vector2 clampedSize = new Vector2(Mathf.Clamp(detailModalViewportSize.x, 0.1f, 0.8f), Mathf.Clamp(detailModalViewportSize.y, 0.1f, 0.85f));
        Vector2 halfSize = clampedSize * 0.5f;
        Vector2 clampedPosition = new Vector2(
            Mathf.Clamp(detailModalViewportPosition.x, halfSize.x, 1f - halfSize.x),
            Mathf.Clamp(detailModalViewportPosition.y, halfSize.y, 1f - halfSize.y));

        detailModalRect.anchorMin = clampedPosition - halfSize;
        detailModalRect.anchorMax = clampedPosition + halfSize;
        detailModalRect.offsetMin = Vector2.zero;
        detailModalRect.offsetMax = Vector2.zero;
        detailModalRect.localScale = Vector3.one;

        if (detailModalPanelImage != null)
        {
            detailModalPanelImage.color = detailModalBackgroundColor;
        }

        if (detailModalAccentRect != null)
        {
            detailModalAccentRect.anchorMin = new Vector2(0f, 0f);
            detailModalAccentRect.anchorMax = new Vector2(0f, 1f);
            detailModalAccentRect.pivot = new Vector2(0f, 0.5f);
            detailModalAccentRect.anchoredPosition = Vector2.zero;
            detailModalAccentRect.sizeDelta = new Vector2(5f, 0f);
        }

        if (detailModalAccentImage != null)
        {
            detailModalAccentImage.color = detailModalBorderColor;
        }

        UpdateDetailModalTextRects();
    }

    private void UpdateDetailModalTextRects()
    {
        if (detailModalRect == null)
            return;

        float panelWidth = Mathf.Max(1f, Screen.width * (detailModalRect.anchorMax.x - detailModalRect.anchorMin.x));
        float panelHeight = Mathf.Max(1f, Screen.height * (detailModalRect.anchorMax.y - detailModalRect.anchorMin.y));
        float padding = Mathf.Min(panelWidth, panelHeight) * Mathf.Clamp(detailModalPaddingRatio, 0.04f, 0.2f);
        float titleHeight = panelHeight * 0.2f;
        float footerHeight = panelHeight * 0.1f;
        float bodyTop = padding + titleHeight + panelHeight * 0.03f;
        float bodyBottom = padding;

        SetRectOffsets(detailModalTitleLabel != null ? detailModalTitleLabel.rectTransform : null, padding, padding, padding, panelHeight - padding - titleHeight);
        SetRectOffsets(detailModalSubtitleLabel != null ? detailModalSubtitleLabel.rectTransform : null, padding, padding + titleHeight * 0.64f, padding, panelHeight - padding - titleHeight);
        SetRectOffsets(detailModalBodyLabel != null ? detailModalBodyLabel.rectTransform : null, padding, bodyTop, padding, bodyBottom);
        SetRectOffsets(detailModalFooterLabel != null ? detailModalFooterLabel.rectTransform : null, padding, panelHeight - padding - footerHeight, padding, padding);
    }

    private static void SetRectOffsets(RectTransform rectTransform, float left, float top, float right, float bottom)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
        rectTransform.localScale = Vector3.one;
    }

    private void UpdateDetailModalText()
    {
        int infoIndex = Mathf.Abs(selectedIndex) % DetailTitles.Length;
        MuseumArtifactInfo artifactInfo = GetSelectedMuseumArtifactInfo();
        bool useMuseumLayout = detailModalMinimalMuseumLayout && artifactInfo != null && HasText(artifactInfo.description);

        if (detailModalBodyLabel != null)
        {
            detailModalBodyLabel.lineSpacing = useMuseumLayout ? detailModalBodyLineSpacing : 1f;
        }

        if (useMuseumLayout)
        {
            SetUiLabelActive(detailModalSubtitleLabel, false);
            SetUiLabelActive(detailModalFooterLabel, false);
            SetUiLabel(detailModalTitleLabel, GetDetailTitle(artifactInfo, infoIndex), detailModalTextColor);
            SetUiLabel(detailModalBodyLabel, BuildMuseumDescriptionText(artifactInfo), detailModalTextColor);
            return;
        }

        SetUiLabel(detailModalTitleLabel, GetDetailTitle(artifactInfo, infoIndex), detailModalTextColor);
        SetUiLabel(detailModalSubtitleLabel, GetDetailSubtitle(artifactInfo, infoIndex), detailModalMutedTextColor);
        SetUiLabel(detailModalBodyLabel, BuildDetailModalBody(artifactInfo, infoIndex), detailModalTextColor);
        SetUiLabel(detailModalFooterLabel, "More Info", detailModalAccentColor);
    }

    private string BuildDetailModalBody(MuseumArtifactInfo artifactInfo, int infoIndex)
    {
        if (artifactInfo != null && HasText(artifactInfo.description))
        {
            return "Description\n"
                + BuildMuseumDescriptionText(artifactInfo);
        }

        return "Material\n"
            + DetailMaterials[infoIndex]
            + "\n\nPeriod\n"
            + DetailPeriods[infoIndex]
            + "\n\nDescription\n"
            + DetailDescriptions[infoIndex];
    }

    private void SetUiLabel(Text label, string text, Color color)
    {
        if (label == null)
            return;

        label.gameObject.SetActive(!string.IsNullOrEmpty(text));
        label.text = text;
        label.color = color;
    }

    private void SetUiLabelActive(Text label, bool isActive)
    {
        if (label != null)
        {
            label.gameObject.SetActive(isActive);
        }
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

    private string BuildMuseumDescriptionText(MuseumArtifactInfo artifactInfo)
    {
        if (artifactInfo == null || !HasText(artifactInfo.description))
            return string.Empty;

        return SplitSentencesIntoParagraphs(LimitText(artifactInfo.description, detailModalMaxDescriptionCharacters));
    }

    private string SplitSentencesIntoParagraphs(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        string normalizedText = text.Replace('\r', ' ').Replace('\n', ' ').Trim();
        string formattedText = string.Empty;
        int sentenceStart = 0;
        for (int i = 0; i < normalizedText.Length; i++)
        {
            char character = normalizedText[i];
            if (character != '.' && character != '!' && character != '?')
                continue;

            bool isSentenceEnd = i == normalizedText.Length - 1 || char.IsWhiteSpace(normalizedText[i + 1]);
            if (!isSentenceEnd)
                continue;

            string sentence = normalizedText.Substring(sentenceStart, i - sentenceStart + 1).Trim();
            if (sentence.Length > 0)
            {
                formattedText = AppendParagraph(formattedText, sentence);
            }

            sentenceStart = i + 1;
        }

        if (sentenceStart < normalizedText.Length)
        {
            string sentence = normalizedText.Substring(sentenceStart).Trim();
            if (sentence.Length > 0)
            {
                formattedText = AppendParagraph(formattedText, sentence);
            }
        }

        return formattedText;
    }

    private static string AppendParagraph(string text, string paragraph)
    {
        if (string.IsNullOrEmpty(text))
            return paragraph;

        return text + "\n\n" + paragraph;
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
