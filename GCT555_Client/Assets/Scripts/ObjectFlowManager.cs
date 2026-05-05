using UnityEngine;

public class ObjectFlowManager : MonoBehaviour
{
    private const int ObjectCount = 9;
    private const float MinSpacing = 0.45f;
    private const float MaxSpacing = 1.05f;
    private const float MinSideDepth = 0.08f;
    private const float MaxSideDepth = 0.55f;
    private const float MaxSideAngle = 62f;
    private const float CenterScale = 0.75f;
    private const float SideScale = 0.52f;
    private const float FlowCurve = 0.82f;

    [Header("Flow Controls")]
    [Range(0f, 1f)]
    public float depth = 0.5f;

    [Range(0f, 1f)]
    public float position = 0.5f;

    private readonly GameObject[] flowObjects = new GameObject[ObjectCount];
    private Material[] generatedMaterials;

    private void Awake()
    {
        EnsureObjects();
        UpdateLayout();
    }

    private void Update()
    {
        EnsureObjects();
        UpdateLayout();
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

        if (generatedMaterials == null)
            return;

        for (int i = 0; i < generatedMaterials.Length; i++)
        {
            if (generatedMaterials[i] != null)
            {
                Destroy(generatedMaterials[i]);
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

        generatedMaterials = new[]
        {
            CreateMaterial(shader, Color.red),
            CreateMaterial(shader, Color.green),
            CreateMaterial(shader, Color.blue)
        };
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

    private void UpdateLayout()
    {
        float clampedDepth = Mathf.Clamp01(depth);
        float focusedIndex = Mathf.Clamp01(position) * (ObjectCount - 1);
        float spacing = Mathf.Lerp(MinSpacing, MaxSpacing, clampedDepth);
        float sideDepth = Mathf.Lerp(MinSideDepth, MaxSideDepth, clampedDepth);

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
            float yRotation = -Mathf.Sign(offset) * MaxSideAngle * sideAmount;
            float scale = Mathf.Lerp(CenterScale, SideScale, sideAmount);

            flowObject.transform.localPosition = new Vector3(x, 0f, z);
            flowObject.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            flowObject.transform.localScale = Vector3.one * scale;
        }
    }
}
