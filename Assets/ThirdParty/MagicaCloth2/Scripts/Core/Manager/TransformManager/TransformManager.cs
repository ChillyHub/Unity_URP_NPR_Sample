// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace MagicaCloth2
{
    public class TransformManager : IManager, IValid
    {
        // フラグ
        internal const byte Flag_Read = 0x01;
        internal const byte Flag_WorldRotWrite = 0x02; // ワールド回転のみ書き込み
        internal const byte Flag_LocalPosRotWrite = 0x04; // ローカル座標・回転書き込み
        internal const byte Flag_Restore = 0x08; // 復元する
        internal ExNativeArray<ExBitFlag8> flagArray;

        /// <summary>
        /// 初期localPosition
        /// </summary>
        internal ExNativeArray<float3> initLocalPositionArray;

        /// <summary>
        /// 初期localRotation
        /// </summary>
        internal ExNativeArray<quaternion> initLocalRotationArray;

        /// <summary>
        /// ワールド座標
        /// </summary>
        internal ExNativeArray<float3> positionArray;

        /// <summary>
        /// ワールド回転
        /// </summary>
        internal ExNativeArray<quaternion> rotationArray;

        /// <summary>
        /// ワールド逆回転
        /// </summary>
        internal ExNativeArray<quaternion> inverseRotationArray;

        /// <summary>
        /// ワールドスケール
        /// Transform.lossyScaleと等価
        /// </summary>
        internal ExNativeArray<float3> scaleArray;

        /// <summary>
        /// ローカル座標
        /// </summary>
        internal ExNativeArray<float3> localPositionArray;

        /// <summary>
        /// ローカル回転
        /// </summary>
        internal ExNativeArray<quaternion> localRotationArray;

        /// <summary>
        /// 読み込み用トランスフォームアクセス配列
        /// この配列は上記の配列グループとインデックが同期している
        /// </summary>
        internal TransformAccessArray transformAccessArray;


        internal int Count => flagArray?.Count ?? 0;

        //=========================================================================================
        /// <summary>
        /// 書き込み用トランスフォームのデータ参照インデックス
        /// つまり上記配列へのインデックス
        /// </summary>
        //internal ExNativeArray<short> writeIndexArray;

        /// <summary>
        /// 書き込み用トランスフォームアクセス配列
        /// </summary>
        //internal TransformAccessArray writeTransformAccessArray;

        bool isValid;

        //=========================================================================================
        public void Dispose()
        {
            isValid = false;

            flagArray?.Dispose();
            initLocalPositionArray?.Dispose();
            initLocalRotationArray?.Dispose();
            positionArray?.Dispose();
            rotationArray?.Dispose();
            inverseRotationArray?.Dispose();
            scaleArray?.Dispose();
            localPositionArray?.Dispose();
            localRotationArray?.Dispose();
            //writeIndexArray?.Dispose();

            flagArray = null;
            initLocalPositionArray = null;
            initLocalRotationArray = null;
            positionArray = null;
            rotationArray = null;
            inverseRotationArray = null;
            scaleArray = null;
            localPositionArray = null;
            localRotationArray = null;
            //writeIndexArray = null;

            if (transformAccessArray.isCreated)
                transformAccessArray.Dispose();
            //if (writeTransformAccessArray.isCreated)
            //    writeTransformAccessArray.Dispose();
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            Dispose();

            const int capacity = 256;
            flagArray = new ExNativeArray<ExBitFlag8>(capacity);
            initLocalPositionArray = new ExNativeArray<float3>(capacity);
            initLocalRotationArray = new ExNativeArray<quaternion>(capacity);
            positionArray = new ExNativeArray<float3>(capacity);
            rotationArray = new ExNativeArray<quaternion>(capacity);
            inverseRotationArray = new ExNativeArray<quaternion>(capacity);
            scaleArray = new ExNativeArray<float3>(capacity);
            localPositionArray = new ExNativeArray<float3>(capacity);
            localRotationArray = new ExNativeArray<quaternion>(capacity);

            transformAccessArray = new TransformAccessArray(capacity);

            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        /// <summary>
        /// TransformDataを追加する
        /// </summary>
        /// <param name="tdata"></param>
        /// <returns></returns>
        internal DataChunk AddTransform(TransformData tdata)
        {
            if (isValid == false)
                return default;

            Debug.Assert(tdata != null);
            int cnt = tdata.Count;

            // データコピー追加
            var c = flagArray.AddRange(tdata.flagArray);
            initLocalPositionArray.AddRange(tdata.initLocalPositionArray);
            initLocalRotationArray.AddRange(tdata.initLocalRotationArray);

            // 領域のみ追加
            positionArray.AddRange(cnt);
            rotationArray.AddRange(cnt);
            inverseRotationArray.AddRange(cnt);
            scaleArray.AddRange(cnt);
            localPositionArray.AddRange(cnt);
            localRotationArray.AddRange(cnt);

            // トランスフォーム
            int nowcnt = transformAccessArray.length;

            // データチャンクの開始まで埋める
            int start = c.startIndex;
            while (nowcnt < start)
            {
                transformAccessArray.Add(null);
                nowcnt++;
            }

            for (int i = 0; i < cnt; i++)
            {
                var t = tdata.transformList[i];
                int index = c.startIndex + i;
                if (index < nowcnt)
                    transformAccessArray[index] = t;
                else
                    transformAccessArray.Add(t);
            }

            return c;
        }

        /// <summary>
        /// Transformの領域のみ追加する
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        internal DataChunk AddTransform(int count)
        {
            if (isValid == false)
                return default;

            // 領域のみ追加する
            var c = flagArray.AddRange(count);
            initLocalPositionArray.AddRange(count);
            initLocalRotationArray.AddRange(count);
            positionArray.AddRange(count);
            rotationArray.AddRange(count);
            inverseRotationArray.AddRange(count);
            scaleArray.AddRange(count);
            localPositionArray.AddRange(count);
            localRotationArray.AddRange(count);

            // トランスフォームはすべてnullで登録する
            int nowcnt = transformAccessArray.length;

            // データチャンクの開始まで埋める
            int start = c.startIndex;
            while (nowcnt < start)
            {
                transformAccessArray.Add(null);
                nowcnt++;
            }

            for (int i = 0; i < count; i++)
            {
                Transform t = null;
                int index = c.startIndex + i;
                if (index < nowcnt)
                    transformAccessArray[index] = t;
                else
                    transformAccessArray.Add(t);
            }

            return c;
        }

        /// <summary>
        /// Transform１つを追加する
        /// </summary>
        /// <param name="t"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        internal DataChunk AddTransform(Transform t, ExBitFlag8 flag)
        {
            if (isValid == false)
                return default;

            // データコピー追加
            var c = flagArray.Add(flag);
            initLocalPositionArray.Add(t.localPosition);
            initLocalRotationArray.Add(t.localRotation);
            positionArray.Add(t.position);
            rotationArray.Add(t.rotation);
            inverseRotationArray.Add(math.inverse(t.rotation));
            scaleArray.Add(t.lossyScale);
            localPositionArray.Add(t.localPosition);
            localRotationArray.Add(t.localRotation);

            // トランスフォーム
            int nowcnt = transformAccessArray.length;
            int index = c.startIndex;
            if (index < nowcnt)
                transformAccessArray[index] = t;
            else
                transformAccessArray.Add(t);

            return c;
        }

        /// <summary>
        /// Transform情報を書き換える
        /// </summary>
        /// <param name="t"></param>
        /// <param name="flag"></param>
        /// <param name="index"></param>
        internal void SetTransform(Transform t, ExBitFlag8 flag, int index)
        {
            if (isValid == false)
                return;

            if (t != null)
            {
                // データ設定
                flagArray[index] = flag;
                initLocalPositionArray[index] = t.localPosition;
                initLocalRotationArray[index] = t.localRotation;
                positionArray[index] = t.position;
                rotationArray[index] = t.rotation;
                inverseRotationArray[index] = math.inverse(t.rotation);
                scaleArray[index] = t.lossyScale;
                localPositionArray[index] = t.localPosition;
                localRotationArray[index] = t.localRotation;
                transformAccessArray[index] = t;
            }
            else
            {
                // データクリア（無効化）
                flagArray[index] = default;
                transformAccessArray[index] = null;
            }
        }

        /// <summary>
        /// Transform情報をコピーする
        /// </summary>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        internal void CopyTransform(int fromIndex, int toIndex)
        {
            if (isValid == false)
                return;

            flagArray[toIndex] = flagArray[fromIndex];
            initLocalPositionArray[toIndex] = initLocalPositionArray[fromIndex];
            initLocalRotationArray[toIndex] = initLocalRotationArray[fromIndex];
            positionArray[toIndex] = positionArray[fromIndex];
            rotationArray[toIndex] = rotationArray[fromIndex];
            inverseRotationArray[toIndex] = inverseRotationArray[fromIndex];
            scaleArray[toIndex] = scaleArray[fromIndex];
            localPositionArray[toIndex] = localPositionArray[fromIndex];
            localRotationArray[toIndex] = localRotationArray[fromIndex];
            transformAccessArray[toIndex] = transformAccessArray[fromIndex];
        }

        /// <summary>
        /// トランスフォームを削除する
        /// </summary>
        /// <param name="c"></param>
        internal void RemoveTransform(DataChunk c)
        {
            if (isValid == false)
                return;
            if (c.IsValid == false)
                return;

            flagArray.RemoveAndFill(c);
            initLocalPositionArray.Remove(c);
            initLocalRotationArray.Remove(c);
            positionArray.Remove(c);
            rotationArray.Remove(c);
            inverseRotationArray.Remove(c);
            scaleArray.Remove(c);
            localPositionArray.Remove(c);
            localRotationArray.Remove(c);

            // トランスフォーム削除
            for (int i = 0; i < c.dataLength; i++)
            {
                int index = c.startIndex + i;
                transformAccessArray[index] = null;
            }
        }

        //=========================================================================================
        /// <summary>
        /// トランスフォームを読み込むジョブを発行する
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle ReadTransform(JobHandle jobHandle)
        {
            if (Count > 0)
            {
                // todo:未来予測などがあると色々複雑化するところ

                var job = new ReadTransformJob()
                {
                    flagList = flagArray.GetNativeArray(),
                    positionArray = positionArray.GetNativeArray(),
                    rotationArray = rotationArray.GetNativeArray(),
                    scaleList = scaleArray.GetNativeArray(),
                    localPositionArray = localPositionArray.GetNativeArray(),
                    localRotationArray = localRotationArray.GetNativeArray(),
                    inverseRotationArray = inverseRotationArray.GetNativeArray(),
                };
                jobHandle = job.ScheduleReadOnly(transformAccessArray, 8, jobHandle);
            }

            return jobHandle;
        }

        [BurstCompile]
        struct ReadTransformJob : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;

            [Unity.Collections.WriteOnly]
            public NativeArray<float3> positionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> rotationArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> scaleList;
            [Unity.Collections.WriteOnly]
            public NativeArray<float3> localPositionArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> localRotationArray;
            [Unity.Collections.WriteOnly]
            public NativeArray<quaternion> inverseRotationArray;

            public void Execute(int index, TransformAccess transform)
            {
                if (transform.isValid == false)
                    return;
                var flag = flagList[index];
                if (flag.IsSet(Flag_Read) == false)
                    return;

                var pos = transform.position;
                var rot = transform.rotation;
                float4x4 LtoW = transform.localToWorldMatrix;

                positionArray[index] = pos;
                rotationArray[index] = rot;
                localPositionArray[index] = transform.localPosition;
                localRotationArray[index] = transform.localRotation;

                // マトリックスから正確なスケール値を算出する（これはTransform.lossyScaleと等価）
                var irot = math.inverse(rot);
                var m2 = math.mul(new float4x4(irot, float3.zero), LtoW);
                var scl = new float3(m2.c0.x, m2.c1.y, m2.c2.z);
                scaleList[index] = scl;

                // ワールド->ローカル変換用の逆クォータニオン
                inverseRotationArray[index] = math.inverse(rot);
            }
        }

        //=========================================================================================
        /// <summary>
        /// Transformを初期姿勢で復元させるジョブを発行する（メインスレッドのみ）
        /// </summary>
        /// <param name="count"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle RestoreTransform(JobHandle jobHandle)
        {
            if (Count > 0)
            {
                var job = new RestoreTransformJob()
                {
                    flagList = flagArray.GetNativeArray(),
                    localPositionArray = initLocalPositionArray.GetNativeArray(),
                    localRotationArray = initLocalRotationArray.GetNativeArray(),
                };
                jobHandle = job.Schedule(transformAccessArray, jobHandle);
            }

            return jobHandle;
        }

        [BurstCompile]
        struct RestoreTransformJob : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> localRotationArray;

            public void Execute(int index, TransformAccess transform)
            {
                if (transform.isValid == false)
                    return;
                var flag = flagList[index];
                if (flag.IsSet(Flag_Restore) == false)
                    return;

                transform.localPosition = localPositionArray[index];
                transform.localRotation = localRotationArray[index];
            }
        }

        //=========================================================================================
        /// <summary>
        /// Transformを書き込むジョブを発行する（メインスレッドのみ）
        /// </summary>
        /// <param name="count"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        public JobHandle WriteTransform(JobHandle jobHandle)
        {
            var job = new WriteTransformJob()
            {
                flagList = flagArray.GetNativeArray(),
                worldPositions = positionArray.GetNativeArray(),
                worldRotations = rotationArray.GetNativeArray(),
                localPositions = localPositionArray.GetNativeArray(),
                localRotations = localRotationArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(transformAccessArray, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct WriteTransformJob : IJobParallelForTransform
        {
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> flagList;

            [Unity.Collections.ReadOnly]
            public NativeArray<float3> worldPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> worldRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> localRotations;

            public void Execute(int index, TransformAccess transform)
            {
                if (transform.isValid == false)
                    return;
                var flag = flagList[index];
                if (flag.IsSet(Flag_WorldRotWrite))
                {
                    // ワールド回転のみ書き込む
                    transform.rotation = worldRotations[index];
                }
                else if (flag.IsSet(Flag_LocalPosRotWrite))
                {
                    // ローカル座標・回転を書き込む
                    transform.localPosition = localPositions[index];
                    transform.localRotation = localRotations[index];
                }
            }
        }

        //=========================================================================================
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Transform Manager. :{Count}");

            int tcnt = transformAccessArray.length;
            for (int i = 0; i < tcnt; i++)
            {
                var t = transformAccessArray[i];
                var flag = flagArray[i];
                sb.AppendLine($"  [{i}] (0x{flag.Value:X}) {t?.name ?? "(null)"}");
            }


            return sb.ToString();
        }
    }
}
