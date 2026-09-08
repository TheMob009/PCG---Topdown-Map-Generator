using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Conecta el Canvas UI con los generadores PCG del pipeline.
///
/// Responsabilidades:
///   - Toggle del panel con ESC.
///   - Dropdown para cambiar entre paneles de algoritmo.
///   - Seed unificada con sub-seeds derivadas por algoritmo.
///   - Botones de generación individual y pipeline completo.
///   - Sliders con labels dinámicos.
///   - Sincronización de dimensiones BSP → Perlin (read-only).
///
/// Asigna todas las referencias desde el Inspector de Unity.
/// </summary>
public class PCGUIManager : MonoBehaviour
{
    // =================================================================
    // Referencias al pipeline
    // =================================================================

    [Header("Pipeline")]
    [SerializeField] private PipelineManager pipelineManager;

    // =================================================================
    // Panel principal (se oculta/muestra con ESC)
    // =================================================================

    [Header("Panel principal")]
    [SerializeField] private GameObject panel;

    // =================================================================
    // Controles globales (siempre visibles dentro del panel)
    // =================================================================

    [Header("Controles globales")]
    [SerializeField] private TMP_Dropdown cmbAlgoritmo;
    [SerializeField] private TMP_InputField fldSeed;
    [SerializeField] private Button btnGenerarAll;

    // =================================================================
    // Sub-paneles de cada algoritmo
    // =================================================================

    [Header("Sub-paneles")]
    [SerializeField] private GameObject panelBSP;
    [SerializeField] private GameObject panelRandomWalk;
    [SerializeField] private GameObject panelPerlinNoise;
    [SerializeField] private GameObject panelMissionGrammar;

    // =================================================================
    // Controles BSP
    // =================================================================

    [Header("BSP")]
    [SerializeField] private TMP_InputField fldAncho;
    [SerializeField] private TMP_InputField fldAlto;
    [SerializeField] private TMP_InputField fldPartition;
    [SerializeField] private TMP_InputField fldIterations;
    [SerializeField] private TMP_InputField fldPadding;
    [SerializeField] private TMP_InputField fldSize;
    [SerializeField] private TMP_InputField fldWidth;
    [SerializeField] private Button btnGenerarBSP;

    // =================================================================
    // Controles Random Walk
    // =================================================================

    [Header("Random Walk")]
    [SerializeField] private TMP_InputField fldCount;
    [SerializeField] private TMP_InputField fldSteps;
    [SerializeField] private TMP_InputField fldWalk;
    [SerializeField] private Button btnGenerarRandomWalk;

    // =================================================================
    // Controles Perlin Noise
    // =================================================================

    [Header("Perlin Noise")]
    [SerializeField] private TMP_InputField fldAnchoPerlin;
    [SerializeField] private TMP_InputField fldAltoPerlin;
    [SerializeField] private Slider sliderFrequency;
    [SerializeField] private TMP_Text lblFrecuency;
    [SerializeField] private TMP_Dropdown dropdownInterpolation;
    [SerializeField] private Button btnGenerarPerlin;

    // =================================================================
    // Controles Mission Grammar
    // =================================================================

    [Header("Mission Grammar")]
    [SerializeField] private TMP_InputField fdlStartSymbol;
    [SerializeField] private TMP_InputField fdlTaskSymbol;
    [SerializeField] private TMP_InputField fldProduction;
    [SerializeField] private Slider sliderExpansionSteps;
    [SerializeField] private TMP_Text lblExpansionStep;
    [SerializeField] private TMP_Dropdown dropdownMissionContext;
    [SerializeField] private Button btnGenerarMission;

    // =================================================================
    // Estado interno
    // =================================================================

    /// <summary>Seed maestra actual.</summary>
    private int _currentSeed;

    // =================================================================
    // INICIALIZACIÓN
    // =================================================================

