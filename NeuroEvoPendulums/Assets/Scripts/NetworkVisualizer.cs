using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkVisualizer : MonoBehaviour
{
    [Header("Referencias")]
    public GeneticAlgorithmManager manager;
    public GameObject nodePrefab;
    public GameObject weightPrefab;

    [Header("Configuración de Diseño")]
    public float visualizerScale = 1.0f; // Controla el tamaño general
    public float nodeSize = 30f;
    public float layerSpacing = 150f; // Espacio horizontal entre capas
    public float nodeSpacing = 50f;  // Espacio vertical entre neuronas
    public float weightWidth = 2f;    // Grosor de las líneas de peso
    public float padding = 40f;       // Espacio desde la esquina

    public Color positiveWeightColor = Color.green;
    public Color negativeWeightColor = Color.red;

    private NeuralNetwork lastBestBrain;
    private List<GameObject> uiElements = new List<GameObject>();

    void Update()
    {
        // Aplicar escala global al objeto visualizador
        this.transform.localScale = new Vector3(visualizerScale, visualizerScale, 1f);

        // Solo redibujar si el cerebro ha cambiado (ej. al final de una generación)
        if (manager.bestBrain != null && manager.bestBrain != lastBestBrain)
        {
            lastBestBrain = manager.bestBrain;
            DrawNetwork(lastBestBrain);
        }
    }

    // Función Principal de Dibujo
    void DrawNetwork(NeuralNetwork brain)
    {
        // 1. Limpiar el dibujo anterior
        foreach (GameObject go in uiElements)
        {
            Destroy(go);
        }
        uiElements.Clear();

        List<Vector2[]> nodePositionsByLayer = new List<Vector2[]>();

        // 2. Dibujar las Neuronas
        for (int i = 0; i < brain.layers.Length; i++)
        {
            float x = -padding - (i * layerSpacing);
            float currentLayerHeight = (brain.layers[i] - 1) * nodeSpacing;
            float startY = -padding - currentLayerHeight / 2f;

            nodePositionsByLayer.Add(new Vector2[brain.layers[i]]);

            for (int j = 0; j < brain.layers[i]; j++)
            {
                float y = startY + j * nodeSpacing;
                Vector2 localPos = new Vector2(x, y);

                GameObject nodeGO = Instantiate(nodePrefab, this.transform);
                
                RectTransform nodeRect = nodeGO.GetComponent<RectTransform>();
                nodeRect.anchoredPosition = localPos;
                nodeRect.sizeDelta = new Vector2(nodeSize, nodeSize);

                uiElements.Add(nodeGO);
                nodePositionsByLayer[i][j] = localPos; 
            }
        }

        // 3. Dibujar las Conexiones
        for (int i = 0; i < brain.weights.Count; i++)
        {
            for (int j = 0; j < brain.weights[i].GetLength(0); j++)
            {
                for (int k = 0; k < brain.weights[i].GetLength(1); k++)
                {
                    Vector2 localPosA = nodePositionsByLayer[i][k];
                    Vector2 localPosB = nodePositionsByLayer[i + 1][j];
                    float weightValue = brain.weights[i][j, k];
                    
                    DrawWeightLine(localPosA, localPosB, weightValue);
                }
            }
        }
    }

    // Dibujar las líneas
    void DrawWeightLine(Vector2 localPosA, Vector2 localPosB, float value)
    {
        GameObject lineGO = Instantiate(weightPrefab, this.transform);
        Image lineImg = lineGO.GetComponent<Image>();
        RectTransform lineRect = lineGO.GetComponent<RectTransform>();

        Vector2 dir = localPosB - localPosA;
        float dist = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        lineRect.anchoredPosition = localPosA;
        lineRect.rotation = Quaternion.Euler(0, 0, angle);
        lineRect.sizeDelta = new Vector2(dist, weightWidth);

        Color c = (value > 0) ? positiveWeightColor : negativeWeightColor;
        c.a = Mathf.Clamp01(Mathf.Abs(value) * 2.0f); 
        lineImg.color = c;
        
        uiElements.Add(lineGO);
    }
}