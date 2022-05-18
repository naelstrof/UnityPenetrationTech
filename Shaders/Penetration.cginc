#define BINORMAL_COUNT 16
#define DISTANCE_COUNT 32
uniform int _PointCount;
uniform float _ArcLength;
uniform float3 _WeightArray[32];
uniform float _DistanceLUT[DISTANCE_COUNT];
uniform float3 _BinormalLUT[BINORMAL_COUNT];

float3 GetBinormalFromT(float t) {
    int count = BINORMAL_COUNT;
    int index = floor(t*(float)(count-1));
    float offseted = t-((float)index/(float)(count-1));
    float lerpT = offseted * (float)(count-1);
    return lerp(_BinormalLUT[index], _BinormalLUT[index+1], lerpT);
}
float GetCurveSegment(float t, out int curveSegmentIndex) {
    curveSegmentIndex = clamp((int)floor(t*(float)(_PointCount-1.0)),0,_PointCount-1);
    float offset = t-((float)curveSegmentIndex/(float)(_PointCount-1.0));
    return offset * (float)(_PointCount-1.0);
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
                float from2 = (float)i/(float)(count-1.0);
                float to2 = (float)(i+1.0)/(float)(count-1.0);
                return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
            }
        }
    }
    return distance/_ArcLength;
}
float3 ToCatmullRomSpace(float3 dickRootPosition, float3 position, float4x4 worldToObject, float4x4 objectToWorld) {
    float3 dickForward = float3(1,0,0);

    float3 worldPosition = mul(objectToWorld,float4(position.xyz,1)).xyz;
    float3 worldDickRootPos = mul(objectToWorld,float4(dickRootPosition.xyz,1)).xyz;
    float3 worldDickForward = mul(objectToWorld,float4(dickForward.xyz,0)).xyz;
    
    float dist = dot(worldDickForward, (worldPosition - worldDickRootPos));

    float t = DistanceToTime(dist);
    int curveSegmentIndex = 0;
    float subT = GetCurveSegment(t, curveSegmentIndex);
    
    int index = curveSegmentIndex*4;
    float3 start =      _WeightArray[index];
    float3 tanPoint1 =  _WeightArray[index+1];
    float3 tanPoint2 =  _WeightArray[index+2];
    float3 end =        _WeightArray[index+3];

    float3 catPosition = (2.0 * t * t * t - 3.0 * t * t + 1.0) * start
                    + (t * t * t - 2.0 * t * t + t) * tanPoint1
                    + (-2.0 * t * t * t + 3.0 * t * t) * end
                    + (t * t * t - t * t) * tanPoint2;

    float3 catTangent = (6.0 * t * t - 6.0 * t) * start
                    + (3.0 * t * t - 4.0 * t + 1.0) * tanPoint1
                    + (-6.0 * t * t + 6.0 * t) * end
                    + (3.0 * t * t - 2.0 * t) * tanPoint2;
    float3 catForward = normalize(catTangent);
    float3 catRight = GetBinormalFromT(t);
    float3 catUp = normalize(cross(catForward,catRight));
    // Get frame at the specified time
    /*float4x4 basisTransform = 0;
    basisTransform[0][0] = catRight.x;
    basisTransform[1][0] = catRight.y;
    basisTransform[2][0] = catRight.z;
    basisTransform[0][1] = catUp.x;
    basisTransform[1][1] = catUp.y;
    basisTransform[2][1] = catUp.z;
    basisTransform[0][2] = catForward.x;
    basisTransform[1][2] = catForward.y;
    basisTransform[2][2] = catForward.z;
    basisTransform[3][3] = 1;
    basisTransform = transpose(basisTransform);

    float3 frameReferencePosition = mul(basisTransform,float4((worldPosition - worldDickForward*dist),0)).xyz;
    float3 catmullSpacePosition = catPosition+frameReferencePosition;*/

    return mul(worldToObject,float4(catPosition,1)).xyz;
}