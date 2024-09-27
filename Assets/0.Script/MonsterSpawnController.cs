using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MonsterSpawnController : MonoBehaviour
{
    [SerializeField] private Player p;
    [SerializeField] public PlayerAgent playeragent;
    [SerializeField] private Monster monster;
    [SerializeField] private Transform parent;
    [SerializeField] private BoxCollider2D[] boxColls;
    [SerializeField] private GameObject MidBossPrefab;
    [SerializeField] private RangedMonster rangedMonster;
    IEnumerator createMonster;
    int range = 10;

    void Awake()
    {
        createMonster = CreateMonster(1f);
        StartCoroutine(createMonster);
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) 
        {
            Instantiate(MidBossPrefab);
        }
    }
    IEnumerator CreateMonster(float time)
    {
        while (true)
        {
            yield return new WaitForSeconds(time);

            // 에이전트의 이동 방향 가져오기
            Vector2 agentDirection = playeragent.movementDirection;

            // 각 스폰 위치에 대한 가중치 계산
            float[] spawnWeights = new float[boxColls.Length];
            float totalWeight = 0f;

            for (int i = 0; i < boxColls.Length; i++)
            {
                Vector2 spawnPosition = GetSpawnPosition(i);
                Vector2 directionToSpawn = (spawnPosition - (Vector2)playeragent.transform.position).normalized;

                // 이동 방향과 스폰 위치 방향 간의 각도 계산
                float angle = Vector2.Angle(agentDirection, directionToSpawn);

                // 각도를 기반으로 가중치 계산 (각도가 작을수록 가중치 높음)
                // 예를 들어, 각도가 0도이면 가중치 최대, 180도이면 가중치 최소
                float weight = Mathf.Cos(angle * Mathf.Deg2Rad); // -1 ~ 1 사이 값
                weight = Mathf.Max(0f, weight); // 음수 값을 0으로 처리
                spawnWeights[i] = weight;
                totalWeight += weight;
            }

            // 가중치에 따라 스폰 위치 선택
            float randomValue = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;
            int selectedIndex = 0;

            for (int i = 0; i < spawnWeights.Length; i++)
            {
                cumulativeWeight += spawnWeights[i];
                if (randomValue <= cumulativeWeight)
                {
                    selectedIndex = i;
                    break;
                }
            }

            // 선택된 위치에서 몬스터 생성
            Vector2 spawnPos = RandomPosition(selectedIndex);
            float spawnProbability = Random.Range(0f, 1f);
            Monster spawnedMonster;

            if (spawnProbability <= 0.05f) // n% 확률로 원거리 몹 생성
            {
                spawnedMonster = Instantiate(rangedMonster, spawnPos, Quaternion.identity);
            }
            else
            {
                spawnedMonster = Instantiate(monster, spawnPos, Quaternion.identity);
            }

            spawnedMonster.SetPlayer(playeragent);
            spawnedMonster.transform.SetParent(parent);
        }
    }
    private Vector2 GetSpawnPosition(int index)
    {
        RectTransform pos = boxColls[index].GetComponent<RectTransform>();
        Vector2 spawnPos;

        if (index == 0 || index == 1)
        {
            spawnPos = new Vector2(pos.position.x, pos.position.y);
        }
        else
        {
            spawnPos = new Vector2(pos.position.x, pos.position.y);
        }

        return spawnPos;
    }

    Vector2 RandomPosition(int index)
    {

        RectTransform pos = boxColls[index].GetComponent<RectTransform>();

        Vector3 randPos = Vector3.zero;
        // Top = 0 , Bottom = 1
        if (index == 0 || index == 1)
        {
            randPos = new Vector2(pos.position.x + Random.Range(-range, range), pos.position.y);
        }
        // 나머지
        else
        {
            randPos = new Vector2(pos.position.x, pos.position.y + Random.Range(-range, range));
        }

        return randPos;
    }
    public void StartSpawn(bool start)
    {
        if (start)
            StartCoroutine(createMonster);
        else
            StopCoroutine(createMonster);
    }
}
