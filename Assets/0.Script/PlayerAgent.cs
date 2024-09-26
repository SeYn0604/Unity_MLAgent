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
    public float reloadTime = 2f;                   // 재장전 시간
    private float reloadTimer = 0f;

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
                Debug.Log("재장전 완료");
            }
        }

        bullettimer += Time.deltaTime;
        insideTimer += Time.deltaTime;

        if(hitcheck == 1) //bullet 스크립트에서 hitcheck 부여
        {
            UI.instance.KillCount++;
            AddReward(+10.0f);
            hitcheck = 0;
            Debug.Log("몬스터맞춤");
        }
        if(wallcheck == 1)
        {
            AddReward(-1f);
            wallcheck = 0;
            Debug.Log("벽맞춤");
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
        sensor.AddObservation(transform.position);
        sensor.AddObservation(target.position);
        sensor.AddObservation(isReloading ? 1f : 0f);
    }

    [SerializeField] float speed = 1.2f;
    Vector3 nextMove;
    public override void OnActionReceived(ActionBuffers actions)
    {
        nextMove.x = actions.ContinuousActions[0]*3;
        nextMove.y = actions.ContinuousActions[1]*3;
        transform.Translate(nextMove * Time.deltaTime * speed);

        float netDistance = Vector3.Distance(transform.position, referencePosition);

        /*if (netDistance >= netDistanceThreshold) // 보상 지급 및 기준 위치와 타이머 재설정
        {
            
            AddReward(0.1f);
            Debug.Log("이동 거리 보상 지급");
            referencePosition = transform.position;
            movementTimer = 0f;
        }
        else if (movementTimer >= movementTimeLimit) // 시간 내에 이동하지 못하면 패널티 부여
        {
            AddReward(-0.2f);
            Debug.Log("이동 시간 초과 패널티 부여");
            movementTimer = 0f;
        }*/
        /*if (bullettimer > 1.0f)
        {
            GameObject detectedMonster = RayCastInfo(m_rayPerceptionSensorComponent2D);
            if (detectedMonster != null && detectedMonster.CompareTag("Monster"))
            {
                Vector2 targetDirection = detectedMonster.transform.position - transform.position;
                CreateBullet(targetDirection.normalized);
                bullettimer = 0;
            }
        }*/ //예전에 사용된 총알 발사 구현부
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

                    // 탄약을 모두 소모하면 자동으로 재장전 시작
                    if (currentAmmo <= 0)
                    {
                        isReloading = true;
                        Debug.Log("탄약 소진, 재장전 시작");
                    }
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
            Debug.Log("impact monster");
            player.Hit(110);           //agent가 닿아도 감점만 되니 적극적인 회피를 하지않으므로 기존과 같이 몬스터와 닿을시 즉사판정으로 변경
            AddReward(-1.0f);
            Destroy(other.gameObject); 
            if(player.HP <= 0f)
            {
                EndEpisode();
                UI.instance.KillCount = 0;
                Debug.Log("초기화");
                Destroymonsters(); //에이전트가 사망하므로 에피소드 초기화 및 모든 몬스터 제거
            }
        }
        else if(other.gameObject.CompareTag("Wall"))
        {
            Debug.Log("impact wall");
            AddReward(-5.0f);
            EndEpisode();
            UI.instance.KillCount = 0;
            Debug.Log("벽에부딫혀초기화");
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