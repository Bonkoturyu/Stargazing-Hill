#ifndef STARGAZING_HILL_ATMOSPHERE_INCLUDED
#define STARGAZING_HILL_ATMOSPHERE_INCLUDED

// Simple astronomical extinction model shared by stars and meteors.
// Airmass is approximated as 1 / sin(altitude), with the low-altitude
// singularity clamped for stable mobile rendering.  The transmission is
// 10^(-0.4 * extinctionMagnitudes), written with exp2 for shader efficiency.
inline float StargazingAtmosphericTransmission(
    float sinAltitude, float extinctionCoefficient, float minimumSinAltitude)
{
    float safeSinAltitude = max(sinAltitude, minimumSinAltitude);
    float airmass = 1.0 / safeSinAltitude;
    float extinctionMagnitudes = extinctionCoefficient * (airmass - 1.0);
    return exp2(-1.32877124 * extinctionMagnitudes);
}

#endif
