using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using TMPro;

[System.Serializable]
public class CameraSettings
{
    [Header("Detector Settings")]
    [Range(-255, 255)]
    public int r = 0;
    [Range(-255, 255)]
    public int g = 0;
    [Range(-255, 255)]
    public int b = 0;
    [Range(0, 255)]
    public int threshold = 20;
    [Range(0.001f, 0.1f)]
    public double epsilon = 0.001f;
    [Range(1, 21)]
    public int gaussian = 1;
    [Range(0, 100000)]
    public int contourMinArea = 0;
    public bool useApprox = true;

    public void CopyTo(CameraSettings settings)
    {
        settings.r = r;
        settings.g = g;
        settings.b = b;
        settings.threshold = threshold;
        settings.epsilon = epsilon;
        settings.gaussian = gaussian;
        settings.contourMinArea = contourMinArea;
        settings.useApprox = useApprox;
    }
}

public class CameraBasedShadowDetectorSetting : MonoBehaviour
{
    private CameraBasedShadowDetector detector;
    private UIManager uiManager;

    [Header("Camera Point Settings")]
    [SerializeField] private CameraBasedPoint[] cameraBasedPoints = new CameraBasedPoint[4];
    private CameraBasedPoint cameraPointTemp = new CameraBasedPoint();
    private int cameraPointSettingCount = 0;
    public int CameraPointSettingCount => cameraPointSettingCount;

    [SerializeField]
    private CameraSettings settings;
    private CameraSettings settingsTemp = new CameraSettings();

    [Header("UI")]
    [SerializeField] private RawImage rawImage;
    [SerializeField] private Slider sliderR;
    [SerializeField] private Slider sliderG;
    [SerializeField] private Slider sliderB;
    [SerializeField] private Slider sliderThreshold;
    [SerializeField] private Slider sliderEpsilon;
    [SerializeField] private Slider sliderGaussian;
    [SerializeField] private Slider sliderContourMinArea;
    [SerializeField] private Toggle toggleUseApprox;
    [SerializeField] private Toggle toggleScreen;
    [SerializeField] private TextMeshProUGUI textR;
    [SerializeField] private TextMeshProUGUI textG;
    [SerializeField] private TextMeshProUGUI textB;
    [SerializeField] private TextMeshProUGUI textThreshold;
    [SerializeField] private TextMeshProUGUI textEpsilon;
    [SerializeField] private TextMeshProUGUI textGaussian;
    [SerializeField] private TextMeshProUGUI textContourMinArea;

    [Header("Mesh")]
    [SerializeField] private Material meshMaterial;
    [SerializeField] private Color meshSettingsColor;
    private Color meshOriginColor;

    private bool isSettingMode = false;
    public bool IsSettingMode => isSettingMode;
    private bool isPointActive = false;

    private int screenWidthRatio;
    private int screenHeightRatio;

    private void Awake()
    {
        detector = FindObjectOfType<CameraBasedShadowDetector>();
        detector.onInitDone.AddListener(Init);
        uiManager = FindObjectOfType<UIManager>();
        uiManager.onCancelPressed.AddListener(Cancel);
        uiManager.onAcceptPressed.AddListener(Accept);
        uiManager.onUIOpened.AddListener(OpenUI);
        uiManager.onUIClosed.AddListener(CloseUI);
    }

    public void Init()
    {
        screenWidthRatio = Screen.width / detector.width;
        screenHeightRatio = Screen.height / detector.height;

        Load();
        settings.CopyTo(settingsTemp);
        meshOriginColor = meshMaterial.color;

        UpdateUI();
    }

    private void UpdateUI()
    {
        sliderR.value = settings.r;
        sliderG.value = settings.g;
        sliderB.value = settings.b;
        sliderThreshold.value = settings.threshold;
        sliderEpsilon.value = (float)settings.epsilon;
        sliderGaussian.value = settings.gaussian;
        sliderContourMinArea.value = settings.contourMinArea;
        toggleUseApprox.isOn = settings.useApprox;
        textR.text = settings.r.ToString();
        textG.text = settings.g.ToString();
        textB.text = settings.b.ToString();
        textThreshold.text = settings.threshold.ToString();
        textEpsilon.text = settings.epsilon.ToString("0.000");
        textGaussian.text = settings.gaussian.ToString();
        textContourMinArea.text = settings.contourMinArea.ToString();
    }

    private void Update()
    {
        if (!detector.HasInitDone)
            return;

        if (rawImage.texture == null)
            rawImage.texture = FindObjectOfType<CameraBasedShadowDetector>().GetFrameTexture();

        if (isSettingMode)
        {
            Vector2 mousePos = GetMousePosition();

            if (Input.GetMouseButtonDown(0))
                ActivatePoint();

            if (Input.GetMouseButton(0) && isPointActive)
                UpdatePointPosition(mousePos);

            if (Input.GetMouseButtonUp(1))
                DeactivatePoint();

            if (Input.GetMouseButtonUp(0) && isPointActive)
            {
                ApplyPointPosition();
                CheckNextPoint();
                SetCameraPointTemp();
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                cameraPointSettingCount = 0;
                SetCameraPointTemp();
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                cameraPointSettingCount = 1;
                SetCameraPointTemp();
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                cameraPointSettingCount = 2;
                SetCameraPointTemp();
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                cameraPointSettingCount = 3;
                SetCameraPointTemp();
            }

            detector.DrawPerspectivePoint();
        }
    }

