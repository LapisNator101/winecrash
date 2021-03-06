﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winecrash.Engine
{
    public static class WMath
    {
        public const double DegToRad = Math.PI / 180D;
        public const double RadToDeg = 180D / Math.PI;

        public static T Clamp<T>(T value, T min, T max) where T : IComparable
        {
            return value.CompareTo(min) < 0 ? min : (value.CompareTo(max) > 0 ? max : value);
        }

        public static T Max<T>(T a, T b) where T : IComparable
        {
            return a.CompareTo(b) > 0 ? a : b;
        }

        public static T Min<T>(T a, T b) where T : IComparable
        {
            return a.CompareTo(b) < 0 ? a : b;
        }

        /// <summary>
        /// Get the index of a 1D array treated as 2D
        /// </summary>
        public static int Flatten2D(int x, int y, int width)
        {
            return x + y * width;
        }

        /// <summary>
        /// Get 2D index from 1D index and size 
        /// </summary>
        public static void FlatTo2D(int index, int width, out int x, out int y)
        {
            x = index % width;
            y = index / width;
        }


        #region CopySign
        public static Int64 CopySign(Int64 x, Int64 y)
        {
            //if x negative
            if (x < 0L)
            {
                //if y negative
                if (y < 0L)
                {
                    return x;
                }

                //else return positive x
                return x * -1L;
            }

            //if y negative
            if (y < 0L)
            {
                //return negative x
                return x * -1L;
            }

            //else return positive x
            return x;
        }
        public static Int32 CopySign(Int32 x, Int32 y)
        {
            //if x negative
            if (x < 0)
            {
                //if y negative
                if (y < 0)
                {
                    return x;
                }

                //else return positive x
                return x * -1;
            }

            //if y negative
            if (y < 0)
            {
                //return negative x
                return x * -1;
            }

            //else return positive x
            return x;
        }
        public static Int16 CopySign(Int16 x, Int16 y)
        {
            //if x negative
            if (x < 0)
            {
                //if y negative
                if (y < 0)
                {
                    return x;
                }

                //else return positive x
                return (short)(x * -1);
            }

            //if y negative
            if (y < 0)
            {
                //return negative x
                return (short)(x * -1);
            }

            //else return positive x
            return x;
        }
        public static decimal CopySign(decimal x, decimal y)
        {
            //if x negative
            if (x < 0.0M)
            {
                //if y negative
                if (y < 0.0M)
                {
                    return x;
                }

                //else return positive x
                return x * -1M;
            }

            //if y negative
            if (y < 0.0M)
            {
                //return negative x
                return x * -1M;
            }

            //else return positive x
            return x;
        }
        public static float CopySign(float x, float y)
        {
            if (Single.IsNaN(x)) return Single.NaN;
            //if x negative
            if (x < 0.0F)
            {
                //if y negative
                if (y < 0.0F)
                {
                    return x;
                }

                //else return positive x
                return x * -1.0F;
            }

            //if y negative
            if (y < 0.0F)
            {
                //return negative x
                return x * -1.0F;
            }

            //else return positive x
            return x;
        }
        public static double CopySign(double x, double y)
        {
            if (Double.IsNaN(x)) return Double.NaN;
            //if x negative
            if (x < 0.0D)
            {
                //if y negative
                if (y < 0.0D)
                {
                    return x;
                }

                //else return positive x
                return x * -1.0D;
            }

            //if y negative
            if (y < 0.0D)
            {
                //return negative x
                return x * -1.0D;
            }

            //else return positive x
            return x;
        }
        #endregion

        public static float Remap(float value, float oldLow, float oldHigh, float newLow, float newHigh)
        {
            return newLow + (value - oldLow) * (newHigh - newLow) / (oldHigh - oldLow);
        }

        public static double Remap(double value, double oldLow, double oldHigh, double newLow, double newHigh)
        {
            return newLow + (value - oldLow) * (newHigh - newLow) / (oldHigh - oldLow);
        }

        public static Vector2F Remap(Vector2F value, Vector2F oldLow, Vector2F oldHigh, Vector2F newLow, Vector2F newHigh)
        {
            return newLow + (value - oldLow) * (newHigh - newLow) / (oldHigh - oldLow);
        }

        public static Vector3F Remap(Vector3F value, Vector3F oldLow, Vector3F oldHigh, Vector3F newLow, Vector3F newHigh)
        {
            return newLow + (value - oldLow) * (newHigh - newLow) / (oldHigh - oldLow);
        }
    }
}
