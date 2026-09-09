using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Conecta el Canvas UI con los generadores PCG del pipeline.
///
/// Responsabilidades:
///   - Toggle del panel con ESC.
///   - Dropdown de contexto global (Caverna / Estación Espacial) con presets.
///   - Limpieza automática del mapa al cambiar de contexto para evitar overlap.
///   - Dropdown para cambiar entre paneles de algoritmo.
///   - Seed unificada con sub-seeds derivadas por algoritmo.
///   - Botones de generación individual y pipeline completo con renderizado en MapVisualizer.
///   - Sliders con labels dinámicos.
///   - Sincronización de dimensiones BSP → Perlin (read-only).
///   - Toggle de tipo de corredor BSP.
///   - Campos de producción terminal y task productions del Mission Grammar.
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
    [SerializeField] private TMP_Dropdown cmbContexto;
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
    [SerializeField] private Toggle tglCorridors;
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
    [SerializeField] private TMP_InputField fldTerminal;
    [SerializeField] private TMP_InputField fldTaskProductions;
    [SerializeField] private Slider sliderExpansionSteps;
    [SerializeField] private TMP_Text lblExpansionStep;
    [SerializeField] private TMP_Dropdown dropdownMissionContext;
    [SerializeField] private Button btnGenerarMission;

    // =================================================================
    // Estado interno
    // =================================================================

    private int _currentSeed;

    // =================================================================
    // INICIALIZACIÓN
    // =================================================================

    private void Start()
    {
        _currentSeed = System.Environment.TickCount;

        // --- Botones ---
        if (btnGenerarAll != null)        btnGenerarAll.onClick.AddListener(OnGenerateAll);
        if (btnGenerarBSP != null)        btnGenerarBSP.onClick.AddListener(OnGenerateBSP);
        if (btnGenerarRandomWalk != null) btnGenerarRandomWalk.onClick.AddListener(OnGenerateRandomWalk);
        if (btnGenerarPerlin != null)     btnGenerarPerlin.onClick.AddListener(OnGeneratePerlin);
        if (btnGenerarMission != null)    btnGenerarMission.onClick.AddListener(OnGenerateMission);

        // --- Dropdowns ---
        if (cmbContexto != null)  cmbContexto.onValueChanged.AddListener(OnContextChanged);
        if (cmbAlgoritmo != null) cmbAlgoritmo.onValueChanged.AddListener(OnAlgorithmChanged);

        // --- Sliders ---
        if (sliderFrequency != null)
            sliderFrequency.onValueChanged.AddListener(v => UpdateFrequencyLabel(v));
        if (sliderExpansionSteps != null)
            sliderExpansionSteps.onValueChanged.AddListener(v => UpdateExpansionStepsLabel(Mathf.RoundToInt(v)));

        // Campos de dimensión Perlin son read-only (sincronizados desde BSP)
        if (fldAnchoPerlin != null) fldAnchoPerlin.interactable = false;
        if (fldAltoPerlin != null)  fldAltoPerlin.interactable  = false;

        // Rellenar UI con valores actuales de los generadores
        PopulateUIFromGenerators();

        // Mostrar el panel del algoritmo actualmente seleccionado
        OnAlgorithmChanged(cmbAlgoritmo != null ? cmbAlgoritmo.value : 0);
    }

    // =================================================================
    // UPDATE — Toggle con ESC
    // =================================================================

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && panel != null)
            panel.SetActive(!panel.activeSelf);
    }

    // =================================================================
    // INICIALIZACIÓN DE LA UI CON VALORES ACTUALES DE LOS GENERADORES
    // =================================================================

    private void PopulateUIFromGenerators()
    {
        var bsp    = pipelineManager?.BspGenerator;
        var rw     = pipelineManager?.RWGenerator;
        var perlin = pipelineManager?.PerlinGenerator;
        var mg     = pipelineManager?.MGGenerator;

        // Seed
        if (fldSeed != null) fldSeed.text = _currentSeed.ToString();

        // --- BSP ---
        if (bsp != null)
        {
            SetField(fldAncho,      bsp.MapWidthValue);
            SetField(fldAlto,       bsp.MapHeightValue);
            SetField(fldPartition,  bsp.GetMinPartitionSize());
            SetField(fldIterations, bsp.GetMaxIterations());
            SetField(fldPadding,    bsp.GetRoomPadding());
            SetField(fldSize,       bsp.GetMinRoomSize());
            SetField(fldWidth,      bsp.GetCorridorWidth());
            if (tglCorridors != null) tglCorridors.isOn = bsp.GetUseStraightCorridors();
        }

        // --- Random Walk ---
        if (rw != null)
        {
            SetField(fldCount, rw.GetAgentCount());
            SetField(fldSteps, rw.GetStepsPerAgent());
            SetField(fldWalk,  rw.GetWalkWidth());
        }

        // --- Perlin Noise ---
        if (perlin != null)
        {
            SyncPerlinDimensionsUI();
            if (sliderFrequency != null)
            {
                sliderFrequency.value = perlin.GetFrequency();
                UpdateFrequencyLabel(perlin.GetFrequency());
            }
            if (dropdownInterpolation != null)
                dropdownInterpolation.value = (int)perlin.GetInterpolationMode();
        }

        // --- Mission Grammar ---
        if (mg != null)
        {
            SetField(fdlStartSymbol, mg.GetStartSymbol());
            SetField(fdlTaskSymbol,  mg.GetTaskSymbol());
            SetField(fldProduction,  mg.GetStartProduction());
            SetField(fldTerminal,    mg.GetTerminalProduction());

            // Task productions como texto separado por comas
            if (fldTaskProductions != null)
            {
                var prods = mg.GetTaskProductions();
                fldTaskProductions.text = prods != null ? string.Join(",", prods) : "";
            }

            if (sliderExpansionSteps != null)
            {
                sliderExpansionSteps.value = mg.GetExpansionSteps();
                UpdateExpansionStepsLabel(mg.GetExpansionSteps());
            }
            if (dropdownMissionContext != null)
                dropdownMissionContext.value = (int)mg.GetMissionContext();
        }
    }

    // =================================================================
    // CAMBIO DE CONTEXTO GLOBAL — limpia mapa y aplica preset
    // =================================================================

    /// <summary>
    /// Aplica el preset del contexto seleccionado, sincroniza MapVisualizer,
    /// actualiza los campos de la UI y limpia el mapa anterior para evitar overlap.
    /// Index: 0 = Caverna, 1 = Estación Espacial.
    /// </summary>
    private void OnContextChanged(int index)
    {
        // 1. Limpiar el mapa actual para evitar overlap visual
        if (pipelineManager != null)
        {
            pipelineManager.ClearAll();
        }

        // 2. Aplicar los parámetros del contexto al PipelineManager
        if (pipelineManager != null)
        {
            if (index == 0)
            {
                pipelineManager.ApplyExcavatedCaveParameters();
            }
            else
            {
                pipelineManager.ApplyLunarStationParameters();
            }
        }

        // 3. Asegurar que MapVisualizer esté en el contexto correcto
        ApplyContextValues();

        // 4. Actualizar todos los campos de la UI con los nuevos valores del preset
        PopulateUIFromGenerators();
    }

    // =================================================================
    // CAMBIO DE PANEL DE ALGORITMO
    // =================================================================

    /// <summary>
    /// Activa el sub-panel del algoritmo seleccionado y desactiva los demás.
    /// Index: 0 = BSP, 1 = Random Walk, 2 = Perlin Noise, 3 = Mission Grammar.
    /// </summary>
    private void OnAlgorithmChanged(int index)
    {
        if (panelBSP != null)            panelBSP.SetActive(index == 0);
        if (panelRandomWalk != null)     panelRandomWalk.SetActive(index == 1);
        if (panelPerlinNoise != null)    panelPerlinNoise.SetActive(index == 2);
        if (panelMissionGrammar != null) panelMissionGrammar.SetActive(index == 3);
    }

    // =================================================================
    // LECTURA DE SEED
    // =================================================================

    private int ReadSeed()
    {
        if (fldSeed != null && int.TryParse(fldSeed.text, out int parsed))
        {
            _currentSeed = parsed;
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

    private void ApplyContextValues()
    {
        if (pipelineManager == null || pipelineManager.MapVisualizer == null) return;

        MapContext context = (cmbContexto != null && cmbContexto.value == 1)
            ? MapContext.EstacionEspacial
            : MapContext.Caverna;

        pipelineManager.MapVisualizer.SetContext(context);
        pipelineManager.MapVisualizer.SetRenderEmptyAsRock(context == MapContext.EstacionEspacial);
    }

    private void ApplyBSPValues()
    {
        var bsp = pipelineManager?.BspGenerator;
        if (bsp == null) return;

        int ancho = ReadInt(fldAncho, bsp.MapWidthValue);
        int alto  = ReadInt(fldAlto,  bsp.MapHeightValue);

        bsp.SetDimensions(ancho, alto);
        bsp.SetMinPartitionSize(ReadInt(fldPartition,  bsp.GetMinPartitionSize()));
        bsp.SetMaxIterations(   ReadInt(fldIterations, bsp.GetMaxIterations()));
        bsp.SetRoomPadding(     ReadInt(fldPadding,    bsp.GetRoomPadding()));
        bsp.SetMinRoomSize(     ReadInt(fldSize,       bsp.GetMinRoomSize()));
        bsp.SetCorridorWidth(   ReadInt(fldWidth,      bsp.GetCorridorWidth()));

        if (tglCorridors != null)
            bsp.SetUseStraightCorridors(tglCorridors.isOn);

        // Propagar dimensiones al PipelineManager y a los campos de Perlin
        if (pipelineManager != null)
        {
            pipelineManager.SetDimensions(ancho, alto);
            pipelineManager.SyncDimensions();
        }
        SyncPerlinDimensionsUI();
    }

    private void ApplyRandomWalkValues()
    {
        var rw = pipelineManager?.RWGenerator;
        if (rw == null) return;

        rw.SetAgentCount(  ReadInt(fldCount, rw.GetAgentCount()));
        rw.SetStepsPerAgent(ReadInt(fldSteps, rw.GetStepsPerAgent()));
        rw.SetWalkWidth(   ReadInt(fldWalk,  rw.GetWalkWidth()));
    }

    private void ApplyPerlinValues()
    {
        var perlin = pipelineManager?.PerlinGenerator;
        if (perlin == null) return;

        if (sliderFrequency != null)
            perlin.SetFrequency(sliderFrequency.value);

        if (dropdownInterpolation != null)
            perlin.SetInterpolationMode(
                (HeightmapGenerator.InterpolationMode)dropdownInterpolation.value);
    }

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

        if (fldTerminal != null && !string.IsNullOrEmpty(fldTerminal.text))
            mg.SetTerminalProduction(fldTerminal.text);

        // Parsear task productions separadas por coma
        if (fldTaskProductions != null && !string.IsNullOrEmpty(fldTaskProductions.text))
        {
            var parts = fldTaskProductions.text.Split(
                new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            var list = new List<string>();
            foreach (var p in parts)
            {
                string trimmed = p.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    list.Add(trimmed);
            }
            if (list.Count > 0)
                mg.SetTaskProductions(list);
        }

        if (sliderExpansionSteps != null)
            mg.SetExpansionSteps(Mathf.RoundToInt(sliderExpansionSteps.value));

        if (dropdownMissionContext != null)
            mg.SetMissionContext((MissionContext)dropdownMissionContext.value);
    }

    // =================================================================
    // CALLBACKS DE BOTONES DE GENERACIÓN
    // =================================================================

    private void OnGenerateAll()
    {
        int seed = ReadSeed();
        ApplyContextValues();
        ApplyBSPValues();
        ApplyRandomWalkValues();
        ApplyPerlinValues();
        ApplyMissionGrammarValues();

        pipelineManager.SetGlobalSeed(seed);
        pipelineManager.GenerateAll();
    }

    private void OnGenerateBSP()
    {
        int seed = ReadSeed();
        ApplyContextValues();
        ApplyBSPValues();

        // Sub-seed BSP: posición 1 en la secuencia (0=Perlin, 1=BSP, ...)
        var rng = new System.Random(seed);
        rng.Next(); // skip perlin
        pipelineManager?.BspGenerator?.SetSeed(rng.Next());

        pipelineManager.GenerateBspOnly();
        pipelineManager.RenderMap(MapRenderStage.BSPOnly);
    }

    private void OnGenerateRandomWalk()
    {
        int seed = ReadSeed();
        ApplyContextValues();
        ApplyRandomWalkValues();

        // Sub-seed RW: posición 2
        var rng = new System.Random(seed);
        rng.Next(); rng.Next(); // skip perlin, bsp
        pipelineManager?.RWGenerator?.SetSeed(rng.Next());

        pipelineManager.GenerateRandomWalkOnly();
        pipelineManager.RenderMap(MapRenderStage.WithRandomWalk);
    }

    private void OnGeneratePerlin()
    {
        int seed = ReadSeed();
        ApplyContextValues();
        ApplyBSPValues(); // sincroniza dimensiones BSP → Perlin
        ApplyPerlinValues();

        // Sub-seed Perlin: posición 0
        var rng = new System.Random(seed);
        pipelineManager?.PerlinGenerator?.SetSeed(rng.Next());

        pipelineManager.GeneratePerlinOnly();
    }

    private void OnGenerateMission()
    {
        int seed = ReadSeed();
        ApplyContextValues();
        ApplyMissionGrammarValues();

        // Sub-seed MG: posición 3
        var rng = new System.Random(seed);
        rng.Next(); rng.Next(); rng.Next(); // skip perlin, bsp, rw
        pipelineManager?.MGGenerator?.SetSeed(rng.Next());

        pipelineManager.GenerateMissionGrammarOnly();
    }

    // =================================================================
    // CALLBACKS DE SLIDERS
    // =================================================================

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

    // =================================================================
    // UTILIDADES
    // =================================================================

    private void SyncPerlinDimensionsUI()
    {
        if (fldAnchoPerlin != null && fldAncho != null)
            fldAnchoPerlin.text = fldAncho.text;
        if (fldAltoPerlin != null && fldAlto != null)
            fldAltoPerlin.text = fldAlto.text;
    }

    private int ReadInt(TMP_InputField field, int fallback)
    {
        if (field != null && int.TryParse(field.text, out int result))
            return result;
        return fallback;
    }

    private void SetField(TMP_InputField field, int value)
    {
        if (field != null) field.text = value.ToString();
    }

    private void SetField(TMP_InputField field, string value)
    {
        if (field != null) field.text = value ?? "";
    }
}
