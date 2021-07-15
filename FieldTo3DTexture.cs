using System;
using System.Collections;
using System.Collections.Generic;
using EMP.Core;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class FieldTo3DTexture : MonoBehaviour
{
    [SerializeField] private Texture3D tex;
    [SerializeField] private ParticleSystemForceField particleSystemForceField;
    
    [SerializeField] private InteractionField fieldType;
    [SerializeField] private float sideLength = 5;
    private const int texSize = 32;
    
    private NativeArray<float3> reactorPositions;
    private NativeArray<float3> fieldsAtReactors;
    private bool listHasChanged = true;

    public static float LogisticScale(float x) {
        // See Mathematica workbook in docs to show what these do
        float A = 0.2f;
        float K = 1f;
        float C = 1f;
        float B = 5f;
        float nu = 0.5f;
        float Q = 50f;
        return A + (K - A) / math.pow(C + Q * math.exp(-B * x), 1 / nu);
    }

    private void OnEnable()
    {
        // Prevents annoying scaling issue where particles act according to a rescaled version of the charge config
        particleSystemForceField.endRange = sideLength / 2f;
    }

    private void OnDisable()
    {
        reactorPositions.Dispose();
        fieldsAtReactors.Dispose();
    }
    
    private void FixedUpdate()
    {
        EMController emp = EMController.Instance;
        List<PointChargeGenerator> generators = emp.GetFieldGenerators<PointChargeGenerator>(fieldType);
        
        
        
        
        if (listHasChanged)
        {
            GenerateNativeArrays();
        }
        
        //foreach (FieldReactor FR in fieldReactors)
        //{
        //    FR.ApplyForces();
        //    FR.ApplyTorques();
        //}

        
        //NativeArray<float3> generatorPositions = new NativeArray<float3>(fieldGenerators.Select(x => ((float3)x.Position)).ToArray(),Allocator.TempJob);
        //reactorPositions.CopyFrom(fieldReactorArray.Select(x => (float3) x.transform.position).ToArray());
        for (int i = 0; i < fieldsAtReactors.Length; i++)
        {
            fieldsAtReactors[i] = Vector3.zero;
            //reactorPositions[i] = fieldReactors[i].transform.position;
        }
        
        JobHandle handle = default;
        NativeArray<JobHandle> handles = new NativeArray<JobHandle>(generators.Count, Allocator.Temp);

        for (int geni = 0; geni < generators.Count; geni++)
        {
            
            // Old approach
            // // Get generator-specific job data
            // PointChargeGenerator.ComputeFieldContributionJob jobData =
            //     ((PointChargeGenerator) fieldGenerators[geni]).GetJobData(reactorPositions, fieldsAtReactors);
            // // Schedule job set
            // handle = jobData.Schedule(reactorPositions.Length, 4, handle);
            
            // New approach, having each generator handle its own job scheduling:
            handle = generators[geni].ScheduleFieldComputationJobs(
                reactorPositions,
                fieldsAtReactors,
                reactorPositions.Length,
                4,
                handle);


            //handles[geni] = jobData.Schedule(reactorPositions.Length, 4);
            // For debugging:
            //handle.Complete();

            //continue;
        }
        

        // With all generators' jobs scheduled, wait for completion
        //handle = JobHandle.CombineDependencies(handles);
        handle.Complete();
        
        
        handles.Dispose();

        float4[] formattedFields = new float4[fieldsAtReactors.Length];
        
        for (int i = 0; i < fieldsAtReactors.Length; i++)
        {
            // take magnitude of float3, put that into a function, get rescaled magnitude out
            // float length = Mathf.Sqrt(Mathf.Pow(fieldsAtReactors[i].x, 2) + Mathf.Pow(fieldsAtReactors[i].y, 2) + Mathf.Pow(fieldsAtReactors[i].z, 2));
            // float length2 = math.length(fieldsAtReactors[i]);
            // float lengthScaled = LogisticScale(length2);

            //fieldsAtReactors[i] = fieldsAtReactors[i] * lengthScaled / length2;

            formattedFields[i] = new float4(fieldsAtReactors[i].x, fieldsAtReactors[i].y, fieldsAtReactors[i].z, 0.2f); // * lengthScaled / length2;
            // formattedFields[i] = new float4(0.2f, 0.8f, 0.2f, 0.2f);
        }
        
        //Debug.Log($"Calculated fields length: {fieldsAtReactors.Length}. Texture data length: {tex.depth*tex.height*tex.width}");
        
        tex.SetPixelData(formattedFields,0);
        tex.Apply(false);
        
        
        
        // Apply to reactors
        // for (int ri = 0; ri < fieldReactors.Count; ri++)
        // {
        //     ((PointChargeElectricReactor) fieldReactors[ri]).ApplyForceFromField(fieldsAtReactors[ri]);
        // }

        ParticleSystem ps = GetComponent<ParticleSystem>();
        ParticleSystem.ExternalForcesModule fmod = ps.externalForces;
        
        if (framesBetweenUpdates <= framesSinceUpdate)
        {
            //particleSystemForceField.vectorField = null;
            //ps.Stop();
            
            fmod.RemoveInfluence(particleSystemForceField);
            fmod.enabled = false;
            particleSystemForceField.vectorField = null;
            //fmod.enabled = false;
            //Destroy(particleSystemForceField);
            // Debug.Log($"Disabled fmod. New state from object: {ps.forceOverLifetime.enabled}");
            Debug.Log("Destroyed old force field.",this);
            framesSinceUpdate = 0;
            GenerateNativeArrays();
        }
        
        if (framesSinceUpdate >= 6 && fmod.enabled == false)
        {
            fmod.enabled = true;
            
            //particleSystemForceField.vectorField = tex;
            // ps.Play();
            // Debug.Log("Updated vector field.",this);
            
            // particleSystemForceField = gameObject.AddComponent<ParticleSystemForceField>();
            // particleSystemForceField.vectorField = tex;
            // particleSystemForceField.vectorFieldAttraction = 1;
            // particleSystemForceField.vectorFieldSpeed = 1;
            // particleSystemForceField.shape = ParticleSystemForceFieldShape.Box;
            Texture3D newTex = Instantiate(tex);
            particleSystemForceField.vectorField = newTex;
            fmod.AddInfluence(particleSystemForceField);
            // fmod.enabled = true;
        }

        framesSinceUpdate++;
    }

    // DEFAULT VALUE 180, TESTING FASTER REFRESH RATE
    private int framesBetweenUpdates = 180;
    private int framesSinceUpdate = 0;
    
    private void GenerateNativeArrays()
    {
        // Dispose of old ones
        try
        {
            reactorPositions.Dispose();
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e);
            //throw;
        }
            
        try
        {
            fieldsAtReactors.Dispose();
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e);
            //throw;
        }

        
        
        reactorPositions = new NativeArray<float3>(texSize*texSize*texSize, Allocator.Persistent);

        float halfBoxStep = sideLength / (2 * texSize);
        float boxStep = halfBoxStep * 2;
        
        for (int i = 0; i < texSize; i++)
        {
            for (int j = 0; j < texSize; j++)
            {
                for (int k = 0; k < texSize; k++)
                {
                    float3 newPos = new float3(
                        halfBoxStep + (k - texSize / 2) * boxStep,
                        halfBoxStep + (j - texSize / 2) * boxStep,
                        halfBoxStep + (i - texSize / 2) * boxStep);
                    newPos = transform.TransformPoint(newPos);
                    reactorPositions[i * 32 * 32 + j * 32 + k] = newPos;
                }
            }
        }

        Debug.Log($"Reactor corner 1: {reactorPositions[0]}, Reactor corner 2: {reactorPositions[reactorPositions.Length - 1]}");


        fieldsAtReactors = new NativeArray<float3>(texSize*texSize*texSize, Allocator.Persistent);

        listHasChanged = false;
        Debug.Log($"Generated new reactor position and field arrays of length {fieldsAtReactors.Length}");
    }
}
