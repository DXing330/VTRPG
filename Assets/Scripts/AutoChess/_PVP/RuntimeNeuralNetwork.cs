using System;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Hierarchical Neural Network Runtime for Unity.
/// Supports a shared encoder with multiple branch heads.
/// Designed for policy networks with expandable head architecture.
/// </summary>
public class RuntimeNeuralNetwork
{
    // ============================================================
    // MAGIC HEADER & VERSION
    // ============================================================
    private const uint MAGIC = 0x314E4E48; // "HNN1" in little-endian
    private const uint CURRENT_VERSION = 1;

    // ============================================================
    // LAYER DEFINITION
    // ============================================================
    [Serializable]
    public class Layer
    {
        public float[][] weights;      // [outFeatures][inFeatures]
        public float[] biases;         // [outFeatures] or null
        public bool hasLayerNorm;
        public float[] lnGamma;        // [outFeatures]
        public float[] lnBeta;         // [outFeatures]
        public float lnEpsilon;
        public ActivationType activation;
        [NonSerialized] public float[] outputCache;
        [NonSerialized] public float[] preActivationCache;
    }
    [Serializable]
    public class Head
    {
        public string name;
        public int outputSize;
        public Layer layer;
    }
    public enum ActivationType { ReLU = 0, Sigmoid = 1, Tanh = 2, Softmax = 3, None = 4 }
    // ============================================================
    // NETWORK STATE
    // ============================================================
    public Layer[] encoder;
    public Head[] heads;
    
    public int InputSize => encoder[0].weights[0].Length;
    public int EncoderOutputSize { get; private set; }

    // Pre-allocated buffers
    private float[] _currentInput;
    private float[] _encodedOutput;
    private float[] _currentOutput;
    private float[] _tempOutput;

    // Head output cache for re-use
    private float[][] _headOutputBuffers;

