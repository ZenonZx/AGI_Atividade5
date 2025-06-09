using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.SceneManagement;

public class NPCs : MonoBehaviour
{
    public enum NPCState
    {
        Idle,
        Patrolling,
        CautiousSearch,
        AggressiveSearchPhase1,
        AggressiveSearchPhase2,
        ReturningToRoutine
    }

    [Header("Configuração do NPC")]
    public NPCState currentState = NPCState.Idle; // Estado atual da máquina de estados do NPC.
    public float walkSpeed = 4f; // Velocidade normal de caminhada do NPC.
    public float cautiousSpeed = 2f; // Velocidade do NPC ao realizar uma busca cautelosa.
    public float alertSpeed = 8f; // Velocidade do NPC em estado de alerta ou busca agressiva.
    public float sightRange = 10f; // Distância máxima em que o NPC pode ver o jogador.
    public float hearingRange = 15f; // Distância máxima em que o NPC pode ouvir estímulos sonoros.
    [Tooltip("Tempo em segundos para a fase 2 da busca agressiva")]
    public float aggressiveSearchPhase2Duration = 10f; // Duração da Fase 2 da busca agressiva.
    [Tooltip("Componente TextMesh para exibir o status/animação do NPC")]
    public TextMesh StatusText; // Referência ao componente TextMesh para mostrar o estado atual do NPC.

    [Header("Navegação")]
    private NavMeshAgent agent;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 stimulusPosition;
    private float searchTimer;

    [Header("Patrulha")]
    public Transform[] patrolPoints; // Array de Transforms que definem os pontos de patrulha do NPC.
    private int currentPatrolIndex = 0;
    private Vector3 initialPosition;

    public Transform playerTransform; // Referência ao Transform do jogador.

    void Start()
    {
        if (NPCManager.Instance != null)
        {
            NPCManager.Instance.RegisterNPC(this);
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("Componente NavMeshAgent não encontrado em " + gameObject.name);
            enabled = false;
            return;
        }

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
            else
            {
                Debug.LogError("Jogador não encontrado. Atribua playerTransform ou marque o jogador com a tag 'Player'.");
            }
        }