    private void Start()
    {
        // Generar una seed inicial si no hay ninguna
        _currentSeed = System.Environment.TickCount;

        // Registrar listeners de botones
        if (btnGenerarAll != null) btnGenerarAll.onClick.AddListener(OnGenerateAll);
        if (btnGenerarBSP != null) btnGenerarBSP.onClick.AddListener(OnGenerateBSP);
        if (btnGenerarRandomWalk != null) btnGenerarRandomWalk.onClick.AddListener(OnGenerateRandomWalk);
        if (btnGenerarPerlin != null) btnGenerarPerlin.onClick.AddListener(OnGeneratePerlin);
        if (btnGenerarMission != null) btnGenerarMission.onClick.AddListener(OnGenerateMission);

        // Registrar listener del dropdown de algoritmo
        if (cmbAlgoritmo != null) cmbAlgoritmo.onValueChanged.AddListener(OnAlgorithmChanged);

        // Registrar listeners de sliders para actualizar labels
        if (sliderFrequency != null) sliderFrequency.onValueChanged.AddListener(OnFrequencySliderChanged);
        if (sliderExpansionSteps != null) sliderExpansionSteps.onValueChanged.AddListener(OnExpansionStepsSliderChanged);

        // Hacer los campos de dimensiones de Perlin no editables (sync desde BSP)
        if (fldAnchoPerlin != null) fldAnchoPerlin.interactable = false;
        if (fldAltoPerlin != null) fldAltoPerlin.interactable = false;

        // Inicializar la UI con los valores actuales de los generadores
        PopulateUIFromGenerators();

        // Mostrar el panel del algoritmo seleccionado actualmente
        OnAlgorithmChanged(cmbAlgoritmo != null ? cmbAlgoritmo.value : 0);
    }

