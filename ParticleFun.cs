using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;


#pragma warning disable 0649

public class ParticleFun : MonoBehaviour
{

    private Vector2 cursorPos;

    // struct
    struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public float life;
    }

    const int SIZE_PARTICLE = 7 * sizeof(float);

    public int particleCount = 1000000;
    public Material material;
    public ComputeShader shader;
    [Range(1, 10)]
    public int pointSize = 2;

    int kernelID;
    ComputeBuffer particleBuffer;
    Controller controller;

    int groupSizeX; 
    
    
    // Use this for initialization
    void Start()
    {
        Init();
    }

    void Init()
    {
        // initialize the particles
        Particle[] particleArray = new Particle[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            //TO DO: Initialize particle\
            Vector3 v = new Vector3();
            v.x = Random.value * 2 - 1.0f;
            v.y = Random.value * 2 - 1.0f;
            v.z = Random.value * 2 - 1.0f;

            v.Normalize();
            v*=Random.value * 0.5f;

            particleArray[i].position.x = v.x;
            particleArray[i].position.y = v.y;
            particleArray[i].position.z = v.z + 3;

            particleArray[i].velocity.x = 0;
            particleArray[i].velocity.y = 0;
            particleArray[i].velocity.z = 0;

            particleArray[i].life = Random.value * 5.0f + 1.0f;
        }

        // create compute buffer
        particleBuffer = new ComputeBuffer(particleCount, SIZE_PARTICLE);

        particleBuffer.SetData(particleArray);

        // find the id of the kernel
        kernelID = shader.FindKernel("CSParticle");

        uint threadsX;
        shader.GetKernelThreadGroupSizes(kernelID, out threadsX, out _, out _);
        groupSizeX = Mathf.CeilToInt((float)particleCount / (float)threadsX);

        // bind the compute buffer to the shader and the compute shader
        shader.SetBuffer(kernelID, "particleBuffer", particleBuffer);
        material.SetBuffer("particleBuffer", particleBuffer);

        material.SetInt("_PointSize", pointSize);

        controller = new Controller();
    }

    void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, particleCount);
    }

    void OnDestroy()
    {
        if (particleBuffer != null)
            particleBuffer.Release();
    }

    // Update is called once per frame
    void Update()
    {

        float[] mousePosition2D = { cursorPos.x, cursorPos.y };

        // Send datas to the compute shader
        shader.SetFloat("deltaTime", Time.deltaTime);
        shader.SetFloats("mousePosition", mousePosition2D);

        // Update the Particles
        shader.Dispatch(kernelID, groupSizeX, 1, 1); 
    }

    void OnGUI()
    {
    ;


        Vector3 p = new Vector3();
        Camera c = Camera.main;
        Event e = Event.current;
        Vector2 mousePos = new Vector2();

        int appWidth = c.pixelWidth;
        int appHeight = c.pixelHeight;
        InteractionBox iBox = leap.Frame().InteractionBox;


        if(leap.Frame().Hands.Count > 0){
            Hand hand = leap.Frame().Hands[0];

            Leap.Vector leapPoint = hand.StabilizedPalmPosition;
            Leap.Vector normalizedPoint = iBox.NormalizePoint(leapPoint, false);
  
            mousePos.x = normalizedPoint.x * appWidth;
            mousePos.y = (1 - normalizedPoint.y) * appHeight;
            //The z-coordinate is not used
        }

        else{
            mousePos.x = appWidth/2;
            mousePos.y = appHeight/2;
        }
        
        
        // Get the mouse position from Event.
        // Note that the y position from Event is inverted.
       // mousePos.x = e.mousePosition.x;
        //mousePos.y = c.pixelHeight - e.mousePosition.y;

        p = c.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, c.nearClipPlane + 14));// z = 3.

        cursorPos.x = p.x;
        cursorPos.y = p.y;
        
    }
}
