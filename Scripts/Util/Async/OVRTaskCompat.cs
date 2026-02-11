// Compatibility shims for older Unity/.NET profiles.
//
// Unity 2020 with ".NET Framework 4.x" scripting backend often does NOT include:
// - System.Threading.Tasks.ValueTask / ValueTask<T>
// - System.Runtime.CompilerServices.AsyncMethodBuilderAttribute
// - System.Runtime.InteropServices.Marshal.PtrToStringUTF8
// - System.Runtime.InteropServices.Marshal.StringToCoTaskMemUTF8
//
// The Meta XR Core SDK uses these. These minimal definitions let the code compile
// without requiring Unity/package upgrades. If Unity provides the real types, these shims
// are excluded via NETSTANDARD2_1/NET5_0_OR_GREATER.
//
// Notes:
// - These shims intentionally implement only the members used by this SDK.
// - ValueTask<T> is implemented as a thin wrapper over Task<T> (and TResult for sync results).

#if !(NETSTANDARD2_1 || NET5_0_OR_GREATER)

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Minimal polyfill so custom task-like types can compile on older profiles.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    internal sealed class AsyncMethodBuilderAttribute : Attribute
    {
        public AsyncMethodBuilderAttribute(Type builderType)
        {
            BuilderType = builderType;
        }

        public Type BuilderType { get; }
    }
}

namespace System.Threading.Tasks
{
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Minimal ValueTask polyfill (non-generic).
    /// </summary>
    public readonly struct ValueTask
    {
        private readonly Task _task;

        public ValueTask(Task task)
        {
            _task = task ?? Task.CompletedTask;
        }

        public Task AsTask() => _task ?? Task.CompletedTask;

        public TaskAwaiter GetAwaiter() => AsTask().GetAwaiter();
    }

    /// <summary>
    /// Minimal ValueTask&lt;TResult&gt; polyfill.
    /// </summary>
    public readonly struct ValueTask<TResult>
    {
        private readonly Task<TResult> _task;
        private readonly TResult _result;
        private readonly bool _hasResult;

        public ValueTask(Task<TResult> task)
        {
            _task = task;
            _result = default;
            _hasResult = false;
        }

        public ValueTask(TResult result)
        {
            _task = null;
            _result = result;
            _hasResult = true;
        }

        public Task<TResult> AsTask() => _hasResult ? Task.FromResult(_result) : _task;

        public TaskAwaiter<TResult> GetAwaiter() => AsTask().GetAwaiter();
    }
}

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Compatibility extensions for Marshal methods missing in .NET Framework 4.x.
    /// Provides PtrToStringUTF8 and StringToCoTaskMemUTF8 for older .NET profiles.
    /// </summary>
    public static class MarshalCompat
    {
        /// <summary>
        /// Polyfill for Marshal.PtrToStringUTF8 (available in .NET 5.0+).
        /// Converts a UTF-8 null-terminated string pointer to a managed string.
        /// </summary>
        public static string PtrToStringUTF8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return null;

            // Calculate length of null-terminated UTF-8 string
            int length = 0;
            unsafe
            {
                byte* p = (byte*)ptr;
                while (p[length] != 0)
                    length++;
            }

            if (length == 0)
                return string.Empty;

            // Copy UTF-8 bytes and decode
            byte[] bytes = new byte[length];
            Marshal.Copy(ptr, bytes, 0, length);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Polyfill for Marshal.StringToCoTaskMemUTF8 (available in .NET 5.0+).
        /// Allocates a UTF-8 encoded string in CoTaskMem and returns the pointer.
        /// </summary>
        public static IntPtr StringToCoTaskMemUTF8(string s)
        {
            if (s == null)
                return IntPtr.Zero;

            byte[] utf8Bytes = Encoding.UTF8.GetBytes(s);
            int size = utf8Bytes.Length + 1; // +1 for null terminator
            IntPtr ptr = Marshal.AllocCoTaskMem(size);
            Marshal.Copy(utf8Bytes, 0, ptr, utf8Bytes.Length);
            unsafe
            {
                ((byte*)ptr)[utf8Bytes.Length] = 0; // null terminator
            }
            return ptr;
        }
    }

}

#endif

