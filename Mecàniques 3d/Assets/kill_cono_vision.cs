﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

public class kill_cono_vision : MonoBehaviour {

    private int angulo = 140;
    private int w_ref = 40;
    private int rango = 175;

    MeshFilter meshFilter;
    Vector3 oldPosition;
    Quaternion oldRotation;
    Vector3 oldScale;

    private struct targetData
    {
        public bool seen;
        public Vector3 killTargetPos;
        public csAreaVision targetState;
        public GameObject target;
        public Renderer targetRenderer;
        public float ghostRef;
    };

    targetData[] targets;
    private int targetI;
    static Animator anim;
    private bool auxPressed;
    public static bool returnPlayer;
    private GameObject player;
    private liquidState liquidKill;
    private movement playerMovement;
    private Vector3 playerPos;
    private float altura;
    private bool aproaching;
    private bool stuck;
    private float stuckReference;
    enum killState { WATCHING, APROACHING, KILLING, RETURNING };
    public static string actualString;
    killState actualState;

    //Nav Mesh
    NavMeshAgent liquidAgent;
    public Material[] textures;

    Mesh Cono()
    {

        Mesh _cono = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();

        Vector3 oldPosition, temp;
        oldPosition = temp = Vector3.zero;

        vertices.Add(Vector3.zero);
        normals.Add(Vector3.up);
        uv.Add(Vector2.one * 0.5f);

        int w, s;
        for (w = w_ref; w < angulo; w++)
        {

            for (s = 0; s < rango; s++)
            {
                temp.x = Mathf.Cos(Mathf.Deg2Rad * w + Mathf.Deg2Rad * (s / rango)) * rango;
                temp.z = Mathf.Sin(Mathf.Deg2Rad * w + Mathf.Deg2Rad * (s / rango)) * rango;

                if (oldPosition != temp)
                {

                    oldPosition = temp;
                    vertices.Add(new Vector3(temp.x, temp.y, temp.z));
                    normals.Add(Vector3.up);
                    uv.Add(new Vector2((rango + temp.x) / (rango * 2), (rango + temp.z) / (rango * 2)));

                }

            }

        }

        int[] triangles = new int[(vertices.Count - 2) * 3];
        s = 0;

        for (w = 1; w < (vertices.Count - 2); w++)
        {

            triangles[s++] = w + 1;
            triangles[s++] = w;
            triangles[s++] = 0;

        }

        _cono.vertices = vertices.ToArray();
        _cono.normals = normals.ToArray();
        _cono.uv = uv.ToArray();
        _cono.triangles = triangles;

        return _cono;

    }

    Vector3[] initialPosition;
    Vector2[] initialUV;

    // Use this for initialization
    void Awake()
    {
        targets = new targetData[2];
        for(int i = 0; i < targets.Length; i++)
        {
            targets[i] = new targetData();
            targets[i].seen = false;
            targets[i].targetRenderer = null;
            targets[i].targetState = null;
            targets[i].target = null;
            targets[i].ghostRef = Time.realtimeSinceStartup;
            targets[i].killTargetPos = new Vector3(0.0f, 0.0f, 0.0f);
        }
        stuckReference = 0.0f;
        stuck = false;
        auxPressed = false;
        player = GameObject.Find("Jugador");
        liquidKill = player.GetComponent<liquidState>();
        playerMovement = player.GetComponent<movement>();
        liquidAgent = player.GetComponent<NavMeshAgent>();
        if (liquidAgent == null)
        {
            Debug.LogError("Nav Mesh error");
        }
        liquidAgent.enabled = false;
        actualState = killState.WATCHING;
        altura = 0;
        aproaching = false;
        meshFilter = transform.GetComponent<MeshFilter>();
        meshFilter.mesh = Cono();
        initialPosition = meshFilter.mesh.vertices;
        initialUV = meshFilter.mesh.uv;
        anim = player.GetComponent<Animator>();
        returnPlayer = false;
    }

