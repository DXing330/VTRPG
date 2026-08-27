using System;
using System.IO;
using UnityEngine;

public class RuntimeNeuralNetwork
{
    [Serializable]
    public class Layer
    {
        public float[][] weights;
        public float[] biases;
        public bool hasLayerNorm;
        public float[] lnGamma;
        public float[] lnBeta;
        public float lnEpsilon;
        public ActivationType activation;
        [NonSerialized] public float[] outputCache;
        [NonSerialized] public float[] preActivationCache;
    }

    public enum ActivationType { ReLU = 0, Sigmoid = 1, Tanh = 2, Softmax = 3, None = 4 }

    public Layer[] layers;
    public int InputSize => layers[0].weights[0].Length;
    public int OutputSize => layers[layers.Length - 1].weights.Length;

    private float[] _currentInput;
    private float[] _currentOutput;

    public void LoadWeights(string binaryPath)
    {
        int numLayers = 0;
        using (var fs = new FileStream(binaryPath, FileMode.Open, FileAccess.Read))
        using (var reader = new BinaryReader(fs))
        {
            numLayers = reader.ReadInt32();
            layers = new Layer[numLayers];

            for (int i = 0; i < numLayers; i++)
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

                layers[i] = layer;
            }

            for (int i = 0; i < numLayers; i++)
                layers[i].activation = (ActivationType)reader.ReadByte();
        }

        foreach (var layer in layers)
        {
            int outSize = layer.weights.Length;
            layer.outputCache = new float[outSize];
            layer.preActivationCache = new float[outSize];
        }

        _currentInput = new float[InputSize];
        _currentOutput = new float[OutputSize];

        Debug.Log($"[RuntimeNN] Loaded {numLayers} layers. Input: {InputSize}, Output: {OutputSize}");
    }

    public void Forward(float[] input, float[] output)
    {
        int copyLen = input.Length < _currentInput.Length ? input.Length : _currentInput.Length;
        for (int i = 0; i < copyLen; i++) _currentInput[i] = input[i];

        float[] current = _currentInput;

        for (int l = 0; l < layers.Length; l++)
        {
            var layer = layers[l];
            int outSize = layer.weights.Length;
            int inSize = layer.weights[0].Length;

            if (inSize >= 8)
            {
                for (int j = 0; j < outSize; j++)
                {
                    float sum = layer.biases != null ? layer.biases[j] : 0f;
                    float[] wRow = layer.weights[j];
                    int k = 0;
                    for (; k <= inSize - 8; k += 8)
                    {
                        sum += current[k] * wRow[k]
                             + current[k+1] * wRow[k+1]
                             + current[k+2] * wRow[k+2]
                             + current[k+3] * wRow[k+3]
                             + current[k+4] * wRow[k+4]
                             + current[k+5] * wRow[k+5]
                             + current[k+6] * wRow[k+6]
                             + current[k+7] * wRow[k+7];
                    }
                    for (; k < inSize; k++) sum += current[k] * wRow[k];
                    layer.preActivationCache[j] = sum;
                }
            }
            else
            {
                for (int j = 0; j < outSize; j++)
                {
                    float sum = layer.biases != null ? layer.biases[j] : 0f;
                    float[] wRow = layer.weights[j];
                    for (int k = 0; k < inSize; k++) sum += current[k] * wRow[k];
                    layer.preActivationCache[j] = sum;
                }
            }

            if (layer.hasLayerNorm) ApplyLayerNorm(layer, outSize);
            ApplyActivation(layer, outSize);
            current = layer.outputCache;
        }

        int outLen = current.Length < output.Length ? current.Length : output.Length;
        for (int i = 0; i < outLen; i++) output[i] = current[i];
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

    void ApplyActivation(Layer layer, int size)
    {
        switch (layer.activation)
        {
            case ActivationType.ReLU:
                for (int j = 0; j < size; j++)
                {
                    float v = layer.preActivationCache[j];
                    layer.outputCache[j] = v > 0f ? v : 0f;
                }
                break;
            case ActivationType.Sigmoid:
                for (int j = 0; j < size; j++)
                    layer.outputCache[j] = 1f / (1f + Mathf.Exp(-layer.preActivationCache[j]));
                break;
            case ActivationType.Tanh:
                for (int j = 0; j < size; j++)
                {
                    float x = layer.preActivationCache[j];
                    float ePos = Mathf.Exp(x);
                    float eNeg = Mathf.Exp(-x);
                    layer.outputCache[j] = (ePos - eNeg) / (ePos + eNeg);
                }
                break;
            case ActivationType.Softmax:
                float maxVal = float.NegativeInfinity;
                for (int j = 0; j < size; j++)
                    if (layer.preActivationCache[j] > maxVal) maxVal = layer.preActivationCache[j];
                float expSum = 0f;
                for (int j = 0; j < size; j++)
                {
                    layer.outputCache[j] = Mathf.Exp(layer.preActivationCache[j] - maxVal);
                    expSum += layer.outputCache[j];
                }
                for (int j = 0; j < size; j++) layer.outputCache[j] /= expSum;
                break;
            case ActivationType.None:
            default:
                for (int j = 0; j < size; j++) layer.outputCache[j] = layer.preActivationCache[j];
                break;
        }
    }

    public float[] Forward(float[] input)
    {
        Forward(input, _currentOutput);
        float[] result = new float[OutputSize];
        for (int i = 0; i < OutputSize; i++) result[i] = _currentOutput[i];
        return result;
    }
}