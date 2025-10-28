using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AgentController : MonoBehaviour
{
    // Referencias
    public NeuralNetwork brain;
    private Rigidbody2D rb;
    private List<Rigidbody2D> rb_links;
    private List<Rigidbody2D> rb_balls;

    // Estado
    public float fitness = 0f;
    public bool isAlive = true;

    // Configuración
    public float moveSpeed = 5f;
    // public int[] networkTopology = { 7, 5, 1 };

    private float[] inputs;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Para que el GeneticAlgorithmManager cree al agente
    public void Init(NeuralNetwork newBrain, List<Rigidbody2D> links, List<Rigidbody2D> balls)
    {
        this.brain = newBrain;
        this.rb_links = links;
        this.rb_balls = balls;
        
        // VALIDACION DE ENTRADAS
        // 1 agente + 2 por cada link + 2 por cada bola
        int expectedInputs = 1 + (rb_links.Count * 2) + (rb_balls.Count * 2);

        if (rb_links.Count != rb_balls.Count) {
             Debug.LogError("Las listas de péndulos no coinciden", this.gameObject);
             isAlive = false;
             return;
        }

        if (brain.layers[0] != expectedInputs)
        {
            Debug.LogError($"¡ERROR DE CONFIGURACIÓN! Se esperan {expectedInputs} entradas " +
                           $"(1 Agente + {rb_links.Count} Péndulos * 4 datos), " +
                           $"pero la red tiene {brain.layers[0]}.", this.gameObject);
            isAlive = false;
            return;
        }

        inputs = new float[brain.layers[0]];
        isAlive = true;
    }


    void FixedUpdate()
    {
        if (!isAlive)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        GatherInputs();
        float[] outputs = brain.FeedForward(inputs);
        float moveForce = outputs[0];

        rb.linearVelocity = new Vector2(moveForce * moveSpeed, 0f);

        fitness += Time.fixedDeltaTime;
    }

    void GatherInputs()
    {
        // Entrada 0: Posición X del propio agente
        inputs[0] = rb.position.x / 10f;

        int currentIndex = 1; // Empezamos a rellenar desde el índice 1

        // Iteramos sobre todos los péndulos dobles que hemos encontrado
        for (int i = 0; i < rb_links.Count; i++)
        {
            // Datos de la Bola 1 (Link)
            inputs[currentIndex++] = rb_links[i].position.x / 10f;
            inputs[currentIndex++] = rb_links[i].linearVelocity.x / 5f;
            
            // Datos de la Bola 2 (Ball)
            inputs[currentIndex++] = rb_balls[i].position.x / 10f;
            inputs[currentIndex++] = rb_balls[i].linearVelocity.x / 5f;
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("PendulumBall") || col.gameObject.CompareTag("Obstacle"))
        {
            Die();
        }
    }
    
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Obstacle"))
        {
            Die();
        }
    }
    
    public void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        
        GetComponent<SpriteRenderer>().color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
        
        // Desactivamos el collider para que se vuelva "fantasma"
        //    y no bloquee los péndulos ni el suelo.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // Le decimos al motor de física que deje de simular este objeto.
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
    }
}