    Mesh areaMesh(Mesh mesh)
    {

        Mesh _mesh = new Mesh();
        Vector3[] vertices = new Vector3[mesh.vertices.Length];
        Vector2[] uv = new Vector2[mesh.uv.Length];

        Vector3 center = transform.localToWorldMatrix.MultiplyPoint3x4(initialPosition[0]);
        uv[0] = initialUV[0];
        Vector3 worldPoint;

        RaycastHit hit = new RaycastHit();

        targets[0].seen = targets[1].seen = false;
        targetI = 0;
        for (int i = 1; i < vertices.Length; i++)
        {

            worldPoint = transform.localToWorldMatrix.MultiplyPoint3x4(initialPosition[i]);
            if (Physics.Linecast(center, worldPoint, out hit))
            {
                if (hit.transform.gameObject.tag == "enemy" && targetI < targets.Length - 1)
                {
                    
                    if (targets[targetI].seen) targetI = 1;
                    targets[targetI].seen = true;
                    targets[targetI].ghostRef = Time.realtimeSinceStartup;
                    targets[targetI].target = hit.transform.gameObject;
                    targets[targetI].target.transform.GetChild(4).gameObject.SetActive(true);
                    targets[targetI].targetRenderer = targets[targetI].target.transform.GetChild(4).gameObject.GetComponent<Renderer>();
                    targets[targetI].targetState = targets[targetI].target.GetComponent<csAreaVision>();
                    if (liquidState.hidratation > 0 && targets[targetI].targetState.canBeKilled())
                    {
                        targets[targetI].targetRenderer.material = textures[0];

                        if (Input.GetKeyDown(KeyCode.Mouse0))
                        {
                            playerPos = player.transform.position;
                            playerMovement.state = movement.playerState.HITTING;
                            liquidAgent.enabled = true;
                            liquidAgent.SetDestination(targets[0].target.transform.GetChild(4).gameObject.transform.position);
                            player.GetComponent<Rigidbody>().useGravity = false;
                            targets[targetI].target.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePosition;
                            player.GetComponent<Collider>().enabled = false;
                            targets[targetI].target.GetComponent<Collider>().enabled = false;
                            targets[targetI].target.GetComponent<Rigidbody>().useGravity = false;
                            anim.SetBool("Is_Damaging", true);
                            targets[targetI].target.GetComponent<NavMeshAgent>().enabled = false;
                            targets[targetI].target.GetComponent<NavMeshObstacle>().enabled = true;
                            targets[targetI].target.transform.GetChild(4).gameObject.SetActive(false);
                            stuckReference = Time.realtimeSinceStartup;
                            liquidKill.firstFrameNormal = false;
                            liquidKill.cooldown = false;
                            liquidKill.showLiquid();
                            actualState = killState.APROACHING;
                            targets[targetI].killTargetPos = targets[targetI].target.transform.position;
                        }
                    }
                    else targets[targetI].targetRenderer.material = textures[1];
                }
                if (hit.transform.position != transform.position)
                {
                    vertices[i] = transform.worldToLocalMatrix.MultiplyPoint3x4(hit.point);
                    uv[i] = new Vector2((rango + vertices[i].x) / (rango * 2), (rango + vertices[i].z) / (rango * 2));
                }

            }
            else
            {

                vertices[i] = initialPosition[i];
                uv[i] = initialUV[i];

            }

        }

        targetI = 0;
        _mesh.vertices = vertices;
        _mesh.uv = uv;
        _mesh.normals = mesh.normals;
        _mesh.triangles = mesh.triangles;

        return _mesh;



    }

    // Update is called once per frame

    private void Update()
    {
        switch(actualState)
        {
            case killState.WATCHING:
                if (anim.GetBool("Is_Detected")) kill_vision();
                if(!anim.GetBool("Is_Damaging")) draw_Weapon();
                actualString = "W";
                break;
            case killState.APROACHING:
                if ((liquidAgent.remainingDistance <= 0.0f && stuckReference + 0.5f < Time.realtimeSinceStartup) || stuckReference + 2.5f < Time.realtimeSinceStartup) aproachEnemy(targets[targetI].killTargetPos);
                actualString = "A";
                break;
            case killState.KILLING:
                if (returnPlayer) setReturn();
                actualString = "K";
                break;
            case killState.RETURNING:
                if ((liquidAgent.remainingDistance <= 0.0f && stuckReference + 0.5f < Time.realtimeSinceStartup) || stuckReference + 2.5f < Time.realtimeSinceStartup) returnToPosition();
                actualString = "R";
                break;
            default:
                break;
        }
    }
    public void kill_vision()
    {
        meshFilter.mesh = areaMesh(meshFilter.mesh);
        CheckGhost();
    }

