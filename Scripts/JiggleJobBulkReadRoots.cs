using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace GatorDragonGames.JigglePhysics {

[BurstCompile]
public struct JiggleJobBulkReadRoots : IJobParallelForTransform {
    public NativeArray<float3> rootOutputPositions;
    public NativeArray<quaternion> rootOutputRotations;

    public JiggleJobBulkReadRoots(JiggleMemoryBus bus) {
        rootOutputPositions = bus.rootOutputPositions;
        rootOutputRotations = bus.rootOutputRotations;
    }

    public void UpdateArrays(JiggleMemoryBus bus) {
        rootOutputPositions = bus.rootOutputPositions;
        rootOutputRotations = bus.rootOutputRotations;
    }
    public void Execute(int index, TransformAccess transform) {
        if (!transform.isValid) {
            return;
        }

        transform.GetPositionAndRotation(out var position, out var rotation);
        rootOutputPositions[index] = position;
        rootOutputRotations[index] = rotation;
    }
}

}
