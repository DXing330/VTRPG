using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessNeuralNetPrepController : MonoBehaviour
{
    [ContextMenu("Test Neural Network")]
    void TestNeuralNetwork()
    {
        LoadModel();
        // Create a deterministic test input
        float[] testState = new float[1918];
        for (int i = 0; i < 1918; i++)
            testState[i] = (i % 10) / 10f;  // 0.0, 0.1, 0.2, ... 0.9, repeat
        bool[] testMask = new bool[44];
        for (int i = 0; i < 44; i++)
            testMask[i] = true;
        // Build hierarchical masks
        BuildHierarchicalMasks(testMask, out var typeMask, out var buyMask, out var sellMask, out var placeMask, out var fieldMask);
        // Run inference
        int action = PredictAction(testState, typeMask, buyMask, sellMask, placeMask, fieldMask, out float testLogProb, out float testValue);
        // Log raw logits for inspection
        var outputs = new Dictionary<string, float[]>();
        _network.Forward(testState, outputs);
        string typeLogits = string.Join(", ", outputs["policy_type"].Select(x => x.ToString("F4")));
        Debug.Log($"Test Action: {action}");
        Debug.Log($"Type Logits: {typeLogits}");
    }
    private string FormatFloatArray(float[] values)
    {
        return string.Join(",", System.Array.ConvertAll(values, x => x.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
        );
    }
    public AutoChessAIPrepController genomeAI;
    public AutoChessStateEncoder stateEncoder;
    public string modelTag = "v1";
    // ============================================
    // NEW: Runtime weight loading — no rebuilds!
    // ============================================
    [Tooltip("Path to weights file (relative to StreamingAssets or PersistentDataPath)")]
    public string weightsPath = "autochess_policy_weights.hnn";
    // The custom neural network — replaces ONNX InferenceSession
    private RuntimeNeuralNetwork _network;
    private Dictionary<string, float[]> _headOutputs;  // Re-used output dictionary
    private string _lastWeightsPath;
    private long _lastFileSize = -1;
    [Header("PPO Collection")]
    public bool ppoCollectionMode = false;
    // Exploration
    public bool addExplorationNoise = false;
    public float temperature = 1.0f;
    public float dirichletAlpha = 0.3f;
    public float noiseWeight = 0.25f;
    // Recording
    private List<AutoActorRollUpData> sortedBench;
    private List<AutoActorRollUpData> sortedField;

    // Logging.
    private float totalInferenceTime = 0f;
    private int inferenceCount = 0;
    private float totalStepTime = 0f;
    private int stepCount = 0;
    public void ResetTimers()
    {
        totalInferenceTime = 0f;
        inferenceCount = 0;
        totalStepTime = 0f;
        stepCount = 0;
    }
    [ContextMenu("Log NN Timing")]
    public void LogTiming()
    {
        float avgInference = inferenceCount > 0 ? totalInferenceTime / inferenceCount : 0f;
        float avgStep = stepCount > 0 ? totalStepTime / stepCount : 0f;
        UnityEngine.Debug.Log($"[NN Timing] Avg inference: {avgInference:F2}ms ({inferenceCount} calls) | Avg full step: {avgStep:F2}ms ({stepCount} calls)");
        ResetTimers();
    }
    void Start()
    {
        ResetTimers();
    }
    // Only Load Fully Once.
    public bool loaded = false;
    public bool LoadModelFromPath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || !System.IO.File.Exists(fullPath))
        {
            Debug.LogError($"NN model file not found: {fullPath}");
            return false;
        }
        // Reuse the existing loader, which initializes the network
        // and loads weights. An absolute path allows the tournament
        // to select a specific file from nnhistory.
        weightsPath = fullPath;
        loaded = false;
        LoadModel();
        if (!loaded)
        {
            Debug.LogError($"Failed to load NN model: {fullPath}");
            return false;
        }
        return true;
    }
    public void LoadModel()
    {
        if (_network == null) { loaded = false; }
        if (loaded) return;
        // Initialize the custom network
        _network = new RuntimeNeuralNetwork();
        _headOutputs = new Dictionary<string, float[]>();
        // Try multiple paths for flexibility
        string[] possiblePaths = new string[]
        {
            Path.Combine(Application.persistentDataPath, weightsPath),
            Path.Combine(Application.streamingAssetsPath, weightsPath),
            weightsPath  // Absolute path
        };
        bool loadedWeights = false;
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    _network.LoadWeights(path);
                    _lastWeightsPath = path;
                    _lastFileSize = new FileInfo(path).Length;
                    loadedWeights = true;
                    UnityEngine.Debug.Log($"[NN] Loaded weights from: {path}");
                    break;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[NN] Failed to load from {path}: {e.Message}");
                }
            }
        }
        if (!loadedWeights)
        {
            UnityEngine.Debug.LogError($"[NN] Could not find weights file: {weightsPath}. Searched in:" + string.Join("\n", possiblePaths.Select(p => "  - " + p)));
            return;
        }
        loaded = true;
    }

    /// <summary>
    /// Call this from external scripts to force a weight reload (e.g., after training round).
    /// </summary>
    [ContextMenu("Reload Weights")]
    public void ReloadWeights()
    {
        if (_network == null || string.IsNullOrEmpty(_lastWeightsPath)) return;
        try
        {
            _network.LoadWeights(_lastWeightsPath);
            _lastFileSize = new FileInfo(_lastWeightsPath).Length;
            UnityEngine.Debug.Log($"[NN] Weights reloaded from: {_lastWeightsPath}");
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"[NN] Failed to reload weights: {e.Message}");
        }
    }

    private void TryReloadWeights()
    {
        if (string.IsNullOrEmpty(_lastWeightsPath) || !File.Exists(_lastWeightsPath)) return;
        var info = new FileInfo(_lastWeightsPath);
        if (info.Length != _lastFileSize)
        {
            UnityEngine.Debug.Log("[NN] Weight file changed, reloading...");
            ReloadWeights();
        }
    }

    // ============================================
    // MAIN ENTRY: Called by MatchDirector
    // ============================================
    public void RunAIPrepPhase(AutoChessDataManager dataManager)
    {
        if (!loaded || _network == null || _headOutputs == null)
        {
            LoadModel();
        }
        // Setup
        genomeAI.prepManager.SetDataManager(dataManager);
        genomeAI.prepManager.DisableUI();
        genomeAI.shopManager.shopData.GeneratePVPCurrentListing();
        genomeAI.shopManager.PVPRefreshData();
        // Decision loop: neural net chooses actions until Done
        RunDecisionLoop(dataManager);
        // Post-processing: field placement + equipment (genome logic)
        ArrangeFieldAndEquipment(dataManager);
    }

    // ============================================
    // DECISION LOOP
    // ============================================
    void RunDecisionLoop(AutoChessDataManager dataManager)
    {
        int steps = 0;
        int maxSteps = 30;  // Safety cap
        while (steps < maxSteps)
        {
            var stepTimer = System.Diagnostics.Stopwatch.StartNew();
            UpdateSortedLists();
            // Encode current state
            float[] state = stateEncoder.Encode(genomeAI.prepManager, dataManager, genomeAI.shopManager, genomeAI.factionManager);
            bool[] validMask = stateEncoder.BuildValidMask(genomeAI.prepManager, dataManager, genomeAI.shopManager, genomeAI.factionManager);
            // Derive hierarchical masks
            BuildHierarchicalMasks(validMask, out var typeMask, out var buyMask, out var sellMask, out var placeMask, out var fieldMask);
            // Run inference
            int action = PredictAction(state, typeMask, buyMask, sellMask, placeMask, fieldMask, out float oldLogProb, out float oldValue);
            // Record for training (log flat mask + flat action for backwards compat)
            dataManager.encoderLogger.LogDecision(state, validMask, action, oldLogProb, oldValue);
            // Execute
            ExecuteAction(action, dataManager);
            steps++;
            stepTimer.Stop();
            totalStepTime += (float)stepTimer.Elapsed.TotalMilliseconds;
            stepCount++;
            // Stop conditions
            if (action == 0) break;  // Done
            // If only Done is valid, stop
            if (!HasNonDoneValidActions(validMask)) break;
        }
    }
    bool HasNonDoneValidActions(bool[] mask)
    {
        for (int i = 1; i < mask.Length; i++)
            if (mask[i]) return true;
        return false;
    }
    // ============================================
    // HIERARCHICAL MASK BUILDER
    // ============================================
    /// <summary>
    /// Converts the flat 44-element valid mask into the 5 hierarchical masks
    /// expected by the neural network.
    /// </summary>
    void BuildHierarchicalMasks(bool[] flatMask, out bool[] actionTypeMask, out bool[] buyMask, out bool[] sellMask, out bool[] placeMask, out bool[] fieldMask)
    {
        actionTypeMask = new bool[7];
        buyMask = new bool[6];
        sellMask = new bool[12];
        placeMask = new bool[12];
        fieldMask = new bool[11];

        // Root actions that are always terminal (no children)
        actionTypeMask[0] = flatMask[0]; // Done
        actionTypeMask[1] = flatMask[1]; // LevelUp
        actionTypeMask[2] = flatMask[2]; // Reroll

        // Child masks derived directly from flat mask
        for (int i = 0; i < 6; i++)  buyMask[i]   = flatMask[3 + i];
        for (int i = 0; i < 12; i++) sellMask[i]  = flatMask[9 + i];
        for (int i = 0; i < 12; i++) placeMask[i] = flatMask[21 + i];
        for (int i = 0; i < 11; i++) fieldMask[i] = flatMask[33 + i];

        // Parent action types are valid if they have ANY valid child
        actionTypeMask[3] = buyMask.Any(b => b);   // Buy
        actionTypeMask[4] = sellMask.Any(b => b);  // SellBench
        actionTypeMask[5] = placeMask.Any(b => b); // PlaceBench
        actionTypeMask[6] = fieldMask.Any(b => b); // ReturnField
    }
    // ============================================
    // ACTION EXECUTION (unchanged)
    // ============================================
    void ExecuteAction(int action, AutoChessDataManager dataManager)
    {
        switch (action)
        {
            case 0: // Done
                break;
            case 1: // Level Up
                genomeAI.prepManager.BuyExp();
                break;
            case 2: // Reroll
                genomeAI.prepManager.FastRerollShop();
                break;
            case int n when n >= 3 && n <= 8: // Buy Shop Slot 0-5
                int shopSlot = action - 3;
                genomeAI.shopManager.Select(shopSlot);
                int cost = genomeAI.shopManager.SelectedCost();
                if (dataManager.GetGold() >= cost)
                {
                    genomeAI.prepManager.FastBuySelectedActor(cost);
                }
                break;
            case int n when n >= 9 && n <= 20: // Sell Bench Unit 0-11
                int benchIdx = action - 9;
                if (benchIdx < sortedBench.Count)
                {
                    var benchUnit = sortedBench[benchIdx];
                    genomeAI.prepManager.FastSellSelectedActor(benchUnit);
                }
                break;
            case int n when n >= 21 && n <= 32: // Place Bench Unit 0-11 -> Field
                int placeIdx = action - 21;
                if (placeIdx < sortedBench.Count)
                {
                    var unit = sortedBench[placeIdx];
                    int bestTile = genomeAI.aegirPlacement.FindBestTileForRole(unit);
                    if (bestTile >= 0)
                    {
                        unit.SetLocation(bestTile);
                        unit.SetDirection(1);
                        genomeAI.prepManager.MoveFromBenchToField(unit);
                        genomeAI.prepManager.SaveToDataManager();
                    }
                }
                break;
            case int n when n >= 33 && n <= 43: // Return Field Unit 0-10 -> Bench
                int fieldIdx = action - 33;
                if (fieldIdx < sortedField.Count)
                {
                    var unit = sortedField[fieldIdx];
                    int benchLoc = FindEmptyBenchSlot();
                    if (benchLoc >= 0)
                    {
                        unit.SetLocation(benchLoc);
                        genomeAI.prepManager.MoveFromFieldToBench(unit);
                        genomeAI.prepManager.SaveToDataManager();
                        break;
                    }
                    bool benchFull = genomeAI.prepManager.benchSlots.Count >= genomeAI.prepManager.GetMaxBenchSlots();
                    bool fieldFull = genomeAI.prepManager.fieldSlots.Count >= genomeAI.prepManager.GetMaxFieldSlots();
                    // Full bench + full field = first half of a swap.
                    if (benchFull && fieldFull)
                    {
                        genomeAI.prepManager.MoveFromFieldToBench(unit);
                        genomeAI.prepManager.SaveToDataManager();
                    }
                }
                break;
        }
    }
    int FindEmptyBenchSlot()
    {
        return genomeAI.prepManager.AvailableBenchSlot();
    }
    // ============================================
    // POST-PROCESSING (unchanged)
    // ============================================
    void ArrangeFieldAndEquipment(AutoChessDataManager dataManager)
    {
        PlaceFieldUnitsGeneric();
        genomeAI.equipmentController.GenericAutoPlaceEquipment(genomeAI);
        genomeAI.aegirPlacement.AegirPlaceFieldUnits();
        genomeAI.prepManager.StartBattle();
    }
    void PlaceFieldUnitsGeneric()
    {
        foreach (var unit in genomeAI.prepManager.fieldSlots.ToList())
        {
            unit.SetLocation(-1);
        }
        foreach (var unit in genomeAI.prepManager.fieldSlots.ToList())
        {
            int bestTile = genomeAI.aegirPlacement.FindBestTileForRole(unit);
            if (bestTile >= 0)
            {
                unit.SetLocation(bestTile);
                unit.SetDirection(1);
            }
        }
        genomeAI.prepManager.SaveToDataManager();
    }
    // ============================================
    // INFERENCE — HIERARCHICAL POLICY
    // ============================================
    int PredictAction(float[] state, bool[] actionTypeMask, bool[] buyMask, bool[] sellMask, bool[] placeMask, bool[] fieldMask, out float oldLogProb, out float oldValue)
    {
        if (!loaded || _network == null || _headOutputs == null)
        {
            LoadModel();
        }
        var inferenceTimer = System.Diagnostics.Stopwatch.StartNew();
        // Run forward pass — all heads computed in one call
        _network.Forward(state, _headOutputs);
        inferenceTimer.Stop();
        totalInferenceTime += (float)inferenceTimer.Elapsed.TotalMilliseconds;
        inferenceCount++;
        oldLogProb = 0f;
        oldValue = _headOutputs["value"][0];
        // PPO must use temperature 1 and no Dirichlet noise
        // so Python can reproduce the behavior probabilities.
        float samplingTemperature = ppoCollectionMode ? 1f : temperature;
        float[] typeLogits = _headOutputs["policy_type"];
        float[] typeProbs = ApplyTemperatureAndSoftmax(typeLogits, actionTypeMask, samplingTemperature);
        int actionType = 0;
        if (ppoCollectionMode || temperature > 0f)
        {
            actionType = SampleCategorical(typeProbs, actionTypeMask, !ppoCollectionMode);
        }
        // 1. Sample action type from masked policy
        else
        {
            actionType = ArgMax(typeLogits, actionTypeMask);
        }
        if (actionType < 0)
        return 0;
        oldLogProb = Mathf.Log(Mathf.Max(typeProbs[actionType], 1e-30f));
        // 2. Route to child head and sample child index
        float[] childLogits;
        bool[] childMask;
        int flatOffset;
        switch (actionType)
        {
            case 0: return 0; // Done
            case 1: return 1; // LevelUp
            case 2: return 2; // Reroll
            case 3: // Buy
                childLogits = _headOutputs["buy"];
                childMask = buyMask;
                flatOffset = 3;
                break;
            case 4: // SellBench
                childLogits = _headOutputs["sell"];
                childMask = sellMask;
                flatOffset = 9;
                break;
            case 5: // PlaceBench
                childLogits = _headOutputs["place"];
                childMask = placeMask;
                flatOffset = 21;
                break;
            case 6: // ReturnField
                childLogits = _headOutputs["field"];
                childMask = fieldMask;
                flatOffset = 33;
                break;
            default:
                return 0; // Fallback to Done
        }
        float[] childProbs = ApplyTemperatureAndSoftmax(childLogits, childMask, samplingTemperature);
        int childIndex;
        if (ppoCollectionMode || temperature > 0f)
        {
            childIndex = SampleCategorical(childProbs, childMask, !ppoCollectionMode);
        }
        else
        {
            childIndex = ArgMax(childLogits, childMask);
        }
        if (childIndex < 0)
            return 0;
        oldLogProb += Mathf.Log(Mathf.Max(childProbs[childIndex], 1e-30f));
        return flatOffset + childIndex;
    }
    // ============================================
    // MASKING & SAMPLING UTILITIES
    // ============================================
    float[] ApplyMask(float[] logits, bool[] mask)
    {
        float[] masked = new float[logits.Length];
        for (int i = 0; i < logits.Length; i++)
            masked[i] = mask[i] ? logits[i] : float.NegativeInfinity;
        return masked;
    }

    float[] ApplyTemperatureAndSoftmax(float[] logits, bool[] mask, float temperatureOverride = -1f)
    {
        float[] probs = new float[logits.Length];
        float effectiveTemperature = temperatureOverride > 0f ? temperatureOverride : temperature;
        effectiveTemperature = Mathf.Max(0.0001f, effectiveTemperature);
        // Apply temperature
        float maxLogit = float.NegativeInfinity;
        for (int i = 0; i < logits.Length; i++)
        {
            if (!mask[i])
            {
                probs[i] = 0f;
                continue;
            }
            probs[i] = logits[i] / effectiveTemperature;
            if (probs[i] > maxLogit)
                maxLogit = probs[i];
        }
        // Stable softmax
        float sum = 0f;
        for (int i = 0; i < logits.Length; i++)
        {
            if (mask[i])
            {
                probs[i] = Mathf.Exp(probs[i] - maxLogit);
                sum += probs[i];
            }
        }
        if (sum > 0f)
        {
            for (int i = 0; i < logits.Length; i++)
            {
                if (mask[i])
                {
                    probs[i] /= sum;
                }
            }
        }
        return probs;
    }

    // ============================================
    // EXPLORATION & SAMPLING (unchanged logic)
    // ============================================
    float[] AddDirichletNoise(float[] probs, bool[] validMask)
    {
        int validCount = validMask.Count(b => b);
        float[] noise = new float[probs.Length];
        float[] alpha = new float[validCount];
        for (int i = 0; i < validCount; i++) alpha[i] = dirichletAlpha;
        float[] gamma = new float[validCount];
        float gammaSum = 0f;
        for (int i = 0; i < validCount; i++)
        {
            gamma[i] = RandomGamma(alpha[i]);
            gammaSum += gamma[i];
        }
        int idx = 0;
        for (int i = 0; i < probs.Length; i++)
        {
            if (validMask[i])
            {
                noise[i] = gamma[idx] / gammaSum;
                idx++;
            }
        }
        float[] blended = new float[probs.Length];
        float sum = 0f;
        for (int i = 0; i < probs.Length; i++)
        {
            if (validMask[i])
            {
                blended[i] = (1 - noiseWeight) * probs[i] + noiseWeight * noise[i];
                sum += blended[i];
            }
        }
        for (int i = 0; i < probs.Length; i++)
            if (validMask[i]) blended[i] /= sum;
        return blended;
    }

    float RandomGamma(float shape)
    {
        if (shape < 1f)
        {
            float u = Random.value;
            return RandomGamma(shape + 1f) * Mathf.Pow(u, 1f / shape);
        }
        float d = shape - 1f / 3f;
        float c = 1f / Mathf.Sqrt(9f * d);
        while (true)
        {
            float x = RandomNormal();
            float v = 1f + c * x;
            if (v <= 0f) { continue; }
            v = v * v * v;
            float u = Random.value;
            if (u < 1f - 0.0331f * x * x * x * x) { return d * v; }
            if (Mathf.Log(u) < 0.5f * x * x + d * (1f - v + Mathf.Log(v))) { return d * v; }
        }
    }

    float RandomNormal()
    {
        float u1 = 1f - Random.value;
        float u2 = 1f - Random.value;
        return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
    }

    int SampleCategorical(float[] probs, bool[] validMask, bool allowNoise = true)
    {
        // Add Dirichlet noise if enabled
        if (allowNoise && addExplorationNoise)
            probs = AddDirichletNoise(probs, validMask);
        float r = Random.value;
        float cumsum = 0f;
        for (int i = 0; i < probs.Length; i++)
        {
            if (!validMask[i]) continue;
            cumsum += probs[i];
            if (r <= cumsum) return i;
        }
        return ArgMax(probs, validMask);
    }

    int ArgMax(float[] probs, bool[] validMask)
    {
        int best = -1;
        float bestProb = -1f;
        for (int i = 0; i < probs.Length; i++)
        {
            if (!validMask[i]) continue;
            if (probs[i] > bestProb)
            {
                bestProb = probs[i];
                best = i;
            }
        }
        return best;
    }

    void UpdateSortedLists()
    {
        sortedBench = genomeAI.prepManager.benchSlots.OrderByDescending(u => int.Parse(genomeAI.unitRarity.ReturnValue(u.GetName()))).ToList();
        sortedField = genomeAI.prepManager.fieldSlots.OrderByDescending(u => int.Parse(genomeAI.unitRarity.ReturnValue(u.GetName()))).ToList();
    }
}