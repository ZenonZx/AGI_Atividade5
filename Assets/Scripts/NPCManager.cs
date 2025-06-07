using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    private List<NPCs> allNpcs = new List<NPCs>();
    public float soundBroadcastRadiusMultiplier = 1.2f;
    public int maxNpcsToInvestigateSound = 2;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterNPC(NPCs npc)
    {
        if (!allNpcs.Contains(npc))
        {
            allNpcs.Add(npc);
            Debug.Log(npc.name + " registrado no NPCManager.");
        }
    }

    public void UnregisterNPC(NPCs npc)
    {
        if (allNpcs.Contains(npc))
        {
            allNpcs.Remove(npc);
            Debug.Log(npc.name + " desregistrado do NPCManager.");
        }
    }

    public void ReportStimulus(Vector3 stimulusPosition, float stimulusRadius, bool isAggressive)
    {
        Debug.Log("NPCManager: Estímulo reportado em " + stimulusPosition + " com raio " + stimulusRadius + ", Agressivo: " + isAggressive);
        List<NPCs> npcsInVicinity = new List<NPCs>();

        foreach (NPCs npc in allNpcs)
        {
            if (npc == null || !npc.isActiveAndEnabled) continue;

            float distanceToNpc = Vector3.Distance(npc.transform.position, stimulusPosition);
            float effectiveRadius = isAggressive ? npc.sightRange : npc.hearingRange;
            if (distanceToNpc <= stimulusRadius * soundBroadcastRadiusMultiplier || distanceToNpc <= effectiveRadius)
            {
                npcsInVicinity.Add(npc);
            }
        }

        if (npcsInVicinity.Count == 0)
        {
            Debug.Log("NPCManager: Nenhum NPC estava ao alcance do estímulo.");
            return;
        }

        Debug.Log("NPCManager: " + npcsInVicinity.Count + " NPCs na vizinhança do estímulo.");

        if (!isAggressive && npcsInVicinity.Count > maxNpcsToInvestigateSound)
        {
            npcsInVicinity = npcsInVicinity.OrderBy(npc => Vector3.Distance(npc.transform.position, stimulusPosition))
                                       .Take(maxNpcsToInvestigateSound)
                                       .ToList();
            Debug.Log("NPCManager: Limitando investigação a " + npcsInVicinity.Count + " NPCs mais próximos.");
        }

        foreach (NPCs npc in npcsInVicinity)
        {
            npc.ReceiveStimulus(stimulusPosition, isAggressive);
        }
    }

    public void ReportPlayerSightingByNPC(NPCs reportingNpc, Vector3 playerPosition)
    {
        Debug.Log("NPCManager: " + reportingNpc.name + " reportou avistamento do jogador em " + playerPosition);
        foreach (NPCs npc in allNpcs)
        {
            if (npc == reportingNpc || npc == null || !npc.isActiveAndEnabled) continue;

            float alertRadius = reportingNpc.sightRange * 1.5f;
            if (Vector3.Distance(npc.transform.position, reportingNpc.transform.position) < alertRadius)
            {
                if (npc.currentState != NPCs.NPCState.AggressiveSearchPhase1 && npc.currentState != NPCs.NPCState.AggressiveSearchPhase2)
                {
                    Debug.Log("NPCManager: Alertando " + npc.name + " sobre avistamento do jogador (via " + reportingNpc.name + ").");
                    npc.ReceiveStimulus(playerPosition, true);
                }
            }
        }
    }
}