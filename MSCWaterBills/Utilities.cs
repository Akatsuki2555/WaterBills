using UnityEngine;

namespace MSCWaterBills
{
    public static class Utilities
    {
        public static T[] AddToArray<T>(this T[] array, T item) where T : class
        {
            var newArray = new T[array.Length + 1];
            for (var i = 0; i < array.Length; i++)
            {
                newArray[i] = array[i];
            }

            newArray[array.Length] = item;

            return newArray;
        }

        private static Transform C(this Transform t, int i)
        {
            return t.GetChild(i);
        }

        public static GameObject C(this GameObject go , int i)
        {
            return go.transform.C(i).gameObject;
        }
    }
}