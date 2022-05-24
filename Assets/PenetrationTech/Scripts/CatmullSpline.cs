using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {
    public class CatmullSpline {
        private static CatmullSpline Lerp(CatmullSpline a, CatmullSpline b, float t) {
            List<Vector3> merged = new List<Vector3>(a.GetWeights());
            for(int i=0;i<merged.Count;i++) {
                merged[i] = Vector3.Lerp(merged[i], b.GetWeights()[i], t);
            }
            return new CatmullSpline().SetWeights(merged);
        }
        private static float Remap (float value, float from1, float to1, float from2, float to2) {
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
            // Second derivative (acceleration)
            // p''(t) = (12t - 6)p₀ + (6t - 4)m₀ + (-12t + 6)p₁ + (6t - 2)m₁
            Vector3 curvature = (12f * t - 6f) * start
                + (6f * t - 4f) * tanPoint1
                + (-12f * t + 6f) * end
                + (6f * t - 2f) * tanPoint2;
            return curvature;
        }
        protected List<Vector3> weights;
        private List<float> distanceLUT;
        private List<Vector3> binormalLUT;
        private List<Bounds> bounds;
        public float arcLength {get; private set;}

        public List<Vector3> GetWeights() => weights;
        public List<float> GetDistanceLUT() => distanceLUT;
        public List<Vector3> GetBinormalLUT() => binormalLUT;
        public List<Bounds> GetBounds() {
            if (bounds.Count != weights.Count/4) {
                GenerateBounds();
            }
            return bounds;
        }

        public CatmullSpline() {
            weights = new List<Vector3>();
            distanceLUT = new List<float>();
            binormalLUT = new List<Vector3>();
            bounds = new List<Bounds>();
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
            curveSegmentIndex = Mathf.Clamp(Mathf.FloorToInt(t*(weights.Count/4)),0,(weights.Count/4)-1);
            float offseted = t-((float)curveSegmentIndex/(float)(weights.Count/4));
            return offseted * (float)(weights.Count/4);
        }
        
        public float GetTimeFromDistance(float distance) {
            if (distance > 0f && distance < arcLength) {
                for(int i=0;i<distanceLUT.Count-1;i++) {
                    if (distance>distanceLUT[i] && distance<distanceLUT[i+1]) {
                        return Remap(distance,distanceLUT[i],distanceLUT[i+1],(float)i/(float)(distanceLUT.Count),(float)(i+1)/(float)(distanceLUT.Count));
                    }
                }
            }
            return distance/arcLength;
        }
        public float GetDistanceFromTime(float t) {
            t = Mathf.Clamp01(t);
            int index = Mathf.Clamp(Mathf.FloorToInt(t*(distanceLUT.Count-1)),0,distanceLUT.Count-2);
            float offseted = t-((float)index/(float)(distanceLUT.Count));
            float lerpT = offseted * (float)(distanceLUT.Count-1);
            return Mathf.Lerp(distanceLUT[index], distanceLUT[index+1], lerpT);
        }
        private bool CheckMinimaMaxima(float tValue) {
            if (float.IsNaN(tValue) || Mathf.Clamp01(tValue) != tValue) {
                return false;
            }
            return true;
        }
        // Again we use Freya Holmer's tutorial to understand the strategy behind generating the bounds: https://youtu.be/aVwxzDHniEw?t=791
        // This is just the derivative written in terms of t, then plugged into the quadratic formula to solve for zeros.
        // that gives us our local extremes to generate our bounds on.
        protected void GenerateBounds() {
            bounds.Clear();
            List<float> tValues = new List<float>();
            for(int i=0;i<weights.Count/4;i++) {
                Vector3 p0 = weights[(i*4)];
                Vector3 m0 = weights[(i*4)+1];
                Vector3 m1 = weights[(i*4)+2];
                Vector3 p1 = weights[(i*4)+3];

                Vector3 a = 6f*p0 + 3f*m0 - 6f*p1 + 3f*m1;
                Vector3 b = -6f*p0 -4f*m0 + 6f*p1 - 2f*m1;
                Vector3 c = m0;

                // Good ol' quadratic formula. We solve it for each axis (X,Y,Z);
                float tpx = (-b.x + Mathf.Sqrt(b.x*b.x-4f*a.x*c.x))/(2f*a.x);
                float tpy = (-b.y + Mathf.Sqrt(b.y*b.y-4f*a.y*c.y))/(2f*a.y);
                float tpz = (-b.z + Mathf.Sqrt(b.z*b.z-4f*a.z*c.z))/(2f*a.z);

                float tnx = (-b.x - Mathf.Sqrt(b.x*b.x-4f*a.x*c.x))/(2f*a.x);
                float tny = (-b.y - Mathf.Sqrt(b.y*b.y-4f*a.y*c.y))/(2f*a.y);
                float tnz = (-b.z - Mathf.Sqrt(b.z*b.z-4f*a.z*c.z))/(2f*a.z);

                Bounds bound = new Bounds(GetPosition(p0, m0, m1, p1, 0f), Vector3.zero);
                // If the floats are out of our range, or if they're NaN (from a negative sqrt)-- we discard them.
                if (CheckMinimaMaxima(tpx)) { bound.Encapsulate(GetPosition(p0,m0,m1,p1,tpx)); }
                if (CheckMinimaMaxima(tnx)) { bound.Encapsulate(GetPosition(p0,m0,m1,p1,tnx)); }
                if (CheckMinimaMaxima(tpy)) { bound.Encapsulate(GetPosition(p0,m0,m1,p1,tpy)); }
                if (CheckMinimaMaxima(tny)) { bound.Encapsulate(GetPosition(p0,m0,m1,p1,tny)); }
                if (CheckMinimaMaxima(tpz)) { bound.Encapsulate(GetPosition(p0,m0,m1,p1,tpz)); }
                if (CheckMinimaMaxima(tnz)) { bound.Encapsulate(GetPosition(p0,m0,m1,p1,tnz)); }
                bound.Encapsulate(GetPosition(p0,m0,m1,p1,1f));
                bounds.Add(bound);
            }
        }
        protected void GenerateDistanceLUT(int resolution) {
            float dist = 0f;
            Vector3 lastPosition = GetPositionFromT(0f);
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
        protected void GenerateBinormalLUT(int resolution) {
            // https://en.wikipedia.org/wiki/Frenet%E2%80%93Serret_formulas
            // https://janakiev.com/blog/framing-parametric-curves/
            binormalLUT.Clear();
            Vector3 lastTangent = GetVelocityFromT(0).normalized;
            // Initial reference frame, uses Vector3.up
            Vector3 lastBinormal = Vector3.Cross(GetVelocityFromT(0),Vector3.up).normalized;
            if (lastBinormal.magnitude == 0f) {
                lastBinormal = Vector3.Cross(GetVelocityFromT(0),Vector3.right).normalized;
            }
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
        public CatmullSpline SetWeights(IList<Vector3> newWeights) {
            // weights come in pairs of 4, otherwise there's been a problem!
            UnityEngine.Assertions.Assert.AreEqual(newWeights.Count%4,0);
            weights.Clear();
            weights.AddRange(newWeights);
            GenerateDistanceLUT(32);
            GenerateBinormalLUT(16);
            bounds.Clear();
            return this;
        }

        public CatmullSpline SetWeightsFromPoints(IList<Vector3> newPoints) {
            weights.Clear();
            for (int i=0;i<newPoints.Count-1;i++) {
                Vector3 p0 = newPoints[i];
                Vector3 p1 = newPoints[i+1];

                Vector3 m0;
                if (i==0) {
                    m0 = (p1 - p0)*0.5f;
                } else {
                    m0 = (p1 - newPoints[i-1])*0.5f;
                }
                Vector3 m1;
                if (i < newPoints.Count - 2) {
                    m1 = (newPoints[(i + 2) % newPoints.Count] - p0)*0.5f;
                } else {
                    m1 = (p1 - p0)*0.5f;
                }
                weights.Add(p0);
                weights.Add(m0);
                weights.Add(m1);
                weights.Add(p1);
            }
            GenerateDistanceLUT(32);
            GenerateBinormalLUT(16);
            bounds.Clear();
            return this;
        }
        public float GetClosestTimeFromPositionFast(Vector3 position, int samples=32) {
            // Broad pass that just finds the closest bounds
            int closestBounds = 0;
            float distToBounds = float.MaxValue;
            List<Bounds> curveBounds = GetBounds();
            for(int i=0;i<curveBounds.Count;i++) {
                float dist = Vector3.Distance(curveBounds[i].ClosestPoint(position),position);
                if (dist < distToBounds) {
                    closestBounds = i;
                    distToBounds = dist;
                }
            }
            // With the closest sub-curve found, we can then do some very deliberate samples.
            float closestTValue = 0f;
            float distToCurve = float.MaxValue;
            for (int i=0;i<samples;i++) {
                float tSample = (float)i/(float)samples;
                Vector3 samplePosition = SampleCurveSegmentPosition(closestBounds, tSample);
                float dist = Vector3.Distance(samplePosition, position);
                if (dist<distToCurve) {
                    closestTValue = tSample;
                    distToCurve = dist;
                }
            }
            // Just gotta take it from subT to overall t value
            return closestTValue/(float)(curveBounds.Count)+((float)closestBounds/(float)(curveBounds.Count));
        }
        public float GetClosestTimeFromPosition(Vector3 position, int samples=32) {
            float closestTValue = 0f;
            float distToCurve = float.MaxValue;
            for (int i=0;i<samples;i++) {
                float tSample = (float)i/(float)samples;
                Vector3 samplePosition = GetPositionFromT(tSample);
                float dist = Vector3.Distance(samplePosition, position);
                if (dist<distToCurve) {
                    closestTValue = tSample;
                    distToCurve = dist;
                }
            }
            return closestTValue;
        }
        public Vector3 GetPositionFromDistance(float distance) {
            float t = GetTimeFromDistance(distance);
            return GetPositionFromT(t);
        }
        public Vector3 GetPositionFromT(float t) {
            int curveSegmentIndex;
            float subT = GetCurveSegmentTimeFromCurveTime(out curveSegmentIndex, t);
            return SampleCurveSegmentPosition(curveSegmentIndex, subT);
        }
        public Vector3 GetVelocityFromDistance(float distance) {
            float t = GetTimeFromDistance(distance);
            return GetVelocityFromT(t);
        }
        public Vector3 GetAccelerationFromDistance(float distance) {
            float t = GetTimeFromDistance(distance);
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
            int index = Mathf.Clamp(Mathf.FloorToInt(t*(binormalLUT.Count-1)),0,binormalLUT.Count-2);
            float offseted = t-((float)index/(float)(binormalLUT.Count));
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
            return BezierBasis.inverse;
        }
    }

}