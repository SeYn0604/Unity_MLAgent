using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System;
using System.Threading;
using UnityEngine.UIElements;
using System.Runtime.CompilerServices;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;

public class PlayerAgent : Agent
{
    [SerializeField] Transform target;
    [SerializeField] Rigidbody2D rb;
    [SerializeField] private Transform parent;
    [SerializeField] RayPerceptionSensorComponent2D m_rayPerceptionSensorComponent2D; //agent가 몬스터 탐지하는 컴포넌트
    public Bullet bullet;
    public int hitcheck = 0;
    public int wallcheck = 0;
    private float bullettimer = 0;
    private float insideTimer = 0;                   //현재 미사용
    public Player player;
    private Vector3 referencePosition;               // 기준 위치
    //private float netDistanceThreshold = 10f;     // 순이동 거리 기준값
    //private float movementTimer = 0f;
    //private float movementTimeLimit = 3f;         // 시간 제한 (초)
    // 탄약 관련 변수 
    public int maxAmmo = 10;                       
    public int currentAmmo; 
    public bool isReloading = false;
    public float reloadTime = 1.5f;                   // 재장전 시간
    private float reloadTimer = 0f;
    public Text stepCounterText;                // 스텝 수를 표시할 Text 컴포넌트
    public Vector2 movementDirection { get; private set; } // 에이전트의 이동 방향
    private Vector2 previousPosition;


