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
            Debug.Log("O jogador entrou na �rea do trigger!");
            // Qualquer l�gica adicional que voc� deseja executar quando o jogador entra no trigger
        }
    }
}
