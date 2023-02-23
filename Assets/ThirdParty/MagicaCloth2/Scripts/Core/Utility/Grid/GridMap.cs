// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// グリッドマップとユーティリティ関数群
    /// Jobで利用するために最低限の管理データのみ
    /// そのためGridSizeなどのデータはこのクラスでは保持しない
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GridMap<T> : IDisposable where T : unmanaged, IEquatable<T>
    {
#if MC2_COLLECTIONS_200
        private NativeMultiHashMap<int3, T> gridMap;
#else
        private NativeParallelMultiHashMap<int3, T> gridMap;
#endif

        //=========================================================================================
        public GridMap(int capacity = 0)
        {
#if MC2_COLLECTIONS_200
            gridMap = new NativeMultiHashMap<int3, T>(capacity, Allocator.Persistent);
#else
            gridMap = new NativeParallelMultiHashMap<int3, T>(capacity, Allocator.Persistent);
#endif
        }

        public void Dispose()
        {
            if (gridMap.IsCreated)
                gridMap.Dispose();
        }

#if MC2_COLLECTIONS_200
        public NativeMultiHashMap<int3, T> GetMultiHashMap() => gridMap;
#else
        public NativeParallelMultiHashMap<int3, T> GetMultiHashMap() => gridMap;
#endif

        public int DataCount => gridMap.Count();

        //=========================================================================================
        /// <summary>
        /// グリッド範囲を走査するEnumeratorを返す
        /// </summary>
        /// <param name="startGrid"></param>
        /// <param name="endGrid"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static GridEnumerator GetArea(int3 startGrid, int3 endGrid, NativeMultiHashMap<int3, T> gridMap)
#else
        public static GridEnumerator GetArea(int3 startGrid, int3 endGrid, NativeParallelMultiHashMap<int3, T> gridMap)
#endif
        {
            return new GridEnumerator
            {
                gridMap = gridMap,
                startGrid = math.min(startGrid, endGrid),
                endGrid = math.max(startGrid, endGrid),
                currentGrid = math.min(startGrid, endGrid),
                isFirst = true,
            };
        }

        /// <summary>
        /// 球範囲を走査するEnumeratorを返す
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
#if MC2_COLLECTIONS_200
        public static GridEnumerator GetArea(float3 pos, float radius, NativeMultiHashMap<int3, T> gridMap, float gridSize)
#else
        public static GridEnumerator GetArea(float3 pos, float radius, NativeParallelMultiHashMap<int3, T> gridMap, float gridSize)
#endif
        {
            // 検索グリッド範囲
            int3 minGrid = GetGrid(pos - radius, gridSize);
            int3 maxGrid = GetGrid(pos + radius, gridSize);

            return GetArea(minGrid, maxGrid, gridMap);
        }

        /// <summary>
        /// グリッド走査用Enumerator
        /// </summary>
        public struct GridEnumerator : IEnumerator<int3>
        {
#if MC2_COLLECTIONS_200
            internal NativeMultiHashMap<int3, T> gridMap;
#else
            internal NativeParallelMultiHashMap<int3, T> gridMap;
#endif
            internal int3 startGrid;
            internal int3 endGrid;
            internal int3 currentGrid;
            internal bool isFirst;

            public int3 Current => currentGrid;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                // データが存在しなくとも走査する
                if (isFirst)
                {
                    isFirst = false;
                    return true;
                }
                currentGrid.x++;
                if (currentGrid.x > endGrid.x)
                {
                    currentGrid.x = startGrid.x;
                    currentGrid.y++;
                    if (currentGrid.y > endGrid.y)
                    {
                        currentGrid.y = startGrid.y;
                        currentGrid.z++;
                        if (currentGrid.z > endGrid.z)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            public void Reset()
            {
                currentGrid = startGrid;
                isFirst = true;
            }

            public GridEnumerator GetEnumerator() { return this; }
        }


        //=========================================================================================
        /// <summary>
        /// 座標から３次元グリッド座標を割り出す
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="gridSize"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 GetGrid(float3 pos, float gridSize)
        {
            return new int3(math.floor(pos / gridSize));
        }

        /// <summary>
        /// グリッドマップにデータを追加する
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="data"></param>
        /// <param name="gridMap"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if MC2_COLLECTIONS_200
        public static void AddGrid(int3 grid, T data, NativeMultiHashMap<int3, T> gridMap)
#else
        public static void AddGrid(int3 grid, T data, NativeParallelMultiHashMap<int3, T> gridMap)
#endif
        {
            gridMap.Add(grid, data);
        }

        /// <summary>
        /// 座標からグリッドマップにデータを追加する
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="data"></param>
        /// <param name="gridMap"></param>
        /// <param name="gridSize"></param>
        /// <param name="aabbRef"></param>
        /// <returns>追加されたグリッドを返す</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if MC2_COLLECTIONS_200
        public static int3 AddGrid(float3 pos, T data, NativeMultiHashMap<int3, T> gridMap, float gridSize)
#else
        public static int3 AddGrid(float3 pos, T data, NativeParallelMultiHashMap<int3, T> gridMap, float gridSize)
#endif
        {
            int3 grid = GetGrid(pos, gridSize);
            gridMap.Add(grid, data);
            return grid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if MC2_COLLECTIONS_200
        public static int3 AddGrid(float3 pos, T data, NativeMultiHashMap<int3, T>.ParallelWriter gridMap, float gridSize)
#else
        public static int3 AddGrid(float3 pos, T data, NativeParallelMultiHashMap<int3, T>.ParallelWriter gridMap, float gridSize)
#endif
        {
            int3 grid = GetGrid(pos, gridSize);
            gridMap.Add(grid, data);
            return grid;
        }

        /// <summary>
        /// グリッドマップからデータを削除する
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="data"></param>
        /// <param name="gridMap"></param>
        /// <returns>削除に成功した場合はtrue</returns>
#if MC2_COLLECTIONS_200
        public static bool RemoveGrid(int3 grid, T data, NativeMultiHashMap<int3, T> gridMap)
#else
        public static bool RemoveGrid(int3 grid, T data, NativeParallelMultiHashMap<int3, T> gridMap)
#endif
        {
            if (gridMap.ContainsKey(grid))
            {
#if MC2_COLLECTIONS_200
                NativeMultiHashMapIterator<int3> it;
#else
                NativeParallelMultiHashMapIterator<int3> it;
#endif
                T item;
                if (gridMap.TryGetFirstValue(grid, out item, out it))
                {
                    do
                    {
                        if (item.Equals(data))
                        {
                            gridMap.Remove(it);
                            return true;
                        }
                    }
                    while (gridMap.TryGetNextValue(out item, ref it));
                }
            }

            return false;
        }

        /// <summary>
        /// グリッドマップからデータを移動させる
        /// </summary>
        /// <param name="fromGrid"></param>
        /// <param name="toGrid"></param>
        /// <param name="data"></param>
        /// <param name="gridMap"></param>
        /// <returns>データが移動された場合true, 移動の必要がない場合はfalse</returns>
#if MC2_COLLECTIONS_200
        public static bool MoveGrid(int3 fromGrid, int3 toGrid, T data, NativeMultiHashMap<int3, T> gridMap)
#else
        public static bool MoveGrid(int3 fromGrid, int3 toGrid, T data, NativeParallelMultiHashMap<int3, T> gridMap)
#endif
        {
            // 移動の必要がなければ終了
            if (fromGrid.Equals(toGrid))
                return false;

            // remove
            RemoveGrid(fromGrid, data, gridMap);

            // add
            AddGrid(toGrid, data, gridMap);

            return true;
        }
    }
}
