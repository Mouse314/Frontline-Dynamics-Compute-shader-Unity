using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class Draw : MonoBehaviour
{
    public GameObject canvas;
    [SerializeField] private GameObject canvasBase;
    public float brushSize = 0.1f;
    public Color brushColor = Color.white;


    [Range(0.0f, 1.0f)]
    public float globalAlpha = 1.0f;

    [SerializeField] private Texture2D mapTexture;

    public Texture2D text;

    private RenderTexture _rtBufferA;
    private RenderTexture _rtBufferB;
    private bool _useBufferA = true;
    public Material _drawMaterial;
    public Material _displayMaterial;

    private static readonly int MainTexID = Shader.PropertyToID("_BaseMap");
    private const string SavedTextureFileName = "drawn-map.png";

    private bool _isInitialized;
    private bool _hasSavedOnExit;

    private string SavedTexturePath => Path.Combine(Application.persistentDataPath, SavedTextureFileName);

    private Vector2 _lastMousePosition;


    void Start()
    {
        // _drawMaterial = canvas.GetComponent<Renderer>().material;
        // _displayMaterial = canvasBase.GetComponent<Renderer>().material;

        Texture2D startingTexture = LoadSavedTexture() ?? mapTexture;
        // Texture2D startingTexture = mapTexture;

        if (startingTexture != null)
        {
            RenderTexture mapRenderTexture = new RenderTexture(startingTexture.width, startingTexture.height, 24, RenderTextureFormat.ARGB32);
            mapRenderTexture.Create();

            RenderTexture.active = mapRenderTexture;
            GL.Clear(true, true, Color.clear);

            Graphics.Blit(startingTexture, mapRenderTexture);

            _rtBufferA = new RenderTexture(startingTexture.width, startingTexture.height, 24);
            _rtBufferB = new RenderTexture(startingTexture.width, startingTexture.height, 24);
            _rtBufferA.Create();
            _rtBufferB.Create();

            Graphics.Blit(mapRenderTexture, _rtBufferA);
            Graphics.Blit(mapRenderTexture, _rtBufferB);
            Destroy(mapRenderTexture);

            _drawMaterial.SetTexture(MainTexID, _rtBufferA);
            canvas.transform.localScale = new Vector3(startingTexture.width / 100f, 1, startingTexture.height / 100f);
            canvasBase.transform.localScale = new Vector3(startingTexture.width / 100f, 1, startingTexture.height / 100f);
            _drawMaterial.SetFloat("_AspectRatio", (float)startingTexture.width / startingTexture.height);
            _displayMaterial.SetFloat("_AspectRatio", (float)startingTexture.width / startingTexture.height);

            _displayMaterial.SetTexture("_BaseMap", _rtBufferA);

            if (startingTexture != mapTexture)
            {
                Destroy(startingTexture);
            }
        }
        else
        {
            _rtBufferA = new RenderTexture(128, 128, 24);
            _rtBufferB = new RenderTexture(128, 128, 24);
            _rtBufferA.Create();
            _rtBufferB.Create();

            Texture2D initialTexture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            Graphics.Blit(initialTexture, _rtBufferA);
            Graphics.Blit(initialTexture, _rtBufferB);
            Destroy(initialTexture);
        }

        _isInitialized = true;
    }

    void Update()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        _drawMaterial.SetInt("_IsDrawing", 0);
        _displayMaterial.SetInt("_IsDrawing", 0);

        _displayMaterial.SetFloat("_GlobalAlpha", globalAlpha);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            _drawMaterial.SetFloat("_BrushSize", brushSize);
            _displayMaterial.SetFloat("_BrushSize", brushSize);
            _drawMaterial.SetColor("_BrushColor", brushColor);

            if (Mouse.current.leftButton.isPressed)
            {
                _drawMaterial.SetInt("_IsDrawing", 1);
                _displayMaterial.SetInt("_IsDrawing", 1);

                RenderTexture activeSource = _useBufferA ? _rtBufferA : _rtBufferB;
                RenderTexture activeDest = _useBufferA ? _rtBufferB : _rtBufferA;

                Graphics.Blit(activeSource, activeDest, _drawMaterial);
                
                _drawMaterial.SetTexture("_BaseMap", activeDest);
                _displayMaterial.SetTexture("_BaseMap", activeSource);
                
                _useBufferA = !_useBufferA;
            }
        } 

        if (Keyboard.current.leftAltKey.isPressed) {
            Vector2 mouseDelta = mousePos - _lastMousePosition;
            brushSize += mouseDelta.y * 0.001f * brushSize * 5; // Adjust the multiplier to control sensitivity
            brushSize = Mathf.Clamp(brushSize, 0.0001f, 1f); // Clamp the brush size to a reasonable range
        } else {
            _drawMaterial.SetVector("_BrushPosition", hit.textureCoord);
            _displayMaterial.SetVector("_BrushPosition", hit.textureCoord);
        }

        _lastMousePosition = mousePos;
    }

    private void SaveDrawing()
    {
        if (!_isInitialized || _hasSavedOnExit)
        {
            return;
        }

        RenderTexture activeTexture = _useBufferA ? _rtBufferA : _rtBufferB;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = activeTexture;

        Texture2D savedTexture = new Texture2D(activeTexture.width, activeTexture.height, TextureFormat.RGBA32, false);
        savedTexture.ReadPixels(new Rect(0, 0, activeTexture.width, activeTexture.height), 0, 0);
        savedTexture.Apply();
        File.WriteAllBytes(SavedTexturePath, savedTexture.EncodeToPNG());

        RenderTexture.active = previousActive;
        Destroy(savedTexture);
        _hasSavedOnExit = true;
        Debug.Log($"Drawing saved to: {SavedTexturePath}");
    }

    private void OnApplicationQuit()
    {
        SaveDrawing();
    }

    private void OnDisable()
    {
        // Unity Editor may stop Play Mode through OnDisable.
        if (Application.isPlaying)
        {
            SaveDrawing();
        }
    }

    private void OnDestroy()
    {
        if (_rtBufferA != null)
        {
            _rtBufferA.Release();
        }

        if (_rtBufferB != null)
        {
            _rtBufferB.Release();
        }
    }

    private Texture2D LoadSavedTexture()
    {
        if (!File.Exists(SavedTexturePath))
        {
            return null;
        }

        Texture2D savedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (savedTexture.LoadImage(File.ReadAllBytes(SavedTexturePath)))
        {
            if (!HasVisiblePixels(savedTexture))
            {
                Destroy(savedTexture);
                return null;
            }

            return savedTexture;
        }

        Destroy(savedTexture);
        return null;
    }

    private static bool HasVisiblePixels(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        int visiblePixelCount = 0;

        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > 10)
            {
                visiblePixelCount++;
                if (visiblePixelCount > 8)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