    // ============================================================
    // BINARY LOADER
    // ============================================================
    public void LoadWeights(string binaryPath)
    {
        int numEncoderLayers = 0;
        int numHeads = 0;
        using (var fs = new FileStream(binaryPath, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(fs))
        {
            // --- Header ---
            uint magic = reader.ReadUInt32();
            if (magic != MAGIC)
                throw new InvalidDataException($"Invalid magic header. Expected 0x{MAGIC:X8}, got 0x{magic:X8}. Is this a flat model file?");
            uint version = reader.ReadUInt32();
            if (version != CURRENT_VERSION)
                throw new InvalidDataException($"Unsupported version {version}. Expected {CURRENT_VERSION}.");
            numEncoderLayers = reader.ReadInt32();
            numHeads = reader.ReadInt32();
            EncoderOutputSize = reader.ReadInt32();
            // --- Encoder ---
            encoder = new Layer[numEncoderLayers];
            for (int i = 0; i < numEncoderLayers; i++)
                encoder[i] = ReadLayer(reader);
            // --- Heads ---
            heads = new Head[numHeads];
            for (int i = 0; i < numHeads; i++)
            {
                int nameLen = reader.ReadInt32();
                if (nameLen < 0 || nameLen > 1000)
                    throw new InvalidDataException(
                        $"Invalid head name length: {nameLen}");
                string name = Encoding.UTF8.GetString(
                    reader.ReadBytes(nameLen)
                );
                var layer = ReadLayer(reader);
                heads[i] = new Head
                {
                    name = name,
                    outputSize = layer.weights.Length,
                    layer = layer
                };
            }
            // --- Activations ---
            for (int i = 0; i < numEncoderLayers; i++)
                encoder[i].activation = (ActivationType)reader.ReadByte();
            for (int i = 0; i < numHeads; i++)
                heads[i].layer.activation = (ActivationType)reader.ReadByte();
        }
        // --- Allocate caches ---
        foreach (var layer in encoder)
        {
            int outSize = layer.weights.Length;
            layer.outputCache = new float[outSize];
            layer.preActivationCache = new float[outSize];
        }
        foreach (var head in heads)
        {
            int outSize = head.layer.weights.Length;
            head.layer.outputCache = new float[outSize];
            head.layer.preActivationCache = new float[outSize];
        }
        _currentInput = new float[InputSize];
        _encodedOutput = new float[EncoderOutputSize];
        _currentOutput = new float[EncoderOutputSize];
        _tempOutput = new float[EncoderOutputSize];
        _headOutputBuffers = new float[heads.Length][];
        for (int i = 0; i < heads.Length; i++)
            _headOutputBuffers[i] = new float[heads[i].outputSize];
        Debug.Log($"[RuntimeNN] Loaded v{CURRENT_VERSION} hierarchical model. " +
                  $"Encoder: {numEncoderLayers} layers ({InputSize} → {EncoderOutputSize}). " +
                  $"Heads: {string.Join(", ", Array.ConvertAll(heads, h => $"{h.name}({h.outputSize})"))}");
    }
    private Layer ReadLayer(BinaryReader reader)
    {
        int outFeatures = reader.ReadInt32();
        int inFeatures = reader.ReadInt32();
        int hasBias = reader.ReadInt32();
        int hasLayerNorm = reader.ReadInt32();

        var layer = new Layer
        {
            weights = new float[outFeatures][],
            biases = hasBias == 1 ? new float[outFeatures] : null,
            hasLayerNorm = hasLayerNorm == 1,
        };

        for (int r = 0; r < outFeatures; r++)
        {
            layer.weights[r] = new float[inFeatures];
            for (int c = 0; c < inFeatures; c++)
                layer.weights[r][c] = reader.ReadSingle();
        }

        if (hasBias == 1)
            for (int j = 0; j < outFeatures; j++)
                layer.biases[j] = reader.ReadSingle();

        if (layer.hasLayerNorm)
        {
            int lnSize = reader.ReadInt32();
            layer.lnEpsilon = reader.ReadSingle();
            layer.lnGamma = new float[lnSize];
            layer.lnBeta = new float[lnSize];
            for (int j = 0; j < lnSize; j++) layer.lnGamma[j] = reader.ReadSingle();
            for (int j = 0; j < lnSize; j++) layer.lnBeta[j] = reader.ReadSingle();
        }

        return layer;
    }

    // ============================================================
    // FORWARD PASS
    // ============================================================

    /// <summary>
    /// Runs the shared encoder and all heads. 
    /// Results are written into the provided outputs dictionary keyed by head name.
    /// </summary>
    public void Forward(float[] input, System.Collections.Generic.Dictionary<string, float[]> outputs)
    {
        // Copy input with truncation protection
        int copyLen = Mathf.Min(input.Length, _currentInput.Length);
        for (int i = 0; i < copyLen; i++) _currentInput[i] = input[i];
        for (int i = copyLen; i < _currentInput.Length; i++) _currentInput[i] = 0f;

        // Run encoder
        float[] current = _currentInput;
        for (int l = 0; l < encoder.Length; l++)
        {
            current = RunLayer(encoder[l], current);
        }

        // Cache encoded representation
        for (int i = 0; i < EncoderOutputSize; i++)
            _encodedOutput[i] = current[i];

        // Run all heads
        for (int h = 0; h < heads.Length; h++)
        {
            var head = heads[h];
            RunLayer(head.layer, _encodedOutput, _headOutputBuffers[h]);
            outputs[head.name] = _headOutputBuffers[h];
        }
    }

    /// <summary>
    /// Convenience: runs forward and returns a specific head's output directly.
    /// </summary>
    public float[] ForwardHead(float[] input, string headName)
    {
        var outputs = new System.Collections.Generic.Dictionary<string, float[]>();
        Forward(input, outputs);
        if (!outputs.TryGetValue(headName, out float[] result))
            throw new ArgumentException($"Head '{headName}' not found. Available: {string.Join(", ", Array.ConvertAll(heads, h => h.name))}");
        return result;
    }

    // ============================================================
    // LAYER EXECUTION
    // ============================================================
    private float[] RunLayer(Layer layer, float[] input, float[] outputBuffer = null)
    {
        int outSize = layer.weights.Length;
        int inSize = layer.weights[0].Length;
        float[] target = outputBuffer ?? layer.outputCache;

        // Optimized matmul with 8-wide unroll
        if (inSize >= 8)
        {
            for (int j = 0; j < outSize; j++)
            {
                float sum = layer.biases != null ? layer.biases[j] : 0f;
                float[] wRow = layer.weights[j];
                int k = 0;
                for (; k <= inSize - 8; k += 8)
                {
                    sum += input[k] * wRow[k] + input[k+1] * wRow[k+1] + input[k+2] * wRow[k+2] + input[k+3] * wRow[k+3] + input[k+4] * wRow[k+4] + input[k+5] * wRow[k+5] + input[k+6] * wRow[k+6] + input[k+7] * wRow[k+7];
                }
                for (; k < inSize; k++) sum += input[k] * wRow[k];
                layer.preActivationCache[j] = sum;
            }
        }
        else
        {
            for (int j = 0; j < outSize; j++)
            {
                float sum = layer.biases != null ? layer.biases[j] : 0f;
                float[] wRow = layer.weights[j];
                for (int k = 0; k < inSize; k++) sum += input[k] * wRow[k];
                layer.preActivationCache[j] = sum;
            }
        }

        if (layer.hasLayerNorm) ApplyLayerNorm(layer, outSize);
        ApplyActivation(layer, outSize, target);
        return target;
    }

    void ApplyLayerNorm(Layer layer, int size)
    {
        float mean = 0f;
        for (int j = 0; j < size; j++) mean += layer.preActivationCache[j];
        mean /= size;
        float var = 0f;
        for (int j = 0; j < size; j++)
        {
            float diff = layer.preActivationCache[j] - mean;
            var += diff * diff;
        }
        var /= size;
        float invStd = 1f / Mathf.Sqrt(var + layer.lnEpsilon);
        for (int j = 0; j < size; j++)
        {
            float normalized = (layer.preActivationCache[j] - mean) * invStd;
            layer.preActivationCache[j] = normalized * layer.lnGamma[j] + layer.lnBeta[j];
        }
    }

    void ApplyActivation(Layer layer, int size, float[] target)
    {
        switch (layer.activation)
        {
            case ActivationType.ReLU:
                for (int j = 0; j < size; j++)
                {
                    float v = layer.preActivationCache[j];
                    target[j] = v > 0f ? v : 0f;
                }
                break;
            case ActivationType.Sigmoid:
                for (int j = 0; j < size; j++)
                    target[j] = 1f / (1f + Mathf.Exp(-layer.preActivationCache[j]));
                break;
            case ActivationType.Tanh:
                for (int j = 0; j < size; j++)
                {
                    float x = layer.preActivationCache[j];
                    float ePos = Mathf.Exp(x);
                    float eNeg = Mathf.Exp(-x);
                    target[j] = (ePos - eNeg) / (ePos + eNeg);
                }
                break;
            case ActivationType.Softmax:
                float maxVal = float.NegativeInfinity;
                for (int j = 0; j < size; j++)
                    if (layer.preActivationCache[j] > maxVal) maxVal = layer.preActivationCache[j];
                float expSum = 0f;
                for (int j = 0; j < size; j++)
                {
                    target[j] = Mathf.Exp(layer.preActivationCache[j] - maxVal);
                    expSum += target[j];
                }
                for (int j = 0; j < size; j++) target[j] /= expSum;
                break;
            case ActivationType.None:
            default:
                for (int j = 0; j < size; j++) target[j] = layer.preActivationCache[j];
                break;
        }
    }

    // ============================================================
    // LEGACY FLAT-MODEL LOADER (for backwards compatibility)
    // ============================================================
    /// <summary>
    /// Loads the old flat sequential format and treats it as a single-head model.
    /// The single head is named "policy_flat".
    /// </summary>
    public void LoadWeightsLegacy(string binaryPath)
    {
        int numLayers = 0;
        using (var fs = new FileStream(binaryPath, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(fs))
        {
            numLayers = reader.ReadInt32();
            encoder = new Layer[numLayers];
            for (int i = 0; i < numLayers; i++)
                encoder[i] = ReadLayer(reader);

            for (int i = 0; i < numLayers; i++)
                encoder[i].activation = (ActivationType)reader.ReadByte();
        }

        // Treat entire network as encoder with one flat head
        EncoderOutputSize = encoder[numLayers - 1].weights.Length;
        
        // Remove last layer from encoder, make it the head
        Layer lastLayer = encoder[numLayers - 1];
        Array.Resize(ref encoder, numLayers - 1);

        heads = new Head[1];
        heads[0] = new Head
        {
            name = "policy_flat",
            outputSize = lastLayer.weights.Length,
            layer = lastLayer
        };

        // Allocate caches
        foreach (var layer in encoder)
        {
            int outSize = layer.weights.Length;
            layer.outputCache = new float[outSize];
            layer.preActivationCache = new float[outSize];
        }
        heads[0].layer.outputCache = new float[heads[0].outputSize];
        heads[0].layer.preActivationCache = new float[heads[0].outputSize];

        _currentInput = new float[InputSize];
        _encodedOutput = new float[EncoderOutputSize];
        _headOutputBuffers = new float[1][];
        _headOutputBuffers[0] = new float[heads[0].outputSize];

        Debug.Log($"[RuntimeNN] Loaded legacy flat model ({numLayers} layers, {heads[0].outputSize} outputs) as single-head 'policy_flat'.");
    }
}