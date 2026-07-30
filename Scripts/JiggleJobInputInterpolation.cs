using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
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
        // The transforms backing currentInputs were read fresh this frame, so they are the most
        // accurate constraint targets the solver can be given.
        //
        // This used to interpolate back toward previousInputs by
        //     t = (currentTime - timeCorrection - previousTimeStamp) / (timeStamp - previousTimeStamp)
        // which was neither clamped nor stable: because the sim ticks at 1/fixedDeltaTime while
        // transforms are read at the render rate, t beat against the frame rate and swung over a
        // range wider than a full frame (+0.33 to -0.80 at 60fps with fixedDeltaTime = 0.02,
        // repeating every 6 frames), extrapolating backwards past previousInputs on most frames.
        //
        // Since JiggleTransform.Lerp is linear in position, that jitter cut chords across the
        // rotation arc of a moving rig, deforming the chain rather than displacing it. A uniform
        // translation of the input is cancelled downstream by the root snap in
        // JiggleJobInterpolation (rootPose jitters with it), but lever-arm error from a rotating
        // root is not, so it surfaced as rubber-banding on rigs whose root is driven by an Aim
        // Constraint. Feeding the fresh pose through removes the jitter and 1-2 fixedDeltaTime of
        // input latency along with it.
        outputInterpolatedPoses[index] = currentInputs[index];
    }
}

}