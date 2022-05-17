float GetCurveSegment(float t) {
    curveSegmentIndex = clamp(floor(t*(_PointCount-1)),0,_PointCount-1);
    float offset = t-(curveSegmentIndex/(_PointCount-1));
    return offset * (_PointCount-1);
}