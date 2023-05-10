﻿/*
 * This Software was obtained from the MechJeb2 project (https://github.com/MuMech/MechJeb2) on 3/25/23
 * and was further modified as needed for compatibility with KSP2 and/or for incorporation into the
 * FlightPlan project (https://github.com/schlosrat/FlightPlan)
 * 
 * This work is relaesed under the same license(s) inherited from the originating version.
 */

using KSP.Game;
using KSP.Sim.impl;
using KSP.Sim.State;
using KSP.Sim;
using FlightPlan;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using UnityEngine;
using FPUtilities;
// using Smooth.Pools;

namespace MuMech
{
    public static class OrbitalManeuverCalculator
    {
        // A stand in for the KSP1 function o.ReferenceBody.GetLatitude(Vector3d pos)
        // KSP2 doesn't appear to have such a function, but does have GetLatLonAltFromRadius
        // ISSUE all the o.*Position*() values return Vector3d, need type Position
        // SOLUTION: Make a new KSP.Sim.Position variable and populate the localPosition component
        public static double GetLatitude(this PatchedConicsOrbit o, double UT)
        {
            double latitude, longitude, altitude;
            Position position = new Position(o.referenceBody.coordinateSystem, o.SwappedAbsolutePositionAtUT(UT));
            o.referenceBody.GetLatLonAltFromRadius(position, out latitude, out longitude, out altitude);

            return latitude; // * UtilMath.Rad2Deg;
        }

        public static double GetLatLon(this PatchedConicsOrbit o, double UT, out double longitude)
        {
            double latitude, altitude;
            Position position = new Position(o.referenceBody.coordinateSystem, o.SwappedAbsolutePositionAtUT(UT));
            o.referenceBody.GetLatLonAltFromRadius(position, out latitude, out longitude, out altitude);
            // longitude *= UtilMath.Rad2Deg;

            return latitude; // * UtilMath.Rad2Deg;
        }

        public static double GetLongitude(this PatchedConicsOrbit o, double UT)
        {
            double latitude, longitude, altitude;
            Position position = new Position(o.referenceBody.coordinateSystem, o.SwappedAbsolutePositionAtUT(UT));
            o.referenceBody.GetLatLonAltFromRadius(position, out latitude, out longitude, out altitude);

            return longitude; // * UtilMath.Rad2Deg;
        }
        
        //Computes the speed of a circular orbit of a given radius for a given body.
        public static double CircularOrbitSpeed(CelestialBodyComponent body, double radius)
        {
            //v = sqrt(GM/r)
            return Math.Sqrt(body.gravParameter / radius);
        }

        //Computes the speed of a circular orbit of a given radius for a given body.
        public static double EscapeVelocity(CelestialBodyComponent body, double radius)
        {
            //v = sqrt(2GM/r)
            return Math.Sqrt(2 * body.gravParameter / radius);
        }

        //Computes the deltaV of the burn needed to circularize an orbit at a given UT.
        public static Vector3d DeltaVToCircularize(PatchedConicsOrbit o, double UT)
        {
            Vector3d desiredVelocity = CircularOrbitSpeed(o.referenceBody, o.Radius(UT)) * o.Horizontal(UT);
            Vector3d actualVelocity = o.SwappedOrbitalVelocityAtUT(UT);
            var deltaV =  desiredVelocity - actualVelocity;
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToCircularize: desiredVelocity [{desiredVelocity.x}, {desiredVelocity.y}, {desiredVelocity.z}] m/s");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToCircularize: actualVelocity  [{actualVelocity.x}, {actualVelocity.y}, {actualVelocity.z}] m/s");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToCircularize: deltaV          [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s = {deltaV.magnitude} m/s");
            return deltaV;
        }

        //Computes the deltaV of the burn needed to set a given PeR and ApR at at a given UT.
        public static Vector3d DeltaVToEllipticize(PatchedConicsOrbit o, double UT, double newPeR, double newApR)
        {
            double radius = o.Radius(UT);

            //sanitize inputs
            newPeR = MuUtils.Clamp(newPeR, 0 + 1, radius - 1);
            newApR = Math.Max(newApR, radius + 1);

            double GM = o.referenceBody.gravParameter;
            double E = -GM / (newPeR + newApR); // total energy per unit mass of new orbit
            double L = Math.Sqrt(Math.Abs((Math.Pow(E * (newApR - newPeR), 2) - GM * GM) / (2 * E))); // angular momentum per unit mass of new orbit
            double kineticE = E + GM / radius; // kinetic energy (per unit mass) of new orbit at UT
            double newHorizontalV = L / radius;   // horizontal velocity of new orbit at UT
            double newUpV = Math.Sqrt(Math.Abs(2 * kineticE - newHorizontalV * newHorizontalV)); //vertical velocity of new orbit at UT

            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: radius {radius} m");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: newPeR {newPeR} m");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: newApR {newApR} m");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: newHorizontalV {newHorizontalV} m/s");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: newUpV         {newUpV} m/s");

            Vector3d actualVelocity = o.SwappedOrbitalVelocityAtUT(UT);
            //var northV = Vector3d.Dot(actualVelocity, o.North(UT));
            //var eastV = Vector3d.Dot(actualVelocity, o.East(UT));  // tried Prograde, but that could include some of all three axes!
            //var upV = Vector3d.Dot(actualVelocity, o.Up(UT));
            //var newEastV = Math.Sqrt(newHorizontalV * newHorizontalV - northV * northV);

