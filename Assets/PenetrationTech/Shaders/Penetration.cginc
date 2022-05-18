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
    // This depends on the model, blender defaults to Y forward, X right, and Z up.
    float3 dickForward = float3(0,1,0);
    float3 dickRight = float3(1,0,0);
    float3 dickUp = float3(0,0,1);

    // We want to work in world space, as everything we're working with is there. Here we convert everything into world space.
    float3 worldPosition = mul(objectToWorld,float4(position.xyz,1)).xyz;
    float3 worldDickRootPos = mul(objectToWorld,float4(dickRootPosition.xyz,1)).xyz;

    // Ensure these are world space directions by normalizing and discarding the w component.
    float3 worldDickForward = normalize(mul(objectToWorld,float4(dickForward.xyz,0)).xyz);
    float3 worldDickRight = normalize(mul(objectToWorld,float4(dickRight.xyz,0)).xyz);
    float3 worldDickUp = normalize(mul(objectToWorld,float4(dickUp.xyz,0)).xyz);
    
    // Dot product gives us how far along an axis a position is. This is the dick length distance from the dick root to the particular position.
    float dist = dot(worldDickForward, (worldPosition - worldDickRootPos));

    // Convert the distance into an overall t sample value
    float t = DistanceToTime(dist);
    // Since our t sample value is based on a piece-wise curve, we need to figure out which curve weights we're meant to sample.
    int curveSegmentIndex = 0;
    float subT = GetCurveSegment(t, curveSegmentIndex);
    
    // Grab the weights, unfortunately they're packed as floats so we access them like this. I don't think this has any effective slowdown though.
    int index = curveSegmentIndex*3*4;
    float3 start =      float3(_WeightArray[index], _WeightArray[index+1], _WeightArray[index+2]);
    float3 tanPoint1 =  float3(_WeightArray[index+3], _WeightArray[index+4], _WeightArray[index+5]);
    float3 tanPoint2 =  float3(_WeightArray[index+6], _WeightArray[index+7], _WeightArray[index+8]);
    float3 end =        float3(_WeightArray[index+9], _WeightArray[index+10], _WeightArray[index+11]);

    // Using the expanded form of a Hermite basis functions
    // https://en.wikipedia.org/wiki/Cubic_Hermite_spline
    // p(t) = (2t³ - 3t² + 1)p₀ + (t³ - 2t² + t)m₀ + (-2t³ + 3t²)p₁ + (t³ - t²)m₁
    float3 catPosition = (2.0 * subT * subT * subT - 3.0 * subT * subT + 1.0) * start
                    + (subT * subT * subT - 2.0 * subT * subT + subT) * tanPoint1
                    + (-2.0 * subT * subT * subT + 3.0 * subT * subT) * end
                    + (subT * subT * subT - subT * subT) * tanPoint2;

    // First derivative (velocity)
    // p'(t) = (6t² - 6t)p₀ + (3t² - 4t + 1)m₀ + (-6t² + 6t)p₁ + (3t² - 2t)m₁
    float3 catTangent = (6.0 * subT * subT - 6.0 * subT) * start
                    + (3.0 * subT * subT - 4.0 * subT + 1.0) * tanPoint1
                    + (-6.0 * subT * subT + 6.0 * subT) * end
                    + (3.0 * subT * subT - 2.0 * subT) * tanPoint2;

    // Forward would be our tangent.
    float3 catForward = normalize(catTangent);
    // We sample the Binormal from a lookup-table, to prevent flipping and twisting.
    // https://en.wikipedia.org/wiki/Frenet%E2%80%93Serret_formulas
    // https://janakiev.com/blog/framing-parametric-curves/
    float3 catRight = GetBinormalFromT(t);
    // We can just figure out our normal with a cross product.
    float3 catUp = normalize(cross(catForward,catRight));


    // Change of basis https://math.stackexchange.com/questions/3540973/change-of-coordinates-and-change-of-basis-matrices
    // It also shows up here: https://docs.unity3d.com/ScriptReference/Vector3.OrthoNormalize.html
    // Goes from dick space into catmull rom space.
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

    // Goes from XYZ world space, into dX,dY,dZ space (where dX,dY,dZ are dick orientations.)
    float3x3 worldToDickBasisTransform = 0;
    worldToDickBasisTransform[0][0] = worldDickRight.x;
    worldToDickBasisTransform[0][1] = worldDickRight.y;
    worldToDickBasisTransform[0][2] = worldDickRight.z;
    worldToDickBasisTransform[1][0] = -worldDickUp.x; // FIXME: Not sure why some of these need to be inverted. I'm guessing it has to do with the model's orientation-- or the bitangent of the curve is backwards.
    worldToDickBasisTransform[1][1] = -worldDickUp.y;
    worldToDickBasisTransform[1][2] = -worldDickUp.z;
    worldToDickBasisTransform[2][0] = worldDickForward.x;
    worldToDickBasisTransform[2][1] = worldDickForward.y;
    worldToDickBasisTransform[2][2] = worldDickForward.z;

    // Frame refers to the particular slice of the model we're working on, 0,0,0 being the core of the cylinder.
    float3 worldFrame = (worldPosition - (worldDickRootPos+worldDickForward*dist));
    // Rotate into dick space, using the basis transform
    float3 localFrame = mul(worldToDickBasisTransform, worldFrame.xyz).xyz;

    // Then we basis transform it again into catmull rom-space, with another basis transform.
    float3 worldFrameRotated = mul(dickToCatmullBasisTransform,localFrame).xyz;

    // It will still be centered around 0,0,0, so we simply add the curve sample position we made earlier.
    float3 catmullSpacePosition = catPosition+worldFrameRotated;

    // Bring it back into object space, now that we're done working on it.
    return mul(worldToObject,float4(catmullSpacePosition,1)).xyz;
}