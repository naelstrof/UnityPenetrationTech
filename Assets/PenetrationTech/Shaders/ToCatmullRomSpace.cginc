float3 ToCatmullRomSpace(float3 position, int curveSegmentIndex) {
    float t = position.z;
    // weights are 3 floats each, 4 weights represent a curve segment
    int index = curveSegmentIndex*4*3;
    float3 start =      float3(_WeightArray[index], _WeightArray[index+1], _WeightArray[index+2]);
    float3 tanPoint1 =  float3(_WeightArray[index+3], _WeightArray[index+4], _WeightArray[index+5]);
    float3 tanPoint2 =  float3(_WeightArray[index+6], _WeightArray[index+7], _WeightArray[index+8]);
    float3 end =        float3(_WeightArray[index+9], _WeightArray[index+10], _WeightArray[index+11]);

    float3 catPosition = (2 * t * t * t - 3 * t * t + 1) * start
                    + (t * t * t - 2 * t * t + t) * tanPoint1
                    + (-2 * t * t * t + 3 * t * t) * end
                    + (t * t * t - t * t) * tanPoint2;

    float3 catTangent = (6 * t * t - 6 * t) * start
                    + (3 * t * t - 4 * t + 1) * tanPoint1
                    + (-6 * t * t + 6 * t) * end
                    + (3 * t * t - 2 * t) * tanPoint2;

    // Raliv's clever upness/downness check to prevent tangent flipping
    float3 upness = lerp(up,-forward,saturate(dot(catTangent,up)));
    float3 downness = lerp(upness,forward,saturate(dot(catTangent,-up)));
    // Orthonormalize
    float3 catForward = normalize(catTangent);
    float3 catUp = normalize(cross(normtangent,normalize(downness)));
    float3 catRight = cross(up,forward);

    return catPosition + dot(up,position)*catUp + dot(right,position)*catright;
}