            //untested:
            newUpV *= Math.Sign(Vector3d.Dot(o.Up(UT), actualVelocity));

            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: newUpV*        {newUpV} m/s");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: actualVelocity [{actualVelocity.x}, {actualVelocity.y}, {actualVelocity.z}] m/s");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: upV            {upV} m/s");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: northV         {northV} m/s");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: eastV          {eastV} m/s");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: newEastV       {newEastV} m/s");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: newUpV         {newUpV} m/s");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToCircularize: Prograde Vec   [{o.Prograde(UT).x}, {o.Prograde(UT).y}, {o.Prograde(UT).z}]");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToCircularize: Horizontal Vec [{o.Horizontal(UT).x}, {o.Horizontal(UT).y}, {o.Horizontal(UT).z}]");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToCircularize: Up Vec         [{o.Up(UT).x}, {o.Up(UT).y}, {o.Up(UT).z}]");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToCircularize: North Vec      [{o.North(UT).x}, {o.North(UT).y}, {o.North(UT).z}]");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToCircularize: East Vec       [{o.East(UT).x}, {o.East(UT).y}, {o.East(UT).z}]");

            // tried o.Prograde(UT) in place of o.Horizontal, tried o.RadialPlus(UT) in place of o.Up
            // Vector3d desiredVelocity = newEastV * o.East(UT) + northV * o.North(UT) + newUpV * o.Up(UT);
            Vector3d desiredVelocity = newHorizontalV * o.Horizontal(UT) + newUpV * o.Up(UT);
            var deltaV = desiredVelocity - actualVelocity;

            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: desiredVelocity [{desiredVelocity.x}, {desiredVelocity.y}, {desiredVelocity.z}] m/s");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToEllipticize: deltaV [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s");
            return deltaV;
        }

        //Computes the delta-V of the burn required to attain a given periapsis, starting from
        //a given orbit and burning at a given UT. Throws an ArgumentException if given an impossible periapsis.
        //The computed burn is always horizontal, though this may not be strictly optimal.
        public static Vector3d DeltaVToChangePeriapsis(PatchedConicsOrbit o, double UT, double newPeR)
        {
            double radius = o.Radius(UT);

            //sanitize input
            newPeR = MuUtils.Clamp(newPeR, 0 + 1, radius - 1);

            //are we raising or lowering the periapsis?
            bool raising = (newPeR > o.Periapsis);
            // Why do we use o.Horizontal here and o.Prograde for DeltaVToChangeApoapsis?
            Vector3d burnDirection = (raising ? 1 : -1) * o.Horizontal(UT);
            var burDir = o.DeltaVToManeuverNodeCoordinates(UT, burnDirection);  // test code - delete

            double minDeltaV = 0;
            double maxDeltaV;
            if (raising)
            {
                maxDeltaV = 0.25;

                // Old Way: This can sometimes give Finite Check failures from BrentRoot due to poorly set max
                //put an upper bound on the required deltaV:
                PatchedConicsOrbit testOrbit = o.PerturbedOrbit(UT, maxDeltaV * burnDirection);
                while (testOrbit.Periapsis < newPeR)
                {
                    minDeltaV = maxDeltaV; //narrow the range
                    maxDeltaV *= 2;
                    try { testOrbit = o.PerturbedOrbit(UT, maxDeltaV * burnDirection); }
                    catch (Exception e)
                    {
                        maxDeltaV = minDeltaV;
                        minDeltaV *= 0.5;
                        FlightPlanPlugin.Logger.LogError($"DeltaVToChangePeriapsis: Unable to find good maxDeltaV {e}");
                        break;
                    }
                    if (maxDeltaV > 100000) break; //a safety precaution
                }
                FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: maxDeltaV = {maxDeltaV}");

                // THIS WAY IS WRONG! It makes no sense to limit delta v based on escape velocity when adjusting Pe.
                // An escape velocity check makes sense when raising Ap, not when raising or lowering Pe
                // New way: This prevents Finite Check failures from BrentRoot by finding a better max
                // Max Delta-V based on escape velocity
                // var maxDeltaVCap = EscapeVelocity(o.ReferenceBody, radius) - o.SwappedOrbitalVelocityAtUT(UT).magnitude - MuUtils.DBL_EPSILON;

                /*
                double lastMax = maxDeltaV;
                double riseFactor = 2;
                var testOrbit = o.PerturbedOrbit(UT, maxDeltaV * burnDirection);
                while (testOrbit.eccentricity < 1)
                {
                    // This will break if the newPeR > current Apoapsis. Once we've applied enough deltaV
                    // To raise the periapsis above the apoapsis, the value we need to test becomes the
                    // apoapsis.
                    if (testOrbit.Periapsis < newPeR) // maxDeltaV is not high enough
                    {
                        lastMax = maxDeltaV;     // record the previous max in case we need to back off
                        minDeltaV = maxDeltaV;   // narrow the range
                        maxDeltaV *= riseFactor; // Boost the max by the riseFactor
                        if (maxDeltaV > maxDeltaVCap)
                        {
                            maxDeltaV = maxDeltaVCap;
                            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: maxDeltaV Capped at {maxDeltaV} m/s");
                            break; // safety precaution
                        }
                        // FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: maxDeltaV    {maxDeltaV} m/s");
                        testOrbit = o.PerturbedOrbit(UT, maxDeltaV * burnDirection); // Get next test orbit
                        //if (testOrbit.eccentricity >= 1) // If we've shot too high
                        //{
                        //    minDeltaV = lastMax *= (1 / riseFactor); // Reset min
                        //    maxDeltaV = lastMax;                     // Reset the max
                        //    if (riseFactor > 1.25)  // If there's still room to trim the riseFactor
                        //        riseFactor -= 0.25; // Trim it
                        //    else
                        //        break; // We're done (in a bad way...)
                        //    maxDeltaV *= riseFactor; // Apply the new riseFactor
                        //    FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: maxDeltaV    {maxDeltaV} m/s");
                        //    testOrbit = o.PerturbedOrbit(UT, maxDeltaV * burnDirection); // Get next test orbit
                        //}
                    }
                    else
                        break; // We're done (in a good way)
                }
                if (testOrbit.eccentricity >= 1 || maxDeltaV < minDeltaV) // We got done in a bad way...
                {
                    FlightPlanPlugin.Logger.LogError($"DeltaVToChangePeriapsis: Unable to find a maxDeltaV that gets to an Periapsis above {newPeR}");
                } */
            }
            else
            {
                //when lowering periapsis, we burn horizontally, and max possible deltaV is the deltaV required to kill all horizontal velocity
                // maxDeltaV = Math.Abs(Vector3d.Dot(o.SwappedOrbitalVelocityAtUT(UT), burnDirection)) - MuUtils.DBL_EPSILON;
                maxDeltaV = 0.999 * Math.Abs(Vector3d.Dot(o.SwappedOrbitalVelocityAtUT(UT), burnDirection));
            }

            /*
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: radius    {radius}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: newPeR    {newPeR}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: raising   {raising}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: deltaVDir [{burnDirection.x}, {burnDirection.y}, {burnDirection.z}]");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: burnDir   [{burDir.x}, {burDir.y}, {burDir.z}]");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: minDeltaV {minDeltaV} m/s");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: maxDeltaV {maxDeltaV} m/s");

            var initialTestOrbitMin = o.PerturbedOrbit(UT, minDeltaV * burnDirection);
            var initialTestOrbitMax = o.PerturbedOrbit(UT, maxDeltaV * burnDirection);
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: initialTestOrbitMin {initialTestOrbitMin}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: initialTestOrbitMin Ap {initialTestOrbitMin.Periapsis}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: initialTestOrbitMax {initialTestOrbitMax}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: initialTestOrbitMax Ap {initialTestOrbitMax.Periapsis}");
            */

            // minDeltaV = 0;
            // FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: minDeltaV* {minDeltaV} m/s");

            Func<double, object, double> f = delegate (double testDeltaV, object ign)
            {
                return o.PerturbedOrbit(UT, testDeltaV * burnDirection).Periapsis - newPeR;
            };
            double dV = 0;
            try { dV = BrentRoot.Solve(f, minDeltaV, maxDeltaV, null); }
            catch (TimeoutException) { FlightPlanPlugin.Logger.LogError("DeltaVToChangePeriapsis: Brents method threw a timeout Error (supressed)"); }
            catch (ArgumentException e) { FlightPlanPlugin.Logger.LogError($"DeltaVToChangePeriapsis: Brents method threw an argument exception Error (supressed): {e.Message}"); }

            var deltaV = dV * burnDirection;

            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangePeriapsis: deltaV [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s");

            return deltaV;
        }

        public static bool ApoapsisIsHigher(double ApR, double than)
        {
            if (than > 0 && ApR < 0) return true;
            if (than < 0 && ApR > 0) return false;
            return ApR > than;
        }

        //Computes the delta-V of the burn at a given UT required to change an orbits apoapsis to a given value.
        //The computed burn is always prograde or retrograde, though this may not be strictly optimal.
        //Note that you can pass in a negative apoapsis if the desired final orbit is hyperbolic
        public static Vector3d DeltaVToChangeApoapsis(PatchedConicsOrbit o, double UT, double newApR)
        {
            double radius = o.Radius(UT);

            //sanitize input
            if (newApR > 0) newApR = Math.Max(newApR, radius + 1);

            //are we raising or lowering the periapsis?
            bool raising = ApoapsisIsHigher(newApR, o.Apoapsis);

            // Why do we use o.Prograde here and o.Horizontal for DeltaVToChangePeriapsis?
            Vector3d burnDirection = (raising ? 1 : -1) * o.Prograde(UT);
            var burDir = o.DeltaVToManeuverNodeCoordinates(UT, burnDirection);  // test code - delete

            double minDeltaV = 0;
            // 10000 dV is a safety factor, max burn when lowering ApR would be to null out our current velocity
            // This logic does not work! Assuming a max of 10000 can get you into a situation where the Apoapsis
            // goes negative due to hyperbolic orbit. In such a case the user has asked for an Apoapsis that is
            // not possible for this body. Perhaps we need a check to make sure newApR is within the SOI? In any
            // event, simply doubling the max each time can get to a spot where SMA is NaN and Apoapsis goes
            // negative (discontinuity). Setting the max too high results in finite check failure exceptions.
            double maxDeltaV = raising ? 10000 : o.SwappedOrbitalVelocityAtUT(UT).magnitude;
            
            // New Way to get maxDeltaV
            if (raising)
            {
                // Max Delta-V based on escape velocity
                var maxDeltaVCap = EscapeVelocity(o.referenceBody, radius) - o.SwappedOrbitalVelocityAtUT(UT).magnitude - MuUtils.DBL_EPSILON;
                //put an upper bound on the required deltaV:
                maxDeltaV = 0.25;
                // double lastMax = maxDeltaV;
                // double riseFactor = 2;
                // bool backoff = false;
                var testOrbit = o.PerturbedOrbit(UT, maxDeltaV * burnDirection);
                while (testOrbit.eccentricity < 1)
                {
                    if (testOrbit.Apoapsis < newApR) // maxDeltaV is not high enough
                    {
                        // lastMax = maxDeltaV;     // record the previous max in case we need to back off
                        minDeltaV = maxDeltaV;   // narrow the range
                        maxDeltaV *= 2; // Boost the max by the riseFactor
                        if (maxDeltaV > maxDeltaVCap)
                        {
                            maxDeltaV = maxDeltaVCap;
                            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: maxDeltaV Capped at {maxDeltaV} m/s");
                            break; // safety precaution
                        }
                        // FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: maxDeltaV    {maxDeltaV} m/s");
                        testOrbit = o.PerturbedOrbit(UT, maxDeltaV * burnDirection); // Get next test orbit
                        //while (testOrbit.eccentricity >= 1 && maxDeltaV > minDeltaV) // If we've shot too high, back off
                        //{
                        //    backoff = true;
                        //    maxDeltaV *= 0.9; // Back off by 10%
                        //    FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: maxDeltaV    {maxDeltaV} m/s");
                        //    testOrbit = o.PerturbedOrbit(UT, maxDeltaV * burnDirection); // Get next test orbit
                        //}
                        //if (backoff)
                        //    break; // We're done
                    }
                    else
                        break; // We're done (in a good way)
                }
                if (testOrbit.eccentricity >= 1 || maxDeltaV < minDeltaV) // We got done in a bad way...
                {
                    FlightPlanPlugin.Logger.LogError($"DeltaVToChangeApoapsis: Unable to find a maxDeltaV that gets to an Apoapsis above {newApR}");
                    return Vector3d.zero;
                }
            }
            else
            {
                //when lowering apoapsis, we burn horizontally, and max possible deltaV is the deltaV required to kill all horizontal velocity
                maxDeltaV = 0.999 * Math.Abs(Vector3d.Dot(o.SwappedOrbitalVelocityAtUT(UT), burnDirection));
            }
            /*
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: radius    {radius} m");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: newApR    {newApR}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: raising   {raising}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: deltaVDir [{burnDirection.x}, {burnDirection.y}, {burnDirection.z}]");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: burnDir   [{burDir.x}, {burDir.y}, {burDir.z}]");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: minDeltaV {minDeltaV} m/s");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: maxDeltaV {maxDeltaV}");

            var initialTestOrbitMin = o.PerturbedOrbit(UT, minDeltaV * burnDirection);
            var initialTestOrbitMax = o.PerturbedOrbit(UT, maxDeltaV * burnDirection);
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: initialTestOrbitMin {initialTestOrbitMin}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: initialTestOrbitMin Ap {initialTestOrbitMin.Apoapsis}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: initialTestOrbitMax {initialTestOrbitMax}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: initialTestOrbitMax Ap {initialTestOrbitMax.Apoapsis}");
            */
            
            // solve for the reciprocal of the ApR which is a continuous function that avoids the parabolic singularity and
            // change of sign for hyperbolic orbits.
            Func<double, object, double> f = delegate(double testDeltaV, object ign)
            {
                return 1.0/o.PerturbedOrbit(UT, testDeltaV * burnDirection).Apoapsis - 1.0/newApR;
            };
            double dV = 0;
            try { dV = BrentRoot.Solve(f, minDeltaV, maxDeltaV, null); }
            catch (TimeoutException) { FlightPlanPlugin.Logger.LogError("DeltaVToChangeApoapsis: Brents method threw a timeout Error (supressed)"); }
            catch (ArgumentException e) { FlightPlanPlugin.Logger.LogError($"DeltaVToChangeApoapsis: Brents method threw an argument exception Error (supressed): {e.Message}"); }

            var deltaV = dV * burnDirection;

            FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeApoapsis: deltaV [{deltaV.x}, {deltaV.y}, {deltaV.z}] m/s");

            return deltaV;
        }

        //Computes the heading of the ground track of an orbit with a given inclination at a given latitude.
        //Both inputs are in degrees.
        //Convention: At equator, inclination    0 => heading 90 (east)
        //                        inclination   90 => heading 0  (north)
        //                        inclination  -90 => heading 180 (south)
        //                        inclination ±180 => heading 270 (west)
        //Returned heading is in degrees and in the range 0 to 360.
        //If the given latitude is too large, so that an orbit with a given inclination never attains the
        //given latitude, then this function returns either 90 (if -90 < inclination < 90) or 270.
        public static double HeadingForInclination(double inclinationDegrees, double latitudeDegrees)
        {
            double cosDesiredSurfaceAngle = Math.Cos(inclinationDegrees * UtilMath.Deg2Rad) / Math.Cos(latitudeDegrees * UtilMath.Deg2Rad);
            if (Math.Abs(cosDesiredSurfaceAngle) > 1.0)
            {
                //If inclination < latitude, we get this case: the desired inclination is impossible
                if (Math.Abs(MuUtils.ClampDegrees180(inclinationDegrees)) < 90) return 90;
                else return 270;
            }
            else
            {
                double angleFromEast = (UtilMath.Rad2Deg) * Math.Acos(cosDesiredSurfaceAngle); //an angle between 0 and 180
                if (inclinationDegrees < 0) angleFromEast *= -1;
                //now angleFromEast is between -180 and 180

                return MuUtils.ClampDegrees360(90 - angleFromEast);
            }
        }

        //See #676
        //Computes the heading for a ground launch at the specified latitude accounting for the body rotation.
        //Both inputs are in degrees.
        //Convention: At equator, inclination    0 => heading 90 (east)
        //                        inclination   90 => heading 0  (north)
        //                        inclination  -90 => heading 180 (south)
        //                        inclination ±180 => heading 270 (west)
        //Returned heading is in degrees and in the range 0 to 360.
        //If the given latitude is too large, so that an orbit with a given inclination never attains the
        //given latitude, then this function returns either 90 (if -90 < inclination < 90) or 270.
        public static double HeadingForLaunchInclination(VesselComponent vessel, VesselState vesselState, double inclinationDegrees)
        {
            CelestialBodyComponent body = vessel.mainBody;
            double lat, lon, alt;
            var latitude = vessel.Latitude;
            var altitude = vessel.AltitudeFromRadius;
            vessel.mainBody.GetLatLonAltFromRadius(vessel.mainBody.Position, out lat, out lon, out alt); //   Latitude; // was: vesselState.latitude;
            double latitudeDegrees = lat * UtilMath.Deg2Rad;
            double orbVel = OrbitalManeuverCalculator.CircularOrbitSpeed(body, alt + body.radius); // was: vesselState.altitudeASL 
            double headingOne = HeadingForInclination(inclinationDegrees, latitudeDegrees) * UtilMath.Deg2Rad;
            double headingTwo = HeadingForInclination(-inclinationDegrees, latitudeDegrees) * UtilMath.Deg2Rad;
            double now = GameManager.Instance.Game.UniverseModel.UniversalTime;
            PatchedConicsOrbit o = vessel.Orbit;

            Vector3d north = vessel._telemetryComponent.HorizonNorth.vector; // vesselState.north; // (from VesselState.cs) north = vessel.north; // IFlightTelemetry has a North Vector, can we use that?
            Vector3d east = vessel._telemetryComponent.HorizonEast.vector; //  vesselState.east;   // (from VesselState.cs) east = vessel.east;   // IFlightTelemetry has a East Vector, can we use that?

            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(now), o.SwappedOrbitalVelocityAtUT(now));
            Vector3d desiredHorizontalVelocityOne = orbVel * ( Math.Sin(headingOne) * east + Math.Cos(headingOne) * north );
            Vector3d desiredHorizontalVelocityTwo = orbVel * ( Math.Sin(headingTwo) * east + Math.Cos(headingTwo) * north );

            Vector3d deltaHorizontalVelocityOne = desiredHorizontalVelocityOne - actualHorizontalVelocity;
            Vector3d deltaHorizontalVelocityTwo = desiredHorizontalVelocityTwo - actualHorizontalVelocity;

            Vector3d desiredHorizontalVelocity;
            Vector3d deltaHorizontalVelocity;
            double UT = GameManager.Instance.Game.UniverseModel.UniversalTime;
            Vector3d up = vessel.Orbit.Position.localPosition.normalized; // (from VesselState.cs) was orbitalPosition.normalized
            Vector3d surfaceVelocity = vessel.Orbit.GetOrbitalVelocityAtUTZup(UT) - vessel.mainBody.GetFrameVelAtUTZup(UT); // (from VesselState.cs) was orbitalVelocity - vessel.mainBody.getRFrmVel(CoM)
            if (Vector3d.Exclude(up, surfaceVelocity).magnitude < 200) // was vesselState.speedSurfaceHorizontal
            { // was: vesselState.speedSurfaceHorizontal
                // at initial launch we have to head the direction the user specifies (90 north instead of -90 south).
                // 200 m/s of surface velocity also defines a 'grace period' where someone can catch a rocket that they meant
                // to launch at -90 and typed 90 into the inclination box fast after it started to initiate the turn.
                // if the rocket gets outside of the 200 m/s surface velocity envelope, then there is no way to tell MJ to
                // take a south travelling rocket and turn north or vice versa.
                desiredHorizontalVelocity = desiredHorizontalVelocityOne;
                deltaHorizontalVelocity = deltaHorizontalVelocityOne;
            } else {
                // now in order to get great circle tracks correct we pick the side which gives the lowest delta-V, which will get
                // ground tracks that cross the maximum (or minimum) latitude of a great circle correct.
                if ( deltaHorizontalVelocityOne.magnitude < deltaHorizontalVelocityTwo.magnitude ) {
                    desiredHorizontalVelocity = desiredHorizontalVelocityOne;
                    deltaHorizontalVelocity = deltaHorizontalVelocityOne;
                }  else {
                    desiredHorizontalVelocity = desiredHorizontalVelocityTwo;
                    deltaHorizontalVelocity = deltaHorizontalVelocityTwo;
                }
            }

            // if you circularize in one burn, towards the end deltaHorizontalVelocity will whip around, but we want to
            // fall back to tracking desiredHorizontalVelocity
            if ( Vector3d.Dot(desiredHorizontalVelocity.normalized, deltaHorizontalVelocity.normalized) < 0.90 )
            {
                // it is important that we do NOT do the fracReserveDV math here, we want to ignore the deltaHV entirely at ths point
                return MuUtils.ClampDegrees360(UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(desiredHorizontalVelocity, east), Vector3d.Dot(desiredHorizontalVelocity, north)));
            }

            return MuUtils.ClampDegrees360(UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(deltaHorizontalVelocity, east), Vector3d.Dot(deltaHorizontalVelocity, north)));
        }

        //Computes the delta-V of the burn required to change an orbit's inclination to a given value
        //at a given UT. If the latitude at that time is too high, so that the desired inclination
        //cannot be attained, the burn returned will achieve as low an inclination as possible (namely, inclination = latitude).
        //The input inclination is in degrees.
        //Note that there are two orbits through each point with a given inclination. The convention used is:
        //   - first, clamp newInclination to the range -180, 180
        //   - if newInclination > 0, do the cheaper burn to set that inclination
        //   - if newInclination < 0, do the more expensive burn to set that inclination
        public static Vector3d DeltaVToChangeInclination(PatchedConicsOrbit o, double UT, double newInclination)
        {
            double latitude = GetLatitude(o, UT); // was o.ReferenceBody.GetLatitude(o.SwappedAbsolutePositionAtUT(UT));
            double desiredHeading = HeadingForInclination(newInclination, latitude);
            // var north = o.North(UT);
            // var east = o.East(UT);
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(UT), o.SwappedOrbitalVelocityAtUT(UT));
            Vector3d eastComponent = actualHorizontalVelocity.magnitude * Math.Sin(UtilMath.Deg2Rad * desiredHeading) * o.East(UT);
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeInclination: latitude {latitude}°, newInclination {newInclination}°");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeInclination: desiredHeading {desiredHeading}°");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeInclination: actualHorizontalVelocity  [{actualHorizontalVelocity.x}, {actualHorizontalVelocity.y}, {actualHorizontalVelocity.z}]");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeInclination: eastComponent             [{eastComponent.x}, {eastComponent.y}, {eastComponent.z}]");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeInclination: o.North(UT)               [{o.North(UT).x}, {o.North(UT).y}, {o.North(UT).z}]");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeInclination: o.East(UT)                [{o.East(UT).x}, {o.East(UT).y}, {o.East(UT).z}]");
            Vector3d northComponent = actualHorizontalVelocity.magnitude * Math.Cos(UtilMath.Deg2Rad * desiredHeading) * o.North(UT);
            if (Vector3d.Dot(actualHorizontalVelocity, northComponent) < 0) northComponent *= -1;
            if (MuUtils.ClampDegrees180(newInclination) < 0) northComponent *= -1;
            Vector3d desiredHorizontalVelocity = eastComponent + northComponent;
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeInclination: northComponent            [{northComponent.x}, {northComponent.y}, {northComponent.z}]");
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeInclination: desiredHorizontalVelocity [{desiredHorizontalVelocity.x}, {desiredHorizontalVelocity.y}, {desiredHorizontalVelocity.z}]");
            var deltaV = desiredHorizontalVelocity - actualHorizontalVelocity;
            //FlightPlanPlugin.Logger.LogDebug($"DeltaVToChangeInclination: deltaV [{deltaV.x}, {deltaV.y}, {deltaV.z}]");
            return deltaV;
        }

        //Computes the delta-V and time of a burn to match planes with the target orbit. The output burnUT
        //will be equal to the time of the first ascending node with respect to the target after the given UT.
        //Throws an ArgumentException if o is hyperbolic and doesn't have an ascending node relative to the target.
        public static Vector3d DeltaVAndTimeToMatchPlanesAscending(PatchedConicsOrbit o, PatchedConicsOrbit target, double UT, out double burnUT)
        {
            burnUT = o.TimeOfAscendingNode(target, UT);
            Vector3d desiredHorizontal = Vector3d.Cross(target.SwappedOrbitNormal(), o.Up(burnUT));
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(burnUT), o.SwappedOrbitalVelocityAtUT(burnUT));
            Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeToMatchPlanesAscending: desiredHorizontal         [{desiredHorizontal.x}, {desiredHorizontal.y}, {desiredHorizontal.z}]");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeToMatchPlanesAscending: actualHorizontalVelocity  [{actualHorizontalVelocity.x}, {actualHorizontalVelocity.y}, {actualHorizontalVelocity.z}]");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeToMatchPlanesAscending: desiredHorizontalVelocity [{desiredHorizontalVelocity.x}, {desiredHorizontalVelocity.y}, {desiredHorizontalVelocity.z}]");
            var deltaV = desiredHorizontalVelocity - actualHorizontalVelocity;
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeToMatchPlanesAscending: deltaV                    [{deltaV.x}, {deltaV.y}, {deltaV.z}]");
            return deltaV;
        }

        //Computes the delta-V and time of a burn to match planes with the target orbit. The output burnUT
        //will be equal to the time of the first descending node with respect to the target after the given UT.
        //Throws an ArgumentException if o is hyperbolic and doesn't have a descending node relative to the target.
        public static Vector3d DeltaVAndTimeToMatchPlanesDescending(PatchedConicsOrbit o, PatchedConicsOrbit target, double UT, out double burnUT)
        {
            burnUT = o.TimeOfDescendingNode(target, UT);
            Vector3d desiredHorizontal = Vector3d.Cross(target.SwappedOrbitNormal(), o.Up(burnUT));
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(burnUT), o.SwappedOrbitalVelocityAtUT(burnUT));
            Vector3d desiredHorizontalVelocity = actualHorizontalVelocity.magnitude * desiredHorizontal;
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeToMatchPlanesDescending: desiredHorizontal         [{desiredHorizontal.x}, {desiredHorizontal.y}, {desiredHorizontal.z}]");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeToMatchPlanesDescending: actualHorizontalVelocity  [{actualHorizontalVelocity.x}, {actualHorizontalVelocity.y}, {actualHorizontalVelocity.z}]");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeToMatchPlanesDescending: desiredHorizontalVelocity [{desiredHorizontalVelocity.x}, {desiredHorizontalVelocity.y}, {desiredHorizontalVelocity.z}]");
            var deltaV = desiredHorizontalVelocity - actualHorizontalVelocity;
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeToMatchPlanesDescending: deltaV                    [{deltaV.x}, {deltaV.y}, {deltaV.z}]");
            return deltaV;
        }

        //Computes the dV of a Hohmann transfer burn at time UT that will put the apoapsis or periapsis
        //of the transfer orbit on top of the target orbit.
        //The output value apsisPhaseAngle is the phase angle between the transferring vessel and the
        //target object as the transferring vessel crosses the target orbit at the apoapsis or periapsis
        //of the transfer orbit.
        //Actually, it's not exactly the phase angle. It's a sort of mean anomaly phase angle. The
        //difference is not important for how this function is used by DeltaVAndTimeForHohmannTransfer.
        private static Vector3d DeltaVAndApsisPhaseAngleOfHohmannTransfer(PatchedConicsOrbit o, PatchedConicsOrbit target, double UT, out double apsisPhaseAngle)
        {
            Vector3d apsisDirection = -o.SwappedRelativePositionAtUT(UT);
            double desiredApsis = target.RadiusAtTrueAnomaly(UtilMath.Deg2Rad * target.TrueAnomalyFromVector(apsisDirection));

            Vector3d dV;
            if (desiredApsis > o.Apoapsis)
            {
                dV = DeltaVToChangeApoapsis(o, UT, desiredApsis);
                PatchedConicsOrbit transferOrbit = o.PerturbedOrbit(UT, dV);
                double transferApTime = transferOrbit.NextApoapsisTime(UT);
                Vector3d transferApDirection = transferOrbit.SwappedRelativePositionAtApoapsis();  // getRelativePositionAtUT was returning NaNs! :(((((
                double targetTrueAnomaly = target.TrueAnomalyFromVector(transferApDirection);
                double meanAnomalyOffset = 360 * (target.TimeOfTrueAnomaly(targetTrueAnomaly, UT) - transferApTime) / target.period;
                apsisPhaseAngle = meanAnomalyOffset;
            }
            else
            {
                dV = DeltaVToChangePeriapsis(o, UT, desiredApsis);
                PatchedConicsOrbit transferOrbit = o.PerturbedOrbit(UT, dV);
                double transferPeTime = transferOrbit.NextPeriapsisTime(UT);
                Vector3d transferPeDirection = transferOrbit.SwappedRelativePositionAtPeriapsis();  // getRelativePositionAtUT was returning NaNs! :(((((
                double targetTrueAnomaly = target.TrueAnomalyFromVector(transferPeDirection);
                double meanAnomalyOffset = 360 * (target.TimeOfTrueAnomaly(targetTrueAnomaly, UT) - transferPeTime) / target.period;
                apsisPhaseAngle = meanAnomalyOffset;
            }

            apsisPhaseAngle = MuUtils.ClampDegrees180(apsisPhaseAngle);

            return dV;
        }

        //Computes the time and dV of a Hohmann transfer injection burn such that at apoapsis the transfer
        //orbit passes as close as possible to the target.
        //The output burnUT will be the first transfer window found after the given UT.
        //Assumes o and target are in approximately the same plane, and orbiting in the same direction.
        //Also assumes that o is a perfectly circular orbit (though result should be OK for small eccentricity).
        public static Vector3d DeltaVAndTimeForHohmannTransfer(PatchedConicsOrbit o, PatchedConicsOrbit target, double UT, out double burnUT)
        {
            //We do a binary search for the burn time that zeros out the phase angle between the
            //transferring vessel and the target at the apsis of the transfer orbit.
            double synodicPeriod = o.SynodicPeriod(target);
            FlightPlanPlugin.Logger.LogDebug($"synodicPeriod: {synodicPeriod}");

            double lastApsisPhaseAngle;
            Vector3d immediateBurnDV = DeltaVAndApsisPhaseAngleOfHohmannTransfer(o, target, UT, out lastApsisPhaseAngle);
            FlightPlanPlugin.Logger.LogDebug($"lastApsisPhaseAngle: {lastApsisPhaseAngle}");

            double minTime = UT;
            double maxTime = UT + 1.5 * synodicPeriod;

            //first find roughly where the zero point is
            const int numDivisions = 30;
            double dt = (maxTime - minTime) / numDivisions;
            for (int i = 1; i <= numDivisions; i++)
            {
                double t = minTime + dt * i;

                double apsisPhaseAngle;
                DeltaVAndApsisPhaseAngleOfHohmannTransfer(o, target, t, out apsisPhaseAngle);
                FlightPlanPlugin.Logger.LogDebug($"apsisPhaseAngle: {apsisPhaseAngle}");

                if ((Math.Abs(apsisPhaseAngle) < 90) && (Math.Sign(lastApsisPhaseAngle) != Math.Sign(apsisPhaseAngle)))
                {
                    minTime = t - dt;
                    maxTime = t;
                    FlightPlanPlugin.Logger.LogDebug($"Found transfer window between {minTime - UT} and {FPUtility.SecondsToTimeString(maxTime - UT)} from now (test {i})");
                    break;
                }

                if ((i == 1) && (Math.Abs(lastApsisPhaseAngle) < 0.5) && (Math.Sign(lastApsisPhaseAngle) == Math.Sign(apsisPhaseAngle)))
                {
                    //In this case we are JUST passed the center of the transfer window, but probably we
                    //can still do the transfer just fine. Don't do a search, just return an immediate burn
                    burnUT = UT;
                    return immediateBurnDV;
                }

                lastApsisPhaseAngle = apsisPhaseAngle;

                if (i == numDivisions)
                {
                    throw new ArgumentException("OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer: couldn't find the transfer window!!");
                }
            }

            burnUT = 0;
            Func<double, object, double> f = delegate(double testTime, object ign) {
                double testApsisPhaseAngle;
                DeltaVAndApsisPhaseAngleOfHohmannTransfer(o, target, testTime, out testApsisPhaseAngle);
                return testApsisPhaseAngle;
            };
            try { burnUT = BrentRoot.Solve(f, maxTime, minTime, null); }
            catch (TimeoutException) { FlightPlanPlugin.Logger.LogError("DeltaVAndTimeForHohmannTransfer: Brents method threw a timeout Error (supressed)"); }
            catch (ArgumentException e) { FlightPlanPlugin.Logger.LogError($"DeltaVAndTimeForHohmannTransfer: Brents method threw an argument exception Error (supressed): {e.Message}"); }

            Vector3d burnDV = DeltaVAndApsisPhaseAngleOfHohmannTransfer(o, target, burnUT, out _);
            FlightPlanPlugin.Logger.LogDebug($"Optimal Time for Transfer: {FPUtility.SecondsToTimeString(burnUT - UT)} from now");

            return burnDV;
        }

        public static Vector3d DeltaVToInterceptAtTime(PatchedConicsOrbit o, double UT, PatchedConicsOrbit target, double DT, double offsetDistance = 0, bool shortway = true)
        {
            Vector3d finalVelocity;
            return  DeltaVToInterceptAtTime(o, UT, target, DT, out finalVelocity, offsetDistance, shortway);
        }

        // Computes the delta-V of a burn at a given time that will put an object with a given orbit on a
        // course to intercept a target at a specific interceptUT.
        //
        // offsetDistance: this is used by the Rendezvous Autopilot and is only going to be valid over very short distances
        // shortway: the shortway parameter to feed into the Lambert solver
        //
        private static Vector3d DeltaVToInterceptAtTime(PatchedConicsOrbit o, double initialUT, PatchedConicsOrbit target, double dt, out Vector3d secondDV, double offsetDistance = 0, bool shortway = true)
        {
            var initialRelPos = o.SwappedRelativePositionAtUT(initialUT).ToV3();
            var finalRelPos = target.SwappedRelativePositionAtUT(initialUT + dt).ToV3();

            var initialVelocity = o.SwappedOrbitalVelocityAtUT(initialUT).ToV3();
            var finalVelocity = target.SwappedOrbitalVelocityAtUT(initialUT + dt).ToV3();

            Gooding.Solve(o.referenceBody.gravParameter, initialRelPos, initialVelocity, finalRelPos, shortway ? dt : -dt, 0, out V3 transferVi, out V3 transferVf);

            if (offsetDistance != 0)
            {
                finalRelPos -= offsetDistance * V3.Cross(finalVelocity, finalRelPos).normalized;
                Gooding.Solve(o.referenceBody.gravParameter, initialRelPos, initialVelocity, finalRelPos, shortway ? dt : -dt, 0, out transferVi, out transferVf);
            }

            secondDV = (finalVelocity - transferVf).ToVector3d();

            return (transferVi - initialVelocity).ToVector3d();
        }

        // Lambert Solver Driver function.
        //
        // This uses Shepperd's method instead of using KSP's orbit class.
        //
        // The reference time is usually 'now' or the first time the burn can start.
        //
        // GM       - grav parameter of the celestial
        // pos      - position of the source orbit at a reference time
        // vel      - velocity of the source orbit at a reference time
        // tpos     - position of the target orbit at a reference time
        // tvel     - velocity of the target orbit at a reference time
        // DT       - time of the burn in seconds after the reference time
        // TT       - transfer time of the burn in seconds after the burn time
        // secondDV - the second burn dV
        // returns  - the first burn dV
        //
        private static Vector3d DeltaVToInterceptAtTime(double GM, Vector3d pos, Vector3d vel, Vector3d tpos, Vector3d tvel, double dt, double tt, out Vector3d secondDV, bool posigrade = true)
        {
            // advance the source orbit to ref + DT
            Shepperd.Solve(GM, dt, pos.ToV3(), vel.ToV3(), out V3 pos1, out V3 vel1);

            // advance the target orbit to ref + DT + TT
            Shepperd.Solve(GM, dt + tt, tpos.ToV3(), tvel.ToV3(), out V3 pos2, out V3 vel2);

            Gooding.Solve(GM, pos1, vel1, pos2, posigrade ? tt : -tt, 0, out V3 transferVi, out V3 transferVf);

            secondDV = (vel2 - transferVf).ToVector3d();

            return (transferVi - vel1).ToVector3d();
        }

        // This does a line-search to find the burnUT for the cheapest course correction that will intercept exactly
        public static Vector3d DeltaVAndTimeForCheapestCourseCorrection(PatchedConicsOrbit o, double UT, PatchedConicsOrbit target, out double burnUT)
        {
            double closestApproachTime = o.NextClosestApproachTime(target, UT + 2); //+2 so that closestApproachTime is definitely > UT

            burnUT = UT;
            Vector3d dV = DeltaVToInterceptAtTime(o, burnUT, target, closestApproachTime - burnUT);

            // FIXME: replace with BrentRoot's 1-d minimization algorithm
            const int fineness = 20;
            for (double step = 0.5; step < fineness; step += 1.0)
            {
                double testUT = UT + (closestApproachTime - UT) * step / fineness;
                Vector3d testDV = DeltaVToInterceptAtTime(o, testUT, target, closestApproachTime - testUT);

                if (testDV.magnitude < dV.magnitude)
                {
                    dV = testDV;
                    burnUT = testUT;
                }
            }

            return dV;
        }

        // This is the entry point for the course-correction to a target orbit which is a celestial
        public static Vector3d DeltaVAndTimeForCheapestCourseCorrection(PatchedConicsOrbit o, double UT, PatchedConicsOrbit target, CelestialBodyComponent targetBody, double finalPeR, out double burnUT)
        {
            Vector3d collisionDV = DeltaVAndTimeForCheapestCourseCorrection(o, UT, target, out burnUT);
            PatchedConicsOrbit collisionOrbit = o.PerturbedOrbit(burnUT, collisionDV);
            double collisionUT = collisionOrbit.NextClosestApproachTime(target, burnUT);
            Vector3d collisionPosition = target.SwappedAbsolutePositionAtUT(collisionUT);
            Vector3d collisionRelVel = collisionOrbit.SwappedOrbitalVelocityAtUT(collisionUT) - target.SwappedOrbitalVelocityAtUT(collisionUT);

            double soiEnterUT = collisionUT - targetBody.sphereOfInfluence / collisionRelVel.magnitude;
            Vector3d soiEnterRelVel = collisionOrbit.SwappedOrbitalVelocityAtUT(soiEnterUT) - target.SwappedOrbitalVelocityAtUT(soiEnterUT);

            double E = 0.5 * soiEnterRelVel.sqrMagnitude - targetBody.gravParameter / targetBody.sphereOfInfluence; //total orbital energy on SoI enter
            double finalPeSpeed = Math.Sqrt(2 * (E + targetBody.gravParameter / finalPeR)); //conservation of energy gives the orbital speed at finalPeR.
            double desiredImpactParameter = finalPeR * finalPeSpeed / soiEnterRelVel.magnitude; //conservation of angular momentum gives the required impact parameter

            Vector3d displacementDir = Vector3d.Cross(collisionRelVel, o.SwappedOrbitNormal()).normalized;
            Vector3d interceptTarget = collisionPosition + desiredImpactParameter * displacementDir;

            Gooding.Solve(o.referenceBody.gravParameter, o.SwappedRelativePositionAtUT(burnUT).ToV3(), o.SwappedOrbitalVelocityAtUT(burnUT).ToV3(), (interceptTarget - o.referenceBody.Position.localPosition).ToV3(),  collisionUT - burnUT, 0, out V3 velAfterBurn, out V3 _);

            Vector3d deltaV = velAfterBurn.ToVector3d() - o.SwappedOrbitalVelocityAtUT(burnUT);
            return deltaV;
        }

        // This is the entry point for the course-correction to a target orbit which is not a celestial
        public static Vector3d DeltaVAndTimeForCheapestCourseCorrection(PatchedConicsOrbit o, double UT, PatchedConicsOrbit target, double caDistance, out double burnUT)
        {
            Vector3d collisionDV = DeltaVAndTimeForCheapestCourseCorrection(o, UT, target, out burnUT);
            PatchedConicsOrbit collisionOrbit = o.PerturbedOrbit(burnUT, collisionDV);
            double collisionUT = collisionOrbit.NextClosestApproachTime(target, burnUT);
            Vector3d targetPos = target.SwappedAbsolutePositionAtUT(collisionUT);

            Vector3d interceptTarget = targetPos + target.NormalPlus(collisionUT) * caDistance;

            Gooding.Solve(o.referenceBody.gravParameter, o.SwappedRelativePositionAtUT(burnUT).ToV3(), o.SwappedOrbitalVelocityAtUT(burnUT).ToV3(), (interceptTarget - o.referenceBody.Position.localPosition).ToV3(), collisionUT - burnUT, 0, out V3 velAfterBurn, out V3 _);

            Vector3d deltaV = velAfterBurn.ToVector3d() - o.SwappedOrbitalVelocityAtUT(burnUT);
            return deltaV;
        }

        //Computes the time and delta-V of an ejection burn to a Hohmann transfer from one planet to another.
        //It's assumed that the initial orbit around the first planet is circular, and that this orbit
        //is in the same plane as the orbit of the first planet around the sun. It's also assumed that
        //the target planet has a fairly low relative inclination with respect to the first planet. If the
        //inclination change is nonzero you should also do a mid-course correction burn, as computed by
        //DeltaVForCourseCorrection (a function that has been removed due to being unused).
        public static Vector3d DeltaVAndTimeForInterplanetaryTransferEjection(PatchedConicsOrbit o, double UT, PatchedConicsOrbit target, bool syncPhaseAngle, out double burnUT)
        {
            PatchedConicsOrbit planetOrbit = o.referenceBody.Orbit;
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryTransferEjection: From {o.referenceBody.Name}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryTransferEjection: To {FlightPlanPlugin.Instance._currentTarget.Name}");

            //Compute the time and dV for a Hohmann transfer where we pretend that we are the planet we are orbiting.
            //This gives us the "ideal" deltaV and UT of the ejection burn, if we didn't have to worry about waiting for the right
            //ejection angle and if we didn't have to worry about the planet's gravity dragging us back and increasing the required dV.
            double idealBurnUT;
            Vector3d idealDeltaV;

            if (syncPhaseAngle)
            {
                //time the ejection burn to intercept the target
                idealDeltaV = DeltaVAndTimeForHohmannTransfer(planetOrbit, target, UT, out idealBurnUT);
            }
            else
            {
                //don't time the ejection burn to intercept the target; we just care about the final peri/apoapsis
                idealBurnUT = UT;
                if (target.semiMajorAxis < planetOrbit.semiMajorAxis) idealDeltaV = DeltaVToChangePeriapsis(planetOrbit, idealBurnUT, target.semiMajorAxis);
                else idealDeltaV = DeltaVToChangeApoapsis(planetOrbit, idealBurnUT, target.semiMajorAxis);
            }
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryTransferEjection: idealDeltaV: [{idealDeltaV.x}, {idealDeltaV.y}, {idealDeltaV.z}] = {idealDeltaV.magnitude} m/s");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryTransferEjection: idealBurnUT: {idealBurnUT} = {FPUtility.SecondsToTimeString(idealBurnUT - UT)} from now");

            //Compute the actual transfer orbit this ideal burn would lead to.
            PatchedConicsOrbit transferOrbit = planetOrbit.PerturbedOrbit(idealBurnUT, idealDeltaV);

            //Now figure out how to approximately eject from our current orbit into the Hohmann orbit we just computed.

            //Assume we want to exit the SOI with the same velocity as the ideal transfer orbit at idealUT -- i.e., immediately
            //after the "ideal" burn we used to compute the transfer orbit. This isn't quite right.
            //We intend to eject from our planet at idealUT and only several hours later will we exit the SOI. Meanwhile
            //the transfer orbit will have acquired a slightly different velocity, which we should correct for. Maybe
            //just add in (1/2)(sun gravity)*(time to exit soi)^2 ? But how to compute time to exit soi? Or maybe once we
            //have the ejection orbit we should just move the ejection burn back by the time to exit the soi?
            Vector3d soiExitVelocity = idealDeltaV;
            //project the desired exit direction into the current orbit plane to get the feasible exit direction
            Vector3d inPlaneSoiExitDirection = Vector3d.Exclude(o.SwappedOrbitNormal(), soiExitVelocity).normalized;

            //compute the angle by which the trajectory turns between periapsis (where we do the ejection burn)
            //and SOI exit (approximated as radius = infinity)
            double soiExitEnergy = 0.5 * soiExitVelocity.sqrMagnitude - o.referenceBody.gravParameter / o.referenceBody.sphereOfInfluence;
            double ejectionRadius = o.semiMajorAxis; //a guess, good for nearly circular orbits

            double ejectionKineticEnergy = soiExitEnergy + o.referenceBody.gravParameter / ejectionRadius;
            double ejectionSpeed = Math.Sqrt(2 * ejectionKineticEnergy);

            //construct a sample ejection orbit
            Vector3d ejectionOrbitInitialVelocity = ejectionSpeed * (Vector3d)o.referenceBody.transform.right.vector;
            Vector3d ejectionOrbitInitialPosition = o.referenceBody.Position.localPosition + ejectionRadius * (Vector3d)o.referenceBody.transform.up.vector;
            PatchedConicsOrbit sampleEjectionOrbit = MuUtils.OrbitFromStateVectors(ejectionOrbitInitialPosition, ejectionOrbitInitialVelocity, o.coordinateSystem, o.referenceBody, 0);
            double ejectionOrbitDuration = sampleEjectionOrbit.NextTimeOfRadius(0, o.referenceBody.sphereOfInfluence);
            Vector3d ejectionOrbitFinalVelocity = sampleEjectionOrbit.SwappedOrbitalVelocityAtUT(ejectionOrbitDuration);

            double turningAngle = Math.Abs(Vector3d.Angle(ejectionOrbitInitialVelocity, ejectionOrbitFinalVelocity));
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryTransferEjection: turningAngle: {turningAngle}");

            //rotate the exit direction by 90 + the turning angle to get a vector pointing to the spot in our orbit
            //where we should do the ejection burn. Then convert this to a true anomaly and compute the time closest
            //to planetUT at which we will pass through that true anomaly.
            Vector3d ejectionPointDirection = Quaternion.AngleAxis(-(float)(90 + turningAngle), o.SwappedOrbitNormal()) * inPlaneSoiExitDirection;
            double ejectionTrueAnomaly = o.TrueAnomalyFromVector(ejectionPointDirection);
            burnUT = o.TimeOfTrueAnomaly(ejectionTrueAnomaly, idealBurnUT - o.period);

            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryTransferEjection: ejectionTrueAnomaly = {ejectionTrueAnomaly}");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryTransferEjection: burnUT = {burnUT} = {FPUtility.SecondsToTimeString(burnUT - UT)} from now.");

            if ((idealBurnUT - burnUT > o.period / 2) || (burnUT < UT))
            {
                burnUT += o.period;
            }

            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryTransferEjection: burnUT = {burnUT} = {FPUtility.SecondsToTimeString(burnUT - UT)} from now.");

            //rotate the exit direction by the turning angle to get a vector pointing to the spot in our orbit
            //where we should do the ejection burn
            Vector3d ejectionBurnDirection = Quaternion.AngleAxis(-(float)(turningAngle), o.SwappedOrbitNormal()) * inPlaneSoiExitDirection;
            Vector3d ejectionVelocity = ejectionSpeed * ejectionBurnDirection;

            Vector3d preEjectionVelocity = o.SwappedOrbitalVelocityAtUT(burnUT);

            var deltaV = ejectionVelocity - preEjectionVelocity;

            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryTransferEjection: ejectionVelocity:    [{ejectionVelocity.x}, {ejectionVelocity.y}, {ejectionVelocity.z}] = {ejectionVelocity.magnitude} m/s");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryTransferEjection: preEjectionVelocity: [{preEjectionVelocity.x}, {preEjectionVelocity.y}, {preEjectionVelocity.z}] = {preEjectionVelocity.magnitude} m/s");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryTransferEjection: deltaV:              [{deltaV.x}, {deltaV.y}, {deltaV.z}] = {deltaV.magnitude} m/s");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryTransferEjection: burnUT:              {FPUtility.SecondsToTimeString(burnUT - UT)} from now");

            return deltaV;
        }

        public struct LambertProblem
        {
            public Vector3d pos, vel;    // position + velocity of source orbit at reference time
            public Vector3d tpos, tvel;  // position + velocity of target orbit at reference time
            public double GM;
            public bool shortway;
            public bool intercept_only;  // omit the second burn from the cost
        }

        // x[0] is the burn time before/after zeroUT
        // x[1] is the time of the transfer
        //
        // f[1] is the cost of the burn
        //
        // prob.shortway is which lambert solution to find
        // prob.intercept_only omits adding the second burn to the cost
        //
        public static void LambertCost(double []x, double []f, object obj)
        {
            LambertProblem prob = (LambertProblem) obj;
            Vector3d secondBurn;

            try {
                f[0] = DeltaVToInterceptAtTime(prob.GM, prob.pos, prob.vel, prob.tpos, prob.tvel, x[0], x[1], out secondBurn, prob.shortway).magnitude;
                if (!prob.intercept_only)
                {
                    f[0] += secondBurn.magnitude;
                }
            }
            catch (Exception)
            {
                // need Sqrt of MaxValue so least-squares can square it without an infinity
                f[0] = Math.Sqrt(Double.MaxValue);
            }
            if (!f[0].IsFinite())
                f[0] = Math.Sqrt(Double.MaxValue);
        }

        // Levenburg-Marquardt local optimization of a two burn transfer.
        public static Vector3d DeltaVAndTimeForBiImpulsiveTransfer(double GM, Vector3d pos, Vector3d vel, Vector3d tpos, Vector3d tvel, double DT, double TT, out double burnDT, out double burnTT, out double burnCost, double minDT = Double.NegativeInfinity, double maxDT = Double.PositiveInfinity, double maxTT = Double.PositiveInfinity, double maxDTplusT = Double.PositiveInfinity, bool intercept_only = false, double eps = 1e-9, int maxIter = 100, bool shortway = false)
        {
            double[] x = { DT, TT };
            double[] scale = new double[2];

            if (maxDT != Double.PositiveInfinity && maxTT != Double.PositiveInfinity )
            {
                scale[0] = maxDT;
                scale[1] = maxTT;
            }
            else
            {
                scale[0] = DT;
                scale[1] = TT;
            }

            // absolute final time constraint: x[0] + x[1] <= maxUTplusT
            double[,] C = { { 1, 1, maxDTplusT } };
            int[] CT = { -1 };

            double[] bndl = { minDT, 0 };
            double[] bndu = { maxDT, maxTT };

            alglib.minlmstate state;
            alglib.minlmreport rep = new alglib.minlmreport();
            alglib.minlmcreatev(1, x, 0.000001, out state);
            alglib.minlmsetscale(state, scale);
            alglib.minlmsetbc(state, bndl, bndu);
            if ( maxDTplusT != Double.PositiveInfinity )
                alglib.minlmsetlc(state, C, CT);
            alglib.minlmsetcond(state, eps, maxIter);

            LambertProblem prob = new LambertProblem
            {
                pos = pos,
                vel = vel,
                tpos = tpos,
                tvel = tvel,
                GM = GM,
                shortway = shortway,
                intercept_only = intercept_only
            };

            alglib.minlmoptimize(state, LambertCost, null, prob);
            alglib.minlmresultsbuf(state, ref x, rep);
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForBiImpulsiveTransfer: iter = " + rep.iterationscount);
            if ( rep.terminationtype < 0 )
            {
                // FIXME: we should not accept this result
                FlightPlanPlugin.Logger.LogError("DeltaVAndTimeForBiImpulsiveTransfer: MechJeb Lambert Transfer minlmoptimize termination code: " + rep.terminationtype);
            }

            //FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForBiImpulsiveTransfer: x[0] = " + x[0] + " x[1] = " + x[1]);

            double[] fout = new double[1];
            LambertCost(x, fout, prob);
            burnCost = fout[0];

            burnDT = x[0];
            burnTT = x[1];

            Vector3d secondBurn; // ignored
            return DeltaVToInterceptAtTime(prob.GM, prob.pos, prob.vel, prob.tpos, prob.tvel, x[0], x[1], out secondBurn, prob.shortway);
        }

        public static double acceptanceProbabilityForBiImpulsive(double currentCost, double newCost, double temp)
        {
            if ( newCost < currentCost )
                return 1.0;
            return Math.Exp( (currentCost - newCost) / temp );
        }

        // Basin-Hopping algorithm global search for a two burn transfer (Note this says "Annealing" but it was converted to Basin-Hopping)
        //
        // FIXME: there's some very confusing nomenclature between DeltaVAndTimeForBiImpulsiveTransfer and this
        //        the minUT/maxUT values here are zero-centered on this methods UT.  the minUT/maxUT parameters to
        //        the other method are proper UT times and not zero centered at all.
        public static Vector3d DeltaVAndTimeForBiImpulsiveAnnealed(PatchedConicsOrbit o, PatchedConicsOrbit target, double UT, out double bestUT, double minDT = 0.0, double maxDT = Double.PositiveInfinity, bool intercept_only = false, bool fixed_ut = false)
        {
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForBiImpulsiveAnnealed: origin = " + o.MuString());
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForBiImpulsiveAnnealed: target = " + target.MuString());

            Vector3d pos = o.SwappedRelativePositionAtUT(UT);
            Vector3d vel = o.SwappedOrbitalVelocityAtUT(UT);
            Vector3d tpos = target.SwappedRelativePositionAtUT(UT);
            Vector3d tvel = target.SwappedOrbitalVelocityAtUT(UT);
            double GM = o.referenceBody.gravParameter;

            double MAXTEMP = 10000;
            double temp = MAXTEMP;
            double coolingRate = 0.01;

            double bestTT = 0;
            double bestDT = 0;
            double bestCost = Double.MaxValue;
            Vector3d bestBurnVec = Vector3d.zero;
            bool bestshortway = false;

            System.Random random = new System.Random();

            double maxDTplusT = Double.PositiveInfinity;

            // min transfer time must be > 0 (no teleportation)
            double minTT = 1e-15;

            // update the patched conic prediction for hyperbolic orbits (important *not* to do this for mutated planetary orbits, since we will
            // get encouters, but we need the patchEndTransition for hyperbolic orbits).
            if (target.eccentricity >= 1.0)
                target.CalculateNextOrbit();

            if (maxDT == Double.PositiveInfinity)
                maxDT = 1.5 * o.SynodicPeriod(target);

            // figure the max transfer time of a Hohmann orbit using the SMAs of the two orbits instead of the radius (as a guess), multiplied by 2
            double a = ( Math.Abs(o.semiMajorAxis) + Math.Abs(target.semiMajorAxis) ) / 2;
            double maxTT = Math.PI * Math.Sqrt( a * a * a / o.referenceBody.gravParameter );   // FIXME: allow tweaking

            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForBiImpulsiveAnnealed: Check1: minDT = " + minDT + " maxDT = " + maxDT + " maxTT = " + maxTT + " maxDTplusT = " + maxDTplusT);
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForBiImpulsiveAnnealed: target.patchEndTransition = " + target.PatchEndTransition);

            if (target.PatchEndTransition != PatchTransitionType.Final && target.PatchEndTransition != PatchTransitionType.Initial)
            {
                // reset the guess to search for start times out to the end of the target orbit
                maxDT = target.EndUT - UT;
                // longest possible transfer time would leave now and arrive at the target patch end
                maxTT = Math.Min(maxTT, target.EndUT - UT);
                // constraint on DT + TT <= maxDTplusT to arrive before the target orbit ends
                maxDTplusT = Math.Min(maxDTplusT, target.EndUT - UT);
            }

            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForBiImpulsiveAnnealed: o.patchEndTransition = " + o.PatchEndTransition);

            // if our orbit ends, search for start times all the way to the end, but don't violate maxDTplusT if its set
            if (o.PatchEndTransition != PatchTransitionType.Final && o.PatchEndTransition != PatchTransitionType.Initial)
            {
                maxDT = Math.Min(o.EndUT - UT, maxDTplusT);
            }

            // user requested a burn at a specific time
            if (fixed_ut)
            {
                maxDT = 0;
                minDT = 0;
            }

            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForBiImpulsiveAnnealed: Check2: minDT = " + minDT + " maxDT = " + maxDT + " maxTT = " + maxTT + " maxDTplusT = " + maxDTplusT);

            double currentCost = Double.MaxValue;
            double currentDT = maxDT / 2;
            double currentTT = maxTT / 2;

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();

            int n = 0;

            stopwatch.Start();
            while( temp > 1000 )
            {
                double burnDT, burnTT, burnCost;

                // shrink the neighborhood based on temp
                double windowDT = temp / MAXTEMP * ( maxDT - minDT );
                double windowTT = temp / MAXTEMP * ( maxTT - minTT );
                double windowminDT = currentDT - windowDT;
                windowminDT = windowminDT < minDT ? minDT : windowminDT;
                double windowmaxDT = currentDT + windowDT;
                windowmaxDT = windowmaxDT > maxDT ? maxDT : windowmaxDT;
                double windowminTT = currentTT - windowTT;
                windowminTT = windowminTT < minTT ? minTT : windowminTT;
                double windowmaxTT = currentTT + windowTT;
                windowmaxTT = windowmaxTT > maxTT ? maxTT : windowmaxTT;

                // compute the neighbor
                double nextDT = random.NextDouble() * (windowmaxDT - windowminDT) + windowminDT;
                double nextTT = random.NextDouble() * (windowmaxTT - windowminTT) + windowminTT;
                nextTT = Math.Min(nextTT, maxDTplusT - nextDT);

                // just randomize the shortway
                bool nextshortway = random.NextDouble() > 0.5;

                //FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForBiImpulsiveAnnealed: nextDT = " + nextDT + " nextTT = " + nextTT);
                Vector3d burnVec = DeltaVAndTimeForBiImpulsiveTransfer(GM, pos, vel, tpos, tvel, nextDT, nextTT, out burnDT, out burnTT, out burnCost, minDT: minDT, maxDT: maxDT, maxTT: maxTT, maxDTplusT: maxDTplusT, intercept_only: intercept_only, shortway: nextshortway);

                //FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForBiImpulsiveAnnealed: burnDT = " + burnDT + " burnTT = " + burnTT + " cost = " + burnCost + " bestCost = " + bestCost);

                if ( burnCost < bestCost )
                {
                    bestDT = burnDT;
                    bestTT = burnTT;
                    bestshortway = nextshortway;
                    bestCost = burnCost;
                    bestBurnVec = burnVec;
                    currentDT = bestDT;
                    currentTT = bestTT;
                    currentCost = bestCost;
                }
                else if ( acceptanceProbabilityForBiImpulsive(currentCost, burnCost, temp) > random.NextDouble() )
                {
                    currentDT = burnDT;
                    currentTT = burnTT;
                    currentCost = burnCost;
                }

                temp *= 1 - coolingRate;

                n++;
            }
            stopwatch.Stop();

            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForBiImpulsiveAnnealed: N = " + n + " time = " + stopwatch.Elapsed);

            bestUT = UT + bestDT;

            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForBiImpulsiveAnnealed: Annealing results burnUT = " + bestUT + " zero'd burnUT = " + bestDT + " TT = " + bestTT + " Cost = " + bestCost + " shortway= " + bestshortway);

            return bestBurnVec;
        }

        //Like DeltaVAndTimeForHohmannTransfer, but adds an additional step that uses the Lambert
        //solver to adjust the initial burn to produce an exact intercept instead of an approximate
        public static Vector3d DeltaVAndTimeForHohmannLambertTransfer(PatchedConicsOrbit o, PatchedConicsOrbit target, double UT, out double burnUT, double subtractProgradeDV = 0)
        {
            Vector3d hohmannDV = DeltaVAndTimeForHohmannTransfer(o, target, UT, out burnUT);
            Vector3d subtractedProgradeDV = subtractProgradeDV * hohmannDV.normalized;

            PatchedConicsOrbit hohmannOrbit = o.PerturbedOrbit(burnUT, hohmannDV);
            double apsisTime; //approximate target  intercept time
            if (hohmannOrbit.semiMajorAxis > o.semiMajorAxis) apsisTime = hohmannOrbit.NextApoapsisTime(burnUT);
            else apsisTime = hohmannOrbit.NextPeriapsisTime(burnUT);

            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForHohmannLambertTransfer: hohmannDV = " + (Vector3)hohmannDV + ", apsisTime = " + apsisTime);

            Vector3d dV = Vector3d.zero;
            double minCost = 999999;

            double minInterceptTime = apsisTime - hohmannOrbit.period / 4;
            double maxInterceptTime = apsisTime + hohmannOrbit.period / 4;
            const int subdivisions = 30;
            for (int i = 0; i < subdivisions; i++)
            {
                double interceptUT = minInterceptTime + i * (maxInterceptTime - minInterceptTime) / subdivisions;

                FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForHohmannLambertTransfer: i + " + i + ", trying for intercept at UT = " + interceptUT);

                //Try both short and long way
                Vector3d interceptBurn = DeltaVToInterceptAtTime(o, burnUT, target, interceptUT - burnUT, 0, true);
                double cost = (interceptBurn - subtractedProgradeDV).magnitude;
                FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForHohmannLambertTransfer: short way dV = " + interceptBurn.magnitude + "; subtracted cost = " + cost);
                if (cost < minCost)
                {
                    dV = interceptBurn;
                    minCost = cost;
                }

                interceptBurn = DeltaVToInterceptAtTime(o, burnUT, target, interceptUT, 0, false);
                cost = (interceptBurn - subtractedProgradeDV).magnitude;
                FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForHohmannLambertTransfer: long way dV = " + interceptBurn.magnitude + "; subtracted cost = " + cost);
                if (cost < minCost)
                {
                    dV = interceptBurn;
                    minCost = cost;
                }
            }

            return dV;
        }

        //Computes the time and delta-V of an ejection burn to a Hohmann transfer from one planet to another.
        //It's assumed that the initial orbit around the first planet is circular, and that this orbit
        //is in the same plane as the orbit of the first planet around the sun. It's also assumed that
        //the target planet has a fairly low relative inclination with respect to the first planet. If the
        //inclination change is nonzero you should also do a mid-course correction burn, as computed by
        //DeltaVForCourseCorrection (a function that has been removed due to being unused).
        public static Vector3d DeltaVAndTimeForInterplanetaryLambertTransferEjection(PatchedConicsOrbit o, double UT, PatchedConicsOrbit target, out double burnUT)
        {
            PatchedConicsOrbit planetOrbit = o.referenceBody.Orbit;

            //Compute the time and dV for a Hohmann transfer where we pretend that we are the planet we are orbiting.
            //This gives us the "ideal" deltaV and UT of the ejection burn, if we didn't have to worry about waiting for the right
            //ejection angle and if we didn't have to worry about the planet's gravity dragging us back and increasing the required dV.
            double idealBurnUT;
            Vector3d idealDeltaV;

            //time the ejection burn to intercept the target
            //idealDeltaV = DeltaVAndTimeForHohmannTransfer(planetOrbit, target, UT, out idealBurnUT);
            double vesselOrbitVelocity = OrbitalManeuverCalculator.CircularOrbitSpeed(o.referenceBody, o.semiMajorAxis);
            idealDeltaV = DeltaVAndTimeForHohmannLambertTransfer(planetOrbit, target, UT, out idealBurnUT, vesselOrbitVelocity);

            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryLambertTransferEjection: idealBurnUT = {idealBurnUT} = {FPUtility.SecondsToTimeString(idealBurnUT - UT)} from now.");
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryLambertTransferEjection: idealDeltaV = {idealDeltaV} m/s");

            //Compute the actual transfer orbit this ideal burn would lead to.
            PatchedConicsOrbit transferOrbit = planetOrbit.PerturbedOrbit(idealBurnUT, idealDeltaV);

            //Now figure out how to approximately eject from our current orbit into the Hohmann orbit we just computed.

            //Assume we want to exit the SOI with the same velocity as the ideal transfer orbit at idealUT -- i.e., immediately
            //after the "ideal" burn we used to compute the transfer orbit. This isn't quite right.
            //We intend to eject from our planet at idealUT and only several hours later will we exit the SOI. Meanwhile
            //the transfer orbit will have acquired a slightly different velocity, which we should correct for. Maybe
            //just add in (1/2)(sun gravity)*(time to exit soi)^2 ? But how to compute time to exit soi? Or maybe once we
            //have the ejection orbit we should just move the ejection burn back by the time to exit the soi?
            Vector3d soiExitVelocity = idealDeltaV;
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: soiExitVelocity = " + (Vector3)soiExitVelocity);

            //compute the angle by which the trajectory turns between periapsis (where we do the ejection burn)
            //and SOI exit (approximated as radius = infinity)
            double soiExitEnergy = 0.5 * soiExitVelocity.sqrMagnitude - o.referenceBody.gravParameter / o.referenceBody.sphereOfInfluence;
            double ejectionRadius = o.semiMajorAxis; //a guess, good for nearly circular orbits
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: soiExitEnergy = " + soiExitEnergy);
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: ejectionRadius = " + ejectionRadius);

            double ejectionKineticEnergy = soiExitEnergy + o.referenceBody.gravParameter / ejectionRadius;
            double ejectionSpeed = Math.Sqrt(2 * ejectionKineticEnergy);
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: ejectionSpeed = " + ejectionSpeed);

            //construct a sample ejection orbit
            Vector3d ejectionOrbitInitialVelocity = ejectionSpeed * (Vector3d)o.referenceBody.transform.right.vector;
            Vector3d ejectionOrbitInitialPosition = o.referenceBody.Position.localPosition + ejectionRadius * (Vector3d)o.referenceBody.transform.up.vector;
            PatchedConicsOrbit sampleEjectionOrbit = MuUtils.OrbitFromStateVectors(ejectionOrbitInitialPosition, ejectionOrbitInitialVelocity, o.coordinateSystem, o.referenceBody, UT); // was 0 in place of UT
            double ejectionOrbitDuration = sampleEjectionOrbit.NextTimeOfRadius(0, o.referenceBody.sphereOfInfluence);
            Vector3d ejectionOrbitFinalVelocity = sampleEjectionOrbit.SwappedOrbitalVelocityAtUT(ejectionOrbitDuration);

            double turningAngle = Vector3d.Angle(ejectionOrbitInitialVelocity, ejectionOrbitFinalVelocity);
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: turningAngle = " + turningAngle);

            //sine of the angle between the vessel orbit and the desired SOI exit velocity
            double outOfPlaneAngle = (UtilMath.Deg2Rad) * (90 - Vector3d.Angle(soiExitVelocity, o.SwappedOrbitNormal()));
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: outOfPlaneAngle (rad) = " + outOfPlaneAngle);

            double coneAngle = Math.PI / 2 - (UtilMath.Deg2Rad) * turningAngle;
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: coneAngle (rad) = " + coneAngle);

            Vector3d exitNormal = Vector3d.Cross(-soiExitVelocity, o.SwappedOrbitNormal()).normalized;
            Vector3d normal2 = Vector3d.Cross(exitNormal, -soiExitVelocity).normalized;

            //unit vector pointing to the spot on our orbit where we will burn.
            //fails if outOfPlaneAngle > coneAngle.
            Vector3d ejectionPointDirection = Math.Cos(coneAngle) * (-soiExitVelocity.normalized)
                + Math.Cos(coneAngle) * Math.Tan(outOfPlaneAngle) * normal2
                - Math.Sqrt(Math.Pow(Math.Sin(coneAngle), 2) - Math.Pow(Math.Cos(coneAngle) * Math.Tan(outOfPlaneAngle), 2)) * exitNormal;

            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: soiExitVelocity = " + (Vector3)soiExitVelocity);
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: vessel orbit normal = " + (Vector3)(1000 * o.SwappedOrbitNormal()));
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: exitNormal = " + (Vector3)(1000 * exitNormal));
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: normal2 = " + (Vector3)(1000 * normal2));
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: ejectionPointDirection = " + ejectionPointDirection);

            double ejectionTrueAnomaly = o.TrueAnomalyFromVector(ejectionPointDirection);
            burnUT = o.TimeOfTrueAnomaly(ejectionTrueAnomaly, idealBurnUT - o.period);

            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: ejectionTrueAnomaly = " + ejectionTrueAnomaly);
            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryLambertTransferEjection: burnUT = {burnUT} = {FPUtility.SecondsToTimeString(burnUT - UT)} from now.");

            if ((idealBurnUT - burnUT > o.period / 2) || (burnUT < UT))
            {
                burnUT += o.period;
            }

            FlightPlanPlugin.Logger.LogDebug($"DeltaVAndTimeForInterplanetaryLambertTransferEjection: burnUT = {burnUT} = {FPUtility.SecondsToTimeString(burnUT - UT)} from now.");

            Vector3d ejectionOrbitNormal = Vector3d.Cross(ejectionPointDirection, soiExitVelocity).normalized;
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: ejectionOrbitNormal = " + ejectionOrbitNormal);
            Vector3d ejectionBurnDirection = Quaternion.AngleAxis(-(float)(turningAngle), ejectionOrbitNormal) * soiExitVelocity.normalized;
            FlightPlanPlugin.Logger.LogDebug("DeltaVAndTimeForInterplanetaryLambertTransferEjection: ejectionBurnDirection = " + ejectionBurnDirection);
            Vector3d ejectionVelocity = ejectionSpeed * ejectionBurnDirection;

            Vector3d preEjectionVelocity = o.SwappedOrbitalVelocityAtUT(burnUT);

            return ejectionVelocity - preEjectionVelocity;
        }

        public static Vector3d DeltaVAndTimeForMoonReturnEjection(PatchedConicsOrbit o, double UT, double targetPrimaryRadius, out double burnUT)
        {
            CelestialBodyComponent moon = o.referenceBody;
            CelestialBodyComponent primary = moon.referenceBody;

            //construct an orbit at the target radius around the primary, in the same plane as the moon. This is a fake target
            // The inputs look like what would be in a KeplerOrbitState, but using that doesn't work?
            PatchedConicsOrbit primaryOrbit = new PatchedConicsOrbit(GameManager.Instance.Game.UniverseModel)
            {
                referenceBody = primary,
                inclination = moon.Orbit.inclination,
                eccentricity = moon.Orbit.eccentricity,
                semiMajorAxis = targetPrimaryRadius,
                longitudeOfAscendingNode = moon.Orbit.longitudeOfAscendingNode,
                argumentOfPeriapsis = moon.Orbit.argumentOfPeriapsis,
                meanAnomalyAtEpoch = moon.Orbit.meanAnomalyAtEpoch,
                epoch = moon.Orbit.epoch
            };

            return DeltaVAndTimeForInterplanetaryTransferEjection(o, UT, primaryOrbit, false, out burnUT);
        }

        //Computes the delta-V of the burn at a given time required to zero out the difference in orbital velocities
        //between a given orbit and a target.
        public static Vector3d DeltaVToMatchVelocities(PatchedConicsOrbit o, double UT, PatchedConicsOrbit target)
        {
            return target.SwappedOrbitalVelocityAtUT(UT) - o.SwappedOrbitalVelocityAtUT(UT);
        }

        // Compute the delta-V of the burn at the givent time required to enter an orbit with a period of (resonanceDivider-1)/resonanceDivider of the starting orbit period
        public static Vector3d DeltaVToResonantOrbit(PatchedConicsOrbit o, double UT, double f)
        {
            double a = o.Apoapsis;
            double p = o.Periapsis;

            // Thanks wolframAlpha for the Math
            // x = (a^3 f^2 + 3 a^2 f^2 p + 3 a f^2 p^2 + f^2 p^3)^(1/3)-a
            double x = Math.Pow(Math.Pow(a, 3) * Math.Pow(f, 2) + 3 * Math.Pow(a, 2) * Math.Pow(f, 2) * p + 3 * a * Math.Pow(f, 2) * Math.Pow(p, 2) + Math.Pow(f, 2) * Math.Pow(p, 3), 1d / 3) - a;

            if (x < 0)
                return Vector3d.zero;

            if (f > 1)
                return OrbitalManeuverCalculator.DeltaVToChangeApoapsis(o, UT, x);
            else
                return OrbitalManeuverCalculator.DeltaVToChangePeriapsis(o, UT, x);
        }

        // Compute the angular distance between two points on a unit sphere
        public static double Distance(double lat_a, double long_a, double lat_b, double long_b)
        {
            // Using Great-Circle Distance 2nd computational formula from http://en.wikipedia.org/wiki/Great-circle_distance
            // Note the switch from degrees to radians and back
            double lat_a_rad = UtilMath.Deg2Rad * lat_a;
            double lat_b_rad = UtilMath.Deg2Rad * lat_b;
            double long_diff_rad = UtilMath.Deg2Rad * (long_b - long_a);

            return UtilMath.Rad2Deg * Math.Atan2(Math.Sqrt(Math.Pow(Math.Cos(lat_b_rad) * Math.Sin(long_diff_rad), 2) +
                        Math.Pow(Math.Cos(lat_a_rad) * Math.Sin(lat_b_rad) - Math.Sin(lat_a_rad) * Math.Cos(lat_b_rad) * Math.Cos(long_diff_rad), 2)),
                    Math.Sin(lat_a_rad) * Math.Sin(lat_b_rad) + Math.Cos(lat_a_rad) * Math.Cos(lat_b_rad) * Math.Cos(long_diff_rad));
        }

        // Compute an angular heading from point a to point b on a unit sphere
        public static double Heading(double lat_a, double long_a, double lat_b, double long_b)
        {
            // Using Great-Circle Navigation formula for initial heading from http://en.wikipedia.org/wiki/Great-circle_navigation
            // Note the switch from degrees to radians and back
            // Original equation returns 0 for due south, increasing clockwise. We add 180 and clamp to 0-360 degrees to map to compass-type headings
            double lat_a_rad = UtilMath.Deg2Rad * lat_a;
            double lat_b_rad = UtilMath.Deg2Rad * lat_b;
            double long_diff_rad = UtilMath.Deg2Rad * (long_b - long_a);

            return MuUtils.ClampDegrees360(180.0 / Math.PI * Math.Atan2(
                        Math.Sin(long_diff_rad),
                        Math.Cos(lat_a_rad) * Math.Tan(lat_b_rad) - Math.Sin(lat_a_rad) * Math.Cos(long_diff_rad)));
        }

        //Computes the deltaV of the burn needed to set a given LAN at a given UT.
        public static Vector3d DeltaVToShiftLAN(PatchedConicsOrbit o, double UT, double newLAN)
        {
            //Vector3d pos = o.SwappedAbsolutePositionAtUT(UT);
            //// Burn Position in the same reference frame as LAN
            //double latitude, longitude, altitude;
            //KSP.Sim.Position Position = new KSP.Sim.Position(o.ReferenceBody.coordinateSystem, pos);
            //o.ReferenceBody.GetLatLonAltFromRadius(Position, out latitude, out longitude, out altitude);
            double longitude;
            double burn_latitude = GetLatLon(o, UT, out longitude); // was o.ReferenceBody.GetLatitude(o.SwappedAbsolutePositionAtUT(UT));
            double burn_longitude = longitude + o.referenceBody.rotationAngle;

            const double target_latitude = 0; // Equator
            double target_longitude = 0; // Prime Meridian

            // Select the location of either the descending or ascending node.
            // If the descending node is closer than the ascending node, or there is no ascending node, target the reverse of the newLAN
            // Otherwise target the newLAN
            if (o.AscendingNodeEquatorialExists() && o.DescendingNodeEquatorialExists())
            {
                if (o.TimeOfDescendingNodeEquatorial(UT) < o.TimeOfAscendingNodeEquatorial(UT))
                {
                    // DN is closer than AN
                    // Burning for the AN would entail flipping the orbit around, and would be very expensive
                    // therefore, burn for the corresponding Longitude of the Descending Node
                    target_longitude = MuUtils.ClampDegrees360(newLAN + 180.0);
                }
                else
                {
                    // DN is closer than AN
                    target_longitude = MuUtils.ClampDegrees360(newLAN);
                }
            }
            else if (o.AscendingNodeEquatorialExists() && !o.DescendingNodeEquatorialExists())
            {
                // No DN
                target_longitude = MuUtils.ClampDegrees360(newLAN);
            }
            else if (!o.AscendingNodeEquatorialExists() && o.DescendingNodeEquatorialExists())
            {
                // No AN
                target_longitude = MuUtils.ClampDegrees360(newLAN + 180.0);
            }
            else
            {
                throw new ArgumentException("OrbitalManeuverCalculator.DeltaVToShiftLAN: No Equatorial Nodes");
            }
            double desiredHeading = MuUtils.ClampDegrees360(Heading(burn_latitude, burn_longitude, target_latitude, target_longitude));
            Vector3d actualHorizontalVelocity = Vector3d.Exclude(o.Up(UT), o.SwappedOrbitalVelocityAtUT(UT));
            Vector3d eastComponent = actualHorizontalVelocity.magnitude * Math.Sin(UtilMath.Deg2Rad * desiredHeading) * o.East(UT);
            Vector3d northComponent = actualHorizontalVelocity.magnitude * Math.Cos(UtilMath.Deg2Rad * desiredHeading) * o.North(UT);
            Vector3d desiredHorizontalVelocity = eastComponent + northComponent;
            return desiredHorizontalVelocity - actualHorizontalVelocity;
        }


        public static Vector3d DeltaVForSemiMajorAxis(PatchedConicsOrbit o, double UT, double newSMA)
        {
            bool raising = o.semiMajorAxis < newSMA;
            Vector3d burnDirection = (raising ? 1 : -1) * o.Prograde(UT);
            double minDeltaV = 0;
            double maxDeltaV = raising ? 10000 :  Math.Abs(Vector3d.Dot(o.SwappedOrbitalVelocityAtUT(UT), burnDirection));

            // solve for the reciprocal of the SMA which is a continuous function that avoids the parabolic singularity and
            // change of sign for hyperbolic orbits.
            Func<double, object, double> f = delegate(double testDeltaV, object ign) { return 1.0/o.PerturbedOrbit(UT, testDeltaV * burnDirection).semiMajorAxis - 1.0/newSMA;  };
            double dV = 0;
            try { dV = BrentRoot.Solve(f, minDeltaV, maxDeltaV, null); }
            catch (TimeoutException) { FlightPlanPlugin.Logger.LogError("DeltaVForSemiMajorAxis: Brents method threw a timeout Error (supressed)"); }
            catch (ArgumentException e) { FlightPlanPlugin.Logger.LogError($"DeltaVForSemiMajorAxis: Brents method threw an argument exception Error (supressed): {e.Message}"); }

            return dV * burnDirection;
        }

        public static Vector3d DeltaVToShiftNodeLongitude(PatchedConicsOrbit o, double UT, double newNodeLong)
        {
            // Get the location underneath the burn location at the current moment.
            // Note that this does NOT account for the rotation of the body that will happen between now
            // and when the vessel reaches the apoapsis.
            Vector3d pos = o.SwappedAbsolutePositionAtUT (UT);
            double burnRadius = o.Radius (UT);
            double oppositeRadius = 0;

            // Back out the rotation of the body to calculate the longitude of the apoapsis when the vessel reaches the node
            double degreeRotationToNode = (UT - GameManager.Instance.Game.UniverseModel.UniversalTime) * 360 / o.referenceBody.rotationPeriod;
            //double latitude, longitude, altitude;
            //KSP.Sim.Position Position = new KSP.Sim.Position(o.ReferenceBody.coordinateSystem, pos);
            //o.ReferenceBody.GetLatLonAltFromRadius(Position, out latitude, out longitude, out altitude);
            double NodeLongitude = GetLongitude(o, UT) - degreeRotationToNode;

            double LongitudeOffset = NodeLongitude - newNodeLong; // Amount we need to shift the Ap's longitude

            // Calculate a semi-major axis that gives us an orbital period that will rotate the body to place
            // the burn location directly over the newNodeLong longitude, over the course of one full orbit.
            // N tracks the number of full body rotations desired in a vessal orbit.
            // If N=0, we calculate the SMA required to let the body rotate less than a full local day.
            // If the resulting SMA would drop us under the 5x time warp limit, we deem it to be too low, and try again with N+1.
            // In other words, we allow the body to rotate more than 1 day, but less then 2 days.
            // As long as the resulting SMA is below the 5x limit, we keep increasing N until we find a viable solution.
            // This may place the apside out the sphere of influence, however.
            // TODO: find the cheapest SMA, instead of the smallest
            int N = -1;
            double target_sma = 0;

            while (oppositeRadius-o.referenceBody.radius < o.referenceBody.TimeWarpAltitudeOffset*4 && N < 20)
            { // was o.ReferenceBody.timeWarpAltitudeLimits[4] -> o.ReferenceBody.TimeWarpAltitudeOffset*4
                N++;
                double target_period = o.referenceBody.rotationPeriod * (LongitudeOffset / 360 + N);
                target_sma = Math.Pow ((o.referenceBody.gravParameter * target_period * target_period) / (4 * Math.PI * Math.PI), 1.0 / 3.0); // cube roo
                oppositeRadius = 2 * (target_sma) - burnRadius;
            }
            return DeltaVForSemiMajorAxis (o, UT, target_sma);
        }

        //
        // Global OrbitPool for re-using orbit objects
        //

        // public static readonly Pool<PatchedConicsOrbit> OrbitPool = new Pool<PatchedConicsOrbit>(createOrbit, resetOrbit);
        private static PatchedConicsOrbit createOrbit() { return new PatchedConicsOrbit(GameManager.Instance.Game.UniverseModel); }
        private static void resetOrbit(PatchedConicsOrbit o) { }

        // private static readonly PatchedConicSolver.SolverParameters solverParameters = new PatchedConicSolver.SolverParameters();

        // Runs the PatchedConicSolver to do initial value "shooting" given an initial orbit, a maneuver dV and UT to execute, to a target Celestial's SOI
        //
        // initial   : initial parkig orbit
        // target    : the Body whose SOI we are shooting towards
        // dV        : the dV of the manuever off of the parking orbit
        // burnUT    : the time of the maneuver off of the parking orbit
        // arrivalUT : this is really more of an upper clamp on the simulation so that if we miss and never hit the body SOI it stops
        // intercept : this is the final computed intercept orbit, it should be in the SOI of the target body, but if it never hits it then the
        //             e.g. heliocentric orbit is returned instead, so the caller needs to check.
        //
        // FIXME: NREs when there's no next patch
        // FIXME: duplicates code with OrbitExtensions.CalculateNextOrbit()
        //
        //public static void PatchedConicInterceptBody(PatchedConicsOrbit initial, CelestialBodyComponent target, Vector3d dV, double burnUT, double arrivalUT, out PatchedConicsOrbit intercept)
        //{
        //    PatchedConicsOrbit orbit = OrbitPool.Borrow();
        //    orbit.UpdateFromStateVectors(initial.SwappedRelativePositionAtUT(burnUT), initial.SwappedOrbitalVelocityAtUT(burnUT) + dV.xzy, initial.ReferenceBody, burnUT);
        //    orbit.StartUT = burnUT;
        //    orbit.EndUT = orbit.eccentricity >= 1.0 ? orbit.period : burnUT + orbit.period;
        //    PatchedConicsOrbit next_orbit = OrbitPool.Borrow();

        //    bool ok = PatchedConics.CalculatePatch(orbit, next_orbit, burnUT, solverParameters, null);
        //    while (ok && (orbit.ReferenceBody != target) && (orbit.EndUT < arrivalUT))
        //    {
        //        OrbitPool.Release(orbit);
        //        orbit = next_orbit;
        //        next_orbit = OrbitPool.Borrow();

        //        ok = PatchedConics.CalculatePatch(orbit, next_orbit, orbit.StartUT, solverParameters, null);
        //    }
        //    intercept = orbit;
        //    intercept.UpdateFromOrbitAtUT(orbit, arrivalUT, orbit.ReferenceBody);
        //    OrbitPool.Release(orbit);
        //    OrbitPool.Release(next_orbit);
        //}

        // Takes an e.g. heliocentric orbit and a target planet celestial and finds the time of the SOI intercept.
        //
        //
        //
        public static void SOI_intercept(PatchedConicsOrbit transfer, CelestialBodyComponent target, double UT1, double UT2, out double UT)
        {
            if ( transfer.referenceBody != target.Orbit.referenceBody )
                throw new ArgumentException("[MechJeb] SOI_intercept: transfer orbit must be in the same SOI as the target celestial");
            Func<double, object, double> f = delegate(double UT, object ign) { return ( transfer.GetRelativePositionAtUT(UT) - target.Orbit.GetRelativePositionAtUT(UT) ).magnitude - target.sphereOfInfluence;  };
            UT = 0;
            try { UT = BrentRoot.Solve(f, UT1, UT2, null); }
            catch (TimeoutException) { FlightPlanPlugin.Logger.LogError("SOI_intercept: Brents method threw a timeout Error (supressed)"); }
            catch (ArgumentException e) { FlightPlanPlugin.Logger.LogError($"SOI_intercept: Brents method threw an argument exception Error (supressed): {e.Message}"); }
        }
    }
}
