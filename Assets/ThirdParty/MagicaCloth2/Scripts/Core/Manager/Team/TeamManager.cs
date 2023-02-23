// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public class TeamManager : IManager, IValid
    {
        /// <summary>
        /// チームフラグ(32bit)
        /// </summary>
        public const int Flag_Valid = 0; // データの有効性
        public const int Flag_Enable = 1; // 動作状態
        public const int Flag_Reset = 2; // 姿勢リセット
        public const int Flag_TimeReset = 3; // 時間リセット
        public const int Flag_Suspend = 4; // 一時停止
        public const int Flag_Running = 5; // 今回のフレームでシミュレーションが実行されたかどうか
        //public const int Flag_UseAnimatedPosture = 5; // Base姿勢はアニメーションされた姿勢から計算する
        public const int Flag_CustomSkinning = 6; // カスタムスキニングを使用
        public const int Flag_Synchronization = 7; // 同期中
        public const int Flag_StepRunning = 8; // ステップ実行中
        public const int Flag_NormalAdjustment = 9; // 法線調整
        public const int Flag_Exit = 10; // 存在消滅時

        // 以下セルフコリジョン
        // !これ以降の順番を変えないこと
        public const int Flag_Self_PointPrimitive = 13; // PointPrimitive+Sortを保持し更新する
        public const int Flag_Self_EdgePrimitive = 14; // EdgePrimitive+Sortを保持し更新する
        public const int Flag_Self_TrianglePrimitive = 15; // TrianglePrimitive+Sortを保持し更新する

        public const int Flag_Self_EdgeEdge = 16;
        public const int Flag_Sync_EdgeEdge = 17;
        public const int Flag_PSync_EdgeEdge = 18;

        public const int Flag_Self_PointTriangle = 19;
        public const int Flag_Sync_PointTriangle = 20;
        public const int Flag_PSync_PointTriangle = 21;

        public const int Flag_Self_TrianglePoint = 22;
        public const int Flag_Sync_TrianglePoint = 23;
        public const int Flag_PSync_TrianglePoint = 24;

        public const int Flag_Self_EdgeTriangleIntersect = 25;
        public const int Flag_Sync_EdgeTriangleIntersect = 26;
        public const int Flag_PSync_EdgeTriangleIntersect = 27;
        public const int Flag_Self_TriangleEdgeIntersect = 28;
        public const int Flag_Sync_TriangleEdgeIntersect = 29;
        public const int Flag_PSync_TriangleEdgeIntersect = 30;

        /// <summary>
        /// チーム基本データ
        /// </summary>
        public struct TeamData
        {
            /// <summary>
            /// フラグ
            /// </summary>
            public BitField32 flag;

            /// <summary>
            /// １秒間の更新頻度
            /// </summary>
            public int frequency;

            /// <summary>
            /// 更新計算用時間
            /// </summary>
            public float time;

            /// <summary>
            /// 前フレームの更新計算用時間
            /// </summary>
            public float oldTime;

            /// <summary>
            /// 現在のシミュレーション更新時間
            /// </summary>
            public float nowUpdateTime;

            /// <summary>
            /// １つ前の最後のシミュレーション更新時間
            /// </summary>
            public float oldUpdateTime;

            /// <summary>
            /// 更新がある場合のフレーム時間
            /// </summary>
            public float frameUpdateTime;

            /// <summary>
            /// 前回更新のフレーム時間
            /// </summary>
            public float frameOldTime;

            /// <summary>
            /// チーム固有のタイムスケール(0.0-1.0)
            /// </summary>
            public float timeScale;

            /// <summary>
            /// 今回のチーム更新回数（０ならばこのフレームは更新なし）
            /// </summary>
            public int updateCount;

            /// <summary>
            /// ステップごとのフレームに対するnowUpdateTime割合
            /// これは(frameStartTime ~ time)間でのnowUpdateTimeの割合
            /// </summary>
            public float frameInterpolation;

            /// <summary>
            /// 重力の影響力(0.0 ~ 1.0)
            /// 1.0は重力が100%影響する
            /// </summary>
            public float gravityRatio;

            public float gravityDot;

            /// <summary>
            /// センタートランスフォーム(ダイレクト値)
            /// </summary>
            public int centerTransformIndex;

            /// <summary>
            /// 現在の中心ワールド座標（この値はCenterData.nowWorldPositionのコピー）
            /// </summary>
            public float3 centerWorldPosition;

            /// <summary>
            /// チームスケール
            /// </summary>
            public float3 initScale;            // データ生成時のセンタートランスフォームスケール
            public float scaleRatio;            // 現在のスケール倍率
            //public float3 scaleDirection;     // フリップ用:スケール値方向(xyz)：(1/-1)のみ
            //public float4 quaternionScale;    // フリップ用:クォータニオン反転用

            /// <summary>
            /// 同期チームID(0=なし)
            /// </summary>
            public int syncTeamId;

            /// <summary>
            /// 自身を同期している親チームID(0=なし)：最大７つ
            /// </summary>
            public FixedList32Bytes<int> syncParentTeamId;

            /// <summary>
            /// 初期姿勢とアニメーション姿勢のブレンド率（制約で利用）
            /// </summary>
            public float animationPoseRatio;

            //-----------------------------------------------------------------
            /// <summary>
            /// ProxyMeshのタイプ
            /// </summary>
            public VirtualMesh.MeshType proxyMeshType;

            /// <summary>
            /// ProxyMeshのTransformデータ
            /// </summary>
            public DataChunk proxyTransformChunk;

            /// <summary>
            /// ProxyMeshの共通部分
            /// -attributes
            /// -vertexToTriangles
            /// -vertexToVertexIndexArray
            /// -vertexDepths
            /// -vertexLocalPositions
            /// -vertexLocalRotations
            /// -vertexRootIndices
            /// -vertexParentIndices
            /// -vertexChildIndexArray
            /// -vertexAngleCalcLocalRotations
            /// -uv
            /// -positions
            /// -rotations
            /// -vertexBindPosePositions
            /// -vertexBindPoseRotations
            /// -normalAdjustmentRotations
            /// </summary>
            public DataChunk proxyCommonChunk;

            /// <summary>
            /// ProxyMeshの頂点接続頂点データ
            /// -vertexToVertexDataArray (-vertexToVertexIndexArrayと対)
            /// </summary>
            //public DataChunk proxyVertexToVertexDataChunk;

            /// <summary>
            /// ProxyMeshの子頂点データ
            /// -vertexChildDataArray (-vertexChildIndexArrayと対)
            /// </summary>
            public DataChunk proxyVertexChildDataChunk;

            /// <summary>
            /// ProxyMeshのTriangle部分
            /// -triangles
            /// -triangleTeamIdArray
            /// -triangleNormals
            /// -triangleTangents
            /// </summary>
            public DataChunk proxyTriangleChunk;

            /// <summary>
            /// ProxyMeshのEdge部分
            /// -edges
            /// -edgeTeamIdArray
            /// </summary>
            public DataChunk proxyEdgeChunk;

            /// <summary>
            /// ProxyMeshのBoneCloth/MeshCloth共通部分
            /// -localPositions
            /// -localNormals
            /// -localTangents
            /// -boneWeights
            /// </summary>
            public DataChunk proxyMeshChunk;

            /// <summary>
            /// ProxyMeshのBoneCloth固有部分
            /// -vertexToTransformRotations
            /// </summary>
            public DataChunk proxyBoneChunk;

            /// <summary>
            /// ProxyMeshのMeshClothのスキニングボーン部分
            /// -skinBoneTransformIndices
            /// -skinBoneBindPoses
            /// </summary>
            public DataChunk proxySkinBoneChunk;

            /// <summary>
            /// ProxyMeshのベースライン部分
            /// -baseLineFlags
            /// -baseLineStartDataIndices
            /// -baseLineDataCounts
            /// </summary>
            public DataChunk baseLineChunk;

            /// <summary>
            /// ProxyMeshのベースラインデータ配列
            /// -baseLineData
            /// </summary>
            public DataChunk baseLineDataChunk;

            /// <summary>
            /// 固定点リスト
            /// </summary>
            public DataChunk fixedDataChunk;

            //-----------------------------------------------------------------
            /// <summary>
            /// 接続しているマッピングメッシュへデータへのインデックスセット(最大15まで)
            /// </summary>
            public FixedList32Bytes<short> mappingDataIndexSet;

            //-----------------------------------------------------------------
            /// <summary>
            /// パーティクルデータ
            /// </summary>
            public DataChunk particleChunk;

            /// <summary>
            /// コライダーデータ
            /// コライダーが有効の場合は未使用であっても最大数まで確保される
            /// </summary>
            public DataChunk colliderChunk;

            /// <summary>
            /// コライダートランスフォーム
            /// コライダーが有効の場合は未使用であっても最大数まで確保される
            /// </summary>
            public DataChunk colliderTransformChunk;

            /// <summary>
            /// 現在有効なコライダー数
            /// </summary>
            public int colliderCount;

            //-----------------------------------------------------------------
            /// <summary>
            /// 距離制約
            /// </summary>
            public DataChunk distanceStartChunk;
            public DataChunk distanceDataChunk;

            /// <summary>
            /// 曲げ制約
            /// </summary>
            public DataChunk bendingPairChunk;
            //public DataChunk bendingDataChunk;
            public DataChunk bendingWriteIndexChunk;
            public DataChunk bendingBufferChunk;

            /// <summary>
            /// セルフコリジョン制約
            /// </summary>
            //public int selfQueueIndex;
            public DataChunk selfPointChunk;
            public DataChunk selfEdgeChunk;
            public DataChunk selfTriangleChunk;

            //-----------------------------------------------------------------
            /// <summary>
            /// １回の更新間隔
            /// </summary>
            public float SimulationDeltaTime => 1.0f / frequency;

            /// <summary>
            /// データの有効性
            /// </summary>
            public bool IsValid => flag.IsSet(Flag_Valid);

            /// <summary>
            /// 有効状態
            /// </summary>
            public bool IsEnable => flag.IsSet(Flag_Enable);

            /// <summary>
            /// 姿勢リセット有無
            /// </summary>
            public bool IsReset => flag.IsSet(Flag_Reset);

            /// <summary>
            /// 今回のフレームでシミュレーションが実行されたかどうか（１回以上実行された場合）
            /// </summary>
            public bool IsRunning => flag.IsSet(Flag_Running);

            /// <summary>
            /// ステップ実行中かどうか
            /// </summary>
            public bool IsStepRunning => flag.IsSet(Flag_StepRunning);

            //public bool IsAnimatedPosture => flag.IsSet(Flag_UseAnimatedPosture);

            public int ParticleCount => particleChunk.dataLength;

            /// <summary>
            /// 現在有効なコライダー数
            /// </summary>
            public int ColliderCount => colliderCount;

            public int BaseLineCount => baseLineChunk.dataLength;

            public int TriangleCount => proxyTriangleChunk.dataLength;

            public int EdgeCount => proxyEdgeChunk.dataLength;

            public int MappingCount => mappingDataIndexSet.Length;

            /// <summary>
            /// 初期スケール（ｘ軸のみで判定、均等スケールしか認めていない）
            /// </summary>
            public float InitScale => initScale.x;
        }
        public ExNativeArray<TeamData> teamDataArray;

        /// <summary>
        /// 登録されているチーム数
        /// </summary>
        public int TeamCount => teamDataArray?.Count ?? 0;

        /// <summary>
        /// マッピングメッシュデータ
        /// </summary>
        public struct MappingData : IValid
        {
            public int teamId;

            /// <summary>
            /// Mappingメッシュのセンタートランスフォーム（ダイレクト値）
            /// </summary>
            public int centerTransformIndex;

            /// <summary>
            /// Mappingメッシュの基本
            /// -attributes
            /// -localPositions
            /// -localNormlas
            /// -localTangents
            /// -boneWeights
            /// -positions
            /// -rotations
            /// </summary>
            public DataChunk mappingCommonChunk;

            /// <summary>
            /// 初期状態でのプロキシメッシュへの変換マトリックスと変換回転
            /// この姿勢は初期化時に固定される
            /// </summary>
            public float4x4 toProxyMatrix;
            public quaternion toProxyRotation;

            /// <summary>
            /// プロキシメッシュとマッピングメッシュの座標空間が同じかどうか
            /// </summary>
            public bool sameSpace;

            /// <summary>
            /// プロキシメッシュからマッピングメッシュへの座標空間変換用
            /// ▲ワールド対応：ここはワールド空間からマッピングメッシュへの座標変換となる
            /// </summary>
            public float4x4 toMappingMatrix;
            public quaternion toMappingRotation;

            public bool IsValid()
            {
                return teamId > 0;
            }

            public int VertexCount => mappingCommonChunk.dataLength;
        }
        public ExNativeArray<MappingData> mappingDataArray;

        /// <summary>
        /// チーム全体の最大更新回数
        /// </summary>
        public NativeReference<int> maxUpdateCount;

        /// <summary>
        /// パラメータ（teamDataArrayとインデックス連動）
        /// </summary>
        public ExNativeArray<ClothParameters> parameterArray;

        /// <summary>
        /// センタートランスフォームデータ
        /// </summary>
        public ExNativeArray<InertiaConstraint.CenterData> centerDataArray;

        /// <summary>
        /// 登録されているマッピングメッシュ数
        /// </summary>
        public int MappingCount => mappingDataArray?.Count ?? 0;

        /// <summary>
        /// チームの有効状態を別途記録
        /// NativeArrayはジョブ実行中にアクセスできないため。
        /// </summary>
        HashSet<int> enableTeamSet = new HashSet<int>();

        /// <summary>
        /// チームIDとClothProcessクラスの関連辞書
        /// </summary>
        Dictionary<int, ClothProcess> clothProcessDict = new Dictionary<int, ClothProcess>();

        /// <summary>
        /// グローバルタイムスケール(0.0 ~ 1.0)
        /// </summary>
        internal float globalTimeScale = 1.0f;

        bool isValid;

        //=========================================================================================
        /// <summary>
        /// エッジコライダーコリジョンのエッジ数合計
        /// </summary>
        internal int edgeColliderCollisionCount;

        //=========================================================================================
        public void Dispose()
        {
            isValid = false;

            teamDataArray?.Dispose();
            mappingDataArray?.Dispose();
            parameterArray?.Dispose();
            centerDataArray?.Dispose();

            teamDataArray = null;
            mappingDataArray = null;
            parameterArray = null;
            centerDataArray = null;

            if (maxUpdateCount.IsCreated)
                maxUpdateCount.Dispose();

            enableTeamSet.Clear();
            clothProcessDict.Clear();
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            Dispose();

            const int capacity = 32;
            teamDataArray = new ExNativeArray<TeamData>(capacity);
            mappingDataArray = new ExNativeArray<MappingData>(capacity);
            parameterArray = new ExNativeArray<ClothParameters>(capacity);
            centerDataArray = new ExNativeArray<InertiaConstraint.CenterData>(capacity);

            // グローバルチーム[0]を追加する
            var gteam = new TeamData();
            teamDataArray.Add(gteam);
            parameterArray.Add(new ClothParameters());
            centerDataArray.Add(new InertiaConstraint.CenterData());

            maxUpdateCount = new NativeReference<int>(Allocator.Persistent);

            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        /// <summary>
        /// チームを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        /// <param name="clothParams"></param>
        /// <returns></returns>
        internal int AddTeam(ClothProcess cprocess, ClothParameters clothParams)
        {
            if (isValid == false)
                return 0;

            var team = new TeamData();
            team.flag.SetBits(Flag_Valid, true);
            team.flag.SetBits(Flag_Reset, true);
            team.flag.SetBits(Flag_TimeReset, true);
            team.flag.SetBits(Flag_CustomSkinning, cprocess.cloth.SerializeData.customSkinningSetting.enable);
            team.flag.SetBits(Flag_NormalAdjustment, cprocess.cloth.SerializeData.normalAlignmentSetting.alignmentMode != NormalAlignmentSettings.AlignmentMode.None);
            // Enableフラグは立てない
            team.frequency = clothParams.solverFrequency;
            team.timeScale = 1.0f;
            team.initScale = cprocess.clothTransformRecord.scale; // 初期スケール
            team.scaleRatio = 1.0f;
            team.centerWorldPosition = cprocess.clothTransformRecord.position;
            team.animationPoseRatio = cprocess.cloth.SerializeData.animationPoseRatio;
            var c = teamDataArray.Add(team);
            int teamId = c.startIndex;

            // パラメータ
            parameterArray.Add(clothParams);

            // 慣性制約（ここでは領域のみ確保する）
            centerDataArray.Add(default);

            clothProcessDict.Add(teamId, cprocess);

            return teamId;
        }

        /// <summary>
        /// チームを解除する
        /// </summary>
        /// <param name="teamId"></param>
        internal void RemoveTeam(int teamId)
        {
            if (isValid == false || teamId == 0)
                return;

            // セルフコリジョン同期解除
            var tdata = GetTeamData(teamId);
            if (tdata.syncTeamId > 0 && ContainsTeamData(tdata.syncTeamId))
            {
                var stdata = GetTeamData(tdata.syncTeamId);
                RemoveSyncParent(ref stdata, teamId);
                SetTeamData(tdata.syncTeamId, stdata);
            }

            // 制約データなど解除

            // チームデータを破棄する
            var c = new DataChunk(teamId, 1);
            teamDataArray.RemoveAndFill(c);
            parameterArray.Remove(c);
            centerDataArray.Remove(c);

            clothProcessDict.Remove(teamId);
        }

        /// <summary>
        /// チームの有効化設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="sw"></param>
        public void SetEnable(int teamId, bool sw)
        {
            if (isValid == false || teamId == 0)
                return;
            var team = teamDataArray[teamId];
            team.flag.SetBits(Flag_Enable, sw);
            teamDataArray[teamId] = team;

            if (sw)
                enableTeamSet.Add(teamId);
            else
                enableTeamSet.Remove(teamId);
        }

        public bool IsEnable(int teamId)
        {
            return enableTeamSet.Contains(teamId);
        }

        public bool ContainsTeamData(int teamId)
        {
            //return teamId >= 0 && teamId < TeamCount;
            return teamId >= 0 && clothProcessDict.ContainsKey(teamId);
        }

        public TeamData GetTeamData(int teamId)
        {
            if (isValid == false || ContainsTeamData(teamId) == false)
                return default;
            return teamDataArray[teamId];
        }

        public void SetTeamData(int teamId, TeamData tdata)
        {
            if (isValid == false)
                return;
            teamDataArray[teamId] = tdata;
        }

        public ClothParameters GetParameters(int teamId)
        {
            if (isValid == false)
                return default;
            return parameterArray[teamId];
        }

        public void SetParameters(int teamId, ClothParameters parameters)
        {
            if (isValid == false)
                return;
            parameterArray[teamId] = parameters;
        }

        public ClothProcess GetClothProcess(int teamId)
        {
            if (clothProcessDict.ContainsKey(teamId))
                return clothProcessDict[teamId];
            else
                return null;
        }

        //=========================================================================================
        /// <summary>
        /// 毎フレーム常に実行するチーム更新
        /// - 時間の更新と実行回数の算出
        /// </summary>
        internal void AlwaysTeamUpdate()
        {
            // フレーム更新時間
            // todo:FixedUpdate対応
            float dtime = Time.deltaTime;
            //Debug.Log($"dtime:{dtime}");

            // 集計
            edgeColliderCollisionCount = 0;

            // ジョブでは実行できないチーム更新
            var clothSet = MagicaManager.Cloth.clothSet;
            foreach (var cprocess in clothSet)
            {
                int teamId = cprocess.TeamId;
                var tdata = GetTeamData(teamId);
                var cloth = cprocess.cloth;

                bool selfCollisionUpdate = false;

                // パラメータ変更反映
                if (cprocess.IsState(ClothProcess.State_ParameterDirty) && cprocess.IsEnable)
                {
                    // コライダー更新(内部でteamData更新)
                    MagicaManager.Collider.UpdateColliders(cprocess);
                    tdata = GetTeamData(teamId); // 再取得の必要あり

                    // パラメータ変更
                    cprocess.SyncParameters();
                    SetParameters(teamId, cprocess.parameters);
                    tdata.animationPoseRatio = cloth.SerializeData.animationPoseRatio;

                    // セルフコリジョン更新
                    selfCollisionUpdate = true;

                    cprocess.SetState(ClothProcess.State_ParameterDirty, false);
                }

                // チーム同期
                int oldSyncTeamId = tdata.syncTeamId;
                var syncCloth = cloth.SyncCloth;
                if (syncCloth)
                {
                    // デッドロック対策
                    var c = syncCloth;
                    while (c)
                    {
                        if (c == cloth)
                        {
                            syncCloth = null;
                            c = null;
                        }
                        else
                            c = c.SyncCloth;
                    }
                }
                tdata.syncTeamId = syncCloth?.Process.TeamId ?? 0;
                tdata.flag.SetBits(Flag_Synchronization, tdata.syncTeamId != 0);
                if (oldSyncTeamId != tdata.syncTeamId)
                {
                    // 変更あり！

                    // 同期解除
                    if (oldSyncTeamId > 0)
                    {
                        var syncTeamData = GetTeamData(oldSyncTeamId);
                        RemoveSyncParent(ref syncTeamData, teamId);
                        SetTeamData(oldSyncTeamId, syncTeamData);
                    }

                    // 同期変更
                    if (syncCloth != null)
                    {
                        var syncTeamData = GetTeamData(syncCloth.Process.TeamId);

                        // 相手に自身を登録
                        AddSyncParent(ref syncTeamData, teamId);
                        SetTeamData(syncCloth.Process.TeamId, syncTeamData);

                        // 時間リセットフラグクリア
                        tdata.flag.SetBits(Flag_TimeReset, false);

                        Develop.DebugLog($"同期! {teamId}->{syncCloth.Process.TeamId}");
                    }
                    else
                    {
                        // 同期解除
                        cloth.SerializeData.selfCollisionConstraint.syncPartner = null;
                        tdata.frequency = cprocess.parameters.solverFrequency;
                    }

                    // セルフコリジョン更新
                    selfCollisionUpdate = true;
                }

                // 時間の同期
                if (syncCloth && tdata.syncTeamId > 0)
                {
                    var syncTeamData = GetTeamData(syncCloth.Process.TeamId);
                    if (syncTeamData.IsValid)
                    {
                        // 時間同期
                        tdata.frequency = syncTeamData.frequency;
                        tdata.time = syncTeamData.time;
                        tdata.oldTime = syncTeamData.oldTime;
                        tdata.nowUpdateTime = syncTeamData.nowUpdateTime;
                        tdata.oldUpdateTime = syncTeamData.oldUpdateTime;
                        tdata.frameUpdateTime = syncTeamData.frameUpdateTime;
                        tdata.frameOldTime = syncTeamData.frameOldTime;
                        tdata.timeScale = syncTeamData.timeScale;
                        tdata.updateCount = syncTeamData.updateCount;
                        tdata.frameInterpolation = syncTeamData.frameInterpolation;
                        //Develop.DebugLog($"Team time sync:{teamId}->{syncCloth.Process.TeamId}");
                    }
                }

                // 一時停止判定
                bool suspend = false;
                if (cprocess.GetSuspendCounter() > 0)
                    suspend = true; // 一時停止カウンターが１以上
                tdata.flag.SetBits(Flag_Suspend, suspend);

                // 集計まわり
                var param = GetParameters(teamId);
                if (param.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Edge)
                    edgeColliderCollisionCount += tdata.EdgeCount;

                SetTeamData(teamId, tdata);

                // セルフコリジョンのフラグやバッファ更新
                if (selfCollisionUpdate)
                {
                    Develop.DebugLog("セルフコリジョン更新");
                    MagicaManager.Simulation.selfCollisionConstraint.UpdateTeam(teamId);
                }
            }

            // この実行はJobSystemでは行わない
            var job = new AlwaysTeamUpdateJob()
            {
                teamCount = TeamCount,
                frameDeltaTime = dtime,
                globalTimeScale = globalTimeScale,

                maxUpdateCount = maxUpdateCount,
                teamDataArray = teamDataArray.GetNativeArray(),
            };
            job.Run();
        }

        [BurstCompile]
        struct AlwaysTeamUpdateJob : IJob
        {
            public int teamCount;
            public float frameDeltaTime;
            public float globalTimeScale;

            public NativeReference<int> maxUpdateCount;
            public NativeArray<TeamData> teamDataArray;

            public void Execute()
            {
                int maxCount = 0;

                for (int i = 0; i < teamCount; i++)
                {
                    if (i == 0)
                    {
                        // グローバルチーム
                        continue;
                    }

                    var tdata = teamDataArray[i];
                    if (tdata.IsEnable == false)
                        continue;

                    //Debug.Log($"Team Enable:{i}");

                    // リセット
                    //if (tdata.IsReset)
                    if (tdata.flag.IsSet(Flag_TimeReset))
                    {
                        //Debug.Log($"Team time Reset:{i}");
                        tdata.time = 0;
                        tdata.oldTime = 0;
                        tdata.nowUpdateTime = 0;
                        tdata.oldUpdateTime = 0;
                        tdata.frameUpdateTime = 0;
                        tdata.frameOldTime = 0;
                    }

                    // 最大更新時間制限
                    var deltaTime = math.min(frameDeltaTime, tdata.SimulationDeltaTime * Define.System.MaxUpdateCount);

                    // 実行回数算出
                    float timeScale = tdata.timeScale * globalTimeScale;
                    timeScale = tdata.flag.IsSet(Flag_Suspend) ? 0.0f : timeScale;
                    float addTime = deltaTime * timeScale; // 今回の加算時間

                    float time = tdata.time + addTime;

                    //Debug.Log($"[{i}] time:{time}, addTime:{addTime}, timeScale:{timeScale}, suspend:{tdata.flag.IsSet(Flag_Suspend)}");

                    float interval = time - tdata.nowUpdateTime;
                    tdata.updateCount = (int)(interval / tdata.SimulationDeltaTime); // 今回の更新回数

                    // 時間まわり更新
                    if (tdata.updateCount > 0)
                    {
                        // 更新時のフレーム開始時間
                        tdata.frameOldTime = tdata.frameUpdateTime;
                        tdata.frameUpdateTime = time;

                        // 前回の更新時間
                        tdata.oldUpdateTime = tdata.nowUpdateTime;

                        //Debug.Log($"TeamUpdate!:{i}");
                    }
                    tdata.oldTime = tdata.time;
                    tdata.time = time;

                    // シミュレーション実行フラグ
                    tdata.flag.SetBits(Flag_Running, tdata.updateCount > 0);

                    teamDataArray[i] = tdata;

                    // 全体の最大実行回数
                    maxCount = math.max(maxCount, tdata.updateCount);

                    //Debug.Log($"[{i}] updateCount:{tdata.updateCount}");
                }

                maxUpdateCount.Value = maxCount;
            }
        }

        bool AddSyncParent(ref TeamData tdata, int parentTeamId)
        {
            // 最大７まで
            if (tdata.syncParentTeamId.Length == tdata.syncParentTeamId.Capacity)
            {
                Develop.LogWarning($"Synchronous team number limit!");
                return false;
            }
            tdata.syncParentTeamId.Add(parentTeamId);

            return true;
        }

        void RemoveSyncParent(ref TeamData tdata, int parentTeamId)
        {
            tdata.syncParentTeamId.RemoveItemAtSwapBack(parentTeamId);
        }

        //=========================================================================================
        /// <summary>
        /// チームごとのセンター姿勢の決定と慣性用の移動量計算
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle CalcCenterAndInertia(JobHandle jobHandle)
        {
            var bm = MagicaManager.Bone;
            var vm = MagicaManager.VMesh;

            var job = new CalcCenterAndInertiaJob()
            {
                teamDataArray = teamDataArray.GetNativeArray(),
                centerDataArray = MagicaManager.Team.centerDataArray.GetNativeArray(),

                positions = vm.positions.GetNativeArray(),
                rotations = vm.rotations.GetNativeArray(),
                vertexBindPoseRotations = vm.vertexBindPoseRotations.GetNativeArray(),

                fixedArray = MagicaManager.Simulation.inertiaConstraint.fixedArray.GetNativeArray(),

                transformPositionArray = bm.positionArray.GetNativeArray(),
                transformRotationArray = bm.rotationArray.GetNativeArray(),
                transformScaleArray = bm.scaleArray.GetNativeArray(),

            };
            jobHandle = job.Schedule(TeamCount, 1, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct CalcCenterAndInertiaJob : IJobParallelFor
        {
            // team
            public NativeArray<TeamData> teamDataArray;
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexBindPoseRotations;

            // inertia
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> fixedArray;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;

            // チームごと
            public void Execute(int teamId)
            {
                if (teamId == 0)
                    return;
                var tdata = teamDataArray[teamId];
                if (tdata.IsEnable == false)
                    return;

                // ■センター
                var cdata = centerDataArray[teamId];
                var centerWorldPos = transformPositionArray[cdata.centerTransformIndex];
                var centerWorldRot = transformRotationArray[cdata.centerTransformIndex];
                var centerWorldScl = transformScaleArray[cdata.centerTransformIndex];
                // 固定点リストがある場合は固定点の姿勢から算出する、ない場合はクロストランスフォームを使用する
                if (tdata.fixedDataChunk.IsValid)
                {
                    float3 cen = 0;
                    float3 nor = 0;
                    float3 tan = 0;
                    int cnt = 0;

                    int v_start = tdata.proxyCommonChunk.startIndex;

                    int fcnt = tdata.fixedDataChunk.dataLength;
                    int fstart = tdata.fixedDataChunk.startIndex;
                    for (int i = 0; i < fcnt; i++)
                    {
                        var l_findex = fixedArray[fstart + i];
                        int vindex = l_findex + v_start;

                        cen += positions[vindex];
                        cnt++;

                        var rot = rotations[vindex];
                        rot = math.mul(rot, vertexBindPoseRotations[vindex]);

                        nor += MathUtility.ToNormal(rot);
                        tan += MathUtility.ToTangent(rot);
                    }

                    centerWorldPos = cen / cnt;
                    centerWorldRot = MathUtility.ToRotation(math.normalize(nor), math.normalize(tan));
                }

                // リセットおよび最新のセンター座標として格納
                if (tdata.IsReset)
                {
                    cdata.frameWorldPosition = centerWorldPos;
                    cdata.frameWorldRotation = centerWorldRot;
                    cdata.frameWorldScale = centerWorldScl;
                    cdata.oldFrameWorldPosition = centerWorldPos;
                    cdata.oldFrameWorldRotation = centerWorldRot;
                    cdata.oldFrameWorldScale = centerWorldScl;
                    cdata.nowWorldPosition = centerWorldPos;
                    cdata.nowWorldRotation = centerWorldRot;
                    cdata.nowWorldScale = centerWorldScl;
                    cdata.oldWorldPosition = centerWorldPos;
                    cdata.oldWorldRotation = centerWorldRot;

                    tdata.centerWorldPosition = centerWorldPos;
                }
                else
                {
                    cdata.frameWorldPosition = centerWorldPos;
                    cdata.frameWorldRotation = centerWorldRot;
                    cdata.frameWorldScale = centerWorldScl;
                }

                // 今回のフレーム移動量を算出
                float3 frameVector = 0;
                quaternion frameRotation = quaternion.identity;
                if (tdata.IsRunning)
                {
                    // 今回の移動量算出
                    frameVector = cdata.frameWorldPosition - cdata.oldFrameWorldPosition;
                    frameRotation = MathUtility.FromToRotation(cdata.oldFrameWorldRotation, cdata.frameWorldRotation);

                }
                cdata.frameVector = frameVector;
                cdata.frameRotation = frameRotation;

                centerDataArray[teamId] = cdata;
                teamDataArray[teamId] = tdata;
            }
        }

        //=========================================================================================
        /// <summary>
        /// ステップごとの前処理（ステップの開始に実行される）
        /// </summary>
        /// <param name="updateIndex"></param>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle SimulationStepTeamUpdate(int updateIndex, JobHandle jobHandle)
        {
            var job = new SimulationStepTeamUpdateJob()
            {
                updateIndex = updateIndex,

                teamDataArray = teamDataArray.GetNativeArray(),
                parameterArray = parameterArray.GetNativeArray(),
                centerDataArray = centerDataArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(TeamCount, 1, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct SimulationStepTeamUpdateJob : IJobParallelFor
        {
            public int updateIndex;

            // team
            public NativeArray<TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // チームごと
            public void Execute(int teamId)
            {
                if (teamId == 0)
                    return;
                var tdata = teamDataArray[teamId];
                if (tdata.IsEnable == false)
                    return;

                // ■ステップ実行時のみ処理する
                bool runStep = updateIndex < tdata.updateCount;
                tdata.flag.SetBits(Flag_StepRunning, runStep);
                if (updateIndex >= tdata.updateCount)
                {
                    teamDataArray[teamId] = tdata;
                    return;
                }

                //Debug.Log($"team[{teamId}] ({updateIndex}/{tdata.updateCount})");

                // パラメータ
                var param = parameterArray[teamId];

                // ■時間更新 ---------------------------------------------------
                // nowUpdateTime更新
                tdata.nowUpdateTime += tdata.SimulationDeltaTime;

                // 今回のフレーム割合を計算する
                // frameStartTimeからtime区間でのnowUpdateTimeの割合
                tdata.frameInterpolation = (tdata.nowUpdateTime - tdata.frameOldTime) / (tdata.time - tdata.frameOldTime);
                //Debug.Log($"Team[{teamId}] time.{tdata.time}, oldTime:{tdata.oldTime}, frameTime:{tdata.frameUpdateTime}, frameOldTime:{tdata.frameOldTime}, nowUpdateTime:{tdata.nowUpdateTime}, frameInterp:{tdata.frameInterpolation}");

                // ■センター ---------------------------------------------------
                // 現在ステップでのセンタートランスフォーム姿勢を求める
                var cdata = centerDataArray[teamId];
                cdata.oldWorldPosition = cdata.nowWorldPosition;
                cdata.oldWorldRotation = cdata.nowWorldRotation;
                cdata.nowWorldPosition = math.lerp(cdata.oldFrameWorldPosition, cdata.frameWorldPosition, tdata.frameInterpolation);
                cdata.nowWorldRotation = math.slerp(cdata.oldFrameWorldRotation, cdata.frameWorldRotation, tdata.frameInterpolation);
                cdata.nowWorldRotation = math.normalize(cdata.nowWorldRotation); // 必要
                float3 wscl = math.lerp(cdata.oldFrameWorldScale, cdata.frameWorldScale, tdata.frameInterpolation);
                cdata.nowWorldScale = wscl;
                //cdata.nowLocalToWorldMatrix = MathUtility.LocalToWorldMatrix(cdata.nowWorldPosition, cdata.nowWorldRotation, cdata.nowWorldScale);

                // 現在座標はteamDataにもコピーする
                tdata.centerWorldPosition = cdata.nowWorldPosition;

                // ステップごとの移動量
                cdata.stepVector = cdata.nowWorldPosition - cdata.oldWorldPosition;
                cdata.stepRotation = MathUtility.FromToRotation(cdata.oldWorldRotation, cdata.nowWorldRotation);
                float stepAngle = MathUtility.Angle(cdata.oldWorldRotation, cdata.nowWorldRotation);

                // 慣性割合
                float moveInertiaRatio = 0.0f;
                float rotationInertiaRatio = 0.0f;

                // 最大速度／最大回転による慣性削減
                float stepSpeed = math.length(cdata.stepVector) / tdata.SimulationDeltaTime;
                float stepRotationSpeed = math.degrees(stepAngle) / tdata.SimulationDeltaTime;
                //Debug.Log($"Team[{teamId}] stepSpeed:{stepSpeed}, stepRotationSpeed:{stepRotationSpeed}");
                if (stepSpeed > param.inertiaConstraint.movementSpeedLimit && param.inertiaConstraint.movementSpeedLimit >= 0.0f)
                {
                    //Debug.Log($"Team[{teamId}] stepSpeed:{stepSpeed}");
                    moveInertiaRatio = math.saturate(math.max(stepSpeed - param.inertiaConstraint.movementSpeedLimit, 0.0f) / stepSpeed);
                }
                if (stepRotationSpeed > param.inertiaConstraint.rotationSpeedLimit && param.inertiaConstraint.rotationSpeedLimit >= 0.0f)
                {
                    //Debug.Log($"Team[{teamId}] stepRotationSpeed:{stepRotationSpeed}");
                    rotationInertiaRatio = math.saturate(math.max(stepRotationSpeed - param.inertiaConstraint.rotationSpeedLimit, 0.0f) / stepRotationSpeed);
                }

                // 全体慣性シフト
                moveInertiaRatio = math.lerp(moveInertiaRatio, 1.0f, 1.0f - param.inertiaConstraint.movementInertia);
                rotationInertiaRatio = math.lerp(rotationInertiaRatio, 1.0f, 1.0f - param.inertiaConstraint.rotationInertia);
                cdata.stepMoveInertiaRatio = moveInertiaRatio;
                cdata.stepRotationInertiaRatio = rotationInertiaRatio;

                // 最終慣性
                cdata.inertiaVector = math.lerp(float3.zero, cdata.stepVector, moveInertiaRatio);
                cdata.inertiaRotation = math.slerp(quaternion.identity, cdata.stepRotation, rotationInertiaRatio);
                //Debug.Log($"Team[{teamId}] stepSpeed:{stepSpeed}, moveInertiaRatio:{moveInertiaRatio}, inertiaVector:{cdata.inertiaVector}, rotationInertiaRatio:{rotationInertiaRatio}");

                // ■遠心力用パラメータ算出
                // 今回ステップでの回転速度と回転軸
                cdata.angularVelocity = stepAngle / tdata.SimulationDeltaTime; // 回転速度(rad/s)
                //cdata.rotationAxis = cdata.angularVelocity > Define.System.Epsilon ? math.normalize(cdata.nowWorldRotation.value.xyz) : 0;
                if (cdata.angularVelocity > Define.System.Epsilon)
                    MathUtility.ToAngleAxis(cdata.stepRotation, out _, out cdata.rotationAxis);
                else
                    cdata.rotationAxis = 0;
                //MathUtility.ToAngleAxis(cdata.stepRotation, out var _angle, out cdata.rotationAxis);
                //cdata.angularVelocity = _angle / tdata.SimulationDeltaTime; // 回転速度(rad/s)
                //Debug.Log($"Team[{teamId}] angularVelocity:{math.degrees(cdata.angularVelocity)}, axis:{cdata.rotationAxis}, q:{cdata.stepRotation.value}");
                //Debug.Log($"Team[{teamId}] angularVelocity:{math.degrees(cdata.angularVelocity)}, now:{cdata.nowWorldRotation.value}, old:{cdata.oldWorldRotation.value}");

                // チームスケール倍率
                tdata.scaleRatio = math.length(wscl) / math.length(tdata.initScale);

                // ■重力方向割合 ---------------------------------------------------
                float gravityDot = 1.0f;
                if (math.lengthsq(param.gravityDirection) > Define.System.Epsilon)
                {
                    var falloffDir = math.mul(cdata.nowWorldRotation, cdata.initLocalGravityDirection);
                    gravityDot = math.dot(falloffDir, param.gravityDirection);
                    gravityDot = math.saturate(gravityDot * 0.5f + 0.5f);
                }
                tdata.gravityDot = gravityDot;
                //Develop.DebugLog($"gdot:{gravityDot}");

                // ■重力減衰 ---------------------------------------------------
                float gravityRatio = 1.0f;
                if (param.gravity > 1e-06f && param.gravityFalloff > 1e-06f)
                {
                    //var falloffDir = math.mul(cdata.nowWorldRotation, cdata.initLocalGravityDirection);
                    //gravityDot = math.dot(falloffDir, param.gravityDirection);
                    //gravityDot = math.saturate(gravityDot * 0.5f + 0.5f);
                    //Debug.Log($"gdot:{gravityDot}");
                    //gravityRatio = math.lerp(math.saturate(1.0f - param.gravityFalloff), 1.0f, math.saturate(1.0f - gravityDot));
                    gravityRatio = math.lerp(math.saturate(1.0f - param.gravityFalloff), 1.0f, math.saturate(1.0f - gravityDot));
                }
                tdata.gravityRatio = gravityRatio;

                // データ格納
                teamDataArray[teamId] = tdata;
                centerDataArray[teamId] = cdata;
                //Debug.Log($"[{updateIndex}/{updateCount}] frameRatio:{data.frameInterpolation}, inertiaPosition:{idata.inertiaPosition}");
            }
        }

        //=========================================================================================
        /// <summary>
        /// クロスシミュレーション更新後処理
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle PostTeamUpdate(JobHandle jobHandle)
        {
            var job = new PostTeamUpdateJob()
            {
                teamDataArray = teamDataArray.GetNativeArray(),
                centerDataArray = centerDataArray.GetNativeArray(),
            };
            jobHandle = job.Schedule(teamDataArray.Length, 1, jobHandle);

            return jobHandle;
        }

        [BurstCompile]
        struct PostTeamUpdateJob : IJobParallelFor
        {
            // team
            public NativeArray<TeamData> teamDataArray;
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // チームごと
            public void Execute(int teamId)
            {
                var tdata = teamDataArray[teamId];
                if (tdata.IsEnable == false)
                    return;

                if (tdata.IsRunning)
                {
                    // ■センターを更新
                    var cdata = centerDataArray[teamId];
                    cdata.oldFrameWorldPosition = cdata.frameWorldPosition;
                    cdata.oldFrameWorldRotation = cdata.frameWorldRotation;
                    cdata.oldFrameWorldScale = cdata.frameWorldScale;
                    centerDataArray[teamId] = cdata;
                }

                // フラグリセット
                tdata.flag.SetBits(Flag_Reset, false);
                tdata.flag.SetBits(Flag_TimeReset, false);
                tdata.flag.SetBits(Flag_Running, false);
                tdata.flag.SetBits(Flag_StepRunning, false);

                // 時間調整（floatの精度問題への対処）
                const float limitTime = 30.0f;
                if (tdata.time > limitTime)
                {
                    tdata.time -= limitTime;
                    tdata.oldTime -= limitTime;
                    tdata.nowUpdateTime -= limitTime;
                    tdata.oldUpdateTime -= limitTime;
                    tdata.frameUpdateTime -= limitTime;
                    tdata.frameOldTime -= limitTime;
                }

                teamDataArray[teamId] = tdata;
            }
        }

        //=========================================================================================
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Team Manager. Team:{TeamCount}, Mapping:{MappingCount}");

            for (int i = 1; i < TeamCount; i++)
            {
                var tdata = teamDataArray[i];
                if (tdata.IsEnable == false)
                    continue;

                var cprocess = GetClothProcess(i);
                var cloth = cprocess.cloth;

                sb.AppendLine($"ID:{i} [{cprocess.Name}] state:0x{cprocess.GetStateFlag().Value:X}, Flag:0x{tdata.flag.Value:X}, Particle:{tdata.ParticleCount}, Collider:{cprocess.ColliderCount} Proxy:{tdata.proxyMeshType}, Mapping:{tdata.MappingCount}");

                // 同期
                sb.AppendLine($"  Sync:{cloth.SyncCloth}, SyncParentCount:{tdata.syncParentTeamId.Length}");

                // chunk情報
                sb.AppendLine($"  -ProxyTransformChunk {tdata.proxyTransformChunk}");
                sb.AppendLine($"  -ProxyCommonChunk {tdata.proxyCommonChunk}");
                sb.AppendLine($"  -ProxyMeshChunk {tdata.proxyMeshChunk}");
                sb.AppendLine($"  -ProxyBoneChunk {tdata.proxyBoneChunk}");
                sb.AppendLine($"  -ProxySkinBoneChunk {tdata.proxySkinBoneChunk}");
                sb.AppendLine($"  -ProxyTriangleChunk {tdata.proxyTriangleChunk}");
                sb.AppendLine($"  -ProxyEdgeChunk {tdata.proxyEdgeChunk}");
                sb.AppendLine($"  -BaseLineChunk {tdata.baseLineChunk}");
                sb.AppendLine($"  -BaseLineDataChunk {tdata.baseLineDataChunk}");
                sb.AppendLine($"  -ParticleChunk {tdata.particleChunk}");
                sb.AppendLine($"  -ColliderChunk {tdata.colliderChunk}");
                sb.AppendLine($"  -ColliderTrnasformChunk {tdata.colliderTransformChunk}");

                // mapping情報
                if (tdata.MappingCount > 0)
                {
                    for (int j = 0; j < tdata.MappingCount; j++)
                    {
                        int mid = tdata.mappingDataIndexSet[j];
                        var mdata = mappingDataArray[mid];
                        sb.AppendLine($"  *Mapping [{mid}] Vertex:{mdata.VertexCount}");
                    }
                }

                // constraint
                sb.AppendLine($"  +DistanceStartChunk {tdata.distanceStartChunk}");
                sb.AppendLine($"  +DistanceDataChunk {tdata.distanceDataChunk}");
                //sb.AppendLine($"  +DistanceVerticalStartChunk {tdata.distanceVerticalStartChunk}");
                //sb.AppendLine($"  +DistanceVerticalDataChunk {tdata.distanceVerticalDataChunk}");
                //sb.AppendLine($"  +DistanceHorizontalStartChunk {tdata.distanceHorizontalStartChunk}");
                //sb.AppendLine($"  +DistanceHorizontalDataChunk {tdata.distanceHorizontalDataChunk}");
                sb.AppendLine($"  +BendingPairChunk {tdata.bendingPairChunk}");
                //sb.AppendLine($"  +BendingDataChunk {tdata.bendingDataChunk}");
                sb.AppendLine($"  +selfPointChunk {tdata.selfPointChunk}");
                sb.AppendLine($"  +selfEdgeChunk {tdata.selfEdgeChunk}");
                sb.AppendLine($"  +selfTriangleChunk {tdata.selfTriangleChunk}");
            }

            return sb.ToString();
        }
    }
}
