using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PenetrationTech {

public class CatmullPath {
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
        // Second derivative (curvature)
        // p''(t) = (12t - 6)p₀ + (6t - 4)m₀ + (-12t + 6)p₁ + (6t - 2)m₁
        Vector3 curvature = (12 * t - 6) * start
            + (6 * t - 4) * tanPoint1
            + (-12 * t + 6) * end
            + (6 * t - 2) * tanPoint2;
        return curvature;
    }
    private List<Vector3> weights;
    private List<Vector3> points;
    private float[] LUT;
    public void SetPoints(Vector3[] points, int lutResolution=8) {
        weights.Clear();
        for (int i=0;i<points.Length;i++) {
            Vector3 p0 = points[i];
            Vector3 p1 = points[i+1];

            // Tangent M[k] = (P[k+1] - P[k-1]) / 2
            Vector3 m0 = (p1 - p0)*0.5f;
            Vector3 m1 = (p1 - p0)*0.5f;
            if (i < points.Length - 2) {
                m1 = points[(i + 2) % points.Length] - p0;
            } else {
                m1 = p1 - p0;
            }
            weights.Add(p0);
            weights.Add(m0);
            weights.Add(m1);
            weights.Add(p1);
        }
    }
}

}