float DistanceLUT(float distance) {
    int count = 32;
    if (distance > 0 && distance < _ArcLength) {
        for(int i=0;i<count-1;i++) {
            if (distance>_DistanceLUT[i] && distance<_DistanceLUT[i+1]) {
                // Remap
                float value = distance;
                float from1 = _DistanceLUT[i];
                float to1 = _DistanceLUT[i+1];
                float from2 = i/(count-1);
                float to2 = (i+1)/(count-1);
                return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
            }
        }
    }
    return distance/_ArcLength;
}