    public override void OnEpisodeBegin()
    {
        transform.position = Vector3.zero;
        hitcheck = 0;
        wallcheck = 0;
        player.HP = 100;
        referencePosition = transform.position;
        currentAmmo = maxAmmo;
        isReloading = false;
        reloadTimer = 0f;
        previousPosition = transform.position;
        //target.position = new Vector2(UnityEngine.Random.Range(-15, 15), UnityEngine.Random.Range(-15, 15));
    }
    private void Update()
    {
        if (isReloading)
        {
            reloadTimer += Time.deltaTime;
            if (reloadTimer >= reloadTime)
            {
                currentAmmo = maxAmmo;
                isReloading = false;
                reloadTimer = 0f;
            }
        }

        bullettimer += Time.deltaTime;
        insideTimer += Time.deltaTime;

        if(hitcheck == 1) //bullet 스크립트에서 hitcheck 부여
        {
            UI.instance.KillCount++;
            AddReward(+10.0f);
            hitcheck = 0;
            Debug.Log("몬스터 처치 - 가점 부여");
        }
        if(wallcheck == 1)
        {
            AddReward(-1f);
            wallcheck = 0;
            Debug.Log("벽 맞춤 - 감점 부여");
        }
        /*if (insideTimer >= 5)
        {
            AddReward(+3f);
            Debug.Log("5초생존3점추가");
            insideTimer = 0;
        }*/
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position / 10f); // 에이전트 위치 (3)
        sensor.AddObservation(target.position / 10f);    // 타겟 위치 (3)
        sensor.AddObservation(rb.velocity / 5f);         // 에이전트 속도 (2)
        sensor.AddObservation(player.HP / 100f);         // 에이전트 체력 (1)
        sensor.AddObservation(bullettimer / 2f);         // 총알 타이머 (1)
        sensor.AddObservation(wallcheck);                // 벽 충돌 여부 (1)
        sensor.AddObservation(isReloading ? 1f : 0f);    // 재장전 중인지 여부 (1)
        sensor.AddObservation(currentAmmo / (float)maxAmmo); // 남은 탄약 비율 (1)
      // 총 관찰 값: 3 + 3 + 2 + 1 + 1 + 1 + 1 + 1 = 13
    }
    [SerializeField] float speed = 1.1f;
    Vector3 nextMove;
    private bool IsMonsterNearby()
    {
        float detectionRadius = 5.0f; // 몬스터 감지 반경
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Monster"))
            {
                return true;
            }
        }
        return false;
    }
    /*private void OnDrawGizmosSelected()
    {
        // 감지 반경을 빨간색으로 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 5.0f); // 5.0f는 detectionRadius 값
    }*/
    public override void OnActionReceived(ActionBuffers actions)
    {
        // 글로벌 스텝 수 가져오기
        int globalStepCount = (int)Academy.Instance.StepCount;

        // UI 업데이트
        if (stepCounterText != null)
        {
            stepCounterText.text = "Step: " + globalStepCount.ToString();
        }
        // 이동 코드
        nextMove.x = actions.ContinuousActions[0] * 3;
        nextMove.y = actions.ContinuousActions[1] * 3;
        transform.Translate(nextMove * Time.deltaTime * speed);
        // 이동 방향 계산
        Vector2 currentPosition = transform.position;
        movementDirection = (currentPosition - previousPosition).normalized;
        previousPosition = currentPosition;

        /*/ 재장전 액션 처리
        float reloadAction = actions.ContinuousActions[2];
        if (reloadAction > 0.5f && !isReloading && currentAmmo < maxAmmo)
        {
            isReloading = true;
            reloadTimer = 0f;

            if (currentAmmo <= 2) // 탄약이 2발 이하일 때만 보상 조건
            {
                if (!IsMonsterNearby())
                {
                    AddReward(0.2f); // 안전한 재장전 보상
                    Debug.Log("안전한 재장전 - 가점 부여");
                }
                else
                {
                    AddReward(-0.5f); // 위험한 재장전 패널티
                    Debug.Log("위험한 재장전 - 감점 부여");
                }
            }
            else
            {
                // 탄약이 충분한데 재장전하면 패널티 부여
                AddReward(-0.3f);
                Debug.Log("불필요한 재장전 - 감점 부여");
            }
        }*/

        // 재장전 로직
        if (isReloading)
        {
            reloadTimer += Time.deltaTime;
            if (reloadTimer >= reloadTime)
            {
                currentAmmo = maxAmmo;
                isReloading = false;
                reloadTimer = 0f;
                Debug.Log("재장전 완료");
            }
        }

        // 사격 로직
        if (bullettimer > 1.0f && !isReloading)
        {
            GameObject detectedMonster = RayCastInfo(m_rayPerceptionSensorComponent2D);
            if (detectedMonster != null && detectedMonster.CompareTag("Monster"))
            {
                if (currentAmmo > 0)
                {
                    Vector2 targetDirection = detectedMonster.transform.position - transform.position;
                    CreateBullet(targetDirection.normalized);
                    currentAmmo--;
                    bullettimer = 0f;
                }
                else
                {
                    // 탄약이 없을 때 자동으로 재장전 시작
                    isReloading = true;
                    reloadTimer = 0f;
                    Debug.Log("탄약 소진, 자동 재장전 시작");
                }
            }
        }
    }

    public void Destroymonsters() //Endepisode용
    {
        GameObject[] monsters = GameObject.FindGameObjectsWithTag("Monster");
        foreach (GameObject monster in monsters)
        {
            Destroy(monster);
        }
    }

    public void CreateBullet(Vector2 direction)
    {
        Bullet spawnedBullet = Instantiate(bullet, this.transform.position, Quaternion.identity);
        spawnedBullet.Initialize(direction); 
        spawnedBullet.SetPlayer(this); 
        spawnedBullet.transform.SetParent(parent);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Monster")) 
        {
            Debug.Log("몬스터와 충돌 - 감점 부여");
            player.Hit(110);           //agent가 닿아도 감점만 되니 적극적인 회피를 하지않으므로 기존과 같이 몬스터와 닿을시 즉사판정으로 변경
            AddReward(-10f);
            Destroy(other.gameObject); 
            if(player.HP <= 0f)
            {
                EndEpisode();
                UI.instance.KillCount = 0;
                Debug.Log("에이전트 사망, EndEpisode");
                Destroymonsters(); //에이전트가 사망하므로 에피소드 초기화 및 모든 몬스터 제거
            }
        }
        else if(other.gameObject.CompareTag("Wall"))
        {
            Debug.Log("벽과 충돌 - 감점 부여");
            AddReward(-5.0f);
            EndEpisode();
            UI.instance.KillCount = 0;
            Debug.Log("벽에 부딫힘, EndEpisode");
            Destroymonsters(); //벽의 경우, 바로 초기화 안할 시 에이전트가 뚫고 지나갈 가능성이 있으므로 닿을 시 무조건 초기화 유지
        }
    }

    private GameObject RayCastInfo(RayPerceptionSensorComponent2D rayComponent)
    {
        var rayOutputs = RayPerceptionSensor.Perceive(rayComponent.GetRayPerceptionInput(), false)
                .RayOutputs;
        float minlength = 100000f;
        int minnum = 0;
        if (rayOutputs != null)
        {
            var lengthOfRayOutputs = RayPerceptionSensor
                    .Perceive(rayComponent.GetRayPerceptionInput(), false)
                    .RayOutputs
                    .Length;

            for (int i = 0; i < lengthOfRayOutputs; i++)
            {
                GameObject goHit = rayOutputs[i].HitGameObject;
                if (goHit != null)
                {
                    var rayDirection = rayOutputs[i].EndPositionWorld - rayOutputs[i].StartPositionWorld;
                    var scaledRayLength = rayDirection.magnitude;

                    float rayHitDistance = rayOutputs[i].HitFraction * scaledRayLength;
                    if (rayHitDistance < minlength)
                    {
                        minlength = rayHitDistance;
                        minnum = i;
                    }
                    /*잘되나 확인
                    string dispStr;
                    dispStr = "__RayPerceptionSensor - HitInfo__:\r\n";
                    dispStr = dispStr + "GameObject name: " + goHit.name + "\r\n";
                    dispStr = dispStr + "GameObject tag: " + goHit.tag + "\r\n";
                    dispStr = dispStr + "Hit distance of Ray: " + rayHitDistance + "\r\n";
                    Debug.Log(dispStr);*/
                }
            }
            return rayOutputs[minnum].HitGameObject;
        }
        else { return null; }
    }
}