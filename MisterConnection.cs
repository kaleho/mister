﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FASTER.core;
using log4net;

namespace Marius.Mister
{
    public static class MisterConnection
    {
        public static MisterConnection<TKey, TValue, TKeyAtomSource, TValueAtomSource> Create<TKey, TValue, TKeyAtomSource, TValueAtomSource>(DirectoryInfo directory, IMisterObjectSerializer<TKey, TKeyAtomSource> keySerializer, IMisterObjectSerializer<TValue, TValueAtomSource> valueSerializer, MisterConnectionSettings settings = null, string name = null)
            where TKeyAtomSource : struct, IMisterAtomSource<MisterObject>
            where TValueAtomSource : struct, IMisterAtomSource<MisterObject>
        {
            return new MisterConnection<TKey, TValue, TKeyAtomSource, TValueAtomSource>(directory, keySerializer, valueSerializer, settings, name);
        }

        public static MisterConnection<TKey, TValue, MisterStreamObjectSource, TValueAtomSource> Create<TKey, TValue, TValueAtomSource>(DirectoryInfo directory, IMisterStreamSerializer<TKey> keyStreamSerializer, IMisterObjectSerializer<TValue, TValueAtomSource> valueSerializer, MisterConnectionSettings settings = null, string name = null, IMisterStreamManager streamManager = null)
            where TValueAtomSource : struct, IMisterAtomSource<MisterObject>
        {
            streamManager = streamManager ?? MisterArrayPoolStreamManager.Default;
            var keySerializer = new MisterStreamSerializer<TKey>(keyStreamSerializer, streamManager);

            return new MisterConnection<TKey, TValue, MisterStreamObjectSource, TValueAtomSource>(directory, keySerializer, valueSerializer, settings, name);
        }

        public static MisterConnection<TKey, TValue, TKeyAtomSource, MisterStreamObjectSource> Create<TKey, TValue, TKeyAtomSource>(DirectoryInfo directory, IMisterObjectSerializer<TKey, TKeyAtomSource> keySerializer, IMisterStreamSerializer<TValue> valueStreamSerializer, MisterConnectionSettings settings = null, string name = null, IMisterStreamManager streamManager = null)
            where TKeyAtomSource : struct, IMisterAtomSource<MisterObject>
        {
            streamManager = streamManager ?? MisterArrayPoolStreamManager.Default;
            var valueSerializer = new MisterStreamSerializer<TValue>(valueStreamSerializer, streamManager);

            return new MisterConnection<TKey, TValue, TKeyAtomSource, MisterStreamObjectSource>(directory, keySerializer, valueSerializer, settings, name);
        }

        public static MisterConnection<TKey, TValue> Create<TKey, TValue>(DirectoryInfo directory, IMisterStreamSerializer<TKey> keySerializer, IMisterStreamSerializer<TValue> valueSerializer, MisterConnectionSettings settings = null, string name = null, IMisterStreamManager streamManager = null)
        {
            return new MisterConnection<TKey, TValue>(directory, keySerializer, valueSerializer, settings, name, streamManager);
        }
    }

    public sealed class MisterConnection<TKey, TValue> : IMisterConnection<TKey, TValue>
    {
        private readonly MisterConnection<TKey, TValue, MisterStreamObjectSource, MisterStreamObjectSource> _underlyingConnection;
        private readonly IMisterStreamManager _streamManager;

        public MisterConnection(DirectoryInfo directory, IMisterStreamSerializer<TKey> keySerializer, IMisterStreamSerializer<TValue> valueSerializer, MisterConnectionSettings settings = null, string name = null)
            : this(directory, keySerializer, valueSerializer, settings, name, null)
        {
        }

