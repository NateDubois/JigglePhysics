using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GatorDragonGames.JigglePhysics {

[BurstCompile]
public struct JiggleJobInputInterpolation : IJobFor {
    [ReadOnly] public NativeArray<JiggleTransform> previousInputs;
    [ReadOnly] public NativeArray<JiggleTransform> currentInputs;

    public double timeStamp;
    public double previousTimeStamp;
    
    public double currentTime;

    public NativeArray<JiggleTransform> outputInterpolatedPoses;
    public float timeCorrection;

    public JiggleJobInputInterpolation(JiggleMemoryBus bus, double time, float fixedDeltaTime) {
        timeCorrection = fixedDeltaTime;
        timeStamp = time - fixedDeltaTime;
        previousTimeStamp = timeStamp - fixedDeltaTime;
        currentTime = timeStamp;
        outputInterpolatedPoses = bus.simulateInputPoses;
        previousInputs = bus.inputPosesPrevious;
        currentInputs = bus.inputPosesCurrent;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        previousInputs = bus.inputPosesPrevious;
        currentInputs = bus.inputPosesCurrent;
        outputInterpolatedPoses = bus.simulateInputPoses;
    }
    
    public void SetFixedDeltaTime(float fixedDeltaTime) {
        timeCorrection = fixedDeltaTime;
    }

    public void Execute(int index) {
        var prevPose = previousInputs[index];
        var newPose = currentInputs[index];

        var diff = timeStamp - previousTimeStamp;
        if (diff <= 0) {
            outputInterpolatedPoses[index] = newPose;
            return;
        }

        // Resample the animated pose onto the simulation's own clock. currentTime is the
        // simulation timestamp, which advances in uniform fixedDeltaTime steps, so every
        // integration step receives an equal slice of animation -- sampling at the render rate
        // instead aliases against it and makes the motion stutter.
        //
        // This deliberately does NOT subtract timeCorrection. Doing so double-lagged the sample
        // (currentTime already trails timeStamp by up to one fixedDeltaTime) and drove t negative,
        // extrapolating backwards past previousInputs. Because the sim ticks at 1/fixedDeltaTime
        // while transforms are read at the render rate, that error beat against the frame rate:
        // at 60fps with fixedDeltaTime = 0.02 it cycled +0.33, -0.80, -0.60, -0.40, -0.20, 0.00
        // every six frames. Since JiggleTransform.Lerp is linear in position, the swing cut chords
        // across the rotation arc of a moving rig, deforming the chain rather than displacing it.
        // A uniform translation of the input is cancelled downstream by the root snap in
        // JiggleJobInterpolation (rootPose jitters along with every bone), but lever-arm error from
        // a rotating root is not, so it surfaced as rubber-banding on rigs whose root is driven by
        // an Aim Constraint.
        var t = math.saturate((currentTime - previousTimeStamp) / diff);
        outputInterpolatedPoses[index] = JiggleTransform.Lerp(prevPose, newPose, (float)t);
    }
}

}