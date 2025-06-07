using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Para testes de trigger
public class TriggerTest : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            Debug.Log("O jogador entrou na área do trigger!");
            // Qualquer lógica adicional que você deseja executar quando o jogador entra no trigger
        }
    }
}