        public MisterConnection(DirectoryInfo directory, IMisterStreamSerializer<TKey> keySerializer, IMisterStreamSerializer<TValue> valueSerializer, MisterConnectionSettings settings = null, string name = null, IMisterStreamManager streamManager = null)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));

            if (keySerializer == null)
                throw new ArgumentNullException(nameof(keySerializer));

            if (valueSerializer == null)
                throw new ArgumentNullException(nameof(valueSerializer));

            _streamManager = streamManager ?? MisterArrayPoolStreamManager.Default;

            var streamKeySerializer = new MisterStreamSerializer<TKey>(keySerializer, _streamManager);
            var streamValueSerializer = new MisterStreamSerializer<TValue>(valueSerializer, _streamManager);

            _underlyingConnection = new MisterConnection<TKey, TValue, MisterStreamObjectSource, MisterStreamObjectSource>(directory, streamKeySerializer, streamValueSerializer, settings, name);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Close()
        {
            _underlyingConnection.Close();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Checkpoint()
        {
            _underlyingConnection.Checkpoint();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task CheckpointAsync()
        {
            return _underlyingConnection.CheckpointAsync();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task FlushAsync(bool waitPending)
        {
            return _underlyingConnection.FlushAsync(waitPending);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<TValue> GetAsync(TKey key)
        {
            return _underlyingConnection.GetAsync(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task<TValue> GetAsync(TKey key, bool waitPending)
        {
            return _underlyingConnection.GetAsync(key, waitPending);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task SetAsync(TKey key, TValue value)
        {
            return _underlyingConnection.SetAsync(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task SetAsync(TKey key, TValue value, bool waitPending)
        {
            return _underlyingConnection.SetAsync(key, value, waitPending);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task DeleteAsync(TKey key)
        {
            return _underlyingConnection.DeleteAsync(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task DeleteAsync(TKey key, bool waitPending)
        {
            return _underlyingConnection.DeleteAsync(key, waitPending);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForEach(Action<TKey, TValue, bool, object> onRecord, Action<object> onCompleted = null, object state = null)
        {
            _underlyingConnection.ForEach(onRecord, onCompleted, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task CompactAsync()
        {
            return _underlyingConnection.CompactAsync();
        }
    }

    public sealed class MisterConnection<TKey, TValue, TKeyAtomSource, TValueAtomSource> : MisterConnection
        <
            TKey,
            TValue,
            MisterObject,
            TKeyAtomSource,
            MisterObject,
            TValueAtomSource,
            FasterKV<MisterObject, MisterObject, byte[], TValue, object, MisterObjectEnvironment<TValue, TValueAtomSource>>
        >
        where TKeyAtomSource : struct, IMisterAtomSource<MisterObject>
        where TValueAtomSource : struct, IMisterAtomSource<MisterObject>
    {
        public MisterConnection(DirectoryInfo directory, IMisterSerializer<TKey, MisterObject, TKeyAtomSource> keySerializer, IMisterSerializer<TValue, MisterObject, TValueAtomSource> valueSerializer, MisterConnectionSettings settings = null, string name = null)
            : base(directory, keySerializer, valueSerializer, settings, name)
        {
            Initialize();
        }

        protected override void Create()
        {
            if (_faster != null)
                _faster.Dispose();

            if (_mainDevice != null)
                _mainDevice.Close();

            var environment = new MisterObjectEnvironment<TValue, TValueAtomSource>(_valueSerializer);
            var variableLengthStructSettings = new VariableLengthStructSettings<MisterObject, MisterObject>()
            {
                keyLength = MisterObjectVariableLengthStruct.Instance,
                valueLength = MisterObjectVariableLengthStruct.Instance,
            };

            _mainDevice = Devices.CreateLogDevice(Path.Combine(_directory.FullName, @"hlog.log"));
            _faster = new FasterKV<MisterObject, MisterObject, byte[], TValue, object, MisterObjectEnvironment<TValue, TValueAtomSource>>(
                _settings.IndexSize,
                environment,
                _settings.GetLogSettings(_mainDevice),
                new CheckpointSettings() { CheckpointDir = _directory.FullName, CheckPointType = CheckpointType.FoldOver },
                serializerSettings: null,
                comparer: MisterObjectEqualityComparer.Instance,
                variableLengthStructSettings: variableLengthStructSettings
            );
        }
    }

    public abstract class MisterConnection<TKey, TValue, TKeyAtom, TKeyAtomSource, TValueAtom, TValueAtomSource, TFaster> : IMisterConnection<TKey, TValue>
        where TKeyAtom : new()
        where TValueAtom : new()
        where TKeyAtomSource : struct, IMisterAtomSource<TKeyAtom>
        where TValueAtomSource : struct, IMisterAtomSource<TValueAtom>
        where TFaster : IFasterKV<TKeyAtom, TValueAtom, byte[], TValue, object>
    {
        private static readonly ILog Log = LogManager.GetLogger("MisterConnection");

        private delegate bool MisterWorkAction(ref MisterWorkItem workItem, long sequence);

        private struct MisterWorkItem
        {
            public TKey Key;
            public TValue Value;
            public object State;
            public bool WaitPending;
            public MisterWorkAction Action;
        }

        private class MisterForEachItem
        {
            public Action<TKey, TValue, bool, object> OnRecord;
            public Action<object> OnCompleted;
            public object State;
        }

        protected readonly DirectoryInfo _directory;
        protected readonly IMisterSerializer<TKey, TKeyAtom, TKeyAtomSource> _keySerializer;
        protected readonly IMisterSerializer<TValue, TValueAtom, TValueAtomSource> _valueSerializer;
        protected readonly MisterConnectionSettings _settings;
        protected readonly string _name;
        protected readonly MisterConnectionMaintenanceService<TValue, TKeyAtom, TValueAtom, TFaster> _maintenanceService;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isClosed;

        private ConcurrentQueue<MisterWorkItem> _workQueue;
        private Thread[] _workerThreads;

        protected TFaster _faster;
        protected IDevice _mainDevice;

        public MisterConnection(DirectoryInfo directory, IMisterSerializer<TKey, TKeyAtom, TKeyAtomSource> keySerializer, IMisterSerializer<TValue, TValueAtom, TValueAtomSource> valueSerializer, MisterConnectionSettings settings = null, string name = null)
        {
            if (directory == null)
                throw new ArgumentNullException(nameof(directory));

            if (keySerializer is null)
                throw new ArgumentNullException(nameof(keySerializer));

            if (valueSerializer is null)
                throw new ArgumentNullException(nameof(valueSerializer));

            _directory = directory;
            _keySerializer = keySerializer;
            _valueSerializer = valueSerializer;
            _settings = settings ?? new MisterConnectionSettings();
            _name = name;
            _cancellationTokenSource = new CancellationTokenSource();

            _maintenanceService = CreateMaintenanceService();
        }

        public void Close()
        {
            if (_isClosed)
                return;

            _isClosed = true;

            _cancellationTokenSource.Cancel();

            lock (_workQueue)
                Monitor.PulseAll(_workQueue);

            _maintenanceService.Stop();

            for (var i = 0; i < _workerThreads.Length; i++)
                _workerThreads[i].Join();

            _maintenanceService.Close();

            _faster.Dispose();
            _mainDevice.Close();
            _cancellationTokenSource.Dispose();
        }

        public void Checkpoint()
        {
            CheckDisposed();

            _maintenanceService.Checkpoint();
        }

        public Task FlushAsync(bool waitPending)
        {
            CheckDisposed();

            var tsc = new TaskCompletionSource<MisterVoid>(TaskCreationOptions.RunContinuationsAsynchronously);
            _workQueue.Enqueue(new MisterWorkItem()
            {
                Action = PerformFlush,
                State = tsc,
                WaitPending = waitPending,
            });

            lock (_workQueue)
                Monitor.Pulse(_workQueue);

            return tsc.Task;
        }

        public Task CheckpointAsync()
        {
            CheckDisposed();

            return _maintenanceService.CheckpointAsync();
        }

        public Task<TValue> GetAsync(TKey key)
        {
            CheckDisposed();

            var tsc = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
            _workQueue.Enqueue(new MisterWorkItem()
            {
                Key = key,
                State = tsc,
                Action = PerformGet,
            });

            lock (_workQueue)
                Monitor.Pulse(_workQueue);

            return tsc.Task;
        }

        public Task<TValue> GetAsync(TKey key, bool waitPending)
        {
            CheckDisposed();

            var tsc = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
            _workQueue.Enqueue(new MisterWorkItem()
            {
                Key = key,
                State = tsc,
                Action = PerformGet,
                WaitPending = waitPending,
            });

            lock (_workQueue)
                Monitor.Pulse(_workQueue);

            return tsc.Task;
        }

        public Task SetAsync(TKey key, TValue value)
        {
            CheckDisposed();

            var tsc = new TaskCompletionSource<MisterVoid>(TaskCreationOptions.RunContinuationsAsynchronously);
            _workQueue.Enqueue(new MisterWorkItem()
            {
                Key = key,
                Value = value,
                State = tsc,
                Action = PerformSet,
            });

            lock (_workQueue)
                Monitor.Pulse(_workQueue);

            return tsc.Task;
        }

        public Task SetAsync(TKey key, TValue value, bool waitPending)
        {
            CheckDisposed();

            var tsc = new TaskCompletionSource<MisterVoid>(TaskCreationOptions.RunContinuationsAsynchronously);
            _workQueue.Enqueue(new MisterWorkItem()
            {
                Key = key,
                Value = value,
                State = tsc,
                WaitPending = waitPending,
                Action = PerformSet,
            });

            lock (_workQueue)
                Monitor.Pulse(_workQueue);

            return tsc.Task;
        }

        public Task DeleteAsync(TKey key)
        {
            CheckDisposed();

            var tsc = new TaskCompletionSource<MisterVoid>(TaskCreationOptions.RunContinuationsAsynchronously);
            _workQueue.Enqueue(new MisterWorkItem()
            {
                Key = key,
                State = tsc,
                Action = PerformDelete,
            });

            lock (_workQueue)
                Monitor.Pulse(_workQueue);

            return tsc.Task;
        }

        public Task DeleteAsync(TKey key, bool waitPending)
        {
            CheckDisposed();

            var tsc = new TaskCompletionSource<MisterVoid>(TaskCreationOptions.RunContinuationsAsynchronously);
            _workQueue.Enqueue(new MisterWorkItem()
            {
                Key = key,
                State = tsc,
                WaitPending = waitPending,
                Action = PerformDelete,
            });

            lock (_workQueue)
                Monitor.Pulse(_workQueue);

            return tsc.Task;
        }

        public void ForEach(Action<TKey, TValue, bool, object> onRecord, Action<object> onCompleted = null, object state = default(object))
        {
            CheckDisposed();

            if (onRecord == null)
                throw new ArgumentNullException(nameof(onRecord));

            _workQueue.Enqueue(new MisterWorkItem()
            {
                Action = PerformForEach,
                State = new MisterForEachItem()
                {
                    OnRecord = onRecord,
                    OnCompleted = onCompleted,
                    State = state,
                },
            });

            lock (_workQueue)
                Monitor.Pulse(_workQueue);
        }

        public Task CompactAsync()
        {
            CheckDisposed();

            var tcs = new TaskCompletionSource<MisterVoid>(TaskCreationOptions.RunContinuationsAsynchronously);
            _workQueue.Enqueue(new MisterWorkItem()
            {
                Action = PerformCompact,
                State = tcs,
            });

            lock (_workQueue)
                Monitor.Pulse(_workQueue);

            return tcs.Task;
        }

        protected abstract void Create();

        protected virtual MisterConnectionMaintenanceService<TValue, TKeyAtom, TValueAtom, TFaster> CreateMaintenanceService()
        {
            return new MisterConnectionMaintenanceService<TValue, TKeyAtom, TValueAtom, TFaster>(_directory, _settings.MaintenanceIntervalMilliseconds, _settings.CheckpointCleanCount, _name);
        }

        protected void Initialize()
        {
            _maintenanceService.Recover(() =>
            {
                Create();
                return _faster;
            });

            _workQueue = new ConcurrentQueue<MisterWorkItem>();
            _workerThreads = new Thread[_settings.WorkerThreadCount];
            for (var i = 0; i < _workerThreads.Length; i++)
                _workerThreads[i] = new Thread(WorkerLoop) { IsBackground = true, Name = $"{_name ?? "Mister"} worker thread #{i + 1}" };

            for (var i = 0; i < _workerThreads.Length; i++)
                _workerThreads[i].Start(i.ToString());

            _maintenanceService.Start();
        }

        protected void Execute(Action<long> action)
        {
            _workQueue.Enqueue(new MisterWorkItem()
            {
                Action = PerformAction,
                State = action,
            });

            lock (_workQueue)
                Monitor.Pulse(_workQueue);
        }

        private unsafe bool PerformGet(ref MisterWorkItem workItem, long sequence)
        {
            var status = default(Status);
            var key = workItem.Key;
            var state = workItem.State;

            try
            {
                var input = default(byte[]);
                var output = default(TValue);

                using (var source = _keySerializer.Serialize(key))
                {
                    ref var misterKey = ref source.GetAtom();

                    status = _faster.Read(ref misterKey, ref input, ref output, state, sequence);
                    if (status == Status.PENDING)
                    {
                        if (workItem.WaitPending)
                            _faster.CompletePending(true);
                        return false;
                    }
                }

                if (state != null)
                {
                    var tsc = Unsafe.As<TaskCompletionSource<TValue>>(state);
                    if (status == Status.ERROR)
                        tsc.SetException(new Exception());
                    else
                        tsc.SetResult(output);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                if (state != null)
                {
                    var tsc = (TaskCompletionSource<TValue>)state;
                    tsc.SetException(ex);
                }
            }

            return status != Status.PENDING;
        }

        private unsafe bool PerformSet(ref MisterWorkItem workItem, long sequence)
        {
            var status = default(Status);
            var key = workItem.Key;
            var value = workItem.Value;
            var state = workItem.State;

            try
            {
                using (var keySource = _keySerializer.Serialize(key))
                using (var valueSource = _valueSerializer.Serialize(value))
                {
                    ref var misterKey = ref keySource.GetAtom();
                    ref var misterValue = ref valueSource.GetAtom();

                    status = _faster.Upsert(ref misterKey, ref misterValue, state, sequence);
                    _maintenanceService.IncrementVersion();

                    if (status == Status.PENDING)
                    {
                        if (workItem.WaitPending)
                            _faster.CompletePending(true);
                        return false;
                    }
                }

                if (state != null)
                {
                    var tsc = Unsafe.As<TaskCompletionSource<MisterVoid>>(state);
                    if (status == Status.ERROR)
                        tsc.SetException(new Exception());
                    else
                        tsc.SetResult(MisterVoid.Value);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                if (state != null)
                {
                    var tsc = (TaskCompletionSource<MisterVoid>)state;
                    tsc.SetException(ex);
                }
            }

            return status != Status.PENDING;
        }

        private unsafe bool PerformDelete(ref MisterWorkItem workItem, long sequence)
        {
            var status = default(Status);
            var key = workItem.Key;
            var state = workItem.State;

            try
            {
                using (var keySource = _keySerializer.Serialize(key))
                {
                    ref var misterKey = ref keySource.GetAtom();

                    status = _faster.Delete(ref misterKey, state, sequence);
                    _maintenanceService.IncrementVersion();

                    if (status == Status.PENDING)
                    {
                        if (workItem.WaitPending)
                            _faster.CompletePending(true);
                        return false;
                    }
                }

                if (state != null)
                {
                    var tsc = Unsafe.As<TaskCompletionSource<MisterVoid>>(state);
                    if (status == Status.ERROR)
                        tsc.SetException(new Exception());
                    else
                        tsc.SetResult(MisterVoid.Value);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                if (state != null)
                {
                    var tsc = (TaskCompletionSource<MisterVoid>)state;
                    tsc.SetException(ex);
                }
            }

            return status != Status.PENDING;
        }

        private bool PerformFlush(ref MisterWorkItem workItem, long sequence)
        {
            var state = workItem.State;
            var wait = workItem.WaitPending;

            try
            {
                _faster.Log.FlushAndEvict(wait);
                _maintenanceService.IncrementVersion();

                if (state != null)
                {
                    var tsc = Unsafe.As<TaskCompletionSource<MisterVoid>>(state);
                    tsc.SetResult(MisterVoid.Value);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                if (state != null)
                {
                    var tsc = (TaskCompletionSource<MisterVoid>)state;
                    tsc.SetException(ex);
                }
            }

            return true;
        }

        private unsafe bool PerformForEach(ref MisterWorkItem workItem, long sequence)
        {
            var forEachItem = Unsafe.As<MisterForEachItem>(workItem.State);
            var iterator = _faster.Log.Scan(_faster.Log.BeginAddress, _faster.Log.TailAddress);
            while (iterator.GetNext(out var recordInfo))
            {
                ref var misterKey = ref iterator.GetKey();
                ref var misterValue = ref iterator.GetValue();

                var key = _keySerializer.Deserialize(ref misterKey);
                var value = _valueSerializer.Deserialize(ref misterValue);

                var isDeleted = recordInfo.Tombstone;
                forEachItem.OnRecord(key, value, isDeleted, forEachItem.State);
            }

            if (forEachItem.OnCompleted != null)
                forEachItem.OnCompleted(forEachItem.State);

            return true;
        }

        private bool PerformCompact(ref MisterWorkItem workItem, long sequence)
        {
            var state = workItem.State;
            try
            {
                _faster.Log.Compact(_faster.Log.SafeReadOnlyAddress);
                _maintenanceService.IncrementVersion();

                if (state != null)
                {
                    var tsc = Unsafe.As<TaskCompletionSource<MisterVoid>>(state);
                    tsc.SetResult(MisterVoid.Value);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                if (state != null)
                {
                    var tsc = (TaskCompletionSource<MisterVoid>)state;
                    tsc.SetException(ex);
                }
            }

            return false;
        }

        private bool PerformAction(ref MisterWorkItem workItem, long sequence)
        {
            try
            {
                var action = Unsafe.As<Action<long>>(workItem.State);
                action(sequence);
            }
            catch { }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDisposed()
        {
            if (_isClosed)
                throw new ObjectDisposedException("MisterConnection");
        }

        private void WorkerLoop(object state)
        {
            try
            {
                var workerRefreshIntervalMilliseconds = _settings.WorkerRefreshIntervalMilliseconds;
                var sequence = 0L;
                var session = _faster.StartSession();
                var hasPending = false;

                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    var needRefresh = false;
                    if (_workQueue.IsEmpty)
                    {
                        lock (_workQueue)
                        {
                            if (_workQueue.IsEmpty && !_cancellationTokenSource.IsCancellationRequested)
                                needRefresh = !Monitor.Wait(_workQueue, workerRefreshIntervalMilliseconds);
                        }
                    }

                    while (_workQueue.TryDequeue(out var item))
                    {
                        needRefresh = false;

                        if (!item.Action(ref item, sequence))
                            hasPending = true;

                        sequence++;
                        if ((sequence & 0x7F) == 0)
                        {
                            if (hasPending)
                            {
                                _faster.CompletePending(true);
                                hasPending = false;
                            }

                            _faster.Refresh();
                        }
                    }

                    if (hasPending)
                    {
                        _faster.CompletePending(true);
                        hasPending = false;
                    }

                    if (needRefresh)
                    {
                        _faster.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                Trace.Write(ex);
            }

            while (_maintenanceService.IsRunning)
            {
                _faster.Refresh();
                _faster.CompletePending(true);
                Thread.Sleep(10);
            }

            while (!_faster.CompletePending(true))
                _faster.Refresh();

            _faster.StopSession();
        }
    }
}
