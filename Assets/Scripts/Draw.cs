using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public class Draw : MonoBehaviour
{
[Header("--- UI & Scene References ---")]
    [SerializeField] private GameObject canvasBase;
    public GameObject canvas;

    [Header("--- Brush Settings ---")]
    [Range(0.0001f, 1f)] public float brushSize = 0.1f;
    public Color brushColor = Color.white;
    [Range(0.0f, 1.0f)] public float globalAlpha = 1.0f;

    [Header("--- Water Cutoff Settings ---")]
    public Color waterColor = new Color(0.0f, 0.5f, 1.0f, 1.0f);
    [SerializeField] [Range(0.0f, 1.0f)] private float waterCutoffTolerance = 0.1f;

    [Header("--- Resources & Textures ---")]
    [SerializeField] private Texture2D mapTexture;

    [Header("--- Materials & Shaders ---")]
    [SerializeField] private Material mapMaterial;
    public Material _drawMaterial;    // Лучше убрать нижнее подчеркивание для public, например: drawMaterial
    public Material _displayMaterial; // Лучше убрать нижнее подчеркивание для public, например: displayMaterial
    public ComputeShader computeShader;

    // --- Private Render Buffers & States ---
    private RenderTexture _rtBufferA;
    private RenderTexture _rtBufferB;
    private bool _useBufferA = true;
    private int _kernelIndex;
    private bool _isComputing = false;

    // --- System & Interaction States ---
    private bool _isInitialized;
    private bool _hasSavedOnExit;
    private Vector2 _lastMousePosition;

    // --- Serialization & Constants ---
    private const string SavedTextureFileName = "drawn-map.png";
    private static readonly int MainTexID = Shader.PropertyToID("_BaseMap");
    private string SavedTexturePath => Path.Combine(Application.persistentDataPath, SavedTextureFileName);


    void Start()
    {
        // _drawMaterial = canvas.GetComponent<Renderer>().material;
        // _displayMaterial = canvasBase.GetComponent<Renderer>().material;

        _displayMaterial.SetTexture("_MapTexture", mapTexture);

        _kernelIndex = computeShader.FindKernel("CSMain");

        Texture2D startingTexture = LoadSavedTexture() ?? mapTexture;
        // Texture2D startingTexture = mapTexture;

        if (startingTexture != null)
        {
            RenderTexture mapRenderTexture = new RenderTexture(startingTexture.width, startingTexture.height, 24, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true
            };
            mapRenderTexture.Create();

            RenderTexture.active = mapRenderTexture;
            GL.Clear(true, true, Color.clear);

            Graphics.Blit(startingTexture, mapRenderTexture);

            _rtBufferA = new RenderTexture(startingTexture.width, startingTexture.height, 24, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true
            };
            _rtBufferB = new RenderTexture(startingTexture.width, startingTexture.height, 24, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true
            };
            _rtBufferA.Create();
            _rtBufferB.Create();

            Graphics.Blit(mapRenderTexture, _rtBufferA);
            Graphics.Blit(mapRenderTexture, _rtBufferB);
            Destroy(mapRenderTexture);

            mapMaterial.SetTexture(MainTexID, mapTexture);

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
            _rtBufferA = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true
            };
            _rtBufferB = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGBFloat)
            {
                enableRandomWrite = true
            };
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
        _displayMaterial.SetColor("_WaterColor", waterColor);
        _displayMaterial.SetFloat("_WaterCutoffTolerance", waterCutoffTolerance);
        _displayMaterial.SetFloat("_GlobalAlpha", globalAlpha);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            _drawMaterial.SetFloat("_BrushSize", brushSize);
            _displayMaterial.SetFloat("_BrushSize", brushSize);
            _drawMaterial.SetColor("_BrushColor", brushColor);
            computeShader.SetFloat("_Time", Time.time);

            RenderTexture activeSource = _useBufferA ? _rtBufferA : _rtBufferB;
            RenderTexture activeDest = _useBufferA ? _rtBufferB : _rtBufferA;

            // Добавляем RenderTextureDescriptor для точной настройки
            RenderTextureDescriptor desc = new RenderTextureDescriptor(activeDest.width, activeDest.height, RenderTextureFormat.ARGBFloat, 0);
            desc.enableRandomWrite = true;
            desc.sRGB = false; // <--- ВАЖНО: Отключает sRGB гамму, делая текстуру Linear

            RenderTexture tempRT = RenderTexture.GetTemporary(desc);

            // --- Draw Pass ---

            if (Mouse.current.leftButton.isPressed)
            {
                _drawMaterial.SetInt("_IsDrawing", 1);
                _displayMaterial.SetInt("_IsDrawing", 1);
            }

            // --- Frontline Dynamics Pass ---

            if (_isComputing)
            {
                Graphics.Blit(activeSource, tempRT, _drawMaterial);

                computeShader.SetTexture(_kernelIndex, "InputTexture", tempRT);
                computeShader.SetTexture(_kernelIndex, "OutputTexture", activeDest);

                int threadGroupsX = Mathf.CeilToInt(activeSource.width / 8.0f);
                int threadGroupsY = Mathf.CeilToInt(activeSource.height / 8.0f);

                computeShader.Dispatch(_kernelIndex, threadGroupsX, threadGroupsY, 1);
            }
            else
            {
                Graphics.Blit(activeSource, activeDest, _drawMaterial);
            }

            RenderTexture.ReleaseTemporary(tempRT);

            _drawMaterial.SetTexture("_BaseMap", activeDest);
            _displayMaterial.SetTexture("_BaseMap", activeDest);

            _useBufferA = !_useBufferA;
        }

        // --- Keyboard Controls ---

        if (Keyboard.current.leftAltKey.isPressed)
        {
            Vector2 mouseDelta = mousePos - _lastMousePosition;
            brushSize += mouseDelta.y * 0.001f * brushSize * 5; // Adjust the multiplier to control sensitivity
            brushSize = Mathf.Clamp(brushSize, 0.0001f, 1f); // Clamp the brush size to a reasonable range
        }
        else
        {
            _drawMaterial.SetVector("_BrushPosition", hit.textureCoord);
            _displayMaterial.SetVector("_BrushPosition", hit.textureCoord);
        }

        if (Keyboard.current.leftCtrlKey.isPressed)
        {
            _drawMaterial.SetInt("_IsErasing", 1);
        }
        else
        {
            _drawMaterial.SetInt("_IsErasing", 0);
        }

        if (Keyboard.current.spaceKey.isPressed)
        {
            _isComputing = true;
        }
        else
        {
            _isComputing = false;
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

        Texture2D savedTexture = new Texture2D(activeTexture.width, activeTexture.height, TextureFormat.RGBAFloat, false, true);
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

        // Добавляем true в конец конструктора (параметр linear)
        Texture2D savedTexture = new Texture2D(2, 2, TextureFormat.RGBAFloat, false, true);

        if (savedTexture.LoadImage(File.ReadAllBytes(SavedTexturePath)))
        {
            // Принудительно отключаем фильтрацию, чтобы пиксели были точными
            savedTexture.filterMode = FilterMode.Point;

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
        Color[] pixels = texture.GetPixels(); // Работаем с float
        int visiblePixelCount = 0;

        for (int i = 0; i < pixels.Length; i++)
        {
            // 10 / 255f это примерно 0.04f
            if (pixels[i].a > 0.04f)
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
