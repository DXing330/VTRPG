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
        float[] testState = new float[1619];
        for (int i = 0; i < 1619; i++)
            testState[i] = (i % 10) / 10f;  // 0.0, 0.1, 0.2, ... 0.9, repeat
        bool[] testMask = new bool[44];
        for (int i = 0; i < 44; i++)
            testMask[i] = true;
        // Run inference
        int action = PredictAction(testState, testMask);
        // Log raw logits for inspection
        _network.Forward(testState, _outputBuffer);
        string logits = string.Join(", ", _outputBuffer.Select(x => x.ToString("F4")));
        Debug.Log($"Test Action: {action}");
        Debug.Log($"Raw Logits: {logits}");
    }
    public AutoChessAIPrepController genomeAI;
    public AutoChessStateEncoder stateEncoder;
    public string modelTag = "v1";
    // ============================================
    // NEW: Runtime weight loading — no rebuilds!
    // ============================================
    [Tooltip("Path to binary weights file (relative to StreamingAssets or PersistentDataPath)")]
    public string weightsPath = "autochess_policy_weights.bin";
    [Tooltip("If true, watches the weights file and auto-reloads when it changes")]
    public bool autoReloadWeights = true;
    [Tooltip("How often to check for weight file changes (seconds)")]
    public float reloadCheckInterval = 2f;
    // The custom neural network — replaces ONNX InferenceSession
    private RuntimeNeuralNetwork _network;
    private float[] _outputBuffer;  // Pre-allocated output buffer
    private string _lastWeightsPath;
    private long _lastFileSize = -1;
    private float _reloadTimer = 0f;
    // ============================================
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
        LoadModel();
        ResetTimers();
    }

    void Update()
    {
        // Auto-reload weights if file changed (for rapid iteration)
        if (autoReloadWeights && _network != null)
        {
            _reloadTimer += Time.deltaTime;
            if (_reloadTimer >= reloadCheckInterval)
            {
                _reloadTimer = 0f;
                TryReloadWeights();
            }
        }
    }

    // Only Load Fully Once.
    public bool loaded = false;

    public void LoadModel()
    {
        if (loaded) return;

        // Initialize the custom network
        _network = new RuntimeNeuralNetwork();

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
        // Pre-allocate output buffer
        _outputBuffer = new float[_network.OutputSize];
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

            // Run inference
            int action = PredictAction(state, validMask);

            // Record for training
            dataManager.encoderLogger.LogDecision(state, validMask, action);

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
    // INFERENCE — REPLACED ONNX with custom network
    // ============================================
    int PredictAction(float[] state, bool[] validMask)
    {
        var inferenceTimer = System.Diagnostics.Stopwatch.StartNew();

        // Run forward pass through our custom network
        _network.Forward(state, _outputBuffer);

        inferenceTimer.Stop();
        totalInferenceTime += (float)inferenceTimer.Elapsed.TotalMilliseconds;
        inferenceCount++;

        // _outputBuffer now contains raw logits (same as ONNX output)
        float[] logits = _outputBuffer;

        // Apply mask and temperature (identical logic to before)
        float[] probs = new float[44];
        float maxLogit = float.NegativeInfinity;

        for (int i = 0; i < 44; i++)
        {
            if (!validMask[i])
            {
                probs[i] = float.NegativeInfinity;
            }
            else
            {
                probs[i] = logits[i] / temperature;
                if (probs[i] > maxLogit) maxLogit = probs[i];
            }
        }

        // Stable softmax
        float sum = 0f;
        for (int i = 0; i < 44; i++)
        {
            if (validMask[i])
            {
                probs[i] = Mathf.Exp(probs[i] - maxLogit);
                sum += probs[i];
            }
        }
        for (int i = 0; i < 44; i++)
            if (validMask[i]) probs[i] /= sum;

        // Add Dirichlet noise for exploration (self-play only)
        if (addExplorationNoise)
        {
            probs = AddDirichletNoise(probs, validMask);
        }

        // Sample or argmax
        if (addExplorationNoise)
            return SampleCategorical(probs, validMask);
        else
            return ArgMax(probs, validMask);
    }

    // ============================================
    // UTILITY METHODS (unchanged)
    // ============================================
    float[] AddDirichletNoise(float[] probs, bool[] validMask)
    {
        int validCount = validMask.Count(b => b);
        float[] noise = new float[44];

        float[] alpha = new float[validCount];
        for (int i = 0; i < validCount; i++) alpha[i] = dirichletAlpha;

        float[] gamma = new float[validCount];
        float gammaSum = 0f;
        for (int i = 0; i < validCount; i++)
        {
            gamma[i] = RandomGamma(alpha[i], 1f);
            gammaSum += gamma[i];
        }

        int idx = 0;
        for (int i = 0; i < 44; i++)
        {
            if (validMask[i])
            {
                noise[i] = gamma[idx] / gammaSum;
                idx++;
            }
        }

        float[] blended = new float[44];
        float sum = 0f;
        for (int i = 0; i < 44; i++)
        {
            if (validMask[i])
            {
                blended[i] = (1 - noiseWeight) * probs[i] + noiseWeight * noise[i];
                sum += blended[i];
            }
        }
        for (int i = 0; i < 44; i++)
            if (validMask[i]) blended[i] /= sum;

        return blended;
    }

    float RandomGamma(float shape, float scale)
    {
        float u = Random.value;
        return -Mathf.Log(u) * scale;
    }

    int SampleCategorical(float[] probs, bool[] validMask)
    {
        float r = Random.value;
        float cumsum = 0f;
        for (int i = 0; i < 44; i++)
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
        for (int i = 0; i < 44; i++)
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