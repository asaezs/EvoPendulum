using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NeuralNetwork
{
    public int[] layers;
    public List<float[]> biases;
    public List<float[,]> weights;
    private List<float[]> neurons;

    // Constructor Original (Gen 0)
    public NeuralNetwork(int[] layers)
    {
        this.layers = (int[])layers.Clone();

        // Inicializar
        neurons = new List<float[]>();
        biases = new List<float[]>();
        weights = new List<float[,]>();

        // Crear las capas de neuronas y sesgos
        for (int i = 0; i < layers.Length; i++)
        {
            neurons.Add(new float[layers[i]]);
            biases.Add(new float[layers[i]]);
        }

        // Crear las matrices de pesos
        for (int i = 0; i < layers.Length - 1; i++)
        {
            weights.Add(new float[layers[i + 1], layers[i]]);
        }

        // Inicializar los pesos y sesgos con valores aleatorios
        for (int i = 0; i < biases.Count; i++)
        {
            for (int j = 0; j < biases[i].Length; j++)
            {
                biases[i][j] = Random.Range(-1.0f, 1.0f);
            }
        }

        for (int i = 0; i < weights.Count; i++)
        {
            for (int j = 0; j < weights[i].GetLength(0); j++)
            {
                for (int k = 0; k < weights[i].GetLength(1); k++)
                {
                    weights[i][j, k] = Random.Range(-1.0f, 1.0f);
                }
            }
        }
    }

    // Constructor de Copia (para Elitismo)
    // (Crea una copia idéntica de un cerebro existente)
    public NeuralNetwork(NeuralNetwork copy)
{
    this.layers = (int[])copy.layers.Clone();

    neurons = new List<float[]>();
    biases = new List<float[]>();
    weights = new List<float[,]>();

    // Inicializar las neuronas
    for (int i = 0; i < this.layers.Length; i++)
    {
        neurons.Add(new float[this.layers[i]]);
    }

    // Copiar los sesgos
    for (int i = 0; i < copy.biases.Count; i++)
    {
        this.biases.Add((float[])copy.biases[i].Clone());
    }

    // Copiar los pesos
    for (int i = 0; i < copy.weights.Count; i++)
    {
        this.weights.Add((float[,])copy.weights[i].Clone());
    }
}

    // Constructor de Cruce
    public NeuralNetwork(NeuralNetwork parentA, NeuralNetwork parentB)
    {
        this.layers = (int[])parentA.layers.Clone();

        neurons = new List<float[]>();
        biases = new List<float[]>();
        weights = new List<float[,]>();

        // Inicializar las neuronas
        for (int i = 0; i < this.layers.Length; i++)
        {
            neurons.Add(new float[this.layers[i]]);
        }

        // Cruce de Sesgos
        for (int i = 0; i < parentA.biases.Count; i++)
        {
            biases.Add(new float[parentA.biases[i].Length]);
            for (int j = 0; j < parentA.biases[i].Length; j++)
            {
                // 50% de probabilidad de heredar el gen (bias) del padre A o del padre B
                biases[i][j] = (Random.value < 0.5f)
                    ? parentA.biases[i][j]
                    : parentB.biases[i][j];
            }
        }

        // Cruce de Pesos
        for (int i = 0; i < parentA.weights.Count; i++)
        {
            weights.Add(new float[parentA.weights[i].GetLength(0), parentA.weights[i].GetLength(1)]);
            for (int j = 0; j < parentA.weights[i].GetLength(0); j++)
            {
                for (int k = 0; k < parentA.weights[i].GetLength(1); k++)
                {
                    // 50% de probabilidad de heredar el gen (peso) del padre A o del padre B
                    weights[i][j, k] = (Random.value < 0.5f)
                        ? parentA.weights[i][j, k]
                        : parentB.weights[i][j, k];
                }
            }
        }
    }

    // Método de Mutación
    public void Mutate(float mutationRate, float mutationStrength)
    {
        // Mutar Sesgos
        for (int i = 0; i < biases.Count; i++)
        {
            for (int j = 0; j < biases[i].Length; j++)
            {
                if (Random.value < mutationRate) // Si muta
                {
                    biases[i][j] += Random.Range(-mutationStrength, mutationStrength);
                }
            }
        }

        // Mutar Pesos
        for (int i = 0; i < weights.Count; i++)
        {
            for (int j = 0; j < weights[i].GetLength(0); j++)
            {
                for (int k = 0; k < weights[i].GetLength(1); k++)
                {
                    if (Random.value < mutationRate)
                    {
                        weights[i][j, k] += Random.Range(-mutationStrength, mutationStrength);
                    }
                }
            }
        }
    }

    public float[] FeedForward(float[] inputs)
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            neurons[0][i] = inputs[i];
        }

        for (int i = 1; i < layers.Length; i++)
        {
            for (int j = 0; j < neurons[i].Length; j++)
            {
                float sum = 0;
                for (int k = 0; k < neurons[i - 1].Length; k++)
                {
                    sum += neurons[i - 1][k] * weights[i - 1][j, k];
                }
                neurons[i][j] = Activation(sum + biases[i][j]);
            }
        }
        return neurons[neurons.Count - 1];
    }

    private float Activation(float value)
    {
        return (float)Math.Tanh(value);
    }
}