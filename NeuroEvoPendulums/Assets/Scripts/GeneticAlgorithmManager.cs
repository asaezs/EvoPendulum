using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GeneticAlgorithmManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject agentPrefab;
    public Transform spawnPoint;

    [Header("Configuración del Entorno")]
    public int[] networkTopology = { 7, 5, 1 };
    
    // Variables del Algoritmo Genético
    [Header("Configuración del Algoritmo Genético")]
    public int populationSize = 50;
    [Tooltip("El % de la población que pasa a la siguiente generación sin cambios")]
    [Range(0f, 1f)]
    public float elitismPercent = 0.1f;
    [Tooltip("El % de la población (los mejores) que pueden ser padres")]
    [Range(0f, 1f)]
    public float parentPercent = 0.5f;
    [Tooltip("La probabilidad (0 a 1) de que un gen individual mute")]
    [Range(0f, 1f)]
    public float mutationRate = 0.01f;
    [Tooltip("La 'fuerza' de la mutación (cuánto puede cambiar un gen)")]
    public float mutationStrength = 0.1f;
    [Tooltip("Acelera la simulación")]
    public float timeScale = 1.0f;

    // Listas de Control
    private List<AgentController> population;
    private List<Rigidbody2D> pendulumLinks_rb;
    private List<Rigidbody2D> pendulumBalls_rb;

    // Listas para guardar el estado inicial del entorno
    private List<Vector2> initialLinkPositions;
    private List<float> initialLinkRotations;
    private List<Vector2> initialBallPositions;
    private List<float> initialBallRotations;

    private int generationNumber = 1;

    [HideInInspector]
    public NeuralNetwork bestBrain;

    void Start()
    {
        population = new List<AgentController>();
        
        // Buscamos los links y los guardamos
        pendulumLinks_rb = GameObject.FindGameObjectsWithTag("PendulumLink")
                            .Select(go => go.GetComponent<Rigidbody2D>())
                            .ToList();
        
        // Buscamos las bolas de peligro y las guardamos
        pendulumBalls_rb = GameObject.FindGameObjectsWithTag("PendulumBall")
                            .Select(go => go.GetComponent<Rigidbody2D>())
                            .ToList();

        // Comprobación
        if (pendulumLinks_rb.Count != pendulumBalls_rb.Count)
        {
            Debug.LogError("¡Error! El número de 'PendulumLink' no coincide con el de 'PendulumBall'.");
        }

        Debug.Log($"Entorno listo con {pendulumBalls_rb.Count} péndulo(s) doble(s). Iniciando Generación {generationNumber}...");

        // Guardar el estado inicial
        initialLinkPositions = new List<Vector2>();
        initialLinkRotations = new List<float>();
        foreach (Rigidbody2D rb in pendulumLinks_rb)
        {
            initialLinkPositions.Add(rb.position);
            initialLinkRotations.Add(rb.rotation);
        }

        initialBallPositions = new List<Vector2>();
        initialBallRotations = new List<float>();
        foreach (Rigidbody2D rb in pendulumBalls_rb)
        {
            initialBallPositions.Add(rb.position);
            initialBallRotations.Add(rb.rotation);
        }

        CreateGeneration(null); 

        if (population.Count > 0)
        {
            bestBrain = new NeuralNetwork(population[0].brain); 
        }
    }

    void Update()
    {
        Time.timeScale = timeScale;

        if (IsGenerationAlive())
        {
            return;
        }

        Evolve();
    }

    void Evolve()
    {
        // 1. Ordenar la población por fitness
        List<AgentController> sortedPopulation = population
            .OrderByDescending(agent => agent.fitness)
            .ToList();

        bestBrain = new NeuralNetwork(sortedPopulation[0].brain);
        Debug.Log("Nuevo Mejor Cerebro Guardado.");

        float bestFitness = sortedPopulation[0].fitness;
        float averageFitness = sortedPopulation.Average(agent => agent.fitness);
        Debug.Log($"--- GENERACIÓN {generationNumber} COMPLETADA ---");
        Debug.Log($"Mejor Fitness: {bestFitness:F2} | Fitness Promedio: {averageFitness:F2}");

        // 2. Preparar la nueva lista de RRNN
        List<NeuralNetwork> newBrains = new List<NeuralNetwork>();

        // 3. Elitismo
        // Copiamos a los mejores agentes directamente a la nueva generación
        int elitismCount = (int)(populationSize * elitismPercent);
        for (int i = 0; i < elitismCount; i++)
        {
            // Usamos el constructor de copia que creamos
            newBrains.Add(new NeuralNetwork(sortedPopulation[i].brain));
        }
        
        // 4. Cruce (Crossover)
        // Creamos un "pool" de padres con los mejores agentes
        int parentPoolSize = (int)(populationSize * parentPercent);
        List<NeuralNetwork> parentPool = sortedPopulation
            .Take(parentPoolSize)
            .Select(agent => agent.brain)
            .ToList();

        // Llenamos el resto de la población con "hijos"
        while (newBrains.Count < populationSize)
        {
            // Elegir dos padres al azar del pool
            NeuralNetwork parentA = parentPool[Random.Range(0, parentPool.Count)];
            NeuralNetwork parentB = parentPool[Random.Range(0, parentPool.Count)];
            
            // Crear el hijo usando el constructor de cruce
            NeuralNetwork childBrain = new NeuralNetwork(parentA, parentB);
            
            // 5. Mutación
            // Aplicamos una pequeña mutación al hijo
            childBrain.Mutate(mutationRate, mutationStrength);

            newBrains.Add(childBrain);
        }

        // 6. Incrementar la generación y crearla
        generationNumber++;
        Debug.Log($"Iniciando Generación {generationNumber}...");
        CreateGeneration(newBrains);
    }

    void CreateGeneration(List<NeuralNetwork> brains)
    {
        // Limpiar la población anterior
        foreach (AgentController agent in population)
        {
            Destroy(agent.gameObject);
        }
        population.Clear();

        ResetEnvironment();

        // Crear la nueva población
        for (int i = 0; i < populationSize; i++)
        {
            GameObject agentGO = Instantiate(agentPrefab, spawnPoint.position, Quaternion.identity);
            agentGO.name = $"Agente_{generationNumber}-{i}";
            
            AgentController agent = agentGO.GetComponent<AgentController>();
            
            // Si 'brains' es null (Gen 1), crea uno aleatorio
            NeuralNetwork brain = (brains == null) 
                ? new NeuralNetwork(networkTopology) 
                : brains[i]; // Si no, usa el cerebro "evolucionado"
            
            agent.Init(brain, pendulumLinks_rb, pendulumBalls_rb); 
            population.Add(agent);
        }
    }

    bool IsGenerationAlive()
    {
        foreach (AgentController agent in population)
        {
            if (agent.isAlive)
            {
                return true;
            }
        }
        return false;
    }

    void ResetEnvironment()
    {
        for (int i = 0; i < pendulumLinks_rb.Count; i++)
        {
            var rb = pendulumLinks_rb[i];
            rb.position = initialLinkPositions[i];
            rb.rotation = initialLinkRotations[i];
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        for (int i = 0; i < pendulumBalls_rb.Count; i++)
        {
            var rb = pendulumBalls_rb[i];
            rb.position = initialBallPositions[i];
            rb.rotation = initialBallRotations[i];
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
    
}