    // =================================================================
    // UPDATE — Toggle con ESC
    // =================================================================

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (panel != null)
            {
                panel.SetActive(!panel.activeSelf);
            }
        }
    }

    // =================================================================
    // INICIALIZACIÓN DE LA UI CON VALORES ACTUALES
    // =================================================================

    /// <summary>
    /// Rellena todos los campos de la UI con los valores actuales de los
    /// generadores, para que el usuario vea la configuración real al iniciar.
    /// </summary>
    private void PopulateUIFromGenerators()
    {
        var bsp = pipelineManager != null ? pipelineManager.BspGenerator : null;
        var rw = pipelineManager != null ? pipelineManager.RWGenerator : null;
        var perlin = pipelineManager != null ? pipelineManager.PerlinGenerator : null;
        var mg = pipelineManager != null ? pipelineManager.MGGenerator : null;

        // Seed
        if (fldSeed != null) fldSeed.text = _currentSeed.ToString();

        // --- BSP ---
        if (bsp != null)
        {
            SetInputField(fldAncho, bsp.MapWidthValue);
            SetInputField(fldAlto, bsp.MapHeightValue);
            SetInputField(fldPartition, bsp.GetMinPartitionSize());
            SetInputField(fldIterations, bsp.GetMaxIterations());
            SetInputField(fldPadding, bsp.GetRoomPadding());
            SetInputField(fldSize, bsp.GetMinRoomSize());
            SetInputField(fldWidth, bsp.GetCorridorWidth());
        }

        // --- Random Walk ---
        if (rw != null)
        {
            SetInputField(fldCount, rw.GetAgentCount());
            SetInputField(fldSteps, rw.GetStepsPerAgent());
            SetInputField(fldWalk, rw.GetWalkWidth());
        }

        // --- Perlin Noise ---
        if (perlin != null)
        {
            // Dimensiones sincronizadas desde BSP (read-only)
            SyncPerlinDimensionsUI();

            // Slider de frecuencia
            if (sliderFrequency != null)
            {
                sliderFrequency.value = perlin.GetFrequency();
                UpdateFrequencyLabel(perlin.GetFrequency());
            }

            // Dropdown de interpolación
            if (dropdownInterpolation != null)
            {
                dropdownInterpolation.value = (int)perlin.GetInterpolationMode();
            }
        }

        // --- Mission Grammar ---
        if (mg != null)
        {
            SetInputField(fdlStartSymbol, mg.GetStartSymbol());
            SetInputField(fdlTaskSymbol, mg.GetTaskSymbol());
            SetInputField(fldProduction, mg.GetStartProduction());

            if (sliderExpansionSteps != null)
            {
                sliderExpansionSteps.value = mg.GetExpansionSteps();
                UpdateExpansionStepsLabel(mg.GetExpansionSteps());
            }

            if (dropdownMissionContext != null)
            {
                dropdownMissionContext.value = (int)mg.GetMissionContext();
            }
        }
    }

    // =================================================================
    // CAMBIO DE PANEL DE ALGORITMO
    // =================================================================

    /// <summary>
    /// Activa el sub-panel del algoritmo seleccionado y desactiva los demás.
    /// Index: 0=BSP, 1=Random Walk, 2=Perlin Noise, 3=Mission Grammar.
    /// </summary>
    private void OnAlgorithmChanged(int index)
    {
        if (panelBSP != null) panelBSP.SetActive(index == 0);
        if (panelRandomWalk != null) panelRandomWalk.SetActive(index == 1);
        if (panelPerlinNoise != null) panelPerlinNoise.SetActive(index == 2);
        if (panelMissionGrammar != null) panelMissionGrammar.SetActive(index == 3);
    }

    // =================================================================
    // LECTURA DE SEED DESDE LA UI
    // =================================================================

    /// <summary>
    /// Lee la seed del input field. Si está vacío o no es un número válido,
    /// genera una seed aleatoria y la muestra en el campo.
    /// </summary>
    private int ReadSeed()
    {
        if (fldSeed != null && int.TryParse(fldSeed.text, out int parsedSeed))
        {
            _currentSeed = parsedSeed;
        }
        else
        {
            _currentSeed = System.Environment.TickCount;
            if (fldSeed != null) fldSeed.text = _currentSeed.ToString();
        }

        return _currentSeed;
    }

    // =================================================================
    // APLICAR VALORES DE LA UI A LOS GENERADORES
    // =================================================================

    /// <summary>
    /// Lee todos los campos de BSP y los aplica al generador.
    /// También sincroniza las dimensiones hacia Perlin.
    /// </summary>
    private void ApplyBSPValues()
    {
        var bsp = pipelineManager?.BspGenerator;
        if (bsp == null) return;

        int ancho = ReadInt(fldAncho, bsp.MapWidthValue);
        int alto = ReadInt(fldAlto, bsp.MapHeightValue);

        bsp.SetDimensions(ancho, alto);
        bsp.SetMinPartitionSize(ReadInt(fldPartition, bsp.GetMinPartitionSize()));
        bsp.SetMaxIterations(ReadInt(fldIterations, bsp.GetMaxIterations()));
        bsp.SetRoomPadding(ReadInt(fldPadding, bsp.GetRoomPadding()));
        bsp.SetMinRoomSize(ReadInt(fldSize, bsp.GetMinRoomSize()));
        bsp.SetCorridorWidth(ReadInt(fldWidth, bsp.GetCorridorWidth()));

        // Sincronizar dimensiones al PipelineManager y Perlin
        if (pipelineManager != null)
        {
            pipelineManager.SetDimensions(ancho, alto);
            pipelineManager.SyncDimensions();
        }

        SyncPerlinDimensionsUI();
    }

    /// <summary>
    /// Lee todos los campos de Random Walk y los aplica al generador.
    /// </summary>
    private void ApplyRandomWalkValues()
    {
        var rw = pipelineManager?.RWGenerator;
        if (rw == null) return;

        rw.SetAgentCount(ReadInt(fldCount, rw.GetAgentCount()));
        rw.SetStepsPerAgent(ReadInt(fldSteps, rw.GetStepsPerAgent()));
        rw.SetWalkWidth(ReadInt(fldWalk, rw.GetWalkWidth()));
    }

    /// <summary>
    /// Lee todos los campos de Perlin Noise y los aplica al generador.
    /// Las dimensiones no se leen de la UI (son read-only, vienen del BSP).
    /// </summary>
    private void ApplyPerlinValues()
    {
        var perlin = pipelineManager?.PerlinGenerator;
        if (perlin == null) return;

        if (sliderFrequency != null) perlin.SetFrequency(sliderFrequency.value);

        if (dropdownInterpolation != null)
        {
            perlin.SetInterpolationMode(
                (HeightmapGenerator.InterpolationMode)dropdownInterpolation.value);
        }
    }

    /// <summary>
    /// Lee todos los campos de Mission Grammar y los aplica al generador.
    /// </summary>
    private void ApplyMissionGrammarValues()
    {
        var mg = pipelineManager?.MGGenerator;
        if (mg == null) return;

        if (fdlStartSymbol != null && !string.IsNullOrEmpty(fdlStartSymbol.text))
            mg.SetStartSymbol(fdlStartSymbol.text);

        if (fdlTaskSymbol != null && !string.IsNullOrEmpty(fdlTaskSymbol.text))
            mg.SetTaskSymbol(fdlTaskSymbol.text);

        if (fldProduction != null && !string.IsNullOrEmpty(fldProduction.text))
            mg.SetStartProduction(fldProduction.text);

        if (sliderExpansionSteps != null)
            mg.SetExpansionSteps(Mathf.RoundToInt(sliderExpansionSteps.value));

        if (dropdownMissionContext != null)
            mg.SetMissionContext((MissionContext)dropdownMissionContext.value);
    }

    // =================================================================
    // CALLBACKS DE BOTONES DE GENERACIÓN
    // =================================================================

    /// <summary>
    /// Genera el pipeline completo: aplica todos los valores de la UI,
    /// propaga la seed maestra, y ejecuta GenerateAll().
    /// </summary>
    private void OnGenerateAll()
    {
        int seed = ReadSeed();

        ApplyBSPValues();
        ApplyRandomWalkValues();
        ApplyPerlinValues();
        ApplyMissionGrammarValues();

        pipelineManager.SetGlobalSeed(seed);
        pipelineManager.GenerateAll();
    }

    /// <summary>
    /// Genera solo el BSP con los valores actuales de la UI.
    /// </summary>
    private void OnGenerateBSP()
    {
        int seed = ReadSeed();
        ApplyBSPValues();

        var bsp = pipelineManager?.BspGenerator;
        if (bsp != null)
        {
            // Derivar sub-seed para BSP (posición 1 en la secuencia)
            var masterRng = new System.Random(seed);
            masterRng.Next(); // skip perlin
            bsp.SetSeed(masterRng.Next());
        }

        pipelineManager.GenerateBspOnly();
    }

    /// <summary>
    /// Genera solo el Random Walk con los valores actuales de la UI.
    /// </summary>
    private void OnGenerateRandomWalk()
    {
        int seed = ReadSeed();
        ApplyRandomWalkValues();

        var rw = pipelineManager?.RWGenerator;
        if (rw != null)
        {
            // Derivar sub-seed para RandomWalk (posición 2 en la secuencia)
            var masterRng = new System.Random(seed);
            masterRng.Next(); // skip perlin
            masterRng.Next(); // skip bsp
            rw.SetSeed(masterRng.Next());
        }

        pipelineManager.GenerateRandomWalkOnly();
    }

    /// <summary>
    /// Genera solo el Perlin Noise con los valores actuales de la UI.
    /// </summary>
    private void OnGeneratePerlin()
    {
        int seed = ReadSeed();
        ApplyBSPValues(); // Sincronizar dimensiones BSP → Perlin
        ApplyPerlinValues();

        var perlin = pipelineManager?.PerlinGenerator;
        if (perlin != null)
        {
            // Derivar sub-seed para Perlin (posición 0 en la secuencia)
            var masterRng = new System.Random(seed);
            perlin.SetSeed(masterRng.Next());
        }

        pipelineManager.GeneratePerlinOnly();
    }

    /// <summary>
    /// Genera solo la Mission Grammar con los valores actuales de la UI.
    /// </summary>
    private void OnGenerateMission()
    {
        int seed = ReadSeed();
        ApplyMissionGrammarValues();

        var mg = pipelineManager?.MGGenerator;
        if (mg != null)
        {
            // Derivar sub-seed para MissionGrammar (posición 3 en la secuencia)
            var masterRng = new System.Random(seed);
            masterRng.Next(); // skip perlin
            masterRng.Next(); // skip bsp
            masterRng.Next(); // skip rw
            mg.SetSeed(masterRng.Next());
        }

        pipelineManager.GenerateMissionGrammarOnly();
    }

    // =================================================================
    // CALLBACKS DE SLIDERS
    // =================================================================

    private void OnFrequencySliderChanged(float value)
    {
        UpdateFrequencyLabel(value);
    }

    private void OnExpansionStepsSliderChanged(float value)
    {
        UpdateExpansionStepsLabel(Mathf.RoundToInt(value));
    }

    // =================================================================
    // UTILIDADES
    // =================================================================

    /// <summary>
    /// Sincroniza los campos de dimensiones de Perlin con los de BSP (read-only).
    /// </summary>
    private void SyncPerlinDimensionsUI()
    {
        if (fldAnchoPerlin != null && fldAncho != null)
            fldAnchoPerlin.text = fldAncho.text;

        if (fldAltoPerlin != null && fldAlto != null)
            fldAltoPerlin.text = fldAlto.text;
    }

    private void UpdateFrequencyLabel(float value)
    {
        if (lblFrecuency != null)
            lblFrecuency.text = "Frecuencia: " + value.ToString("F1");
    }

    private void UpdateExpansionStepsLabel(int value)
    {
        if (lblExpansionStep != null)
            lblExpansionStep.text = "Expansion Steps: " + value;
    }

    /// <summary>
    /// Lee un entero de un TMP_InputField. Si no es válido, devuelve el fallback.
    /// </summary>
    private int ReadInt(TMP_InputField field, int fallback)
    {
        if (field != null && int.TryParse(field.text, out int result))
            return result;
        return fallback;
    }

    /// <summary>
    /// Escribe un entero en un TMP_InputField.
    /// </summary>
    private void SetInputField(TMP_InputField field, int value)
    {
        if (field != null) field.text = value.ToString();
    }

    /// <summary>
    /// Escribe un string en un TMP_InputField.
    /// </summary>
    private void SetInputField(TMP_InputField field, string value)
    {
        if (field != null) field.text = value ?? "";
    }
}
