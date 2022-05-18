// Each of these are float3's
#define WEIGHT_COUNT 24 
#define BINORMAL_COUNT 16
// These are just floats, we can have more
#define DISTANCE_COUNT 32

uniform int _PointCount;
uniform float _ArcLength;
uniform float _WeightArray[WEIGHT_COUNT*3];
uniform float _DistanceLUT[DISTANCE_COUNT];
uniform float _BinormalLUT[BINORMAL_COUNT*3];

float3 GetBinormalFromT(float t) {
    int count = BINORMAL_COUNT;
    int index = floor(t*(float)(count-1));
    float offseted = t-((float)index/(float)(count-1));
    float lerpT = offseted * (float)(count-1);
    float3 a = float3(_BinormalLUT[index*3],_BinormalLUT[index*3+1], _BinormalLUT[index*3+2]);
    float3 b = float3(_BinormalLUT[(index+1)*3],_BinormalLUT[(index+1)*3+1], _BinormalLUT[(index+1)*3+2]);
    return lerp(a, b, lerpT);
}
float GetCurveSegment(float t, out int curveSegmentIndex) {
    curveSegmentIndex = clamp((int)floor(t*(_PointCount-1)),0,_PointCount-1);
    float offset = t-((float)curveSegmentIndex/(float)(_PointCount-1));
    return offset * (float)(_PointCount-1);
}
float DistanceToTime(float distance) {
    int count = DISTANCE_COUNT;
    if (distance > 0 && distance < _ArcLength) {
        for(int i=0;i<count-1;i++) {
            if (distance>_DistanceLUT[i] && distance<_DistanceLUT[i+1]) {
                // Remap
                float value = distance;
                float from1 = _DistanceLUT[i];
                float to1 = _DistanceLUT[i+1];
                float from2 = (float)i/(float)(count-1);
                float to2 = (float)(i+1)/(float)(count-1);
                return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
            }
        }
    }
    return distance/_ArcLength;
}
float3 ToCatmullRomSpace(float3 dickRootPosition, float3 position, float4x4 worldToObject, float4x4 objectToWorld) {
    float3 dickForward = float3(0,1,0);
    float3 dickRight = float3(1,0,0);
    float3 dickUp = float3(0,0,1);

    float3 worldPosition = mul(objectToWorld,float4(position.xyz,1)).xyz;
    float3 worldDickRootPos = mul(objectToWorld,float4(dickRootPosition.xyz,1)).xyz;

    float3 worldDickForward = normalize(mul(objectToWorld,float4(dickForward.xyz,0)).xyz);
    float3 worldDickRight = normalize(mul(objectToWorld,float4(dickRight.xyz,0)).xyz);
    float3 worldDickUp = normalize(mul(objectToWorld,float4(dickUp.xyz,0)).xyz);
    
    float dist = dot(worldDickForward, (worldPosition - worldDickRootPos));

    float t = DistanceToTime(dist);
    int curveSegmentIndex = 0;
    float subT = GetCurveSegment(t, curveSegmentIndex);
    
    int index = curveSegmentIndex*3*4;
    float3 start =      float3(_WeightArray[index], _WeightArray[index+1], _WeightArray[index+2]);
    float3 tanPoint1 =  float3(_WeightArray[index+3], _WeightArray[index+4], _WeightArray[index+5]);
    float3 tanPoint2 =  float3(_WeightArray[index+6], _WeightArray[index+7], _WeightArray[index+8]);
    float3 end =        float3(_WeightArray[index+9], _WeightArray[index+10], _WeightArray[index+11]);

    float3 catPosition = (2.0 * subT * subT * subT - 3.0 * subT * subT + 1.0) * start
                    + (subT * subT * subT - 2.0 * subT * subT + subT) * tanPoint1
                    + (-2.0 * subT * subT * subT + 3.0 * subT * subT) * end
                    + (subT * subT * subT - subT * subT) * tanPoint2;

    float3 catTangent = (6.0 * subT * subT - 6.0 * subT) * start
                    + (3.0 * subT * subT - 4.0 * subT + 1.0) * tanPoint1
                    + (-6.0 * subT * subT + 6.0 * subT) * end
                    + (3.0 * subT * subT - 2.0 * subT) * tanPoint2;
    float3 catForward = normalize(catTangent);
    float3 catRight = GetBinormalFromT(t);
    float3 catUp = normalize(cross(catForward,catRight));

    float3x3 dickToCatmullBasisTransform = 0;
    dickToCatmullBasisTransform[0][0] = catRight.x;
    dickToCatmullBasisTransform[0][1] = catRight.y;
    dickToCatmullBasisTransform[0][2] = catRight.z;
    dickToCatmullBasisTransform[1][0] = catUp.x;
    dickToCatmullBasisTransform[1][1] = catUp.y;
    dickToCatmullBasisTransform[1][2] = catUp.z;
    dickToCatmullBasisTransform[2][0] = catForward.x;
    dickToCatmullBasisTransform[2][1] = catForward.y;
    dickToCatmullBasisTransform[2][2] = catForward.z;
    dickToCatmullBasisTransform = transpose(dickToCatmullBasisTransform);

    float3x3 worldToDickBasisTransform = 0;
    worldToDickBasisTransform[0][0] = worldDickRight.x;
    worldToDickBasisTransform[0][1] = worldDickRight.y;
    worldToDickBasisTransform[0][2] = worldDickRight.z;
    worldToDickBasisTransform[1][0] = -worldDickUp.x;
    worldToDickBasisTransform[1][1] = -worldDickUp.y;
    worldToDickBasisTransform[1][2] = -worldDickUp.z;
    worldToDickBasisTransform[2][0] = worldDickForward.x;
    worldToDickBasisTransform[2][1] = worldDickForward.y;
    worldToDickBasisTransform[2][2] = worldDickForward.z;

    float3 worldFrame = (worldPosition - (worldDickRootPos+worldDickForward*dist));
    //// Rotate back into local space
    float3 localFrame = mul(worldToDickBasisTransform, worldFrame.xyz).xyz;

    // Then finally switch basis into catmull space
    float3 worldFrameRotated = mul(dickToCatmullBasisTransform,localFrame).xyz;
    // Move into correct position
    float3 catmullSpacePosition = catPosition+worldFrameRotated;

    return mul(worldToObject,float4(catmullSpacePosition,1)).xyz;
}