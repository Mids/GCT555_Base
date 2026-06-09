using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class QuadDisplay : MonoBehaviour
{
    public string snapshotUrl = "http://127.0.0.1:5000/snapshot";
    public Renderer quadRenderer;
    public float refreshRate = 0.033f; // ~30 FPS
    public bool inputSourceUpsideDown = false;

    void Start()
    {
        if (quadRenderer == null)
            quadRenderer = GetComponent<Renderer>();

        ApplyTextureOrientation();
        StartCoroutine(FetchTextureLoop());
    }

    IEnumerator FetchTextureLoop()
    {
        while (true)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(snapshotUrl))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    // Debug.LogWarning($"Stream Error: {uwr.error}");
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                    Material material = quadRenderer.material;
                    if (material.mainTexture != null)
                    {
                        Destroy(material.mainTexture);
                    }

                    material.mainTexture = texture;
                    ApplyTextureOrientation();
                }
            }

            yield return new WaitForSeconds(refreshRate);
        }
    }

    private void ApplyTextureOrientation()
    {
        if (quadRenderer == null)
            return;

        Material material = quadRenderer.material;
        material.mainTextureScale = inputSourceUpsideDown ? new Vector2(-1f, -1f) : Vector2.one;
        material.mainTextureOffset = inputSourceUpsideDown ? Vector2.one : Vector2.zero;
    }
}
