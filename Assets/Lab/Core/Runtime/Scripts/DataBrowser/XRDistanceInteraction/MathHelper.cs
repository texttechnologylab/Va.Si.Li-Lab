using UnityEngine;


namespace VaSiLi.Helper
{
    public class BezierCurve
    {

        private static float t;

        public static Vector3[] CalculateCurvePoints(Vector3 start, Vector3 middle, Vector3 end, Vector3[] result)
        {
            for (int i = 0; i < result.Length; i++)
            {
                t = (float)i / (result.Length - 1);
                result[i] = Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * middle + Mathf.Pow(t, 2) * end;
            }

            return result;
        }

        public static Vector3[] CalculateCurvePoints(Vector3 start, Vector3 end, Vector3[] result)
        {
            Vector3 middle = (start + end) / 2;
            middle.y += 0.1f;
            for (int i = 0; i < result.Length; i++)
            {
                t = (float)i / (result.Length - 1);
                result[i] = Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * middle + Mathf.Pow(t, 2) * end;
            }

            return result;
        }

        public static Vector3[] CalculateCurvePoints(Vector3 start, Vector3 end, int midpoints)
        {
            Vector3[] result = new Vector3[midpoints];
            Vector3 middle = (start + end) / 2;
            middle.y += 0.1f;
            for (int i = 0; i < result.Length; i++)
            {
                t = (float)i / (result.Length - 1);
                result[i] = Mathf.Pow(1 - t, 2) * start + 2 * (1 - t) * t * middle + Mathf.Pow(t, 2) * end;
            }

            return result;
        }
    }

}