using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class CatmullPath {
        public static float Remap (float value, float from1, float to1, float from2, float to2) {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }
        private static Vector3 GetPosition(Vector3 start, Vector3 tanPoint1, Vector3 tanPoint2, Vector3 end, float t) {
            // Using the expanded form of a Hermite basis functions
            // https://en.wikipedia.org/wiki/Cubic_Hermite_spline
            // p(t) = (2t³ - 3t² + 1)p₀ + (t³ - 2t² + t)m₀ + (-2t³ + 3t²)p₁ + (t³ - t²)m₁
            Vector3 position = (2f * t * t * t - 3f * t * t + 1f) * start
                + (t * t * t - 2f * t * t + t) * tanPoint1
                + (-2f * t * t * t + 3f * t * t) * end
                + (t * t * t - t * t) * tanPoint2;
            return position;
        }
        private static Vector3 GetVelocity(Vector3 start, Vector3 tanPoint1, Vector3 tanPoint2, Vector3 end, float t) {
            // First derivative (velocity)
            // p'(t) = (6t² - 6t)p₀ + (3t² - 4t + 1)m₀ + (-6t² + 6t)p₁ + (3t² - 2t)m₁
            Vector3 tangent = (6f * t * t - 6f * t) * start
                + (3f * t * t - 4f * t + 1f) * tanPoint1
                + (-6f * t * t + 6f * t) * end
                + (3f * t * t - 2f * t) * tanPoint2;
            return tangent;
        }
        private static Vector3 GetAcceleration(Vector3 start, Vector3 tanPoint1, Vector3 tanPoint2, Vector3 end, float t) {
            // Second derivative (jerk)
            // p''(t) = (12t - 6)p₀ + (6t - 4)m₀ + (-12t + 6)p₁ + (6t - 2)m₁
            Vector3 curvature = (12f * t - 6f) * start
                + (6f * t - 4f) * tanPoint1
                + (-12f * t + 6f) * end
                + (6f * t - 2f) * tanPoint2;
            return curvature;
        }
        private List<Vector3> weights;
        private List<Vector3> points;
        private List<float> distanceLUT;
        private List<Vector3> binormalLUT;
        public float arcLength {get; private set;}
        public List<Vector3> GetWeights() => weights;
        public List<float> GetDistanceLUT() => distanceLUT;

        public CatmullPath(Vector3[] newPoints) {
            points = new List<Vector3>();
            weights = new List<Vector3>();
            distanceLUT = new List<float>();
            binormalLUT = new List<Vector3>();
            SetWeightsFromPoints(newPoints);
        }

        private Vector3 SampleCurveSegmentPosition(int curveSegmentIndex, float t) {
            return GetPosition(weights[curveSegmentIndex*4], weights[curveSegmentIndex*4+1], weights[curveSegmentIndex*4+2], weights[curveSegmentIndex*4+3], t);
        }
        private Vector3 SampleCurveSegmentVelocity(int curveSegmentIndex, float t) {
            return GetVelocity(weights[curveSegmentIndex*4], weights[curveSegmentIndex*4+1], weights[curveSegmentIndex*4+2], weights[curveSegmentIndex*4+3], t);
        }
        private Vector3 SampleCurveSegmentAcceleration(int curveSegmentIndex, float t) {
            return GetAcceleration(weights[curveSegmentIndex*4], weights[curveSegmentIndex*4+1], weights[curveSegmentIndex*4+2], weights[curveSegmentIndex*4+3], t);
        }

        private float GetCurveSegmentTimeFromCurveTime(out int curveSegmentIndex, float t) {
            curveSegmentIndex = Mathf.FloorToInt(t*(points.Count-1));
            float offseted = t-((float)curveSegmentIndex/(float)(points.Count-1));
            return offseted * (float)(points.Count-1);
        }
        
        private float DistToTime(float distance) {
            if (distance > 0f && distance < arcLength) {
                for(int i=0;i<distanceLUT.Count-1;i++) {
                    if (distance>distanceLUT[i] && distance<distanceLUT[i+1]) {
                        return Remap(distance,distanceLUT[i],distanceLUT[i+1],(float)i/(distanceLUT.Count-1f),(float)(i+1)/(distanceLUT.Count-1f));
                    }
                }
            }
            return distance/arcLength;
        }
        private void GenerateDistanceLUT(int resolution) {
            float dist = 0f;
            Vector3 lastPosition = SampleCurveSegmentPosition(0, 0f);
            distanceLUT.Clear();
            for(int i=0;i<resolution;i++) {
                float t = (((float)i)/(float)resolution);
                Vector3 position = GetPositionFromT(t);
                dist += Vector3.Distance(lastPosition, position);
                lastPosition = position;
                distanceLUT.Add(dist);
            }
            arcLength = dist;
        }
        private void GenerateNormalLUT(int resolution) {
            // https://en.wikipedia.org/wiki/Frenet%E2%80%93Serret_formulas
            // https://janakiev.com/blog/framing-parametric-curves/
            binormalLUT.Clear();
            Vector3 lastTangent = GetVelocityFromT(0).normalized;
            // Initial reference frame, uses Vector3.up
            Vector3 lastBinormal = Vector3.Cross(GetVelocityFromT(0),Vector3.up).normalized;
            for(int i=0;i<resolution;i++) {
                float t = (((float)i)/(float)resolution);
                Vector3 point = GetPositionFromT(t);
                Vector3 tangent = GetVelocityFromT(t).normalized;
                //Vector3 binormal = Vector3.Cross(GetTangentFromT(t),Vector3.up).normalized;
                Vector3 binormal = Vector3.Cross(lastTangent, tangent);
                if (binormal.magnitude == 0f) {
                    binormal = lastBinormal;
                } else {
                    float theta = Vector3.Angle(lastTangent, tangent); // Mathf.Acos(Vector3.Dot(lastTangent,tangent))
                    binormal = Quaternion.AngleAxis(theta,binormal.normalized)*lastBinormal;
                }
                lastTangent = tangent;
                lastBinormal = binormal;
                binormalLUT.Add(binormal);
            }

            // Undo any twist.
            float overallAngle = Vector3.Angle(binormalLUT[0], binormalLUT[resolution-1]);
            for(int i=0;i<resolution;i++) {
                float t = (float)i/(float)resolution;
                binormalLUT[i] = Quaternion.AngleAxis(-overallAngle*t, GetVelocityFromT(t).normalized)*binormalLUT[i];
            }
        }
        public void SetWeightsFromPoints(Vector3[] newPoints) {
            points.Clear();
            points.AddRange(newPoints);
            weights.Clear();
            for (int i=0;i<points.Count-1;i++) {
                Vector3 p0 = points[i];
                Vector3 p1 = points[i+1];

                Vector3 m0;
                if (i==0) {
                    m0 = (p1 - p0)*0.5f;
                } else {
                    m0 = (p1 - points[i-1])*0.5f;
                }
                Vector3 m1;
                if (i < points.Count - 2) {
                    m1 = (points[(i + 2) % points.Count] - p0)*0.5f;
                } else {
                    m1 = (p1 - p0)*0.5f;
                }
                weights.Add(p0);
                weights.Add(m0);
                weights.Add(m1);
                weights.Add(p1);
            }
            GenerateDistanceLUT(32);
            GenerateNormalLUT(32);
        }
        public Vector3 GetPositionFromDistance(float distance) {
            float t = DistToTime(distance);
            return GetPositionFromT(t);
        }
        public Vector3 GetPositionFromT(float t) {
            int curveSegmentIndex;
            float subT = GetCurveSegmentTimeFromCurveTime(out curveSegmentIndex, t);
            return SampleCurveSegmentPosition(curveSegmentIndex, subT);
        }
        public Vector3 GetVelocityFromDistance(float distance) {
            float t = DistToTime(distance);
            return GetVelocityFromT(t);
        }
        public Vector3 GetAccelerationFromDistance(float distance) {
            float t = DistToTime(distance);
            return GetAccelerationFromT(t);
        }
        public Vector3 GetVelocityFromT(float t) {
            int curveSegmentIndex;
            float subT = GetCurveSegmentTimeFromCurveTime(out curveSegmentIndex, t);
            return SampleCurveSegmentVelocity(curveSegmentIndex, subT);
        }
        public Vector3 GetAccelerationFromT(float t) {
            int curveSegmentIndex;
            float subT = GetCurveSegmentTimeFromCurveTime(out curveSegmentIndex, t);
            return SampleCurveSegmentAcceleration(curveSegmentIndex, subT);
        }
        public Vector3 GetBinormalFromT(float t) {
            int index = Mathf.FloorToInt(t*(binormalLUT.Count-1));
            float offseted = t-((float)index/(float)(binormalLUT.Count-1));
            float lerpT = offseted * (float)(binormalLUT.Count-1);
            return Vector3.Lerp(binormalLUT[index], binormalLUT[index+1], lerpT);
        }
        public Matrix4x4 GetReferenceFrameFromT(float t) {
            Vector3 point = GetPositionFromT(t);
            Vector3 tangent = GetVelocityFromT(t).normalized;
            Vector3 binormal = GetBinormalFromT(t).normalized;
            Vector3 normal = Vector3.Cross(tangent, binormal);

            // Change of basis https://math.stackexchange.com/questions/3540973/change-of-coordinates-and-change-of-basis-matrices
            // It also shows up here: https://docs.unity3d.com/ScriptReference/Vector3.OrthoNormalize.html
            Matrix4x4 BezierBasis = new Matrix4x4();
            BezierBasis.SetRow(0,binormal); // Our X axis
            BezierBasis.SetRow(1,normal); // Y Axis
            BezierBasis.SetRow(2,tangent); // Z Axis
            BezierBasis[3,3] = 1f;
            // Change of basis formula is B = P⁻¹ * A * P, where P is the basis transform.
            return Matrix4x4.Translate(point)*BezierBasis.inverse;
        }
    }

}