    private void aproachEnemy(Vector3 destination)
    {
        liquidKill.hideLiquid();
        //player.transform.position = target.transform.GetChild(4).transform.position;
        player.GetComponent<Rigidbody>().transform.LookAt(destination);
        actualState = killState.KILLING;
        liquidAgent.enabled = false;
        //StartCoroutine(ExecuteAfterTime(1.0f));
        aproaching = false;
        player.GetComponent<Rigidbody>().velocity *= 0;
        targets[targetI].target.gameObject.GetComponent<Animator>().SetTrigger("Is_Dying");
        targets[targetI].targetState.dead = true;
        killEnemy();
    }

    private void killEnemy()
    {
        
        anim.SetBool("Is_Running", false);
            anim.SetBool("Is_Crouching", false);
            anim.SetBool("Is_Walking", false);
            anim.SetBool("Is_Idle", false);
            anim.SetTrigger("Is_Hitting");
    }

    private void returnToPosition()
    {
        player.GetComponent<Collider>().enabled = true;
        player.GetComponent<Rigidbody>().useGravity = true;
        liquidKill.setHidratation();
        liquidKill.hideLiquid();
        playerMovement.state = movement.playerState.IDLE;
        liquidAgent.enabled = false;
        actualState = killState.WATCHING;
    }

    void CheckGhost()
    {
        if(targets[targetI].target != null && targets[targetI].ghostRef + 0.1f < Time.realtimeSinceStartup)
            targets[targetI].target.transform.GetChild(4).gameObject.SetActive(false);
    }
   /* public void finishAnim(int message)
    {
        if (message == 1)
        {
            targetI++;
            anim.SetBool("Is_Damaging", false);
            stuckReference = Time.realtimeSinceStartup;
            liquidKill.showLiquid();
            liquidAgent.enabled = true;
            if (targetI < targets.Length && targets[targetI].seen)
            {
                liquidAgent.SetDestination(playerPos);
            }
            else
            {
                actualState = killState.RETURNING;
                liquidAgent.SetDestination(playerPos);
            }
        }

    }*/

    private void draw_Weapon()
    {
        if (!auxPressed && anim.GetBool("Is_Draw"))
        {
            if (Input.GetKeyDown(KeyCode.Mouse1) && anim.GetBool("Is_Detected"))
            {
                anim.SetBool("Is_Detected", false);
                anim.ResetTrigger("Is_Sheathing");
                anim.SetTrigger("Is_Sheathing");
                auxPressed = true;
            }
            else if ((Input.GetKeyDown(KeyCode.Mouse1) || Input.GetKeyDown(KeyCode.Mouse0)) && !anim.GetBool("Is_Detected"))
            {
                anim.SetBool("Is_Detected", true);
                anim.ResetTrigger("Is_Withdrawing");
                anim.SetTrigger("Is_Withdrawing");
                auxPressed = true;
            }
        }
        else if (!Input.GetKeyDown(KeyCode.Mouse1)) auxPressed = false;
    }


    void setReturn()
    {
        targetI++;
        anim.SetBool("Is_Damaging", false);
       // actualState = killState.RETURNING;
        stuckReference = Time.realtimeSinceStartup;
        liquidKill.showLiquid();
        liquidAgent.enabled = true;
        //liquidAgent.SetDestination(playerPos);
        if (targetI < targets.Length && targets[targetI].seen)
        {
            actualState = killState.APROACHING;
            liquidAgent.SetDestination(targets[targetI].target.transform.GetChild(4).gameObject.transform.position);
            Debug.Log("dos");
        }
        else
        {
            actualState = killState.RETURNING;
            liquidAgent.SetDestination(playerPos);
        }
        returnPlayer = false;
    }
}
