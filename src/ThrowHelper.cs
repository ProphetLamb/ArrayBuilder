using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ArrayBuilder
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void IfNullAndNullsAreIllegalThenThrow<T>(object? value, string argName)
        {
            if (!(default(T) == null) && value == null)
                ThrowHelper.ThrowArgumentNullException(argName);
        }

        [DoesNotReturn]
        internal static void ThrowArgumentNullException(string argument)
        {
            throw new ArgumentNullException(argument);
        }

        [DoesNotReturn]
        internal static void ThrowWrongValueTypeArgumentException<T>(T value, Type targetType)
        {
            throw GetWrongValueTypeArgumentException((object?)value, targetType);
        }

        [DoesNotReturn]
        internal static void ThrowArgumentException(string message)
        {
            throw GetArgumentException(message);
        }

        [DoesNotReturn]
        internal static void ThrowArgumentException_Argument_InvalidArrayType()
        {
            throw new ArgumentException("Argument_InvalidArrayType");
        }

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException(string argument, string message)
        {
            throw GetArgumentOutOfRangeException(argument, message);
        }

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRange_IndexException()
        {
            throw GetArgumentOutOfRangeException("index", "ArgumentOutOfRange_Index");
        }

        [DoesNotReturn]
        internal static void ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count()
        {
            throw GetArgumentOutOfRangeException("count", "ArgumentOutOfRange_Count");
        }

        [DoesNotReturn]
        internal static void ThrowIndexArgumentOutOfRange_NeedNonNegNumException()
        {
            throw GetArgumentOutOfRangeException("index", "ArgumentOutOfRange_NeedNonNegNum");
        }
        
        [DoesNotReturn]
        internal static void ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion()
        {
            throw new InvalidOperationException("InvalidOperation_EnumFailedVersion");
        }
        
        [DoesNotReturn]
        internal static void ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen()
        {
            throw new InvalidOperationException("InvalidOperation_EnumOpCantHappen");
        }
        
        [DoesNotReturn]
        internal static void ThrowObjectDisposedException(string objectName, string? message)
        {
            throw new ObjectDisposedException(objectName, message);
        }

        [DoesNotReturn]
        internal static void ThrowObjectDisposedException_Closed(string objectName)
        {
            throw new ObjectDisposedException(objectName, "closed");
        }

        
        private static ArgumentException GetArgumentException(string message)
        {
            return new ArgumentException(message);
        }

        private static ArgumentException GetWrongValueTypeArgumentException(object? value, Type targetType)
        {
            return new ArgumentException($"Arg_WrongType {value} {targetType}", nameof(value));
        }
        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(string argument, string message)
        {
            return new ArgumentOutOfRangeException(argument, message);
        }
    }
}