    public void SetSettingMode(bool value)
    {
        isSettingMode = value;
        rawImage.gameObject.SetActive(value);
        cameraPointSettingCount = 0;

        if (!value)
        {
            toggleScreen.isOn = true;
            DeactivatePoint();
        }
    }

    private Vector2 GetMousePosition()
    {
        Vector2 mousePos = new Vector2(Input.mousePosition.x / screenWidthRatio, detector.height - Input.mousePosition.y / screenHeightRatio);
        mousePos.x = Mathf.Clamp(mousePos.x, 0, detector.width);
        mousePos.y = Mathf.Clamp(mousePos.y, 0, detector.height);
        return mousePos;
    }

    private void ActivatePoint()
    {
        isPointActive = true;
    }

    private void UpdatePointPosition(Vector2 mousePos)
    {
        cameraPointTemp.x = mousePos.x;
        cameraPointTemp.y = mousePos.y;
    }

    private void DeactivatePoint()
    {
        SetCameraPointTemp();
        isPointActive = false;
    }

    private void ApplyPointPosition()
    {
        cameraBasedPoints[cameraPointSettingCount].x = cameraPointTemp.x;
        cameraBasedPoints[cameraPointSettingCount].y = cameraPointTemp.y;

    }

    private void CheckNextPoint()
    {
        cameraPointSettingCount++;
        if (cameraPointSettingCount >= cameraBasedPoints.Length)
            cameraPointSettingCount = 0;
    }

    private void SetCameraPointTemp()
    {
        cameraPointTemp.x = cameraBasedPoints[cameraPointSettingCount].x;
        cameraPointTemp.y = cameraBasedPoints[cameraPointSettingCount].y;
    }

    public Point GetCameraBasedPoint(int index)
    {
        if (isSettingMode && index == cameraPointSettingCount)
            return cameraPointTemp.Get();
        else
            return cameraBasedPoints[index].Get();
    }

    public int GetCameraBasedPointLength()
    {
        return cameraBasedPoints.Length;
    }

    public int GetR() { return settings.r; }
    public void SetR(float value) { settings.r = (int)value; UpdateUI(); }
    public int GetG() { return settings.g; }
    public void SetG(float value) { settings.g = (int)value; UpdateUI(); }
    public int GetB() { return settings.b; }
    public void SetB(float value) { settings.b = (int)value; UpdateUI(); }
    public int GetThreshold() { return settings.threshold; }
    public void SetThreshold(float value) { settings.threshold = (int)value; UpdateUI(); }
    public double GetEpsilon() { return settings.epsilon; }
    public void SetEpsilon(float value) { settings.epsilon = value; UpdateUI(); }
    public int GetGaussian() { return settings.gaussian; }
    public void SetGaussian(float value) { settings.gaussian = (int)value; UpdateUI(); }
    public int GetContourMinArea() { return settings.contourMinArea; }
    public void SetContourMinArea(float value) { settings.contourMinArea = (int)value; UpdateUI(); }
    public bool GetUseApprox() { return settings.useApprox; }
    public void SetUseApprox(bool value) { settings.useApprox = value; UpdateUI(); }

    private void OpenUI()
    {
        settings.CopyTo(settingsTemp);
        meshOriginColor = meshMaterial.color;
        meshMaterial.color = meshSettingsColor;
    }

    private void CloseUI()
    {
        meshMaterial.color = meshOriginColor;
    }

    public void Cancel()
    {
        settingsTemp.CopyTo(settings);
        UpdateUI();
    }

    public void Accept()
    {
        Save();
    }

    private void Save()
    {
        PlayerPrefs.SetInt("r", settings.r);
        PlayerPrefs.SetInt("g", settings.g);
        PlayerPrefs.SetInt("b", settings.b);
        PlayerPrefs.SetInt("threshold", settings.threshold);
        PlayerPrefs.SetFloat("epsilon", (float)settings.epsilon);
        PlayerPrefs.SetInt("gaussian", settings.gaussian);
        PlayerPrefs.SetInt("contourMinArea", settings.contourMinArea);
        PlayerPrefs.SetInt("useApprox", System.Convert.ToInt16(settings.useApprox));
    }

    private void Load()
    {
        settings.r = PlayerPrefs.GetInt("r");
        settings.g = PlayerPrefs.GetInt("g");
        settings.b = PlayerPrefs.GetInt("b");
        settings.threshold = PlayerPrefs.GetInt("threshold");
        settings.epsilon = PlayerPrefs.GetFloat("epsilon");
        settings.gaussian = PlayerPrefs.GetInt("gaussian");
        settings.contourMinArea = PlayerPrefs.GetInt("contourMinArea");
        settings.useApprox = System.Convert.ToBoolean(PlayerPrefs.GetInt("useApprox"));
    }

    private void OnApplicationQuit()
    {
        meshMaterial.color = meshOriginColor;
    }
}