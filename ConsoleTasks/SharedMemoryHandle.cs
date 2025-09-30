using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace ConsoleTasks
{
    internal sealed class SharedMemoryHandle<CONTAINER_T>
        : IDisposable
        where CONTAINER_T : unmanaged
    {
        private readonly MemoryMappedFile _mmf;
        private readonly MemoryMappedViewAccessor _view;
        private bool _disposed;

        private SharedMemoryHandle(MemoryMappedFile mmf)
        {
            _mmf = mmf;
            _view = _mmf.CreateViewAccessor();
        }

        public static SharedMemoryHandle<CONTAINER_T> CreateNew(string name)
            => new(MemoryMappedFile.CreateNew($"{Constants.InterProcessResourceNamePrefix}.mmf.{name}", Unsafe.SizeOf<CONTAINER_T>()));

        [SupportedOSPlatform("windows")]
        public static SharedMemoryHandle<CONTAINER_T> CreateOrOpen(string name)
            => new(MemoryMappedFile.CreateOrOpen($"{Constants.InterProcessResourceNamePrefix}.mmf.{name}", Unsafe.SizeOf<CONTAINER_T>()));

        [SupportedOSPlatform("windows")]
        public static SharedMemoryHandle<CONTAINER_T> OpenExisting(string name)
            => new(MemoryMappedFile.OpenExisting($"{Constants.InterProcessResourceNamePrefix}.mmf.{name}"));

        public CONTAINER_T Value
        {
            get
            {
                _view.Read<CONTAINER_T>(0, out var value);
                return value;
            }

            set => _view.Write(0L, ref value);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _view.Dispose();
                    _mmf.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