        initialPosition = transform.position;

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            SetState(NPCState.Patrolling);
        }
        else
        {
            SetState(NPCState.Idle);
        }
    }

    void OnDisable()
    {
        if (NPCManager.Instance != null)
        {
            NPCManager.Instance.UnregisterNPC(this);
        }
    }

    void Update()
    {
        if (playerTransform != null && CanSeePlayer())
        {
            ReceiveStimulus(playerTransform.position, true);

            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.ReportPlayerSightingByNPC(this, playerTransform.position);
            }
        }

        switch (currentState)
        {
            case NPCState.Idle:
                HandleIdleState();
                break;
            case NPCState.Patrolling:
                HandlePatrollingState();
                break;
            case NPCState.CautiousSearch:
                HandleCautiousSearchState();
                break;
            case NPCState.AggressiveSearchPhase1:
                HandleAggressiveSearchPhase1State();
                break;
            case NPCState.AggressiveSearchPhase2:
                HandleAggressiveSearchPhase2State();
                break;
            case NPCState.ReturningToRoutine:
                HandleReturningToRoutineState();
                break;
        }
        UpdateStatusText();
    }

    void SetState(NPCState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case NPCState.Idle:
                agent.speed = walkSpeed;
                agent.isStopped = true;
                break;
            case NPCState.Patrolling:
                agent.speed = walkSpeed;
                agent.isStopped = false;
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                }
                else
                {
                    SetState(NPCState.Idle);
                }
                break;
            case NPCState.CautiousSearch:
                agent.speed = cautiousSpeed;
                agent.isStopped = false;
                agent.SetDestination(stimulusPosition);
                break;
            case NPCState.AggressiveSearchPhase1:
                agent.speed = alertSpeed;
                agent.isStopped = false;
                agent.SetDestination(lastKnownPlayerPosition);
                break;
            case NPCState.AggressiveSearchPhase2:
                agent.speed = alertSpeed;
                agent.isStopped = false;
                searchTimer = aggressiveSearchPhase2Duration;
                Vector3 randomDirection = Random.insideUnitSphere * 5f;
                randomDirection += lastKnownPlayerPosition;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
                else
                {
                    Debug.Log(gameObject.name + ": Não encontrou ponto para Busca Agressiva Fase 2. Retornando.");
                    SetState(NPCState.ReturningToRoutine);
                }
                break;
            case NPCState.ReturningToRoutine:
                agent.speed = walkSpeed;
                agent.isStopped = false;
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    agent.SetDestination(patrolPoints[currentPatrolIndex].position);
                }
                else
                {
                    agent.SetDestination(initialPosition);
                }
                break;
        }
        UpdateStatusText();
    }

    void HandleIdleState()
    {
        if (StatusText) StatusText.text = "Parado";
    }

    void HandlePatrollingState()
    {
        if (StatusText) StatusText.text = "Patrulhando";
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
            else
            {
                SetState(NPCState.Idle);
            }
        }
    }

    void HandleCautiousSearchState()
    {
        if (StatusText) StatusText.text = "Investigando (Cauteloso)";
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            Debug.Log(gameObject.name + " chegou ao ponto de busca cautelosa. Retornando.");
            SetState(NPCState.ReturningToRoutine);
        }
    }

    void HandleAggressiveSearchPhase1State()
    {
        if (StatusText) StatusText.text = "Procurando Jogador (Alerta)";
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            Debug.Log(gameObject.name + " chegou à LKP. Iniciando Busca Agressiva Fase 2.");
            SetState(NPCState.AggressiveSearchPhase2);
        }
    }

    void HandleAggressiveSearchPhase2State()
    {
        if (StatusText) StatusText.text = "Busca Ativa";
        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0)
        {
            Debug.Log(gameObject.name + ": tempo da busca agressiva fase 2 esgotado. Retornando.");
            SetState(NPCState.ReturningToRoutine);
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                Vector3 randomDirection = Random.insideUnitSphere * 5f;
                randomDirection += lastKnownPlayerPosition;
                NavMeshHit hit;
                if (NavMesh.SamplePosition(randomDirection, out hit, 5f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
                else
                {
                    Debug.Log(gameObject.name + ": não encontrou próximo ponto de busca. Retornando.");
                    SetState(NPCState.ReturningToRoutine);
                }
            }
        }
    }

    void HandleReturningToRoutineState()
    {
        if (StatusText) StatusText.text = "Retornando";
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                SetState(NPCState.Patrolling);
            }
            else
            {
                SetState(NPCState.Idle);
            }
        }
    }

    public void ReceiveStimulus(Vector3 position, bool isAggressiveStimulus)
    {
        if (currentState == NPCState.AggressiveSearchPhase1 || currentState == NPCState.AggressiveSearchPhase2)
        {
            if (!isAggressiveStimulus) return;
        }

        stimulusPosition = position;
        if (isAggressiveStimulus)
        {
            lastKnownPlayerPosition = position;
            Debug.Log(gameObject.name + " recebeu estímulo agressivo em " + position);
            SetState(NPCState.AggressiveSearchPhase1);
        }
        else
        {
            Debug.Log(gameObject.name + " recebeu estímulo cauteloso em " + position);
            SetState(NPCState.CautiousSearch);
        }
    }

    public void HearSoundStimulus(Vector3 soundPosition)
    {
        Debug.Log(gameObject.name + " recebeu chamada HearSoundStimulus da posição " + soundPosition);
        float distanceToSound = Vector3.Distance(transform.position, soundPosition);
        Debug.Log(gameObject.name + " - Distância para o som: " + distanceToSound + ", Alcance de Audição: " + hearingRange);
        if (distanceToSound <= hearingRange)
        {
            Debug.Log(gameObject.name + " está dentro do alcance de audição. Recebendo estímulo.");
            ReceiveStimulus(soundPosition, false);
        }
        else
        {
            Debug.Log(gameObject.name + " está fora do alcance de audição.");
        }
    }

    public void PlayerDetected(Vector3 playerPos)
    {
        ReceiveStimulus(playerPos, true);
    }

    bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= sightRange)
        {
            Vector3 directionToPlayer = (playerTransform.position - transform.position).normalized;
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, directionToPlayer, out hit, sightRange))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    return true;
                }
            }
        }
        return false;
    }

    void UpdateStatusText()
    {
        if (StatusText == null) return;

        string currentActionText = currentState.ToString(); // Padrão é o nome do estado
        switch (currentState)
        {
            case NPCState.Idle:
                currentActionText = "Parado";
                break;
            case NPCState.Patrolling:
                currentActionText = "...";
                if (agent.pathPending) currentActionText += " (Calculando)";
                else if (agent.hasPath) currentActionText += " (Andando)";
                break;
            case NPCState.CautiousSearch:
                currentActionText = "?";
                if (agent.pathPending) currentActionText += " (Calculando)";
                else if (agent.hasPath) currentActionText += " (Movendo)";
                break;
            case NPCState.AggressiveSearchPhase1:
                currentActionText = "!";
                if (agent.pathPending) currentActionText += " (Calculando)";
                else if (agent.hasPath) currentActionText += " (Correndo)";
                break;
            case NPCState.AggressiveSearchPhase2:
                currentActionText = "?!";
                if (agent.pathPending) currentActionText += " (Calculando)";
                else if (agent.hasPath) currentActionText += " (Procurando)";
                break;
            case NPCState.ReturningToRoutine:
                currentActionText = "?";
                if (agent.pathPending) currentActionText += " (Calculando)";
                else if (agent.hasPath) currentActionText += " (Andando)";
                break;
        }
        StatusText.text = currentActionText;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hearingRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.blue;
            Vector3[] corners = agent.path.corners;
            if (corners.Length > 1)
            {
                for (int i = 0; i < corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(corners[i], corners[i + 1]);
                }
            }
        }

        if (currentState == NPCState.CautiousSearch && stimulusPosition != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, stimulusPosition);
            Gizmos.DrawWireSphere(stimulusPosition, 0.8f);
        }
        else if ((currentState == NPCState.AggressiveSearchPhase1 || currentState == NPCState.AggressiveSearchPhase2) && lastKnownPlayerPosition != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, lastKnownPlayerPosition);
            Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.8f);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log(gameObject.name + " colidiu com o Jogador: " + collision.gameObject.name);

            Destroy(collision.gameObject);
            
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}