using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleManager : MonoBehaviour
{
    public GameObject[] obstaclePrefabs; // 장애물 프리팹 배열
    [SerializeField] private BoxCollider2D[] boxColls;
    public Transform parent; // 장애물이 생성될 부모 오브젝트
    public int maxObstacles = 100; // 최대 장애물 수
    private List<GameObject> currentObstacles = new List<GameObject>(); // 현재 활성화된 장애물 리스트

    private Camera mainCamera;
    private float checkInterval = 1f; // 생성 간격
    private Vector2 screenBounds;

    void Start()
    {
        mainCamera = Camera.main;
        screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.transform.position.z));
        StartCoroutine(SpawnObstacles());
    }

    IEnumerator SpawnObstacles()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            if (currentObstacles.Count >= maxObstacles) continue; // 최대 개수에 도달하면 생성 중단

                int rand = Random.Range(0, boxColls.Length);
                int randomIndex = Random.Range(0, obstaclePrefabs.Length);
                Vector2 spawnPosition = RandomPosition(rand);
                GameObject newObstacle = Instantiate(obstaclePrefabs[randomIndex], spawnPosition, Quaternion.identity, parent);
                currentObstacles.Add(newObstacle);
                StartCoroutine(FadeOutAndDestroy(newObstacle, 20f)); // n초 후 투명화 시작
        }
    }
    Vector2 RandomPosition(int index)
    {
        BoxCollider2D collider = boxColls[index];
        Bounds bounds = collider.bounds;

        // 실제 게임 월드에서의 x, y 좌표를 랜덤하게 계산
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);

        return new Vector2(x, y);
    }

    /*Vector2 RandomPosition(int index)
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
    }*/
    IEnumerator FadeOutAndDestroy(GameObject obstacle, float delay)
    {
        yield return new WaitForSeconds(10f); // N초 대기
        SpriteRenderer sr = obstacle.GetComponent<SpriteRenderer>();
        while (sr.color.a > 0.05f)
        {
            Color newColor = sr.color;
            newColor.a -= Time.deltaTime / 2; // 투명도 점차 감소
            sr.color = newColor;
            yield return null;
        }
        currentObstacles.Remove(obstacle);
        Destroy(obstacle);
    }
}
